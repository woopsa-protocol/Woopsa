using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Woopsa
{
    public static class WoopsaReflection
    {

        public static TypeDescription ReflectType(
            Type targetType,
            WoopsaConverters customValueTypeConverters = null)
        {
            TypeDescription typeDescription = new TypeDescription(targetType);
            ReflectProperties(targetType, typeDescription.Properties,
                typeDescription.Items, customValueTypeConverters);
            ReflectMethods(targetType, typeDescription.Methods, customValueTypeConverters);
            return typeDescription;
        }

        public static void ReflectProperties(
            Type targetType,
            PropertyDescriptions propertyDescriptions,
            ItemDescriptions itemDescriptions,
            WoopsaConverters customValueTypeConverters = null)
        {
            PropertyInfo[] properties;
            if (!targetType.IsInterface)
                properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            else
                properties = (new Type[] { targetType }).Concat(targetType.GetInterfaces()).SelectMany(i => i.GetProperties()).ToArray();
            foreach (var propertyInfo in properties)
            {
                WoopsaValueTypeAttribute attribute = GetCustomAttribute<WoopsaValueTypeAttribute>(propertyInfo);
                WoopsaValueType woopsaPropertyType;
                WoopsaConverter converter;
                bool isValidWoopsaProperty = false;
                isValidWoopsaProperty = InferWoopsaType(customValueTypeConverters, propertyInfo.PropertyType, out woopsaPropertyType, out converter);
                if (attribute != null)
                {
                    woopsaPropertyType = attribute.ValueType;
                    isValidWoopsaProperty = true;
                }
                if (isValidWoopsaProperty)
                {
                    //This property is a C# property of a valid basic Woopsa Type, it can be published as a Woopsa property
                    PropertyDescription newPropertyDescription = new PropertyDescription(
                        woopsaPropertyType, propertyInfo,
                        !propertyInfo.CanWrite || propertyInfo.GetSetMethod(false) == null,
                        converter);
                    propertyDescriptions.Add(newPropertyDescription);
                }
                else if (!propertyInfo.PropertyType.IsValueType)
                {
                    // This property is not of a WoopsaType, if it is a reference type, assume it is an inner item
                    ItemDescription newItem = new ItemDescription(propertyInfo);
                    itemDescriptions.Add(newItem);
                }
            }
        }

        public static void ReflectMethods(
            Type targetType,
            MethodDescriptions methodDescriptions,
            WoopsaConverters customValueTypeConverters = null)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            List<MethodInfo> methods = new List<MethodInfo>(targetType.GetMethods(flags));
            foreach (Type inter in targetType.GetInterfaces())
            {
                foreach (MethodInfo method in inter.GetMethods(flags))
                    if (!methods.Contains(method))
                        methods.Add(method);
            }
            foreach (var methodInfo in methods)
            {
                WoopsaConverter converter;
                WoopsaValueTypeAttribute attribute = GetCustomAttribute<WoopsaValueTypeAttribute>(methodInfo);
                WoopsaValueType woopsaReturnType;
                bool isValidWoopsaMethod = false;
                isValidWoopsaMethod = InferWoopsaType(customValueTypeConverters, methodInfo.ReturnType, out woopsaReturnType, out converter);
                if (attribute != null)
                {
                    woopsaReturnType = attribute.ValueType;
                    isValidWoopsaMethod = true;
                }
                if (isValidWoopsaMethod)
                {
                    bool argumentsTypeCompatible = true;
                    ArgumentDescriptions arguments = new ArgumentDescriptions();
                    int parameterIndex = 0;
                    foreach (var parameter in methodInfo.GetParameters())
                    {
                        WoopsaConverter argumentConverter;
                        WoopsaValueType argumentType;
                        if (InferWoopsaType(customValueTypeConverters, parameter.ParameterType, out argumentType, out argumentConverter))
                        {
                            string parameterName;
                            parameterName = parameter.Name;
                            if (string.IsNullOrEmpty(parameterName))
                            {
                                if (typeof(Array).IsAssignableFrom(targetType))
                                    if (methodInfo.Name == "Set")
                                    {
                                        if (parameterIndex == 0)
                                            parameterName = "index";
                                        else if (parameterIndex == 1)
                                            parameterName = "value";
                                    }
                                    else if (methodInfo.Name == "Get")
                                        if (parameterIndex == 0)
                                            parameterName = "index";
                                if (parameterName == null)
                                    parameterName = "p" + parameterIndex.ToString();
                            }
                            ArgumentDescription newArgument = new ArgumentDescription(
                                new WoopsaMethodArgumentInfo(parameterName, argumentType),
                                parameter.ParameterType, argumentConverter);
                            arguments.Add(newArgument);
                        }
                        else
                        {
                            argumentsTypeCompatible = false;
                            break;
                        }
                        parameterIndex++;
                    }
                    if (argumentsTypeCompatible)
                    {
                        MethodDescription newMethod = new MethodDescription(
                            woopsaReturnType, arguments, methodInfo, converter);
                        methodDescriptions.Add(newMethod);
                    }
                }
            }
        }

        private static bool InferWoopsaType(WoopsaConverters customValueTypeConverters, Type type, out WoopsaValueType woopsaValueType, out WoopsaConverter converter)
        {
            if (customValueTypeConverters != null)
                return customValueTypeConverters.InferWoopsaType(type, out woopsaValueType, out converter);
            else
            {
                converter = WoopsaConverterDefault.Default;
                return WoopsaTypeUtils.InferWoopsaType(type, out woopsaValueType);
            }
        }

        public static T GetCustomAttribute<T>(MemberInfo element) where T : Attribute
        {
            return element.GetCustomAttributes(true).OfType<T>().FirstOrDefault();
        }
    }

    public abstract class Description
    {
        public abstract string Name { get; }
    }

    public class Descriptions<T> : IEnumerable<T> where T : Description
    {
        public Descriptions()
        {
            _items = new List<T>();
            _itemsByName = new Dictionary<string, T>();
        }

        public T this[int index]
        {
            get { return _items[index]; }
        }

        public bool Contains(string name)
        {
            return _itemsByName.ContainsKey(name);
        }

        public T this[string name]
        {
            get { return _itemsByName[name]; }
        }

        public bool TryGetValue(string name, out T value)
        {
            return _itemsByName.TryGetValue(name, out value);
        }

        public int Count { get { return _items.Count; } }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        internal void Add(T item)
        {
            _items.Add(item);
            _itemsByName[item.Name] = item;
        }

        private List<T> _items;
        private Dictionary<string, T> _itemsByName;
    }

    public class ItemDescription : Description
    {
        public ItemDescription(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public override string Name { get { return PropertyInfo.Name; } }
        public PropertyInfo PropertyInfo { get; private set; }
    }

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

        public override string Name { get { return PropertyInfo.Name; } }
        public WoopsaValueType WoopsaType { get; private set; }
        public PropertyInfo PropertyInfo { get; private set; }
        public bool IsReadOnly { get; private set; }
        public WoopsaConverter Converter { get; private set; }
    }

    public class ArgumentDescription : Description
    {
        public ArgumentDescription(WoopsaMethodArgumentInfo argumentInfo,
            Type type, WoopsaConverter converter)
        {
            ArgumentInfo = argumentInfo;
            Type = type;
            Converter = converter;
        }

        public override string Name { get { return ArgumentInfo.Name; } }
        public WoopsaMethodArgumentInfo ArgumentInfo { get; private set; }
        public Type Type { get; private set; }
        public WoopsaConverter Converter { get; private set; }
    }

    public class ArgumentDescriptions : Descriptions<ArgumentDescription> { }

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

        public override string Name { get { return MethodInfo.Name; } }
        public ArgumentDescriptions Arguments { get; private set; }
        public WoopsaValueType WoopsaReturnType { get; private set; }
        public WoopsaConverter Converter { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public IEnumerable<WoopsaMethodArgumentInfo> WoopsaArguments
        {
            get
            {
                foreach (var item in Arguments)
                    yield return item.ArgumentInfo;
            }
        }
    }

    public class ItemDescriptions : Descriptions<ItemDescription> { }

    public class PropertyDescriptions : Descriptions<PropertyDescription> { }

    public class MethodDescriptions : Descriptions<MethodDescription> { }

    public class TypeDescription
    {
        public TypeDescription(Type type)
        {
            Type = type;
            Items = new ItemDescriptions();
            Properties = new PropertyDescriptions();
            Methods = new MethodDescriptions();
        }

        public Type Type { get; private set; }
        public ItemDescriptions Items { get; private set; }
        public PropertyDescriptions Properties { get; private set; }
        public MethodDescriptions Methods { get; private set; }
    }

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
                TypeDescription result;
                if (!_typeDescriptions.TryGetValue(type, out result))
                {
                    result = WoopsaReflection.ReflectType(type, CustomTypeConverters);
                    _typeDescriptions[type] = result;
                }
                return result;
            }
            else
                return null;
        }

        public WoopsaConverters CustomTypeConverters { get; private set; }
        private Dictionary<Type, TypeDescription> _typeDescriptions;
    }

    public abstract class WoopsaConverter
    {
        public abstract object GetDefaultValue(Type type);

        public abstract object FromWoopsaValue(IWoopsaValue value, Type targetType);

        public abstract WoopsaValue ToWoopsaValue(object value, WoopsaValueType woopsaValueType,
            DateTime? timeStamp);
    }

    public class WoopsaConverterDefault : WoopsaConverter
    {
        static WoopsaConverterDefault()
        {
            Default = new Woopsa.WoopsaConverterDefault();
        }

        public static WoopsaConverterDefault Default { get; private set; }

        public override object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                // create a default value of the type
                return Activator.CreateInstance(type);
            else
                return null;
        }
        public override object FromWoopsaValue(IWoopsaValue value, Type targetType)
        {
            return value.ToBaseType(targetType);
        }

        public override WoopsaValue ToWoopsaValue(object value, WoopsaValueType woopsaValueType,
            DateTime? timeStamp)
        {
            return WoopsaValue.ToWoopsaValue(value, woopsaValueType, timeStamp);
        }
    }

    public class WoopsaConverters
    {
        public WoopsaConverters()
        {
            _converterDescriptions = new Dictionary<Type, WoopsaConverterDescription>();
        }

        public bool IsWoopsaValueType(Type type)
        {
            WoopsaValueType woopsaValueType;
            WoopsaConverter converter;
            return InferWoopsaType(type, out woopsaValueType, out converter);
        }

        public virtual bool InferWoopsaType(Type type, out WoopsaValueType woopsaValueType, out WoopsaConverter converter)
        {
            WoopsaConverterDescription converterDescription;
            if (_converterDescriptions.TryGetValue(type, out converterDescription))
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
            _converterDescriptions[type] = new WoopsaConverterDescription()
            {
                Converter = converter,
                WoopsaValueType = woopsaValueType
            };
        }

        private Dictionary<Type, WoopsaConverterDescription> _converterDescriptions;

        private class WoopsaConverterDescription
        {
            public WoopsaConverter Converter { get; set; }
            public WoopsaValueType WoopsaValueType { get; set; }
        }
    }

}
