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
        public const WoopsaVisibility DefaultVisibility = WoopsaVisibility.DefaultIsVisible |
             WoopsaVisibility.Inherited;

        public static TypeDescription ReflectType(
            Type targetType,
            WoopsaVisibility visibility,
            Action<EventArgsMemberVisibilityCheck> visibilityCheck)
        {
            TypeDescription typeDescription = new TypeDescription(targetType);
            ReflectProperties(targetType, typeDescription.Properties,
                typeDescription.Items, visibility, visibilityCheck);
            ReflectMethods(targetType, typeDescription.Methods,
                visibility, visibilityCheck);
            return typeDescription;
        }

        public static TypeDescription ReflectType(
            Type targetType,
            WoopsaVisibility visibility = DefaultVisibility)
        {
            return ReflectType(targetType, visibility, (e) => { });
        }

        public static void ReflectProperties(
            Type targetType,
            PropertyDescriptions propertyDescriptions,
            ItemDescriptions itemDescriptions,
            WoopsaVisibility visibility,
            Action<EventArgsMemberVisibilityCheck> visibilityCheck)
        {
            PropertyInfo[] properties;
            if (!targetType.IsInterface)
                properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            else
                // TODO : improve performances
                properties = (new Type[] { targetType }).Concat(targetType.GetInterfaces()).SelectMany(i => i.GetProperties()).ToArray();
            foreach (var propertyInfo in properties)
                if (IsMemberWoopsaVisible(targetType, propertyInfo, visibility, visibilityCheck))
                {
                    WoopsaValueTypeAttribute attribute = GetCustomAttribute<WoopsaValueTypeAttribute>(propertyInfo);
                    WoopsaValueType woopsaPropertyType;
                    bool isValidWoopsaProperty = false;
                    if (attribute != null)
                    {
                        woopsaPropertyType = attribute.ValueType;
                        isValidWoopsaProperty = true;
                    }
                    else
                        isValidWoopsaProperty = WoopsaTypeUtils.InferWoopsaType(propertyInfo.PropertyType, out woopsaPropertyType);
                    if (isValidWoopsaProperty)
                    {
                        //This property is a C# property of a valid basic Woopsa Type, it can be published as a Woopsa property
                        PropertyDescription newPropertyDescription = new PropertyDescription(
                            woopsaPropertyType, propertyInfo,
                            !propertyInfo.CanWrite || propertyInfo.GetSetMethod(false) == null);
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

        public static void ReflectProperties(
            Type targetType,
            PropertyDescriptions propertyDescriptions,
            ItemDescriptions itemDescriptions,
            WoopsaVisibility visibility = DefaultVisibility)
        {
            ReflectProperties(targetType, propertyDescriptions, itemDescriptions,
                visibility, (e) => { });
        }

        public static void ReflectMethods(
            Type targetType,
            MethodDescriptions methodDescriptions,
            WoopsaVisibility visibility,
            Action<EventArgsMemberVisibilityCheck> visibilityCheck)
        {
            MethodInfo[] methods;

            methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
                if (IsMemberWoopsaVisible(targetType, methodInfo, visibility, visibilityCheck))
                {
                    WoopsaValueTypeAttribute attribute = GetCustomAttribute<WoopsaValueTypeAttribute>(methodInfo);
                    WoopsaValueType woopsaReturnType;
                    bool isValidWoopsaMethod = false;
                    if (attribute != null)
                    {
                        woopsaReturnType = attribute.ValueType;
                        isValidWoopsaMethod = true;
                    }
                    else
                        isValidWoopsaMethod = WoopsaTypeUtils.InferWoopsaType(methodInfo.ReturnType, out woopsaReturnType);
                    if (isValidWoopsaMethod)
                    {
                        bool argumentsTypeCompatible = true;
                        ArgumentDescriptions arguments = new ArgumentDescriptions();
                        int parameterIndex = 0;
                        foreach (var parameter in methodInfo.GetParameters())
                        {
                            WoopsaValueType argumentType;
                            if (WoopsaTypeUtils.InferWoopsaType(parameter.ParameterType, out argumentType))
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
                                    parameter.ParameterType);
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
                                woopsaReturnType, arguments, methodInfo);
                            methodDescriptions.Add(newMethod);
                        }
                    }
                }
        }

        public static void ReflectMethods(Type targetType,
            MethodDescriptions methodDescriptions,
            WoopsaVisibility visibility = DefaultVisibility)
        {
            ReflectMethods(targetType, methodDescriptions, visibility, (e) => { });
        }

        public static bool IsMemberWoopsaVisible(
            Type targetType,
            MemberInfo member,
            WoopsaVisibility visibility,
            Action<EventArgsMemberVisibilityCheck> visibilityCheck)
        {
            var woopsaVisibleAttribute = GetCustomAttribute<WoopsaVisibleAttribute>(member);
            bool isVisible;
            if (woopsaVisibleAttribute != null)
                isVisible = woopsaVisibleAttribute.Visible;
            else
                isVisible = visibility.HasFlag(WoopsaVisibility.DefaultIsVisible);
            if (isVisible)
            {
                if (member.DeclaringType != targetType)
                    isVisible = visibility.HasFlag(WoopsaVisibility.Inherited);
            }
            if (isVisible)
            {
                if (member.DeclaringType == typeof(object))
                    isVisible = visibility.HasFlag(WoopsaVisibility.ObjectClassMembers);
            }
            if (isVisible)
            {
                if (member is MethodBase)
                    if ((member as MethodBase).IsSpecialName)
                        isVisible = visibility.HasFlag(WoopsaVisibility.MethodSpecialName);
            }
            if (isVisible)
            {
                if (member is PropertyInfo)
                {
                    PropertyInfo property = (PropertyInfo)member;
                    if (typeof(IEnumerable<object>).IsAssignableFrom(property.PropertyType))
                        isVisible = visibility.HasFlag(WoopsaVisibility.IEnumerableObject);
                }
            }
            EventArgsMemberVisibilityCheck e = new EventArgsMemberVisibilityCheck(member);
            e.IsVisible = isVisible;
            visibilityCheck(e);
            isVisible = e.IsVisible;
            return isVisible;
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
            bool isReadOnly)
        {
            WoopsaType = type;
            PropertyInfo = propertyInfo;
            IsReadOnly = isReadOnly;
        }

        public override string Name { get { return PropertyInfo.Name; } }
        public WoopsaValueType WoopsaType { get; private set; }
        public PropertyInfo PropertyInfo { get; private set; }
        public bool IsReadOnly { get; private set; }
    }

    public class ArgumentDescription : Description
    {
        public ArgumentDescription(WoopsaMethodArgumentInfo argumentInfo,
            Type type)
        {
            ArgumentInfo = argumentInfo;
            Type = type;
        }

        public override string Name { get { return ArgumentInfo.Name; } }
        public WoopsaMethodArgumentInfo ArgumentInfo { get; private set; }
        public Type Type { get; private set; }
    }

    public class ArgumentDescriptions : Descriptions<ArgumentDescription> { }

    public class MethodDescription : Description
    {
        public MethodDescription(WoopsaValueType returnType, ArgumentDescriptions arguments,
            MethodInfo methodInfo)
        {
            WoopsaReturnType = returnType;
            Arguments = arguments;
            MethodInfo = methodInfo;
        }

        public override string Name { get { return MethodInfo.Name; } }
        public ArgumentDescriptions Arguments { get; private set; }
        public WoopsaValueType WoopsaReturnType { get; private set; }
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
        public TypeDescriptions(WoopsaVisibility visibility,
            Action<EventArgsMemberVisibilityCheck> visibilityCheck)
        {
            _visibility = visibility;
            _visibilityCheck = visibilityCheck;
            _typeDescriptions = new Dictionary<Type, TypeDescription>();
        }

        public TypeDescriptions(WoopsaVisibility visibility = WoopsaReflection.DefaultVisibility):
            this(visibility, (e) => { })
        {
        }
        public TypeDescription GetTypeDescription(Type type)
        {
            TypeDescription result;
            if (!_typeDescriptions.TryGetValue(type, out result))
            {
                result = WoopsaReflection.ReflectType(type,
                    _visibility, _visibilityCheck);
                _typeDescriptions[type] = result;
            }
            return result;
        }

        private WoopsaVisibility _visibility;
        private Action<EventArgsMemberVisibilityCheck> _visibilityCheck;
        private Dictionary<Type, TypeDescription> _typeDescriptions;
    }

}
