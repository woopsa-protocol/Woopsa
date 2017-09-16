using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaFormat
    {
        // The 4 woopsa verbs used in the protocol
        public const string VerbRead = "read";
        public const string VerbWrite = "write";
        public const string VerbInvoke = "invoke";
        public const string VerbMeta = "meta";

        #region Helpers

        public static bool ToBool(string text)
        {
            bool result;
            if (TryParseWoopsa(text, out result))
                return result;
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("bool", text));
        }

        public static bool TryParseWoopsa(string value, out bool result)
        {
            if (value == WoopsaConst.WoopsaTrue)
            {
                result = true;
                return true;
            }
            else if (value == WoopsaConst.WoopsaFalse)
            {
                result = false;
                return true;
            }
            else
            {
                result = false;
                return false;
            }
        }

        public static bool TryParseWoopsa(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out sbyte result)
        {
            return sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out Int16 result)
        {
            return Int16.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out Int32 result)
        {
            return Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out Int64 result)
        {
            return Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out byte result)
        {
            return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out UInt16 result)
        {
            return UInt16.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseWoopsa(string value, out UInt32 result)
        {
            return UInt32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
        public static bool TryParseWoopsa(string value, out UInt64 result)
        {
            return UInt64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static string ToStringWoopsa(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringWoopsa(Int64 value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringWoopsa(bool value)
        {
            return value ? WoopsaConst.WoopsaTrue : WoopsaConst.WoopsaFalse;
        }

        public static string ToStringWoopsa(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringWoopsa(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("o");
        }

        public static string ToStringWoopsa(TimeSpan timeSpan)
        {
            return ToStringWoopsa(timeSpan.TotalSeconds);
        }

        public static string ToStringWoopsa(object o)
        {
            return Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        #endregion


        public static string Serialize(this Exception e)
        {
            return String.Format(ErrorFormat, JsonEscape(e.GetFullMessage()), e.GetType().Name);
        }

        private class WoopsaErrorResult
        {
            public string Type { get; set; }
            public string Message { get; set; }
        }

        public static Exception DeserializeError(string jsonErrorText)
        {
            var serializer = new JsonSerializer();
            var error = serializer.Deserialize<WoopsaErrorResult>(jsonErrorText);

            // Generate one of the possible Woopsa exceptions based
            // on the JSON-serialized error
            if (error.Type == typeof(WoopsaNotFoundException).Name)
                return new WoopsaNotFoundException(error.Message);
            else if (error.Type == typeof(WoopsaNotificationsLostException).Name)
                return new WoopsaNotificationsLostException(error.Message);
            else if (error.Type == typeof(WoopsaInvalidOperationException).Name)
                return new WoopsaInvalidOperationException(error.Message);
            else if (error.Type == typeof(WoopsaInvalidSubscriptionChannelException).Name)
                return new WoopsaInvalidSubscriptionChannelException(error.Message);
            else if (error.Type == typeof(WoopsaException).Name)
                return new WoopsaException(error.Message);
            else
                return new Exception(error.Message);
        }

        public static void JsonEscape(StringBuilder stringBuilder, string value)
        {
            foreach (var item in value)
                switch (item)
                {
                    case '\\': stringBuilder.Append("\\\\"); break;
                    case '\"': stringBuilder.Append("\\\""); break;
                    case '\n': stringBuilder.Append("\\n"); break;
                    case '\b': stringBuilder.Append("\\b"); break;
                    case '\f': stringBuilder.Append("\\f"); break;
                    case '\r': stringBuilder.Append("\\r"); break;
                    case '\t': stringBuilder.Append("\\t"); break;
                    // TODO : compléter l'échappement des caractères spéciaux
                    default: stringBuilder.Append(item); break;
                }
        }

        /// <summary>
        /// Escapes a string so it safe for being put into quotes "" into Json Data
        /// </summary>
        /// <param name="value">The string to escape</param>
        /// <returns>A string with the following special characters handled, in this order: \ " \n \b \f \r \t</returns>
        public static string JsonEscape(string value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            JsonEscape(stringBuilder, value);
            return stringBuilder.ToString();
        }

        public static void Serialize(StringBuilder stringBuilder, 
            Dictionary<string, WoopsaValue> keyValuePairs)
        {
            bool next = false;
            stringBuilder.Append(ElementOpen);
            foreach (var item in keyValuePairs)
            {
                if (next)
                    stringBuilder.Append(MultipleElementsSeparator);
                SerializeKeyValue(stringBuilder, item.Key, item.Value.JsonValueText(), false, false);
                next = true;
            }
            stringBuilder.Append(ElementClose);
        }

        public static void Serialize(StringBuilder stringBuilder, ClientRequest request)
        {
            stringBuilder.Append(ElementOpen);
            SerializeKeyValue(stringBuilder, KeyId, request.Id.ToString(), false, false);
            stringBuilder.Append(ElementSeparator);
            SerializeKeyValue(stringBuilder, KeyVerb, request.Verb, true, false);
            stringBuilder.Append(ElementSeparator);
            SerializeKeyValue(stringBuilder, KeyPath, request.Path, true, false);
            switch (request.Verb)
            {
                case VerbWrite:
                    stringBuilder.Append(ElementSeparator);
                    SerializeKeyValue(stringBuilder, KeyValue, request.Value.JsonValueText(), false, false);
                    break;
                case VerbInvoke:
                    stringBuilder.Append(ElementSeparator);
                    SerializeKeyValuePrefix(stringBuilder, KeyArguments);
                    Serialize(stringBuilder, request.Arguments);
                    break;
            }
            stringBuilder.Append(ElementClose);
        }

        public static void Serialize(StringBuilder stringBuilder, IEnumerable<ClientRequest> requests)
        {
            bool next = false;
            stringBuilder.Append(MultipleElementsOpen);
            foreach (var item in requests)
            {
                if (next)
                    stringBuilder.Append(MultipleElementsSeparator);
                Serialize(stringBuilder, item);
                next = true;
            }
            stringBuilder.Append(MultipleElementsClose);
        }

        public static string Serialize(this IEnumerable<ClientRequest> requests)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Serialize(stringBuilder, requests);
            return stringBuilder.ToString();
        }

        public static string Serialize(this IEnumerable<MultipleRequestResponse> responses)
        {
            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var response in responses)
            {
                if (!first)
                    builder.Append(MultipleElementsSeparator);
                else
                    first = false;
                builder.Append(response.Serialize());
            }
            return String.Format(MultipleElementsFormat, builder.ToString());
        }

        public static string Serialize(this MultipleRequestResponse response)
        {
            return String.Format(MultipleRequestResponseFormat, response.Id, response.Result);
        }

        public static string Serialize(this IWoopsaNotifications notifications)
        {
            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var notification in notifications.Notifications)
            {
                if (!first)
                    builder.Append(MultipleElementsSeparator);
                else
                    first = false;
                builder.Append(notification.Serialize());
            }
            return String.Format(MultipleElementsFormat, builder.ToString());
        }

        public static string Serialize(this IWoopsaNotification notification)
        {
            return String.Format(NotificationFormat, notification.Value.Serialize(), notification.SubscriptionId, notification.Id);
        }

        public static void SerializeKeyValuePrefix(StringBuilder stringBuilder, string key)
        {
            stringBuilder.
                Append(ValueEscapeCharacter).Append(key).Append(ValueEscapeCharacter).
                Append(KeyValueSeparator);
        }

        public static void SerializeKeyValue(StringBuilder stringBuilder, string key, string value,
            bool escapeValue, bool jsonEscapeValue)
        {
            SerializeKeyValuePrefix(stringBuilder, key);
            if (escapeValue)
                stringBuilder.Append(ValueEscapeCharacter);
            if (jsonEscapeValue)
                JsonEscape(stringBuilder, value);
            else
                stringBuilder.Append(value);
            if (escapeValue)
                stringBuilder.Append(ValueEscapeCharacter);
        }

        private static bool MustQuote(WoopsaValueType type)
        {
            return
                type != WoopsaValueType.JsonData &&
                type != WoopsaValueType.Real &&
                type != WoopsaValueType.Integer &&
                type != WoopsaValueType.Logical &&
                type != WoopsaValueType.TimeSpan;
        }

        public static string JsonValueText(this IWoopsaValue value)
        {
            if (MustQuote(value.Type))
                return ValueEscapeCharacter + JsonEscape(value.AsText) + ValueEscapeCharacter;
            else
                return value.AsText;
        }

        public static void Serialize(StringBuilder stringBuilder, IWoopsaValue value)
        {
            stringBuilder.Append(ElementOpen);
            // Value
            if (MustQuote(value.Type))
                SerializeKeyValue(stringBuilder, KeyValue, value.AsText, true, true);
            else
                SerializeKeyValue(stringBuilder, KeyValue, value.AsText, false, false);
            stringBuilder.Append(ElementSeparator);
            // Type
            SerializeKeyValue(stringBuilder, KeyType, value.Type.ToString(), true, false);
            // TimeStamp
            if (value.TimeStamp.HasValue)
            {
                stringBuilder.Append(ElementSeparator);
                SerializeKeyValue(stringBuilder, KeyTimeStamp,
                        WoopsaFormat.ToStringWoopsa(value.TimeStamp.Value), true, false);
            }
            stringBuilder.Append(ElementClose);
        }

        public static string Serialize(this IWoopsaValue value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Serialize(stringBuilder, value);
            return stringBuilder.ToString();
        }

        public static string SerializeMetadata(this IWoopsaContainer container, bool justName = false)
        {
            if (justName)
            {
                return String.Format(StringFormat, JsonEscape(container.Name));
            }
            if (container is IWoopsaObject)
            {
                return (container as IWoopsaObject).SerializeMetadata();
            }
            else
            {
                string items = container.Items.SerializeMetadata();
                return String.Format(MetadataContainer, container.Name, items);
            }
        }

        public static string SerializeMetadata(this IWoopsaObject obj)
        {
            string items = obj.Items.SerializeMetadata();
            string properties = obj.Properties.SerializeMetadata();
            string methods = obj.Methods.SerializeMetadata();
            return String.Format(MetadataObject, obj.Name, items, properties, methods);
        }

        private static string SerializeMetadata(this IEnumerable<IWoopsaElement> elements)
        {
            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var elem in elements)
            {
                if (!first)
                    builder.Append(MultipleElementsSeparator);
                else
                    first = false;
                if (elem is IWoopsaMethod)
                {
                    builder.Append((elem as IWoopsaMethod).SerializeMetadata());
                }
                else if (elem is IWoopsaProperty)
                {
                    builder.Append((elem as IWoopsaProperty).SerializeMetadata());
                }
                else if (elem is IWoopsaContainer)
                {
                    builder.Append((elem as IWoopsaContainer).SerializeMetadata(true));
                }
            }
            return String.Format(MultipleElementsFormat, builder.ToString());
        }

        // TODO : optimize performances
        private static string SerializeMetadata(this IWoopsaProperty property)
        {
            return String.Format(MetadataProperty, JsonEscape(property.Name), property.Type, property.IsReadOnly.ToString().ToLower());
        }

        private static string SerializeMetadata(this IWoopsaMethod method)
        {
            string arguments = method.ArgumentInfos.SerializeMetadata();
            return String.Format(MetadataMethod, JsonEscape(method.Name), method.ReturnType, arguments);
        }

        private static string SerializeMetadata(this IEnumerable<IWoopsaMethodArgumentInfo> argumentInfos)
        {
            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var arg in argumentInfos)
            {
                if (!first)
                    builder.Append(MultipleElementsSeparator);
                else
                    first = false;
                builder.Append(arg.SerializeMetadata());
            }
            return String.Format(MultipleElementsFormat, builder.ToString());
        }

        private static string SerializeMetadata(this IWoopsaMethodArgumentInfo argumentInfo)
        {
            return String.Format(MetadataArgumentInfo, JsonEscape(argumentInfo.Name), argumentInfo.Type);
        }

        public static WoopsaMetaResult DeserializeMeta(string jsonText)
        {
            var serializer = new JsonSerializer();
            var result = serializer.Deserialize<WoopsaMetaResult>(jsonText);
            return result;
        }

        public static WoopsaValue DeserializeWoopsaValue(string jsonText)
        {
            var serializer = new JsonSerializer();
            var result = serializer.Deserialize<WoopsaReadResult>(jsonText);
            if (result != null)
            {
                var valueType = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type);
                WoopsaValue resultWoopsaValue;
                DateTime? timeStamp;
                if (result.TimeStamp != null)
                    timeStamp = DateTime.Parse(result.TimeStamp, CultureInfo.InvariantCulture);
                else
                    timeStamp = null;
                if (valueType == WoopsaValueType.JsonData)
                    resultWoopsaValue = new WoopsaValue(WoopsaJsonData.CreateFromDeserializedData(result.Value), timeStamp);
                else
                    resultWoopsaValue = WoopsaValue.CreateChecked(WoopsaFormat.ToStringWoopsa(result.Value),
                        valueType, timeStamp);
                return resultWoopsaValue;
            }
            else
                return WoopsaValue.Null;
        }

        private class WoopsaReadResult
        {
            public object Value { get; set; }
            public string Type { get; set; }
            public string TimeStamp { get; set; }
        }

        public const string KeyValue = "Value";
        public const string KeyType = "Type";
        public const string KeyTimeStamp = "TimeStamp";
        public const string KeyName = "Name";
        public const string KeyReadOnly = "ReadOnly";
        public const string KeyArgumentInfos = "ArgumentInfos";
        public const string KeyReturnType = "ReturnType";
        public const string KeyProperties = "Properties";
        public const string KeyMethods = "Methods";
        public const string KeyItems = "Items";
        public const string KeyError = "Error";
        public const string KeyMessage = "Message";
        public const string KeySubscriptionId = "SubscriptionId";
        public const string KeyResult = "Result";
        public const string KeyId = "Id";
        public const string KeyVerb = "Verb";
        public const string KeyPath = "Path";
        public const string KeyArguments = "Arguments";

        public const char ElementOpen = '{';
        public const char ElementSeparator = ',';
        public const char ElementClose = '}';
        public const char KeyValueSeparator = ':';

        public const char MultipleElementsOpen = '[';
        public const char MultipleElementsSeparator = ',';
        public const char MultipleElementsClose = ']';

        public const char ValueEscapeCharacter = '"';

        const string NotificationFormat = "{{\"" + KeyValue + "\":{0},\"" + KeySubscriptionId + "\":{1}, \"" + KeyId + "\": {2}}}";

        const string ValueFormatNoDate = "{{\"" + KeyValue + "\":{0},\"" + KeyType + "\":\"{1}\"}}";
        const string ValueFormatWithDate = "{{\"" + KeyValue + "\":{0},\"" + KeyType + "\":\"{1}\",\"" + KeyTimeStamp + "\":\"{2}\"}}";

        const string MetadataContainer = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyItems + "\":{1}}}";
        const string MetadataObject = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyItems + "\":{1},\"" + KeyProperties + "\":{2},\"" + KeyMethods + "\":{3}}}";
        const string MetadataProperty = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyType + "\":\"{1}\",\"" + KeyReadOnly + "\":{2}}}";
        const string MetadataMethod = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyReturnType + "\":\"{1}\",\"" + KeyArgumentInfos + "\":{2}}}";
        const string MetadataArgumentInfo = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyType + "\":\"{1}\"}}";

        const string ErrorFormat = "{{\"" + KeyError + "\":true, \"" + KeyMessage + "\":\"{0}\", \"" + KeyType + "\":\"{1}\"}}";

        const string MultipleRequestResponseFormat = "{{\"" + KeyId + "\":{0}, \"" + KeyResult + "\":{1}}}";

        const string MultipleElementsFormat = "[{0}]";

        const string ObjectFormat = "{{{0}}}";
        const string StringFormat = "\"{0}\"";
    }

    public class WoopsaMetaResult
    {
        public string Name { get; set; }
        public string[] Items { get; set; }
        public WoopsaPropertyMeta[] Properties { get; set; }
        public WoopsaMethodMeta[] Methods { get; set; }
    }

    public class WoopsaPropertyMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsReadOnly { get; set; }
    }

    public class WoopsaMethodMeta
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public WoopsaMethodArgumentInfoMeta[] ArgumentInfos { get; set; }
    }

    public class WoopsaMethodArgumentInfoMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

}

