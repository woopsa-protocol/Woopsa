using System;

namespace Woopsa
{
    public class TypeDescription
    {
        public TypeDescription(Type type)
        {
            Type = type;
            Items = new ItemDescriptions();
            Properties = new PropertyDescriptions();
            Methods = new MethodDescriptions();
        }

        public Type Type { get; }
        public ItemDescriptions Items { get; }
        public PropertyDescriptions Properties { get; }
        public MethodDescriptions Methods { get; }
    }

}
