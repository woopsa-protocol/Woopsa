using System;
using System.Globalization;
using System.Text;

namespace Woopsa
{
    public class WoopsaValue : IWoopsaValue
    {
        private WoopsaValue(string text, WoopsaValueType type, DateTime? timestamp)
        {
            _text = text;
            _type = type;
            _timestamp = timestamp;
            if (type == WoopsaValueType.JsonData)
                _jsonData = Woopsa.WoopsaJsonData.CreateFromText(text);
        }

        internal static WoopsaValue CreateUnchecked(string text, WoopsaValueType type, DateTime? timestamp = null)
        {
            return new WoopsaValue(text, type, timestamp);
        }

        public static WoopsaValue CreateChecked(string text, WoopsaValueType type, DateTime? timestamp = null)
        {
            try
            {
                // Sanity check for the value passed in argument
                switch (type)
                {
                    case WoopsaValueType.Integer:
                        Int64.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case WoopsaValueType.Real:
                        Double.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case WoopsaValueType.Logical:
                        Boolean.Parse(text);
                        text = text.ToLower(); // .NET and JSON serialize booleans differently (.NET uses a capital first letter) :/
                        break;
                    case WoopsaValueType.Text:
                        if (text == null)
                            text = string.Empty;
                        break;
                    case WoopsaValueType.DateTime:
                        DateTime.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case WoopsaValueType.TimeSpan:
                        Double.Parse(text, CultureInfo.InvariantCulture);
                        break;
                }
            }
            catch (Exception)
            {
                throw new WoopsaException(String.Format("Cannot create a WoopsaValue of type {0} from string \"{1}\"", type.ToString(), text));
            }
            return CreateUnchecked(text, type, timestamp);
        }

        public static WoopsaValue ToWoopsaValue(object value, WoopsaValueType type, DateTime? timeStamp = null)
        {
            try
            {
                switch (type)
                {
                    case WoopsaValueType.Logical:
                        return new WoopsaValue((bool)value, timeStamp);
                    case WoopsaValueType.Integer:
                        return new WoopsaValue(Convert.ToInt64(value), timeStamp);
                    case WoopsaValueType.Real:
                        return new WoopsaValue(Convert.ToDouble(value), timeStamp);
                    case WoopsaValueType.DateTime:
                        return new WoopsaValue((DateTime)value, timeStamp);
                    case WoopsaValueType.TimeSpan:
                        return new WoopsaValue((TimeSpan)value, timeStamp);
                    case WoopsaValueType.Text:
                        if (string.IsNullOrEmpty((string)value))
                            return new WoopsaValue(string.Empty, timeStamp);
                        else
                            return new WoopsaValue(WoopsaFormat.ToStringWoopsa(value), timeStamp);
                    default:
                        return WoopsaValue.CreateUnchecked(WoopsaFormat.ToStringWoopsa(value), type, timeStamp);
                }
            }
            catch (InvalidCastException)
            {
                throw new WoopsaException(String.Format("Cannot typecast object of type {0} to Woopsa Type {1}", value.GetType(), type.ToString()));
            }
        }

        private WoopsaValue(string text, WoopsaValueType type)
            : this(text, type, null)
        {
        }

        public WoopsaValue(WoopsaJsonData jsonData, DateTime? timestamp = null)
        {
            _jsonData = jsonData;
            _type = WoopsaValueType.JsonData;
            _timestamp = timestamp;
        }

        public WoopsaValue(bool value, DateTime? timestamp = null)
            : this(value ? WoopsaConst.WoopsaTrue : WoopsaConst.WoopsaFalse, WoopsaValueType.Logical, timestamp)
        {
        }

        public WoopsaValue(Int64 value, DateTime? timestamp = null)
            : this(WoopsaFormat.ToStringWoopsa(value), WoopsaValueType.Integer, timestamp)
        {
        }

        public WoopsaValue(double value, DateTime? timestamp = null)
            : this(WoopsaFormat.ToStringWoopsa(value), WoopsaValueType.Real, timestamp)
        {
        }

        public WoopsaValue(DateTime value, DateTime? timestamp = null)
            : this(WoopsaFormat.ToStringWoopsa(value), WoopsaValueType.DateTime, timestamp)
        {
        }

        public WoopsaValue(TimeSpan value, DateTime? timestamp = null)
            : this(WoopsaFormat.ToStringWoopsa(value), WoopsaValueType.TimeSpan, timestamp)
        {
        }

        public WoopsaValue(string value, DateTime? timestamp = null)
            : this(value ?? string.Empty, WoopsaValueType.Text, timestamp)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj is IWoopsaValue)
            {
                IWoopsaValue right = (IWoopsaValue)obj;
                return right.Type == _type && right.AsText == _text;
            }
            else if (obj == null && _type == WoopsaValueType.Null)
                return true;
            else if (obj is bool && _type == WoopsaValueType.Logical)
                return ((bool)obj) == (bool)this;
            else if (obj is sbyte || obj is Int16 || obj is Int32 || obj is Int64)
                return Convert.ToInt64(obj) == (Int64)this;
            else if (obj is Byte || obj is UInt16 || obj is UInt32 || obj is UInt64)
                return Convert.ToUInt64(obj) == (UInt64)this;
            else if (obj is float || obj is double || obj is decimal)
                return Convert.ToDouble(obj) == (double)this;
            else if (obj is DateTime)
                return ((DateTime)obj) == (DateTime)this;
            else if (obj is TimeSpan)
                return ((TimeSpan)obj) == (TimeSpan)this;
            else if (obj is string)
                return (string)obj == _text;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return AsText;
        }

        public static WoopsaValue Null
        {
            get { return _null; }
        }

        #region IWoopsaValue

        public string AsText
        {
            get
            {
                if (_jsonData != null)
                    if (_text == null)
                        _text = _jsonData.Serialize();
                return _text;
            }
        }

        public WoopsaValueType Type
        {
            get { return _type; }
        }


        public DateTime? TimeStamp
        {
            get { return _timestamp; }
        }

        #endregion IWoopsaValue

        public WoopsaJsonData JsonData
        {
            get
            {
                if (_jsonData == null)
                    throw new InvalidOperationException("JsonData is only available on WoopsaValue of type JsonData");
                else
                    return _jsonData;
            }
        }

        public static implicit operator bool (WoopsaValue value)
        {
            return value.ToBool();
        }

        public static implicit operator sbyte (WoopsaValue value)
        {
            return value.ToSByte();
        }

        public static implicit operator Int16(WoopsaValue value)
        {
            return value.ToInt16();
        }

        public static implicit operator Int32(WoopsaValue value)
        {
            return value.ToInt32();
        }

        public static implicit operator Int64(WoopsaValue value)
        {
            return value.ToInt64();
        }

        public static implicit operator byte (WoopsaValue value)
        {
            return value.ToByte();
        }

        public static implicit operator UInt16(WoopsaValue value)
        {
            return value.ToUInt16();
        }

        public static implicit operator UInt32(WoopsaValue value)
        {
            return value.ToUInt32();
        }

        public static implicit operator UInt64(WoopsaValue value)
        {
            return value.ToUInt64();
        }

        public static implicit operator float (WoopsaValue value)
        {
            return value.ToFloat();
        }

        public static implicit operator double (WoopsaValue value)
        {
            return value.ToDouble();
        }

        public static implicit operator DateTime(WoopsaValue value)
        {
            return value.ToDateTime();
        }

        public static implicit operator TimeSpan(WoopsaValue value)
        {
            return value.ToTimeSpan();
        }

        public static implicit operator string (WoopsaValue value)
        {
            return value._text;
        }

        public static implicit operator WoopsaValue(bool value)
        {
            return new WoopsaValue(value);
        }

        public static implicit operator WoopsaValue(Int64 value)
        {
            return new WoopsaValue(value);
        }

        public static implicit operator WoopsaValue(double value)
        {
            return new WoopsaValue(value);
        }

        public static implicit operator WoopsaValue(DateTime value)
        {
            return new WoopsaValue(value);
        }

        public static implicit operator WoopsaValue(TimeSpan value)
        {
            return new WoopsaValue(value);
        }

        public static implicit operator WoopsaValue(string value)
        {
            return new WoopsaValue(value);
        }

        #region Woopsa extended types		

        public static string FormatRelativeWoopsaLink(string woopsaItemPath)
        {
            return WoopsaUtils.RemoveInitialSeparator(woopsaItemPath);
        }

        public static string FormatAbsoluteWoopsaLink(string woopsaServerUrl, string woopsaItemPath)
        {
            StringBuilder sb = new StringBuilder();
            if (woopsaServerUrl != null)
            {
                sb.Append(woopsaServerUrl.TrimEnd(WoopsaConst.UrlSeparator));
                sb.Append(WoopsaConst.WoopsaLinkSeparator);
            }
            sb.Append(FormatRelativeWoopsaLink(woopsaItemPath));
            return sb.ToString();
        }

        public static WoopsaValue WoopsaAbsoluteLink(string woopsaServerUrl, string woopsaItemPath)
        {
            return new WoopsaValue(FormatAbsoluteWoopsaLink(woopsaServerUrl, woopsaItemPath), WoopsaValueType.WoopsaLink, null);
        }

        public static WoopsaValue WoopsaRelativeLink(string woopsaItemPath)
        {
            return new WoopsaValue(FormatRelativeWoopsaLink(woopsaItemPath), WoopsaValueType.WoopsaLink, null);
        }

        public static WoopsaValue WoopsaResourceUrl(string resourceUrl)
        {
            return new WoopsaValue(resourceUrl, WoopsaValueType.ResourceUrl, null);
        }

        public static WoopsaValue WoopsaJsonData(string jsonData)
        {
            return new WoopsaValue(jsonData, WoopsaValueType.JsonData, null);
        }

        #endregion Woopsa extended types

        private string _text;
        private WoopsaValueType _type;
        private DateTime? _timestamp;
        private WoopsaJsonData _jsonData = null;

        private static readonly WoopsaValue _null = new WoopsaValue(string.Empty, WoopsaValueType.Null);

    }
}
