using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaTypeUtils
    {
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

        /// <summary>
        /// Determines if the .net type matches a WoopsaValueType
        /// Some .net reference types (like string) are WoopsaValueType
        /// </summary>
        /// <param name="type">The .NET type to test</param>
        /// <returns>true if the type matches a woopsa value type</returns>
        public static bool IsWoopsaValueType(Type type)
        {
            WoopsaValueType woopsaType;
            return InferWoopsaType(type, out woopsaType);
        }

        /// <summary>
        /// Determines if the .net type matches a Woopsa reference type (object)
        /// Some value types of .net have no corresponding Woopsa type, and are not reference type.
        /// </summary>
        /// <param name="type">The .NET type to test</param>
        /// <returns>true if the type matches a woopsa reference type</returns>
        public static bool IsWoopsaReferenceType(Type type)
        {
            return !IsWoopsaValueType(type) && !type.IsValueType;
        }

    }
}
