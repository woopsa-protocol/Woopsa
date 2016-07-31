using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Woopsa
{
    /// <summary>
    /// Use this attribute to decorate the methods and properties of normal objects and qualify it hey must be published by woopsa
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class WoopsaVisibleAttribute : Attribute
    {
        public WoopsaVisibleAttribute(bool visible = true)
        {
            Visible = visible;
        }

        public bool Visible { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class WoopsaValueTypeAttribute : Attribute
    {
        public WoopsaValueTypeAttribute(WoopsaValueType valueType)
        {
            ValueType = valueType;
        }

        public WoopsaValueType ValueType { get; private set; }
    }

    [Flags]
    public enum WoopsaVisibility
    {
        /// <summary>
        /// Publish normal members decorated with WoopsaVisible attribute and declared within the class
        /// </summary>
        None = 0,
        /// <summary>
        /// For members not decorated with WoopsaVisibleAttribute, consider the default value of WoopsaVisible as true
        /// </summary>
        DefaultIsVisible = 1,
        /// <summary>
        /// Publish methods with special names (like property getters, setters).
        /// </summary>
        MethodSpecialName = 2,
        /// <summary>
        /// Publish inherited members.
        /// </summary>
        Inherited = 4,
        /// <summary>
        /// Publish IEnumerable<Object> compatible types as a collection of items.
        /// </summary>
        IEnumerableObject = 8,
        /// <summary>
        /// Publish members inherited from Object, like ToString. Requires flag Inherited to have an effect.
        /// </summary>
        ObjectClassMembers = 16
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class WoopsaVisibilityAttribute : Attribute
    {
        public WoopsaVisibilityAttribute(WoopsaVisibility visibility)
        {
            Visibility = visibility;
        }

        public WoopsaVisibility Visibility { get; private set; }
    }

    [Flags]
    public enum WoopsaObjectAdapterOptions
    {
        None = 0,
        DisableClassesCaching = 1,
        SendTimestamps = 2
    }

    public class EventArgsMemberVisibilityCheck : EventArgs
    {
        public EventArgsMemberVisibilityCheck(MemberInfo member)
        {
            Member = member;
        }

        public MemberInfo Member { get; private set; }

        public bool IsVisible { get; set; }
    }

    public class WoopsaObjectAdapter : WoopsaObject
    {
        public const string IEnumerableIndexerFormat = "{0}[{1}]";

        public const string IEnumerableItemBaseName = "Item";

        public WoopsaObjectAdapter(WoopsaContainer container, string name, object targetObject,
            WoopsaObjectAdapterOptions options = WoopsaObjectAdapterOptions.None,
            WoopsaVisibility defaultVisibility = WoopsaVisibility.DefaultIsVisible)
            : base(container, name)
        {
            TargetObject = targetObject;
            DefaultVisibility = defaultVisibility;
            Options = options;
            if (targetObject != null)
            {
                WoopsaVisibilityAttribute woopsaVisibilityAttribute =
                    GetCustomAttribute<WoopsaVisibilityAttribute>(targetObject.GetType());
                if (woopsaVisibilityAttribute != null)
                    Visibility = woopsaVisibilityAttribute.Visibility;
                else
                    Visibility = defaultVisibility;
            }
            else
                Visibility = defaultVisibility;
        }


        /// <summary>
        /// To customize the woopsa visibility of a member. 
        /// This event is triggered for every member, including the members of the inner items.
        /// It can be used to force the visibility of any member to true or false.
        /// </summary>
        public event EventHandler<EventArgsMemberVisibilityCheck> MemberWoopsaVisibilityCheck;

        public object TargetObject { get; private set; }
        public WoopsaObjectAdapterOptions Options { get; private set; }

        /// <summary>
        /// Visibility for the WoopsaObjectAdapter and its inner WoopsaObjectAdapters.
        /// Applies if the TargetObject is not decorated with the WoopsaVisilibityAttribute
        /// </summary>
        public WoopsaVisibility DefaultVisibility { get; private set; }

        /// <summary>
        /// Visibility for this WoopsaObjectAdapter
        /// </summary>
        public WoopsaVisibility Visibility { get; private set; }

        public override void Refresh()
        {
            base.Refresh();
            Clear();
        }

        #region private members

        private static Dictionary<Type, TypeDescription> _typesCache = new Dictionary<Type, TypeDescription>();

        #endregion

        #region Private/Protected Methods

        protected virtual void OnMemberWoopsaVisibilityCheck(MemberInfo member, ref bool isVisible)
        {
            if (MemberWoopsaVisibilityCheck != null)
            {
                EventArgsMemberVisibilityCheck e = new EventArgsMemberVisibilityCheck(member);
                e.IsVisible = isVisible;
                MemberWoopsaVisibilityCheck(this, e);
                isVisible = e.IsVisible;
            }
            else if (Owner is WoopsaObjectAdapter)
                ((WoopsaObjectAdapter)Owner).OnMemberWoopsaVisibilityCheck(member, ref isVisible);
        }

        protected bool IsMemberWoopsaVisible(MemberInfo member)
        {
            var woopsaVisibleAttribute = GetCustomAttribute<WoopsaVisibleAttribute>(member);
            bool isVisible;
            if (woopsaVisibleAttribute != null)
                isVisible = woopsaVisibleAttribute.Visible;
            else
                isVisible = Visibility.HasFlag(WoopsaVisibility.DefaultIsVisible);
            if (isVisible)
            {
                if (TargetObject != null)
                    if (member.DeclaringType != TargetObject.GetType())
                        isVisible = Visibility.HasFlag(WoopsaVisibility.Inherited);
            }
            if (isVisible)
            {
                if (member.DeclaringType == typeof(object))
                    isVisible = Visibility.HasFlag(WoopsaVisibility.ObjectClassMembers);
            }
            if (isVisible)
            {
                if (member is MethodBase)
                    if ((member as MethodBase).IsSpecialName)
                        isVisible = Visibility.HasFlag(WoopsaVisibility.MethodSpecialName);
            }
            if (isVisible)
            {
                if (member is PropertyInfo)
                {
                    PropertyInfo property = (PropertyInfo)member;
                    if (typeof(IEnumerable<object>).IsAssignableFrom(property.PropertyType))
                        isVisible = Visibility.HasFlag(WoopsaVisibility.IEnumerableObject);
                }
            }
            OnMemberWoopsaVisibilityCheck(member, ref isVisible);
            return isVisible;
        }

        protected override void PopulateObject()
        {
            TypeDescription cache;

            base.PopulateObject();
            if (!Options.HasFlag(WoopsaObjectAdapterOptions.DisableClassesCaching) &&
                    _typesCache.ContainsKey(TargetObject.GetType()))
                cache = _typesCache[TargetObject.GetType()];
            else
            {
                cache = new TypeDescription();
                ReflectProperties(cache.Properties, cache.Items);
                ReflectMethods(cache.Methods);
                if (!Options.HasFlag(WoopsaObjectAdapterOptions.DisableClassesCaching))
                    _typesCache.Add(TargetObject.GetType(), cache);
            }
            PopulateProperties(cache.Properties);
            PopulateMethods(cache.Methods);
            PopulateItems(cache.Items);
            if (TargetObject is IEnumerable && Visibility.HasFlag(WoopsaVisibility.IEnumerableObject))
            {
                IEnumerable enumerable = (IEnumerable)TargetObject;
                PopulateEnumerableItems(enumerable);
            }
        }

        protected virtual void PopulateProperties(IEnumerable<PropertyDescription> properties)
        {
            foreach (var property in properties)
                AddWoopsaProperty(property);
        }
        protected virtual void PopulateMethods(IEnumerable<MethodDescription> methods)
        {
            foreach (var method in methods)
                AddWoopsaMethod(method);
        }

        protected virtual void PopulateItems(IEnumerable<ItemDescription> items)
        {
            foreach (var item in items)
                AddWoopsaItem(item);
        }

        protected virtual void PopulateEnumerableItems(IEnumerable enumerable)
        {
            int index = 0;
            foreach (object item in enumerable)
            {
                string name = String.Format(IEnumerableIndexerFormat, IEnumerableItemBaseName, index);
                index++;
                new WoopsaObjectAdapter(this, name, item, Options, DefaultVisibility);
            }
        }

        private void ReflectProperties(IList<PropertyDescription> propertyDescriptions, IList<ItemDescription> itemDescriptions)
        {
            PropertyInfo[] properties;
            properties = TargetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in properties)
                if (IsMemberWoopsaVisible(propertyInfo))
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
                        PropertyDescription newPropertyDescription = new PropertyDescription();
                        newPropertyDescription.PropertyInfo = propertyInfo;
                        if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null)
                            newPropertyDescription.ReadOnly = false;
                        else
                            newPropertyDescription.ReadOnly = true;
                        newPropertyDescription.Type = woopsaPropertyType;
                        propertyDescriptions.Add(newPropertyDescription);
                    }
                    else if (!propertyInfo.PropertyType.IsValueType)
                    {
                        // This property is not of a WoopsaType, if it is a reference type, assume it is an inner item
                        ItemDescription newItem = new ItemDescription();
                        newItem.PropertyInfo = propertyInfo;
                        itemDescriptions.Add(newItem);
                    }
                }
        }

        private void ReflectMethods(IList<MethodDescription> methodDescriptions)
        {
            MethodInfo[] methods;

            methods = TargetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
                if (IsMemberWoopsaVisible(methodInfo))
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
                        List<ArgumentDescription> arguments = new List<ArgumentDescription>();
                        int parameterIndex = 0;
                        foreach (var parameter in methodInfo.GetParameters())
                        {
                            WoopsaValueType argumentType;
                            if (WoopsaTypeUtils.InferWoopsaType(parameter.ParameterType, out argumentType))
                            {
                                ArgumentDescription newArgument = new ArgumentDescription();
                                string parameterName;
                                newArgument.Type = parameter.ParameterType;
                                parameterName = parameter.Name;
                                if (string.IsNullOrEmpty(parameterName))
                                {
                                    if (TargetObject is Array)
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
                                newArgument.ArgumentInfo = new WoopsaMethodArgumentInfo(parameterName, argumentType);
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
                            MethodDescription newMethod = new MethodDescription();
                            newMethod.Arguments = arguments;
                            newMethod.ReturnType = woopsaReturnType;
                            newMethod.MethodInfo = methodInfo;
                            methodDescriptions.Add(newMethod);
                        }
                    }
                }
        }

        private DateTime? GetTimeStamp()
        {
            if (Options.HasFlag(WoopsaObjectAdapterOptions.SendTimestamps))
                return DateTime.Now;
            else
                return null;
        }

        protected void AddWoopsaProperty(PropertyDescription property)
        {
            if (property.ReadOnly)
                new WoopsaProperty(this, property.PropertyInfo.Name, property.Type,
                    (sender) => (WoopsaValue.ToWoopsaValue(property.PropertyInfo.GetValue(TargetObject, EmptyParameters), property.Type, GetTimeStamp()))
                );
            else
                new WoopsaProperty(this, property.PropertyInfo.Name, property.Type,
                    (sender) => (WoopsaValue.ToWoopsaValue(property.PropertyInfo.GetValue(TargetObject, EmptyParameters), property.Type, GetTimeStamp())),
                    (sender, value) => property.PropertyInfo.SetValue(TargetObject, value.ConvertTo(property.PropertyInfo.PropertyType), null)
                );
        }

        protected void AddWoopsaItem(ItemDescription item)
        {
            try
            {
                object value = item.PropertyInfo.GetValue(TargetObject, EmptyParameters);
                // If an inner item is null, ignore it for the Woopsa hierarchy
                if (value != null)
                    CreateItemWoopsaAdapter(item.PropertyInfo.Name, value);
            }
            catch (Exception)
            {
                // Items from property getters that throw exceptions are not added to the object hierarchy
                // Ignore silently
            }
        }

        protected virtual WoopsaObjectAdapter CreateItemWoopsaAdapter(string itemName, object item)
        {
            return new WoopsaObjectAdapter(this, itemName, item, Options, DefaultVisibility);
        }

        protected void AddWoopsaMethod(MethodDescription method)
        {
            try
            {
                new WoopsaMethod(this, method.MethodInfo.Name, method.ReturnType, method.WoopsaArguments, (args) =>
                {
                    try
                    {
                        List<object> typedArguments = new List<object>();
                        for (var i = 0; i < method.Arguments.Count; i++)
                        {
                            typedArguments.Add(((WoopsaValue)args.ElementAt(i)).ConvertTo(method.Arguments.ElementAt(i).Type));
                        }
                        if (method.MethodInfo.ReturnType == typeof(void))
                        {
                            method.MethodInfo.Invoke(this.TargetObject, typedArguments.ToArray());
                            return null;
                        }
                        else
                            return WoopsaValue.ToWoopsaValue(
                                method.MethodInfo.Invoke(this.TargetObject, typedArguments.ToArray()), method.ReturnType,
                                GetTimeStamp());
                    }
                    catch (TargetInvocationException e)
                    {
                        // Because we are invoking using reflection, the 
                        // exception that is actually thrown is a TargetInvocationException
                        // containing the actual exception
                        if (e.InnerException != null)
                            throw e.InnerException;
                        else
                            throw;
                    }
                });
            }
            catch (Exception)
            {
                // This can happen when methods are overloaded. This isn't supported in Woopsa.
                // Ignore silently, the method won't be published
            }

        }

        #endregion

        public class ItemDescription
        {
            public PropertyInfo PropertyInfo { get; set; }
        }

        public class PropertyDescription
        {
            public WoopsaValueType Type { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public bool ReadOnly { get; set; }
        }

        public class MethodDescription
        {
            public IList<ArgumentDescription> Arguments { get; set; }
            public IEnumerable<WoopsaMethodArgumentInfo> WoopsaArguments
            {
                get
                {
                    foreach (var argument in Arguments)
                        yield return argument.ArgumentInfo;
                }
            }
            public WoopsaValueType ReturnType { get; set; }
            public MethodInfo MethodInfo { get; set; }
        }

        public class ArgumentDescription
        {
            public WoopsaMethodArgumentInfo ArgumentInfo { get; set; }
            public Type Type { get; set; }
        }

        public class TypeDescription
        {
            public TypeDescription()
            {
                Items = new List<ItemDescription>();
                Properties = new List<PropertyDescription>();
                Methods = new List<MethodDescription>();
            }

            public IList<ItemDescription> Items { get; private set; }
            public IList<PropertyDescription> Properties { get; private set; }
            public IList<MethodDescription> Methods { get; private set; }

        }

        private static readonly object[] EmptyParameters = new object[] { };

        private static T GetCustomAttribute<T>(MemberInfo element) where T : Attribute
        {
            return element.GetCustomAttributes(true).OfType<T>().FirstOrDefault();
        }

    }
}
