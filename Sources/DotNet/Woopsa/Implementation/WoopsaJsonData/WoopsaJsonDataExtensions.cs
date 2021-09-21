using System;

namespace Woopsa
{
    public static class WoopsaJsonDataExtensions
    {
        #region Public Static Methods

        public static bool ToBool(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return WoopsaFormat.ToBool(WoopsaFormat.ToStringWoopsa(data.InternalObject));
#else
                return data.InternalObject.GetBoolean();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("bool", data.InternalObject.GetType().ToString()));
        }

        public static sbyte ToSByte(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToSByte(data.InternalObject);
#else
                return data.InternalObject.GetSByte();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("SByte", data.InternalObject.GetType().ToString()));
        }

        public static short ToInt16(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToInt16(data.InternalObject);
#else
                return data.InternalObject.GetInt16();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int16", data.InternalObject.GetType().ToString()));
        }

        public static int ToInt32(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToInt32(data.InternalObject);
#else
                return data.InternalObject.GetInt32();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int32", data.InternalObject.GetType().ToString()));
        }

        public static long ToInt64(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToInt64(data.InternalObject);
#else
                return data.InternalObject.GetInt64();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Int64", data.InternalObject.GetType().ToString()));
        }

        public static byte ToByte(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToByte(data.InternalObject);
#else
                return data.InternalObject.GetByte();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("Byte", data.InternalObject.GetType().ToString()));
        }

        public static ushort ToUInt16(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToUInt16(data.InternalObject);
#else
                return data.InternalObject.GetUInt16();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt16", data.InternalObject.GetType().ToString()));
        }

        public static uint ToUInt32(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToUInt32(data.InternalObject);
#else
                return data.InternalObject.GetUInt32();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt32", data.InternalObject.GetType().ToString()));
        }

        public static ulong ToUInt64(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToUInt64(data.InternalObject);
#else
                return data.InternalObject.GetUInt64();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("UInt64", data.InternalObject.GetType().ToString()));
        }

        public static float ToFloat(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return (float) Convert.ToDouble(data.InternalObject);
#else
                return (float)data.InternalObject.GetDouble();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("float", data.InternalObject.GetType().ToString()));
        }

        public static double ToDouble(this WoopsaJsonData data)
        {
            if (data.IsSimple)
#if UNITY_WSA
                return Convert.ToDouble(data.InternalObject);
#else
                return data.InternalObject.GetDouble();
#endif
            else
                throw new WoopsaException(WoopsaExceptionMessage.WoopsaCastTypeMessage("double", data.InternalObject.GetType().ToString()));
        }

        public static string Serialize(this WoopsaJsonData data)
        {

#if UNITY_WSA
            return UnityJsonSerializer.Serialize(data.InternalObject);
#else
            return data.InternalObject.GetRawText();
#endif
        }

        #endregion
    }
}
