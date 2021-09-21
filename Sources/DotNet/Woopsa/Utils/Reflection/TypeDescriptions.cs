using System;
using System.Collections.Generic;

namespace Woopsa
{
    public class TypeDescriptions
    {
        public TypeDescriptions(WoopsaConverters customValueTypeConverters)
        {
            _typeDescriptions = new Dictionary<Type, TypeDescription>();
            CustomTypeConverters = customValueTypeConverters;
        }

        public TypeDescription GetTypeDescription(Type type)
        {
            if (type != null)
            {
                if (!_typeDescriptions.TryGetValue(type, out TypeDescription result))
                {
                    result = WoopsaReflection.ReflectType(type, CustomTypeConverters);
                    _typeDescriptions[type] = result;
                }
                return result;
            }
            else
                return null;
        }

        public WoopsaConverters CustomTypeConverters { get; }
        private Dictionary<Type, TypeDescription> _typeDescriptions;
    }

}
