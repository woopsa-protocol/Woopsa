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

        public static bool ToBool(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Logical)
                return WoopsaFormat.ToBool(value.AsText);
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("bool", value.Type.ToString()));
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
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("sbyte", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("sbyte", value.Type.ToString()));
        }

        public static Int16 ToInt16(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                Int16 result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("Int16", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int16", value.Type.ToString()));
        }

        public static Int32 ToInt32(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                Int32 result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("Int32", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int32", value.Type.ToString()));
        }

        public static Int64 ToInt64(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                Int64 result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("Int64", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int64", value.Type.ToString()));
        }

        public static byte ToByte(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                byte result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("byte", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("byte", value.Type.ToString()));
        }

        public static UInt16 ToUInt16(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                UInt16 result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("UInt16", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt16", value.Type.ToString()));
        }

        public static UInt32 ToUInt32(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                UInt32 result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("UInt32", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt32", value.Type.ToString()));
        }

        public static UInt64 ToUInt64(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Integer)
            {
                UInt64 result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("UInt64", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt64", value.Type.ToString()));
        }

        public static float ToFloat(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Real)
            {
                float result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("float", value.AsText));
            }
            else if (value.Type == WoopsaValueType.Integer)
                return value.ToInt64();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("float", value.Type.ToString()));
        }

        public static double ToDouble(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.Real)
            {
                double result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("double", value.AsText));
            }
            else if (value.Type == WoopsaValueType.Integer)
                return value.ToInt64();
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("double", value.Type.ToString()));
        }

        public static DateTime ToDateTime(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.DateTime)
            {
                DateTime result;
                if (DateTime.TryParse(value.AsText, null, DateTimeStyles.RoundtripKind, out result))
                    return result;
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("DateTime", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("DateTime", value.Type.ToString()));
        }

        public static TimeSpan ToTimeSpan(this IWoopsaValue value)
        {
            if (value.Type == WoopsaValueType.TimeSpan)
            {
                double result;
                if (WoopsaFormat.TryParseWoopsa(value.AsText, out result))
                    return TimeSpan.FromSeconds(result);
                else
                    throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastValueMessage("TimeSpan", value.AsText));
            }
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("TimeSpan", value.Type.ToString()));
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


        public static IWoopsaElement ByPathOrNull(this IWoopsaContainer element, string path)
        {
            string workPath = path.TrimStart(WoopsaConst.WoopsaPathSeparator);
            string[] pathElements = workPath.Split(WoopsaConst.WoopsaPathSeparator);

            IWoopsaElement result = element;
            foreach (var item in pathElements)
                if (result is IWoopsaContainer)
                    result = ((IWoopsaContainer)result).ByNameOrNull(item);
                else
                {
                    result = null;
                    break;
                }
            return result;
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
            IWoopsaElement result = ByPathOrNull(element, path);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa element not found at path : {0}", path));
        }


        public static T ByNameOrNull<T>(this IEnumerable<T> items, string name) where T : IWoopsaElement
        {
            foreach (var item in items)
                if (item.Name == name)
                    return item;
            return default(T);
        }

        public static T ByName<T>(this IEnumerable<T> items, string name) where T : IWoopsaElement
        {
            T result = ByNameOrNull(items, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa element not found : {0}", name));
        }

        public static IWoopsaElement ByNameOrNull(this IWoopsaContainer woopsaContainer, string name)
        {
            IWoopsaElement result;
            result = (IWoopsaElement)woopsaContainer.Items.ByNameOrNull(name);
            if (result == null && woopsaContainer is IWoopsaObject)
            {
                IWoopsaObject woopsaObject = (IWoopsaObject)woopsaContainer;
                result = (IWoopsaElement)woopsaObject.Properties.ByNameOrNull(name) ??
                         (IWoopsaElement)woopsaObject.Methods.ByNameOrNull(name);
            }
            return result;
        }

        public static IWoopsaElement ByName(this IWoopsaContainer woopsaContainer, string name)
        {
            IWoopsaElement result = ByNameOrNull(woopsaContainer, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa element not found : {0}", name));
        }

        #endregion
    }
}
