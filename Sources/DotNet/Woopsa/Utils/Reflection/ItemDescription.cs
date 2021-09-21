using System.Reflection;

namespace Woopsa
{
    public class ItemDescription : Description
    {
        public ItemDescription(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public override string Name => PropertyInfo.Name;
        public PropertyInfo PropertyInfo { get; }
    }

}
