using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Woopsa
{
    public class WoopsaJsonData
    {
        public WoopsaJsonData(string serializedData)
        {
            JavaScriptSerializer deserializer = new JavaScriptSerializer();
            _data = deserializer.Deserialize<object>(serializedData);
            _asDictionary = (_data as Dictionary<string, object>);
            _asArray = _data as object[];
        }

        public WoopsaJsonData(object deserializedData)
        {
            _data = deserializedData;
            _asDictionary = (_data as Dictionary<string, object>);
            _asArray = _data as object[];
        }

        public WoopsaJsonData this[string key]
        {
            get
            {
                if (IsDictionary)
                {
                    var dic = (_data as Dictionary<string, object>);
                    return new WoopsaJsonData(dic[key]);
                }
                else
                    throw new InvalidOperationException("String indexer is only available on WoopsaJsonData of type Object.");
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                if (IsDictionary)
                    return (_data as Dictionary<string, object>).Keys;
                else
                    return new string[0];
            }
        }

        public WoopsaJsonData this[int key]
        {
            get
            {
                if (IsArray)
                {
                    var arr = (_data as object[]);
                    return new WoopsaJsonData(arr[key]);
                }
                else
                    throw new InvalidOperationException("Integer indexer is only available on JsonDataof type Array.");
            }
        }

        public int Length
        {
            get
            {
                if (IsArray)
                    return (_data as object[]).Length;
                else
                    throw new InvalidOperationException("Length is only available on WoopsaJsonData of type Array.");
            }
        }

        public bool IsArray { get { return _asArray != null; } }

        public bool IsDictionary { get { return _asDictionary != null; } }

        public bool IsSimple { get { return (_asArray == null) && (_asDictionary == null); } }

        public string AsText()
        {
            if (IsSimple)
                return (string)_data.ToString();
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("string", _data.GetType().ToString()));
        }

        internal object InternalObject{ get { return _data; } }

        private object _data;
        private Dictionary<string, object> _asDictionary;
        private object[] _asArray;

        #region Static methods for type casting
        public static implicit operator bool(WoopsaJsonData value)
        {
            return value.ToBool();
        }

        public static implicit operator sbyte(WoopsaJsonData value)
        {
            return value.ToSByte();
        }

        public static implicit operator Int16(WoopsaJsonData value)
        {
            return value.ToInt16();
        }

        public static implicit operator Int32(WoopsaJsonData value)
        {
            return value.ToInt32();
        }

        public static implicit operator Int64(WoopsaJsonData value)
        {
            return value.ToInt64();
        }

        public static implicit operator byte(WoopsaJsonData value)
        {
            return value.ToByte();
        }

        public static implicit operator UInt16(WoopsaJsonData value)
        {
            return value.ToUInt16();
        }

        public static implicit operator UInt32(WoopsaJsonData value)
        {
            return value.ToUInt32();
        }

        public static implicit operator UInt64(WoopsaJsonData value)
        {
            return value.ToUInt64();
        }

        public static implicit operator float(WoopsaJsonData value)
        {
            return value.ToFloat();
        }

        public static implicit operator double(WoopsaJsonData value)
        {
            return value.ToDouble();
        }

        public static implicit operator string(WoopsaJsonData value)
        {
            return value.AsText();
        }
        #endregion
    }

    public static class WoopsaJsonDataExtensions
    {
        public static bool ToBool(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (bool)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("bool", data.InternalObject.GetType().ToString()));
        }

        public static SByte ToSByte(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (SByte)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("SByte", data.InternalObject.GetType().ToString()));
        }

        public static Int16 ToInt16(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (Int16)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("Int16", data.InternalObject.GetType().ToString()));
        }

        public static Int32 ToInt32(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (Int32)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("Int32", data.InternalObject.GetType().ToString()));
        }

        public static Int64 ToInt64(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (Int64)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("Int64", data.InternalObject.GetType().ToString()));
        }

        public static Byte ToByte(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (Byte)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("Byte", data.InternalObject.GetType().ToString()));
        }

        public static UInt16 ToUInt16(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (UInt16)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("UInt16", data.InternalObject.GetType().ToString()));
        }

        public static UInt32 ToUInt32(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (UInt32)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("UInt32", data.InternalObject.GetType().ToString()));
        }

        public static UInt64 ToUInt64(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (UInt64)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("UInt64", data.InternalObject.GetType().ToString()));
        }

        public static float ToFloat(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (float)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("float", data.InternalObject.GetType().ToString()));
        }

        public static double ToDouble(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (double)data.InternalObject;
            else
                throw new WoopsaException(WoopsaExtensions.WoopsaCastTypeExceptionMessage("double", data.InternalObject.GetType().ToString()));
        }

        public static string Serialize(this WoopsaJsonData data)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(data.InternalObject);
        }
    }
}
