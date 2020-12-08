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

        internal static WoopsaValue CreateUnchecked(string text, WoopsaValueType type, DateTime? timestamp = null) =>
            new WoopsaValue(text, type, timestamp);

        public static WoopsaValue CreateChecked(string text, WoopsaValueType type, DateTime? timestamp = null)
        {
            try
            {
                // Sanity check for the value passed in argument
                switch (type)
                {
                    case WoopsaValueType.Integer:
                        long.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case WoopsaValueType.Real:
                        double.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case WoopsaValueType.Logical:
                        bool.Parse(text);
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
                        double.Parse(text, CultureInfo.InvariantCulture);
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
                        if (value is TimeSpan)
                            return new WoopsaValue((TimeSpan)value, timeStamp);
                        else
                            return new WoopsaValue(TimeSpan.FromSeconds(Convert.ToDouble(value)),
                                timeStamp);
                    case WoopsaValueType.Text:
                        if (value == null)
                            return new WoopsaValue(string.Empty, timeStamp);
                        else if (value.GetType().IsEnum)
                            return new WoopsaValue(value.ToString(), timeStamp);
                        else if (string.IsNullOrEmpty((string)value))
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

        public static WoopsaValue DeserializedJsonToWoopsaValue(object deserializedJson,
            WoopsaValueType type, DateTime? timeStamp = null)
        {
            if (type == WoopsaValueType.JsonData)
                return new WoopsaValue(Woopsa.WoopsaJsonData.CreateFromDeserializedData(deserializedJson));
            else
                return ToWoopsaValue(deserializedJson, type, timeStamp);
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
            : this(WoopsaFormat.ToStringWoopsa(value), WoopsaValueType.Logical, timestamp)
        {
        }

        public WoopsaValue(long value, DateTime? timestamp = null)
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
            else if (obj is sbyte || obj is short || obj is int || obj is long)
                return Convert.ToInt64(obj) == (long)this;
            else if (obj is byte || obj is ushort || obj is uint || obj is ulong)
                return Convert.ToUInt64(obj) == (ulong)this;
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

        public override int GetHashCode() => base.GetHashCode(); // Note : Avoid a warning due to the override of equals

        public override string ToString() => AsText;

        public static WoopsaValue Null => _null;

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

        public WoopsaValueType Type => _type;


        public DateTime? TimeStamp => _timestamp;

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

        public static implicit operator bool(WoopsaValue value) => value.ToBool();

        public static implicit operator sbyte(WoopsaValue value) => value.ToSByte();

        public static implicit operator short(WoopsaValue value) => value.ToInt16();

        public static implicit operator int(WoopsaValue value) => value.ToInt32();

        public static implicit operator long(WoopsaValue value) => value.ToInt64();

        public static implicit operator byte(WoopsaValue value) => value.ToByte();

        public static implicit operator ushort(WoopsaValue value) => value.ToUInt16();

        public static implicit operator uint(WoopsaValue value) => value.ToUInt32();

        public static implicit operator ulong(WoopsaValue value) => value.ToUInt64();

        public static implicit operator float(WoopsaValue value) => value.ToFloat();

        public static implicit operator double(WoopsaValue value) => value.ToDouble();

        public static implicit operator DateTime(WoopsaValue value) => value.ToDateTime();

        public static implicit operator TimeSpan(WoopsaValue value) => value.ToTimeSpan();

        public static implicit operator string(WoopsaValue value) => value._text;

        public static implicit operator WoopsaValue(bool value) => new WoopsaValue(value);

        public static implicit operator WoopsaValue(Int64 value) => new WoopsaValue(value);

        public static implicit operator WoopsaValue(double value) => new WoopsaValue(value);

        public static implicit operator WoopsaValue(DateTime value) => new WoopsaValue(value);

        public static implicit operator WoopsaValue(TimeSpan value) => new WoopsaValue(value);

        public static implicit operator WoopsaValue(string value) => new WoopsaValue(value);

        #region Woopsa extended types		

        public static string FormatRelativeWoopsaLink(string woopsaItemPath) =>
            WoopsaUtils.RemoveInitialSeparator(woopsaItemPath);

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

        public static WoopsaValue WoopsaAbsoluteLink(string woopsaServerUrl, string woopsaItemPath) =>
            new WoopsaValue(FormatAbsoluteWoopsaLink(woopsaServerUrl, woopsaItemPath), WoopsaValueType.WoopsaLink, null);

        public static WoopsaValue WoopsaRelativeLink(string woopsaItemPath) =>
            new WoopsaValue(FormatRelativeWoopsaLink(woopsaItemPath), WoopsaValueType.WoopsaLink, null);

        public static WoopsaValue WoopsaResourceUrl(string resourceUrl) =>
            new WoopsaValue(resourceUrl, WoopsaValueType.ResourceUrl, null);

        public static WoopsaValue WoopsaJsonData(string jsonData) =>
            new WoopsaValue(jsonData, WoopsaValueType.JsonData, null);

        #endregion Woopsa extended types

        private string _text;
        private WoopsaValueType _type;
        private DateTime? _timestamp;
        private WoopsaJsonData _jsonData = null;

        private static readonly WoopsaValue _null = new WoopsaValue(string.Empty, WoopsaValueType.Null);

    }
}
