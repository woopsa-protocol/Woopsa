using System.Collections.Generic;
using System.Reflection;

namespace Woopsa
{
    public class MethodDescription : Description
    {
        public MethodDescription(WoopsaValueType returnType, ArgumentDescriptions arguments,
            MethodInfo methodInfo, WoopsaConverter converter)
        {
            WoopsaReturnType = returnType;
            Arguments = arguments;
            MethodInfo = methodInfo;
            Converter = converter;
        }

        public override string Name => MethodInfo.Name;
        public ArgumentDescriptions Arguments { get; }
        public WoopsaValueType WoopsaReturnType { get; }
        public WoopsaConverter Converter { get; }
        public MethodInfo MethodInfo { get; }
        public IEnumerable<WoopsaMethodArgumentInfo> WoopsaArguments
        {
            get
            {
                foreach (var item in Arguments)
                    yield return item.ArgumentInfo;
            }
        }
    }

}
