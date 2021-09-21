using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_WSA
#else
    using System.Text.Json;
#endif

namespace Woopsa
{
#if UNITY_WSA

    public class WoopsaJsonData
    {
        public static WoopsaJsonData CreateFromText(string jsonText)
        {
            object deserializedData = UnityJsonSerializer.Deserialize<object>(jsonText);
            return new WoopsaJsonData(deserializedData, jsonText);
        }

        public static WoopsaJsonData CreateFromDeserializedData(object deserializedJson)
        {
            return new WoopsaJsonData(deserializedJson, null);
        }

        private WoopsaJsonData(object deserializedData, string serializedData)
        {
            _data = deserializedData;
            _serializedData = serializedData;
            _asDictionary = UnityJsonSerializer.ToDictionnary(_data);
            _asArray = UnityJsonSerializer.ToArray(_data);
        }

        public WoopsaJsonData this[string key]
        {
            get
            {
                WoopsaJsonData result;
                if (TryGetDictionaryKey(key, out result))
                    return result;
                else
                    throw new InvalidOperationException("String indexer is only available on WoopsaJsonData of type Object.");
            }
        }

        public bool TryGetDictionaryKey(string key, out WoopsaJsonData value)
        {
            if (IsDictionary)
            {
                object dictionnaryEntry;
                if (_asDictionary.TryGetValue(key, out dictionnaryEntry))
                {
                    value = CreateFromDeserializedData(_asDictionary[key]);
                    return true;
                }
            }

            // else
            value = null;
            return false;
        }

        public IEnumerable<string> Keys
        {
            get
            {
                if (IsDictionary)
                    return _asDictionary.Keys;
                else
                    return new string[0];
            }
        }

        public bool ContainsKey(string key)
        {
            if (IsDictionary)
                return _asDictionary.ContainsKey(key);
            else
                return false;
        }

        public WoopsaJsonData this[int index]
        {
            get
            {
                WoopsaJsonData result;
                if (TryGetArrayIndex(index, out result))
                    return result;
                else
                    throw new InvalidOperationException("Integer indexer is only available on WoopsaJsonData of type Array.");
            }
        }

        public bool TryGetArrayIndex(int index, out WoopsaJsonData result)
        {
            if (IsArray)
            {
                result = CreateFromDeserializedData(_asArray[index]);
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
                    return _asArray.Length;
                else
                    throw new InvalidOperationException("Length is only available on WoopsaJsonData of type Array.");
            }
        }

        public bool IsArray { get { return _asArray != null; } }

        public bool IsDictionary { get { return _asDictionary != null; } }

        public bool IsSimple { get { return (_asArray == null) && (_asDictionary == null); } }

        public string AsText
        {
            get
            {
                if (_serializedData == null)
                {
                    if (IsSimple)
                        _serializedData = WoopsaFormat.ToStringWoopsa(_data);
                    else
                    {
                        _serializedData = UnityJsonSerializer.Serialize(_data);
                    }
                }
                return _serializedData;
            }
        }

        public override string ToString()
        {
            return AsText;
        }

        internal object InternalObject { get { return _data; } }

        private readonly object _data;
        private Dictionary<string, object> _asDictionary;
        private object[] _asArray;
        private string _serializedData;

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
            return value.AsText;
        }
#endregion
    }

#else

    public class WoopsaJsonData
    {
        #region Fields / Attributes

        internal JsonElement InternalObject => _jsonElement;

        private readonly JsonElement _jsonElement;

        #endregion

        #region Properties

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

        #endregion

        #region Public Static Methods

                            public static WoopsaJsonData CreateFromText(string jsonText)
                            {
                                object deserializedData = JsonSerializer.Deserialize<object>(jsonText);
                                return new WoopsaJsonData(deserializedData);
                            }
                            public static WoopsaJsonData CreateFromDeserializedData(object deserializedJson)
                            {
                                return new WoopsaJsonData(deserializedJson);
                            }

        #endregion

        #region Public Methods

                            public override string ToString()
                            {
                                return AsText;
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

        #endregion

        #region Private methods

        private WoopsaJsonData(object deserializedData)
        {
            if (deserializedData is JsonElement jsonElement)
                _jsonElement = jsonElement;
            else
                throw new NotSupportedException("Error must be JsonElement");
        }

        #endregion

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
#endif
}
