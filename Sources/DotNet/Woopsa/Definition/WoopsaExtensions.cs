using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Woopsa
{
    public static class WoopsaExtensions
    {
        #region IWoopsaValue

        public static string WoopsaCastTypeExceptionMessage(string destinationType, string sourceType)
        {
            return string.Format("Cannot typecast woopsa value of type {0} to type {1}", sourceType, destinationType);
        }

        public static string WoopsaCastValueExceptionMessage(string destinationType, string sourceValue)
        {
            return string.Format("Cannot typecast woopsa value {0} to type {1}", sourceValue, destinationType);
        }

        public static string WoopsaElementNotFoundMessage(string path)
        {
            return string.Format("Cannot find WoopsaElement specified by path {0}", path);
        }

        public static bool ToBool(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Logical)
                if (value.AsText == WoopsaConst.WoopsaTrue)
                    return true;
                else if (value.AsText == WoopsaConst.WoopsaFalse)
                    return false;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("bool", value.AsText));
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("bool", value.Type.ToString()));
        }

        public static bool IsNull(this IWoopsaValue value)
        {
            return value.Type == WoopsaValueType.Null;
        }

        public static sbyte ToSByte(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                sbyte result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("sbyte", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("sbyte", value.Type.ToString()));
        }

        public static Int16 ToInt16(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                Int16 result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("Int16", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("Int16", value.Type.ToString()));
        }

        public static Int32 ToInt32(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                Int32 result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("Int32", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("Int32", value.Type.ToString()));
        }

        public static Int64 ToInt64(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                Int64 result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("Int64", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("Int64", value.Type.ToString()));
        }

        public static byte ToByte(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                byte result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("byte", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("byte", value.Type.ToString()));
        }

        public static UInt16 ToUInt16(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                UInt16 result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("UInt16", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("UInt16", value.Type.ToString()));
        }

        public static UInt32 ToUInt32(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                UInt32 result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("UInt32", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("UInt32", value.Type.ToString()));
        }

        public static UInt64 ToUInt64(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                UInt64 result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("UInt64", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("UInt64", value.Type.ToString()));
        }

        public static float ToFloat(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Real)
            {
                float result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("float", value.AsText));
            }
            else if (value.Type == WoopsaValueType.Integer)
                return value.ToInt64();
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("float", value.Type.ToString()));
        }

        public static double ToDouble(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Real)
            {
                double result;
                if (TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("double", value.AsText));
            }
            else if (value.Type == WoopsaValueType.Integer)
                return value.ToInt64();
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("double", value.Type.ToString()));
        }

        public static DateTime ToDateTime(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.DateTime)
            {
                DateTime result;
                if (DateTime.TryParse(value.AsText, null, DateTimeStyles.RoundtripKind, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("DateTime", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("DateTime", value.Type.ToString()));
        }

        public static TimeSpan ToTimeSpan(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.TimeSpan)
            {
                double result;
                if (TryParseWoopsa(value.AsText, out result))
                    return TimeSpan.FromSeconds(result);
                else
                    throw new WoopsaException(WoopsaCastValueExceptionMessage("TimeSpan", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaCastTypeExceptionMessage("TimeSpan", value.Type.ToString()));
        }

        public static void DecodeWoopsaLink(this IWoopsaValue value, out string woopsaServerUrl, out string woopsaItemPath)
        {
            if (value.Type == WoopsaValueType.WoopsaLink)
            {
                string[] parts = value.AsText.Split(WoopsaConst.WoopsaLinkSeparator);
                if (parts.Length == 1)
                {
                    woopsaServerUrl = null;
                    woopsaItemPath = parts[0];
                }
                else if (parts.Length == 2)
                {
                    woopsaServerUrl = parts[0];
                    woopsaItemPath = parts[1];
                }
                else
                    throw new WoopsaException(string.Format("Badly formed WoopsaLink {0} ", value.AsText));
            }
            else
                throw new WoopsaException(string.Format("Cannot decode WoopsaValue of type {0} as a WoopsaLink", value.Type));
        }
        #endregion IWoopsaValue

        #region Helpers
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

        public static string ToStringWoopsa(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringWoopsa(this Int64 value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringWoopsa(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringWoopsa(this DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("o");
        }

        public static string ToStringWoopsa(this TimeSpan timeSpan)
        {
            return timeSpan.TotalSeconds.ToStringWoopsa();
        }

        #endregion

        #region IWoopsaObject

        /// <summary>
		/// Gets the path of a WoopsaElement relative to a root
		/// </summary>
		/// <param name="element">The WoopsaElement to get the path for</param>
		/// <param name="root">The root WoopsaElement to consider as root. Must be in the element's Owner chain.</param>
		/// <returns></returns>
		public static string GetPath(this IWoopsaElement element, IWoopsaContainer root)
        {
            StringBuilder stringBuilder = new StringBuilder();
            BuildPath(stringBuilder, element, root);
            if (stringBuilder.Length == 0) //Special case when it's the root
                return WoopsaConst.WoopsaRootPath;
            else
                return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the path of a WoopsaElement, going all the way to the root
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string GetPath(this IWoopsaElement element)
        {
            StringBuilder stringBuilder = new StringBuilder();
            BuildPath(stringBuilder, element);
            if (stringBuilder.Length == 0) //Special case when it's the root
                return WoopsaConst.WoopsaRootPath;
            else
                return stringBuilder.ToString();
        }

        private static void BuildPath(StringBuilder stringBuilder, IWoopsaElement element)
        {
            if (element != null)
            {
                BuildPath(stringBuilder, element.Owner);
                if (element.Owner != null) // it is not the root
                {
                    stringBuilder.Append(WoopsaConst.WoopsaPathSeparator);
                    stringBuilder.Append(element.Name);
                }
            }
        }

        private static void BuildPath(StringBuilder stringBuilder, IWoopsaElement element, IWoopsaContainer root)
        {
            if (element != null)
            {
                if (element == root)
                {
                    stringBuilder.Append(WoopsaConst.WoopsaPathSeparator);
                }
                else
                {
                    if (element.Owner != root)
                    {
                        BuildPath(stringBuilder, element.Owner, root);
                    }
                    if (element.Owner != null) // it is not the root
                    {
                        stringBuilder.Append(WoopsaConst.WoopsaPathSeparator);
                        stringBuilder.Append(element.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the WoopsaElement specified by path, relative to a container
        /// </summary>
        /// <param name="element">The element in which to start the search</param>
        /// <param name="path">The path, specified relative to elemen</param>
        /// <returns>An IWoopsaElement if found, throws IWoopsaNotFoundException if not</returns>
        /// 
        public static IWoopsaElement ByPath(this IWoopsaContainer element, string path)
        {
            string workPath = path.TrimStart('/');
            string[] pathElements = workPath.Split(WoopsaConst.WoopsaPathSeparator);
            IWoopsaElement result = element.Items.ByNameOrNull(pathElements[0]);

            if (result != null)
                if (pathElements.Length > 1)
                    return (result as IWoopsaContainer).ByPath(String.Join(WoopsaConst.WoopsaPathSeparator.ToString(), pathElements.Skip(1).ToArray()));
                else
                    return result;

            if (element is IWoopsaObject)
            {
                IWoopsaObject asObject = element as IWoopsaObject;
                result = asObject.Methods.ByNameOrNull(pathElements[0]);
                if (result == null)
                {
                    result = asObject.Properties.ByNameOrNull(pathElements[0]);
                    if (result != null)
                        return result;
                }
                else
                    return result;
            }
            throw new WoopsaNotFoundException(string.Format("Woopsa element not found at path : {0}", path));
        }

        public static IWoopsaElement ByName(this IWoopsaObject container, string name)
        {
            IWoopsaElement result = ByNameOrNull(container, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa element not found : {0}", name));
        }

        public static IWoopsaContainer ByName(this IEnumerable<IWoopsaContainer> containers, string name)
        {
            IWoopsaContainer result = ByNameOrNull(containers, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa container not found : {0}", name));
        }

        public static IWoopsaProperty ByName(this IEnumerable<IWoopsaProperty> properties, string name)
        {
            IWoopsaProperty result = ByNameOrNull(properties, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa property not found : {0}", name));
        }

        public static IWoopsaMethod ByName(this IEnumerable<IWoopsaMethod> methods, string name)
        {
            IWoopsaMethod result = ByNameOrNull(methods, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa method not found : {0}", name));
        }

        internal static IWoopsaElement ByNameOrNull(this IWoopsaObject obj, string name)
        {
            foreach (var item in obj.Items)
                if (item.Name.Equals(name))
                    return item;

            foreach (var method in obj.Methods)
                if (method.Name.Equals(name))
                    return method;

            foreach (var property in obj.Properties)
                if (property.Name.Equals(name))
                    return property;

            return null;
        }

        public static IWoopsaContainer ByNameOrNull(this IEnumerable<IWoopsaContainer> containers, string name)
        {
            foreach (var item in containers)
                if (item.Name == name)
                    return item;
            return null;
        }

        public static IWoopsaProperty ByNameOrNull(this IEnumerable<IWoopsaProperty> properties, string name)
        {
            foreach (var item in properties)
                if (item.Name == name)
                    return item;
            return null;
        }

        public static IWoopsaMethod ByNameOrNull(this IEnumerable<IWoopsaMethod> methods, string name)
        {
            foreach (var item in methods)
                if (item.Name == name)
                    return item;
            return null;
        }
        #endregion
    }
}
