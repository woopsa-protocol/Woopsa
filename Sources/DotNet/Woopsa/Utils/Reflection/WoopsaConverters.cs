using System;
using System.Collections.Generic;

namespace Woopsa
{
    public class WoopsaConverters
    {
        public bool IsWoopsaValueType(Type type) => InferWoopsaType(type, out _, out _);

        public virtual bool InferWoopsaType(Type type, out WoopsaValueType woopsaValueType, out WoopsaConverter converter)
        {
            if (ConverterDescriptions.TryGetValue(type, out WoopsaConverterDescription converterDescription))
            {
                woopsaValueType = converterDescription.WoopsaValueType;
                converter = converterDescription.Converter;
                return true;
            }
            else
            {
                converter = WoopsaConverterDefault.Default;
                return WoopsaTypeUtils.InferWoopsaType(type, out woopsaValueType);
            }
        }

        public void RegisterConverter(Type type, WoopsaConverter converter, WoopsaValueType woopsaValueType)
        {
            ConverterDescriptions[type] = new WoopsaConverterDescription()
            {
                Converter = converter,
                WoopsaValueType = woopsaValueType
            };
        }

        private Dictionary<Type, WoopsaConverterDescription> ConverterDescriptions { get; } = new Dictionary<Type, WoopsaConverterDescription>();

        private class WoopsaConverterDescription
        {
            public WoopsaConverter Converter { get; set; }
            public WoopsaValueType WoopsaValueType { get; set; }
        }
    }

}
