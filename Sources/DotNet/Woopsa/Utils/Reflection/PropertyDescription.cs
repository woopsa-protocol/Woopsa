using System.Reflection;

namespace Woopsa
{
    public class PropertyDescription : Description
    {
        public PropertyDescription(WoopsaValueType type, PropertyInfo propertyInfo,
            bool isReadOnly, WoopsaConverter converter)
        {
            WoopsaType = type;
            PropertyInfo = propertyInfo;
            IsReadOnly = isReadOnly;
            Converter = converter;
        }

        public override string Name => PropertyInfo.Name;
        public WoopsaValueType WoopsaType { get; }
        public PropertyInfo PropertyInfo { get; }
        public bool IsReadOnly { get; }
        public WoopsaConverter Converter { get; }
    }

}
