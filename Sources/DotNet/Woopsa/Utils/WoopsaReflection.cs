using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

                bool isValidWoopsaProperty = false;
                isValidWoopsaProperty = InferWoopsaType(customValueTypeConverters, propertyInfo.PropertyType,
                    out WoopsaValueType woopsaPropertyType,
                    out WoopsaConverter converter);
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
                WoopsaValueTypeAttribute attribute = GetCustomAttribute<WoopsaValueTypeAttribute>(methodInfo);
                bool isValidWoopsaMethod = InferWoopsaType(customValueTypeConverters, methodInfo.ReturnType,
                                                out WoopsaValueType woopsaReturnType, out WoopsaConverter converter);
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
                        if (InferWoopsaType(customValueTypeConverters, parameter.ParameterType,
                            out WoopsaValueType argumentType, out WoopsaConverter argumentConverter))
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

        public static bool InferWoopsaType(WoopsaConverters customValueTypeConverters, Type type, out WoopsaValueType woopsaValueType, out WoopsaConverter converter)
        {
            if (customValueTypeConverters != null)
                return customValueTypeConverters.InferWoopsaType(type, out woopsaValueType, out converter);
            else
            {
                converter = WoopsaConverterDefault.Default;
                return WoopsaTypeUtils.InferWoopsaType(type, out woopsaValueType);
            }
        }

        public static bool IsWoopsaValueType(WoopsaConverters customValueTypeConverters, Type type) => 
            InferWoopsaType(customValueTypeConverters, type,
                out WoopsaValueType woopsaValueType, out WoopsaConverter converter);

        public static T GetCustomAttribute<T>(MemberInfo element) where T : Attribute =>
            element.GetCustomAttributes(true).OfType<T>().FirstOrDefault();
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

        public T this[int index] => _items[index];

        public bool Contains(string name) => _itemsByName.ContainsKey(name);

        public T this[string name] => _itemsByName[name];

        public bool TryGetValue(string name, out T value) => _itemsByName.TryGetValue(name, out value);

        public int Count => _items.Count;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

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

        public override string Name => PropertyInfo.Name;
        public PropertyInfo PropertyInfo { get; }
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

        public override string Name => PropertyInfo.Name;
        public WoopsaValueType WoopsaType { get; }
        public PropertyInfo PropertyInfo { get; }
        public bool IsReadOnly { get; }
        public WoopsaConverter Converter { get; }
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

        public override string Name => ArgumentInfo.Name;
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

        public Type Type { get; }
        public ItemDescriptions Items { get; }
        public PropertyDescriptions Properties { get; }
        public MethodDescriptions Methods { get; }
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

    public abstract class WoopsaConverter
    {
        public abstract object GetDefaultValue(Type type);

        public abstract object FromWoopsaValue(IWoopsaValue value, Type targetType);

        public abstract WoopsaValue ToWoopsaValue(object value, WoopsaValueType woopsaValueType,
            DateTime? timeStamp);
    }

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
