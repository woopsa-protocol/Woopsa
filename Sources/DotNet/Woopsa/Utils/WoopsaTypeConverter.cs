using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaTypeUtils
    {
        public static object ConvertTo(this IWoopsaValue value, Type targetType)
        {
            if (targetType == typeof(bool))
                return value.ToBool();
            else if (targetType == typeof(float))
                return value.ToFloat();
            else if (targetType == typeof(double))
                return value.ToDouble();
            else if (targetType == typeof(byte))
                return value.ToByte();
            else if (targetType == typeof(sbyte))
                return value.ToSByte();
            else if (targetType == typeof(string))
                return value.AsText;
            else if (targetType == typeof(Int16))
                return value.ToInt16();
            else if (targetType == typeof(Int32))
                return value.ToInt32();
            else if (targetType == typeof(Int64))
                return value.ToInt64();
            else if (targetType == typeof(UInt16))
                return value.ToUInt16();
            else if (targetType == typeof(UInt32))
                return value.ToUInt32();
            else if (targetType == typeof(UInt64))
                return value.ToUInt64();
            else if (targetType == typeof(TimeSpan))
                return value.ToTimeSpan();
            else if (targetType == typeof(DateTime))
                return value.ToDateTime();
            else if (targetType.IsEnum)
                return Enum.Parse(targetType, value.AsText);
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
            else
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
