using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Woopsa
{
    public class WoopsaJsonData
    {
        public static WoopsaJsonData CreateFromText(string jsonText)
        {
            object deserializedData = JsonSerializer.Deserialize<object>(jsonText);
            return new WoopsaJsonData(deserializedData);
        }
        public static WoopsaJsonData CreateFromDeserializedData(object deserializedJson)
        {
            return new WoopsaJsonData(deserializedJson);
        }

        private WoopsaJsonData(object deserializedData)
        {
            if (deserializedData is JsonElement jsonElement)
                _jsonElement = jsonElement;
            else
                throw new NotSupportedException("Error must be JsonElement");
        }

        public WoopsaJsonData this[string key]
        {
            get
            {
                if (TryGetDictionaryKey(key, out WoopsaJsonData result))
                    return result;
                else
                    throw new InvalidOperationException("String indexer is only available on WoopsaJsonData of type Object.");
            }
        }

        public bool TryGetDictionaryKey(string key, out WoopsaJsonData value)
        {
            if (IsDictionary)
            {
                if (_jsonElement.TryGetProperty(key, out var dictionnaryEntry))
                {
                    value = CreateFromDeserializedData(dictionnaryEntry);
                    return true;
                }
            }

            // else
            value = null;
            return false;
        }

        public bool ContainsKey(string key)
        {
            if (IsDictionary)
                return _jsonElement.TryGetProperty(key, out _);
            else
                return false;
        }

        public WoopsaJsonData this[int index]
        {
            get
            {
                if (TryGetArrayIndex(index, out WoopsaJsonData result))
                    return result;
                else
                    throw new InvalidOperationException("Integer indexer is only available on WoopsaJsonData of type Array.");
            }
        }

        public bool TryGetArrayIndex(int index, out WoopsaJsonData result)
        {
            if (IsArray)
            {
                result = CreateFromDeserializedData(_jsonElement[index]);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public int Length
        {
            get
            {
                if (IsArray)
                    return _jsonElement.GetArrayLength();
                else
                    throw new InvalidOperationException("Length is only available on WoopsaJsonData of type Array.");
            }
        }

        public bool IsArray => _jsonElement.ValueKind == JsonValueKind.Array;

        public bool IsDictionary => _jsonElement.ValueKind == JsonValueKind.Object;

        public bool IsSimple => !IsArray && !IsDictionary;

        public string AsText
        {
            get
            {
                if (_jsonElement.ValueKind == JsonValueKind.String)
                    return _jsonElement.GetString();
                return _jsonElement.GetRawText();
            }
        }

        public override string ToString()
        {
            return AsText;
        }

        public IEnumerable<string> Keys
        {
            get
            {
                if (IsDictionary)
                    return _jsonElement.EnumerateObject().Select(p => p.Name);
                else
                    return new string[0];
            }
        }

        internal JsonElement InternalObject => _jsonElement;

        private readonly JsonElement _jsonElement;

        #region Static methods for type casting
        public static implicit operator bool (WoopsaJsonData value)
        {
            return value.ToBool();
        }

        public static implicit operator sbyte (WoopsaJsonData value)
        {
            return value.ToSByte();
        }

        public static implicit operator short(WoopsaJsonData value)
        {
            return value.ToInt16();
        }

        public static implicit operator int(WoopsaJsonData value)
        {
            return value.ToInt32();
        }

        public static implicit operator long(WoopsaJsonData value)
        {
            return value.ToInt64();
        }

        public static implicit operator byte (WoopsaJsonData value)
        {
            return value.ToByte();
        }

        public static implicit operator ushort(WoopsaJsonData value)
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

        public static implicit operator float (WoopsaJsonData value)
        {
            return value.ToFloat();
        }

        public static implicit operator double (WoopsaJsonData value)
        {
            return value.ToDouble();
        }

        public static implicit operator string (WoopsaJsonData value)
        {
            return value.AsText;
        }
        #endregion
    }

    public static class WoopsaJsonDataExtensions
    {
        public static bool ToBool(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetBoolean();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("bool", data.InternalObject.GetType().ToString()));
        }

        public static sbyte ToSByte(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetSByte();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("SByte", data.InternalObject.GetType().ToString()));
        }

        public static short ToInt16(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetInt16();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int16", data.InternalObject.GetType().ToString()));
        }

        public static int ToInt32(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetInt32();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int32", data.InternalObject.GetType().ToString()));
        }

        public static long ToInt64(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetInt64();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int64", data.InternalObject.GetType().ToString()));
        }

        public static byte ToByte(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetByte();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Byte", data.InternalObject.GetType().ToString()));
        }

        public static ushort ToUInt16(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetUInt16();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt16", data.InternalObject.GetType().ToString()));
        }

        public static uint ToUInt32(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetUInt32();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt32", data.InternalObject.GetType().ToString()));
        }

        public static ulong ToUInt64(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetUInt64();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt64", data.InternalObject.GetType().ToString()));
        }

        public static float ToFloat(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return (float)data.InternalObject.GetDouble();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("float", data.InternalObject.GetType().ToString()));
        }

        public static double ToDouble(this WoopsaJsonData data)
        {
            if (data.IsSimple)
                return data.InternalObject.GetDouble();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("double", data.InternalObject.GetType().ToString()));
        }

        public static string Serialize(this WoopsaJsonData data)
        {
            return data.InternalObject.GetRawText();
        }
    }
}
