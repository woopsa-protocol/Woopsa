using System;
using System.Collections.Generic;
using TwinCAT.Ads;
using Woopsa;

namespace WoopsaAds
{
    class WoopsaAdsServer:IDisposable
    {
        private TcAdsClient _tcAds;
        private TcAdsSymbolInfoLoader _symbolLoader;
        private Dictionary<string, WoopsaObject> _woopsaObjects;
        private Dictionary<string, WoopsaAdsProperty> _woopsaProperties;
        private const string _PROPERTIES_ROOT_OBJECT_NAME = "RootProperties";
        private WoopsaPropertyGet _woopsaAdsPropertyGet;
        private WoopsaPropertySet _woopsaAdsPropertySet;
        private int _onlineChangeCount = 0;
        private const int _PORT = 851;
        private string _netId;

        public readonly DateTime BeckhoffPlcReferenceDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public bool isAdsConnected { get; set; }
        public bool isHierarchieLoaded { get; set; }

        public WoopsaAdsServer(string netId)
        {
            _netId = netId;
            _tcAds = new TcAdsClient();
            try
            {
                _tcAds.Connect(netId, _PORT);
            }
            catch (Exception)
            {
                isAdsConnected = false;
            }
            _woopsaAdsPropertyGet = this.ReadAdsValue;
            _woopsaAdsPropertySet = this.WriteAdsValue;
        }

        public bool IsHeartBeatAlive()
        {
            try
            {              
                AdsStream dataStream = new AdsStream(4);
                AdsBinaryReader binReader = new AdsBinaryReader(dataStream);

                int iHandle = 0;
                int iValue = 0;
                iHandle = _tcAds.CreateVariableHandle("TwinCAT_SystemInfoVarList._AppInfo.OnlineChangeCnt");

                _tcAds.Read(iHandle, dataStream);
                iValue = binReader.ReadInt32();
                dataStream.Position = 0;
                if(iValue != _onlineChangeCount)
                {
                    _onlineChangeCount = iValue;
                    isHierarchieLoaded = false;
                }
            }
            catch(Exception e)
            {
                DiagnosticWindow.AddToDebug(e.Message);
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            if(!_tcAds.Disposed)
                _tcAds.Dispose();
        }

        public bool BeckhoffToWoopsaValueType(string beckhoffType, out WoopsaValueType woopsaType)
        {
            switch (beckhoffType)
            {
                case BeckhoffValueType.BOOL:
                    woopsaType = WoopsaValueType.Logical;
                    break;
                case BeckhoffValueType.BYTE:
                case BeckhoffValueType.WORD:
                case BeckhoffValueType.DWORD:
                case BeckhoffValueType.SINT:
                case BeckhoffValueType.INT:
                case BeckhoffValueType.DINT:
                case BeckhoffValueType.LINT:
                case BeckhoffValueType.USINT:
                case BeckhoffValueType.UINT:
                case BeckhoffValueType.UDINT:
                case BeckhoffValueType.ULINT:
                    woopsaType = WoopsaValueType.Integer;
                    break;
                case BeckhoffValueType.REAL:
                case BeckhoffValueType.LREAL:
                    woopsaType = WoopsaValueType.Real;
                    break;
                case BeckhoffValueType.STRING:
                    woopsaType = WoopsaValueType.Text;
                    break;
                case BeckhoffValueType.TIME:
                    woopsaType = WoopsaValueType.TimeSpan;
                    break;
                case BeckhoffValueType.TIME_OF_DAY:
                case BeckhoffValueType.DATE:
                case BeckhoffValueType.DATE_AND_TIME:
                    woopsaType = WoopsaValueType.DateTime;
                    break;

                default:
                    // specific length string in TwinCAT for example : STRING(63)
                    if (beckhoffType.Contains("STRING(") && beckhoffType.Contains(")"))
                    {
                        woopsaType = WoopsaValueType.Text;
                        return true;
                    }
                    woopsaType = WoopsaValueType.Null;
                    return false;
            }
            return true;
        }

        public void loadHierarchy(WoopsaObject root)
        {
            try
            {
                _symbolLoader = _tcAds.CreateSymbolInfoLoader();
                _woopsaObjects = new Dictionary<string, WoopsaObject>();
                _woopsaProperties = new Dictionary<string, WoopsaAdsProperty>();
            
                foreach (TcAdsSymbolInfo symbol in _symbolLoader)
                {
                    WoopsaObject newObject = null;
                    WoopsaAdsProperty newProperty = null;
                    TcAdsSymbolInfo parentInfo;
                    WoopsaValueType propertyType;
                    string[] path = symbol.Name.Split('.');
                    string name = path[path.Length - 1];
                    bool isProperties = BeckhoffToWoopsaValueType(symbol.Type, out propertyType);
                    if (symbol.Parent != null)
                    {
                        parentInfo = symbol.Parent;
                        if (_woopsaObjects.ContainsKey(parentInfo.Name))
                        {
                            if (isProperties)
                            {
                                newProperty = new WoopsaAdsProperty(_woopsaObjects[parentInfo.Name], name, propertyType,
                                                                    _woopsaAdsPropertyGet, _woopsaAdsPropertySet, symbol);
                            }
                            else
                                newObject = new WoopsaObject(_woopsaObjects[parentInfo.Name], name);
                        }
                        else
                        {
                            throw new Exception("Parent WoopsaObject not found !");
                        }
                    }
                    else
                    {
                        if (isProperties)
                        {
                            newProperty = new WoopsaAdsProperty(root, name, propertyType, _woopsaAdsPropertyGet, _woopsaAdsPropertySet, symbol);
                        }
                        else
                            newObject = new WoopsaObject(root, name);
                    }

                    if (isProperties)
                        _woopsaProperties.Add(symbol.Name, newProperty);
                    else
                        _woopsaObjects.Add(symbol.Name, newObject);
                }
            }catch(Exception e)
            {
                DiagnosticWindow.AddToDebug(e.Message);
            }
            isHierarchieLoaded = true;
        }
        public WoopsaValue ReadAdsValue(IWoopsaProperty woopsaProperty)
        {
            WoopsaAdsProperty property = (WoopsaAdsProperty)woopsaProperty;
            AdsStream stream = new AdsStream(80); // for STRING(80)
            long data = 0;

            stream.Flush();
            try
            {
                _tcAds.Read(property.AdsInfo.IndexGroup, property.AdsInfo.IndexOffset, stream);
            }
            catch (Exception)
            {
                isAdsConnected = false;
                return null;
            }
            switch (property.Type)
            {
                case WoopsaValueType.Integer:
                    if (property.AdsInfo.Type == BeckhoffValueType.WORD ||
                        property.AdsInfo.Type == BeckhoffValueType.UINT)
                        return BitConverter.ToUInt16(stream.GetBuffer(), 0);
                    else if (property.AdsInfo.Type == BeckhoffValueType.DWORD ||
                            property.AdsInfo.Type == BeckhoffValueType.UDINT)
                        return BitConverter.ToUInt32(stream.GetBuffer(), 0);
                    else if (property.AdsInfo.Type == BeckhoffValueType.SINT)
                        return Convert.ToSByte((sbyte)stream.GetBuffer()[0]);
                    else if (property.AdsInfo.Type == BeckhoffValueType.INT)
                        return BitConverter.ToInt16(stream.GetBuffer(), 0);
                    else if (property.AdsInfo.Type == BeckhoffValueType.DINT)
                        return BitConverter.ToInt32(stream.GetBuffer(), 0);
                    else if (property.AdsInfo.Type == BeckhoffValueType.LINT || property.AdsInfo.Type == BeckhoffValueType.ULINT)
                        return BitConverter.ToInt64(stream.GetBuffer(), 0);
                    else if (property.AdsInfo.Type == BeckhoffValueType.USINT || property.AdsInfo.Type == BeckhoffValueType.BYTE)
                        return (byte)stream.GetBuffer()[0];                    
                    else
                        throw new WoopsaException("Ads type not compatible with Woopsa Integer");
                case WoopsaValueType.Logical:
                    return Convert.ToBoolean(stream.GetBuffer()[0]);
                case WoopsaValueType.Real:
                    if (property.AdsInfo.Type == BeckhoffValueType.REAL)
                        return BitConverter.ToSingle(stream.GetBuffer(), 0);
                    else if (property.AdsInfo.Type == BeckhoffValueType.LREAL)
                        return BitConverter.ToDouble(stream.GetBuffer(), 0);
                    else
                        throw new WoopsaException("Ads type not compatible with Woopsa Real");
                case WoopsaValueType.Text:
                    string s = System.Text.Encoding.ASCII.GetString(stream.GetBuffer());
                    int index = s.IndexOf('\0');
                    return s.Remove(index, s.Length - index);
                case WoopsaValueType.TimeSpan:
                    data = bufferToLong(stream.GetBuffer(), 4);
                    TimeSpan timeSpan = new TimeSpan(TimeSpan.TicksPerMillisecond * data);
                    return timeSpan;
                case WoopsaValueType.DateTime:
                    TimeSpan timeSp;
                    DateTime dateTime;
                    data = bufferToLong(stream.GetBuffer(), 4);
                    dateTime = BeckhoffPlcReferenceDateTime;
                    if (property.AdsInfo.Type == BeckhoffValueType.TIME_OF_DAY)
                        timeSp = TimeSpan.FromMilliseconds(data);
                    else
                        timeSp = TimeSpan.FromSeconds(data);
                    dateTime = dateTime + timeSp;
                    WoopsaValue value = new WoopsaValue(dateTime);
                    return value;
                default:
                    return null;
            }
        }
        private long bufferToLong(byte[] buffer, int bufferLength)
        {
            long data = 0;
            for (int i = 0; i < bufferLength; i++)
            {
                data += buffer[i] * (long)Math.Pow(256, i);
            }
            return data;
        }

        public void WriteAdsValue(IWoopsaProperty property, IWoopsaValue value)
        {
            WoopsaAdsProperty adsProperty = (WoopsaAdsProperty)property;
            AdsStream stream = new AdsStream();

            switch (property.Type)
            {
                case WoopsaValueType.Integer:
                    if (adsProperty.AdsInfo.Type == BeckhoffValueType.WORD ||
                        adsProperty.AdsInfo.Type == BeckhoffValueType.UINT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToUInt16()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.DWORD ||
                            adsProperty.AdsInfo.Type == BeckhoffValueType.UDINT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToUInt32()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.SINT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToSByte()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.INT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToInt16()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.DINT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToInt32()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.LINT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToInt64()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.USINT ||
                             adsProperty.AdsInfo.Type == BeckhoffValueType.BYTE)
                    {
                        stream = new AdsStream(1);
                        stream.WriteByte(value.ToByte());
                    }
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.ULINT)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToUInt64()));
                    else
                        throw new WoopsaException("Ads type not compatible with Woopsa Integer");
                    break;
                case WoopsaValueType.Logical:
                    stream = new AdsStream(BitConverter.GetBytes(value.ToBool()));
                    break;
                case WoopsaValueType.Real:
                    if (adsProperty.AdsInfo.Type == BeckhoffValueType.REAL)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToFloat()));
                    else if (adsProperty.AdsInfo.Type == BeckhoffValueType.LREAL)
                        stream = new AdsStream(BitConverter.GetBytes(value.ToDouble()));
                    else
                        throw new WoopsaException("Ads type not compatible with Woopsa Real");
                    break;
                case WoopsaValueType.Text:
                    stream = new AdsStream(80);
                    byte[] byteString = System.Text.Encoding.ASCII.GetBytes(value.ToString());
                    stream.Write(byteString, 0, byteString.Length);
                    break;
                case WoopsaValueType.TimeSpan:
                    stream = new AdsStream(BitConverter.GetBytes((uint)(value.ToTimeSpan().Ticks / TimeSpan.TicksPerMillisecond)));
                    break;
                case WoopsaValueType.DateTime:
                    TimeSpan timeSp;
                    DateTime dateTime;
                    if (adsProperty.AdsInfo.Type == BeckhoffValueType.TIME_OF_DAY)
                    {
                        dateTime = value.ToDateTime();
                        uint timeOfDay = (uint)dateTime.Millisecond + ((uint)dateTime.Second + ((uint)dateTime.Minute + ((uint)dateTime.Hour * 60)) * 60) * 1000;
                        stream = new AdsStream(BitConverter.GetBytes(timeOfDay));
                    }
                    else
                    {
                        timeSp = value.ToDateTime() - BeckhoffPlcReferenceDateTime;
                        stream = new AdsStream(BitConverter.GetBytes((uint)timeSp.TotalSeconds));
                    }
                    break;
                default:
                    stream = new AdsStream(1);
                    break;
            }
            _tcAds.Write(adsProperty.AdsInfo.IndexGroup, adsProperty.AdsInfo.IndexOffset, stream);
        }
    }
}
