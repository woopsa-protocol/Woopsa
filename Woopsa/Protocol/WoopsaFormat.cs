using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
	public static class WoopsaFormat
	{
        public static string ToWoopsaDateTime(this DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("o");
        }

        public static string ToWoopsaTimeSpan(this TimeSpan timeSpan)
        {
            return timeSpan.TotalSeconds.ToStringWoopsa();
        }

        public static string WoopsaError(string error)
        {
            return String.Format(ErrorFormat, error);
        }

        /// <summary>
        /// Escapes a string so it safe for being put into quotes "" into Json Data
        /// </summary>
        /// <param name="value">The string to escape</param>
        /// <returns>A string with the following special characters handled, in this order: \ " \n \b \f \r \t</returns>
        public static string JsonEscape(this string value)
        {
            var s = new StringBuilder(value);
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n","\\n")
                .Replace("\b","\\b")
                .Replace("\f","\\f")
                .Replace("\r","\\r")
                .Replace("\t","\\t")
                .ToString();
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
            return String.Format(NotificationFormat, notification.Value.Serialise(), notification.PropertyLink.Serialise());
        }

        public static string Serialise(this IWoopsaValue value)
        {
            StringBuilder valueAsText = new StringBuilder();
            if ( value.Type != WoopsaValueType.JsonData 
                && value.Type != WoopsaValueType.Real
                && value.Type != WoopsaValueType.Integer
                && value.Type != WoopsaValueType.Logical
                && value.Type != WoopsaValueType.TimeSpan )
                valueAsText.Append(ValueEscapeCharacter).Append(value.AsText.JsonEscape()).Append(ValueEscapeCharacter);
            else
                valueAsText.Append(value.AsText);

            if (value.TimeStamp.HasValue)
            {
                return String.Format(ValueFormatWithDate, valueAsText.ToString(), value.Type.ToString(), value.TimeStamp.Value.ToWoopsaDateTime());
            }
            else
            {
                return String.Format(ValueFormatNoDate, valueAsText.ToString(), value.Type.ToString());
            }
        }

        public static string SerializeMetadata(this IWoopsaContainer container, bool justName = false)
        {
            if (justName)
            {
                return String.Format(StringFormat, container.Name.JsonEscape());
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
            foreach(var elem in elements)
            {
                if (!first)
                    builder.Append(MultipleElementsSeparator);
                else
                    first = false;
                if (elem is IWoopsaMethod)
                {
                    builder.Append((elem as IWoopsaMethod).SerializeMetadata());
                }
                else if ( elem is IWoopsaProperty)
                {
                    builder.Append((elem as IWoopsaProperty).SerializeMetadata());
                }
                else if(elem is IWoopsaContainer)
                {
                    builder.Append((elem as IWoopsaContainer).SerializeMetadata(true));
                }
            }
            return String.Format(MultipleElementsFormat, builder.ToString());
        }

        private static string SerializeMetadata(this IWoopsaProperty property)
        {
            return String.Format(MetadataProperty, property.Name.JsonEscape(), property.Type, property.IsReadOnly.ToString().ToLower());
        }

        private static string SerializeMetadata(this IWoopsaMethod method)
        {
            string arguments = method.ArgumentInfos.SerializeMetadata();
            return String.Format(MetadataMethod, method.Name.JsonEscape(), method.ReturnType, arguments);
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
            return String.Format(MetadataArgumentInfo, argumentInfo.Name.JsonEscape(), argumentInfo.Type);
        }
        
		public const string KeyValue            = "Value";
		public const string KeyType             = "Type";
		public const string KeyTimeStamp        = "TimeStamp";
        public const string KeyName             = "Name";
        public const string KeyReadOnly         = "ReadOnly";
        public const string KeyArgumentInfos    = "ArgumentInfos";
        public const string KeyReturnType       = "ReturnType";
        public const string KeyProperties       = "Properties";
        public const string KeyMethods          = "Methods";
        public const string KeyItems            = "Items";
        public const string KeyError            = "Error";
        public const string KeyMessage          = "Message";
        public const string KeyPropertyLink     = "PropertyLink";
        public const string KeyId               = "Id";
        public const string KeyResult           = "Result";

        const string NotificationFormat         = "{{\"" + KeyValue + "\":{0},\"" + KeyPropertyLink + "\":{1}}}";

        const string ValueFormatNoDate          = "{{\"" + KeyValue + "\":{0},\"" + KeyType + "\":\"{1}\"}}";
        const string ValueFormatWithDate        = "{{\"" + KeyValue + "\":{0},\"" + KeyType + "\":\"{1}\",\"" + KeyTimeStamp + "\":\"{2}\"}}";

        const string MetadataContainer          = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyItems + "\":{1}}}";
        const string MetadataObject             = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyItems + "\":{1},\"" + KeyProperties + "\":{2},\"" + KeyMethods + "\":{3}}}";
        const string MetadataProperty           = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyType + "\":\"{1}\",\"" + KeyReadOnly + "\":{2}}}";
        const string MetadataMethod             = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyReturnType + "\":\"{1}\",\"" + KeyArgumentInfos + "\":{2}}}";
        const string MetadataArgumentInfo       = "{{\"" + KeyName + "\":\"{0}\",\"" + KeyType + "\":\"{1}\"}}";

        const string ErrorFormat                = "{{\"" + KeyError + "\":true, \"" + KeyMessage + "\":\"{0}\"}}";

        const string MultipleRequestResponseFormat = "{{\"" + KeyId + "\":{0}, \"" + KeyResult + "\":{1}}}";

        const string MultipleElementsFormat     = "[{0}]";
        const char MultipleElementsSeparator    = ',';
        const string ObjectFormat               = "{{{0}}}";
        const string StringFormat               = "\"{0}\"";
        const char ValueEscapeCharacter         = '"';
	}
}
