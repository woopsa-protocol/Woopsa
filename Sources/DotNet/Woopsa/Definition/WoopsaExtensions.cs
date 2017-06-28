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

        /// <summary>
        /// Compare the values ignoring the timestamp
        /// </summary>
        public static bool IsSameValue(this IWoopsaValue left, IWoopsaValue right)
        {
            if (left != null)
                if (right != null)
                    return left.Type == right.Type &&
                        left.AsText == right.AsText;
                else
                    return false;
            else
                return right == null;
        }

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

        public static object ToBaseType(this IWoopsaValue value, Type targetType)
        {
            switch (value.Type)
            {
                case WoopsaValueType.Null:
                    if (targetType == typeof(void) || !targetType.IsValueType)
                        return null;
                    else
                        break;
                case WoopsaValueType.Logical:
                    if (targetType == typeof(bool))
                        return value.ToBool();
                    else
                        break;
                case WoopsaValueType.Integer:
                    if (targetType == typeof(byte))
                        return value.ToByte();
                    else if (targetType == typeof(sbyte))
                        return value.ToSByte();
                    else if (targetType == typeof(Int16))
                        return value.ToInt16();
                    else if (targetType == typeof(UInt16))
                        return value.ToUInt16();
                    else if (targetType == typeof(Int32))
                        return value.ToInt32();
                    else if (targetType == typeof(UInt32))
                        return value.ToUInt32();
                    else if (targetType == typeof(Int64))
                        return value.ToInt64();
                    else if (targetType == typeof(UInt64))
                        return value.ToUInt64();
                    else if (targetType == typeof(float))
                        return value.ToFloat();
                    else if (targetType == typeof(double))
                        return value.ToDouble();
                    else
                        break;
                case WoopsaValueType.Real:
                    if (targetType == typeof(float))
                        return value.ToFloat();
                    else if (targetType == typeof(double))
                        return value.ToDouble();
                    else
                        break;
                case WoopsaValueType.DateTime:
                    if (targetType == typeof(DateTime))
                        return value.ToDateTime();
                    else
                        break;
                case WoopsaValueType.TimeSpan:
                    if (targetType == typeof(TimeSpan))
                        return value.ToTimeSpan();
                    else
                        break;
                case WoopsaValueType.Text:
                    if (targetType == typeof(string))
                        return value.AsText;
                    else if (targetType.IsEnum)
                        return Enum.Parse(targetType, value.AsText);
                    else
                        break;
                case WoopsaValueType.WoopsaLink:
                case WoopsaValueType.JsonData:
                case WoopsaValueType.ResourceUrl:
                    if (targetType == typeof(string))
                        return value.AsText;
                    else
                        break;
                default:
                    break;
            }
            throw new InvalidCastException(
                string.Format("Unable to cast WoopsaValue '{0}' to type '{1}'",
                value.Serialize(), targetType.FullName));
        }

        public static object ToBaseType(this IWoopsaValue value)
        {
            switch (value.Type)
            {
                case WoopsaValueType.Null:
                    return null;
                case WoopsaValueType.Logical:
                    return value.ToBool();
                case WoopsaValueType.Integer:
                    return value.ToInt64();
                case WoopsaValueType.Real:
                    return value.ToDouble();
                case WoopsaValueType.DateTime:
                    return value.ToDateTime();
                case WoopsaValueType.TimeSpan:
                    return value.ToTimeSpan();
                case WoopsaValueType.Text:
                case WoopsaValueType.WoopsaLink:
                case WoopsaValueType.JsonData:
                case WoopsaValueType.ResourceUrl:
                    return value.AsText;
                default:
                    return null;
            }
        }
        public static void DecodeWoopsaLink(this IWoopsaValue value, out string woopsaServerUrl, out string woopsaItemPath)
        {
            if (value.Type == WoopsaValueType.WoopsaLink)
            {
                string[] parts = value.AsText.Split(WoopsaConst.WoopsaLinkSeparator);
                if (parts.Length == 1)
                {
                    woopsaServerUrl = null;
                    woopsaItemPath = WoopsaUtils.RemoveInitialSeparator(parts[0]);
                }
                else if (parts.Length == 2)
                {
                    woopsaServerUrl = parts[0];
                    woopsaItemPath = WoopsaUtils.RemoveInitialSeparator(parts[1]);
                }
                else
                    throw new WoopsaException(string.Format("Badly formed WoopsaLink {0} ", value.AsText));
            }
            else
                throw new WoopsaException(string.Format("Cannot decode WoopsaValue of type {0} as a WoopsaLink", value.Type));
        }

        public static string DecodeWoopsaLocalLink(this IWoopsaValue value)
        {
            string woopsaServerUrl;
            string woopsaItemPath;
            DecodeWoopsaLink(value, out woopsaServerUrl, out woopsaItemPath);
            if (woopsaServerUrl == null) // it is a local path
                return woopsaItemPath;
            else
                throw new WoopsaException(String.Format("{0} is not a local woopsa link", value.AsText));
        }

        #endregion IWoopsaValue

        #region IWoopsaElement

        public static IWoopsaContainer GetRoot(this IWoopsaElement element)
        {
            if (element == null)
                return null;
            else if (element is IWoopsaContainer)
                return GetContainerRoot(element as IWoopsaContainer);
            else
                return GetContainerRoot(element.Owner);
        }

        private static IWoopsaContainer GetContainerRoot(IWoopsaContainer element)
        {
            IWoopsaContainer result = element;
            if (result != null)
                while (result.Owner != null)
                    result = result.Owner;
            return result;
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

        public static IWoopsaElement ByPathOrNull(this IWoopsaContainer element, string path)
        {
            string workPath = WoopsaUtils.RemoveInitialSeparator(path);
            if (workPath == string.Empty)
                return element;
            else
            {
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
            // For performance optimization, use directly the methods of classes WoopsaObject and WoopsaContainer 
            // Complexity : O(1)
            if (woopsaContainer is WoopsaObject)
            {
                WoopsaObject woopsaObject = (WoopsaObject)woopsaContainer;
                result = woopsaObject.ByNameOrNull(name);
            }
            else if (woopsaContainer is WoopsaContainer)
            {
                WoopsaContainer container = (WoopsaContainer)woopsaContainer;
                result = container.ByNameOrNull(name);
            }
            else
            {
                // The code below can manage all the cases, but is used only for elements not 
                // of type WoopsaContainer or WoopsaObject 
                // Complexity : O(n)
                result = woopsaContainer.Items.ByNameOrNull(name);
                if (result == null && woopsaContainer is IWoopsaObject)
                {
                    IWoopsaObject woopsaObject = (IWoopsaObject)woopsaContainer;
                    result = woopsaObject.Properties.ByNameOrNull(name);
                    if (result == null)
                        woopsaObject.Methods.ByNameOrNull(name);
                }
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

        #region IArgumentInfos 

        static public bool IsSame(this IEnumerable<IWoopsaMethodArgumentInfo> left,
            IEnumerable<IWoopsaMethodArgumentInfo> right)
        {
            var rightCount = 0;
            Dictionary<string, IWoopsaMethodArgumentInfo> dictionaryLeft = new Dictionary<string, IWoopsaMethodArgumentInfo>();
            foreach (var item in left)
                dictionaryLeft.Add(item.Name, item);
            foreach (var item in right)
            {
                rightCount++;
                if (!dictionaryLeft.ContainsKey(item.Name))
                    return false;
                else
                {
                    var leftItem = dictionaryLeft[item.Name];
                    if (leftItem.Type != item.Type)
                        return false;
                }
            }
            return (dictionaryLeft.Count == rightCount);
        }

        #endregion
    }
}
