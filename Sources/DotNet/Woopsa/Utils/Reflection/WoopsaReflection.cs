using System;
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
}
