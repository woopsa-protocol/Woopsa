using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                _jsonData = new WoopsaJsonData(text);
		}

        public static WoopsaValue CreateUnchecked(string text, WoopsaValueType type, DateTime? timestamp)
        {
            return new WoopsaValue(text, type, timestamp);
        }

        public static WoopsaValue CreateUnchecked(string text, WoopsaValueType type)
        {
            return CreateUnchecked(text, type, null);
        }

        public static WoopsaValue CreateChecked(string text, WoopsaValueType type, DateTime? timestamp)
        {
            try
            {
                // Sanity check for the value passed in argument
                switch (type)
                {
                    case WoopsaValueType.Integer:
                        Int64.Parse(text);
                        break;
                    case WoopsaValueType.Real:
                        Double.Parse(text);
                        break;
                    case WoopsaValueType.Logical:
                        Boolean.Parse(text);
                        text = text.ToLower(); // .NET and JSON serialize booleans differently (.NET uses a capital first letter) :/
                        break;
                    case WoopsaValueType.DateTime:
                        DateTime.Parse(text);
                        break;
                    case WoopsaValueType.TimeSpan:
                        Double.Parse(text);
                        break;
                }
            }
            catch (Exception)
            {
                throw new WoopsaException(String.Format("Cannot create a WoopsaValue of type {0} from string \"{1}\"", type.ToString(), text));
            }
            return CreateUnchecked(text, type, timestamp);
        }

        public static WoopsaValue CreateChecked(string text, WoopsaValueType type)
        {
            return CreateChecked(text, type, null);
        }

        private WoopsaValue(string text, WoopsaValueType type)
            : this(text, type, null)
		{
		}

        public WoopsaValue(object value, DateTime? timestamp)
        {
            _jsonData = new WoopsaJsonData(value);
            _type = WoopsaValueType.JsonData;
            _timestamp = timestamp;
        }

        public WoopsaValue(object value)
            : this(value, null)
        {
        }

		public WoopsaValue(bool value)
			: this(value ? WoopsaConst.WoopsaTrue : WoopsaConst.WoopsaFalse, WoopsaValueType.Logical)
		{
		}

		public WoopsaValue(Int64 value)
			: this(value.ToStringWoopsa(), WoopsaValueType.Integer)
		{
		}

		public WoopsaValue(double value)
			: this(value.ToStringWoopsa(), WoopsaValueType.Real)
		{
		}

		public WoopsaValue(DateTime value)
			: this(value.ToWoopsaDateTime(), WoopsaValueType.DateTime)
		{
		}

		public WoopsaValue(TimeSpan value)
			: this(value.ToWoopsaTimeSpan(), WoopsaValueType.TimeSpan)
		{
		}

		public WoopsaValue(string value)
			: this(value.ToString(), WoopsaValueType.Text)
		{
		}

		public override bool Equals(object obj)
		{
			if (obj is WoopsaValue)
			{
				WoopsaValue right = (WoopsaValue)obj;
				return right._type == _type && right._text == _text;
			}
			else if (obj == null && _type == WoopsaValueType.Null)
				return true;
			else if (obj is bool && _type == WoopsaValueType.Logical)
				return ((bool)obj) == (bool)this;
			else if (obj is sbyte || obj is Int16 || obj is Int32 || obj is Int64)
				return Int64.Parse(obj.ToString()) == (Int64)this;
			else if (obj is Byte || obj is UInt16 || obj is UInt32 || obj is UInt64)
				return UInt64.Parse(obj.ToString()) == (UInt64)this;
			else if (obj is float || obj is double || obj is decimal)
				return double.Parse(obj.ToString()) == (double)this;
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
                if (_jsonData == null)
                    return _text;
                else
                    return _jsonData.Serialize();
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

		public static implicit operator bool(WoopsaValue value)
		{
			return value.ToBool();
		}

		public static implicit operator sbyte(WoopsaValue value)
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

		public static implicit operator byte(WoopsaValue value)
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

		public static implicit operator float(WoopsaValue value)
		{
			return value.ToFloat();
		}

		public static implicit operator double(WoopsaValue value)
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

		public static implicit operator string(WoopsaValue value)
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
			return woopsaItemPath.TrimStart(WoopsaConst.WoopsaPathSeparator);
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
		private static WoopsaValue _null = new WoopsaValue(string.Empty, WoopsaValueType.Null);
        private WoopsaJsonData _jsonData = null;
	}
}
