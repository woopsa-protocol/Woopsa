using System;

namespace Woopsa
{
    public class WoopsaConverterDefault : WoopsaConverter
    {
        public static WoopsaConverterDefault Default { get; } = new WoopsaConverterDefault();

        public override object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                // create a default value of the type
                return Activator.CreateInstance(type);
            else
                return null;
        }
        public override object FromWoopsaValue(IWoopsaValue value, Type targetType) =>
            value.ToBaseType(targetType);

        public override WoopsaValue ToWoopsaValue(object value, WoopsaValueType woopsaValueType,
            DateTime? timeStamp) => WoopsaValue.ToWoopsaValue(value, woopsaValueType, timeStamp);
    }

}
