using System;

namespace Woopsa
{
    public class ArgumentDescription : Description
    {
        public ArgumentDescription(WoopsaMethodArgumentInfo argumentInfo,
            Type type, WoopsaConverter converter)
        {
            ArgumentInfo = argumentInfo;
            Type = type;
            Converter = converter;
        }

        public override string Name => ArgumentInfo.Name;
        public WoopsaMethodArgumentInfo ArgumentInfo { get; private set; }
        public Type Type { get; private set; }
        public WoopsaConverter Converter { get; private set; }
    }

}
