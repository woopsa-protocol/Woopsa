using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaTypeUtils
    {
        public static object ConvertTo(this WoopsaValue value, Type targetType)
        {
            if (targetType == typeof(bool))
                return (bool)value;
            else if (targetType == typeof(float))
                return (float)value;
            else if (targetType == typeof(double))
                return (double)value;
            else if (targetType == typeof(byte))
                return (byte)value;
            else if (targetType == typeof(sbyte))
                return (sbyte)value;
            else if (targetType == typeof(string))
                return (string)value;
            else if (targetType == typeof(Int16))
                return (Int16)value;
            else if (targetType == typeof(Int32))
                return (Int32)value;
            else if (targetType == typeof(Int64))
                return (Int64)value;
            else if (targetType == typeof(UInt16))
                return (UInt16)value;
            else if (targetType == typeof(UInt32))
                return (UInt32)value;
            else if (targetType == typeof(UInt64))
                return (UInt64)value;
            else if (targetType == typeof(TimeSpan))
                return (TimeSpan)value;
            else if (targetType == typeof(DateTime))
                return (DateTime)value;
            else if (targetType.IsEnum)
                return Enum.Parse(targetType, value);
            else
                throw new WoopsaException(String.Format("The type value \"{0}\" is not supported in parameters of dynamic function call.", targetType.Name));
        }

        /// <summary>
        /// Determines the WoopsaValueType based on a .NET type.
        /// </summary>
        /// <param name="targetType">The .NET type to try to get the WoopsaValueType from</param>
        /// <param name="resultType">The inferred WoopsaValueType. If the type cannot be inferred, this value will be WoopsaValueType.Null</param>
        /// <returns>true if the type could be inferred, false otherwise. A return value of false will also result in WoopsaValueType.Null</returns>
        public static bool InferWoopsaType(Type targetType, out WoopsaValueType resultType)
        {
            if (targetType == typeof(void))
            {
                resultType = WoopsaValueType.Null;
                return true;
            }
            switch (System.Type.GetTypeCode(targetType))
            {
                case TypeCode.Boolean:
                    resultType = WoopsaValueType.Logical;
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    resultType = WoopsaValueType.Integer;
                    return true;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    resultType = WoopsaValueType.Real;
                    return true;
                case TypeCode.DateTime:
                    resultType = WoopsaValueType.DateTime;
                    return true;
                case TypeCode.String:
                    resultType = WoopsaValueType.Text;
                    return true;
                default:
                    if (targetType == typeof(TimeSpan))
                    {
                        resultType = WoopsaValueType.TimeSpan;
                        return true;
                    }
                    else
                    {
                        resultType = WoopsaValueType.Null;
                        return false;
                    }
            }
        }
    }
}
