using System;

namespace Woopsa
{
    public abstract class WoopsaConverter
    {
        public abstract object GetDefaultValue(Type type);

        public abstract object FromWoopsaValue(IWoopsaValue value, Type targetType);

        public abstract WoopsaValue ToWoopsaValue(object value, WoopsaValueType woopsaValueType,
            DateTime? timeStamp);
    }

}
