using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Woopsa
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    public class WoopsaVisible : Attribute
    {
        public WoopsaVisible(bool visible)
        {
            Visible = visible;
        }

        public bool Visible { get; private set; }
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
        DefaultVisible = 1,
        /// <summary>
        /// Publish methods with special names (like property getters, setters).
        /// </summary>
        MethodSpecialName = 2,
        /// <summary>
        /// Publish inherited members.
        /// </summary>
        Inherited = 4,
        /// <summary>
        /// Publish IEnumerable as a collection of items.
        /// </summary>
        IEnumerable = 8,
        /// <summary>
        /// Publish members inherited from Object, like ToString. Requires flag Inherited to have an effect.
        /// </summary>
        Object = 16
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

    public delegate void MemberVisibilityCheck(object sender, EventArgsMemberVisibilityCheck e);

    public class WoopsaObjectAdapter : WoopsaObject
    {
        public const string IEnumerableIndexerFormat = "{0}[{1}]";

        public const string IEnumerableItemBaseName = "Item";

        static public void ClearTypeCache()
        {
            _typesCache = new Dictionary<Type, TypeCache>();
        }

        public WoopsaObjectAdapter(WoopsaContainer container, string name, object targetObject,
            WoopsaObjectAdapterOptions options = WoopsaObjectAdapterOptions.None,
            WoopsaVisibility defaultVisibility = WoopsaVisibility.DefaultVisible)
            : base(container, name)
        {
            TargetObject = targetObject;
            DefaultVisibility = defaultVisibility;
            Options = options;
            if (targetObject != null)
            {
                WoopsaVisibilityAttribute woopsaVisibilityAttribute =
                    targetObject.GetType().GetCustomAttribute<WoopsaVisibilityAttribute>();
                if (woopsaVisibilityAttribute != null)
                    Visibility = woopsaVisibilityAttribute.Visibility;
                else
                    Visibility = defaultVisibility;
            }
            else
                Visibility = defaultVisibility;
        }

        public void ClearCache()
        {
            //TODO check this implementation

            // Clear all items and all the children of each item.
            base.Clear();

            // Clear the properties and the methods of the current object.
            ClearProperties();
            ClearMethods();

        }

        /// <summary>
        /// To customize the woopsa visibility of a member. 
        /// This event is triggered for every member, including the members of the inner items.
        /// It can be used to force the visibility of any member to true or false.
        /// </summary>
        public event MemberVisibilityCheck MemberWoopsaVisibilityCheck;

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

        #region private members

        private static Dictionary<Type, TypeCache> _typesCache = new Dictionary<Type, TypeCache>();

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
            else if (Container is WoopsaObjectAdapter)
                ((WoopsaObjectAdapter)Container).OnMemberWoopsaVisibilityCheck(member, ref isVisible);
        }

        protected bool IsMemberWoopsaVisible(MemberInfo member)
        {
            var woopsaVisibleAttribute = member.GetCustomAttribute<WoopsaVisible>();
            bool isVisible;
            if (woopsaVisibleAttribute != null)
                isVisible = woopsaVisibleAttribute.Visible;
            else
                isVisible = Visibility.HasFlag(WoopsaVisibility.DefaultVisible);
            if (isVisible)
            {
                if (TargetObject != null)
                    if (member.DeclaringType != TargetObject.GetType())
                        isVisible = Visibility.HasFlag(WoopsaVisibility.Inherited);
            }
            if (isVisible)
            {
                if (member.DeclaringType == typeof(object))
                    isVisible = Visibility.HasFlag(WoopsaVisibility.Object);
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
                    if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                        isVisible = Visibility.HasFlag(WoopsaVisibility.IEnumerable);
                }
            }
            OnMemberWoopsaVisibilityCheck(member, ref isVisible);
            return isVisible;
        }

        protected override void PopulateObject()
        {
            TypeCache cache;

            base.PopulateObject();
            if (!Options.HasFlag(WoopsaObjectAdapterOptions.DisableClassesCaching) &&
                    _typesCache.ContainsKey(TargetObject.GetType()))
                cache = _typesCache[TargetObject.GetType()];
            else
            {
                cache = new TypeCache();
                PopulateProperties(cache);
                PopulateMethods(cache);
                PopulateItems(cache);
                if (!Options.HasFlag(WoopsaObjectAdapterOptions.DisableClassesCaching))
                    _typesCache.Add(TargetObject.GetType(), cache);
            }
            // Publish properties
            foreach (var property in cache.Properties)
                    AddPropertyFromCache(property);
            // Publish methods
            foreach (var method in cache.Methods)
                    AddMethodFromCache(method);
            // Publish inner items
            foreach (var item in cache.Items)
                    AddItemFromCache(item);
            // Publish inner items for TargetObjects implementing IEnumerable
            if (TargetObject is IEnumerable && Visibility.HasFlag(WoopsaVisibility.IEnumerable))
            {
                IEnumerable enumerable = (IEnumerable)TargetObject;
                int index = 0;
                foreach (object item in enumerable)
                {
                    string name = String.Format(IEnumerableIndexerFormat, IEnumerableItemBaseName, index);
                    index++;
                    new WoopsaObjectAdapter(this, name, item, Options, DefaultVisibility);
                }
            }
        }

        protected virtual void PopulateProperties(TypeCache typeCache)
        {
            PropertyInfo[] properties;
            properties = TargetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in properties)
                if (IsMemberWoopsaVisible(propertyInfo))
                {
                    WoopsaValueType woopsaType;
                    if (WoopsaTypeUtils.InferWoopsaType(propertyInfo.PropertyType, out woopsaType))
                    {
                        //This property is a C# property of a valid basic Woopsa Type, it can be published as a Woopsa property
                        PropertyCache newProperty = new PropertyCache();
                        newProperty.PropertyInfo = propertyInfo;
                        if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null)
                            newProperty.ReadOnly = false;
                        else
                            newProperty.ReadOnly = true;
                        newProperty.Type = woopsaType;
                        typeCache.Properties.Add(newProperty);
                    }
                }
        }

        protected virtual void PopulateItems(TypeCache typeCache)
        {
            PropertyInfo[] properties;
            properties = TargetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in properties)
                if (IsMemberWoopsaVisible(propertyInfo))
                {
                    WoopsaValueType woopsaType;
                    if (!WoopsaTypeUtils.InferWoopsaType(propertyInfo.PropertyType, out woopsaType) &&
                        !propertyInfo.PropertyType.IsValueType)
                    {
                        // This property is not of a WoopsaType, it is a reference type, assume it is an inner item
                        ItemCache newItem = new ItemCache();
                        newItem.PropertyInfo = propertyInfo;
                        typeCache.Items.Add(newItem);
                    }
                }
        }

        protected virtual void PopulateMethods(TypeCache typeCache)
        {
            MethodInfo[] methods;

            methods = TargetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
                if (IsMemberWoopsaVisible(method))
                {
                    // Check if return type is type compatible with Woopsa
                    WoopsaValueType returnType;
                    if (WoopsaTypeUtils.InferWoopsaType(method.ReturnType, out returnType))
                    {
                        bool argumentsTypeCompatible = true;
                        List<ArgumentCache> arguments = new List<ArgumentCache>();
                        foreach (var parameter in method.GetParameters())
                        {
                            WoopsaValueType argumentType;
                            if (WoopsaTypeUtils.InferWoopsaType(parameter.ParameterType, out argumentType))
                            {
                                ArgumentCache newArgument = new ArgumentCache();
                                newArgument.Type = parameter.ParameterType;
                                newArgument.ArgumentInfo = new WoopsaMethodArgumentInfo(parameter.Name, argumentType);
                                arguments.Add(newArgument);
                            }
                            else
                            {
                                argumentsTypeCompatible = false;
                                break;
                            }
                        }
                        if (argumentsTypeCompatible)
                        {
                            MethodCache newMethod = new MethodCache();
                            newMethod.Arguments = arguments;
                            newMethod.ReturnType = returnType;
                            newMethod.MethodInfo = method;                            
                            typeCache.Methods.Add(newMethod);
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

        private void AddPropertyFromCache(PropertyCache property)
        {
            if (property.ReadOnly)
                new WoopsaProperty(this, property.PropertyInfo.Name, property.Type,
                    (sender) => (WoopsaValue.ToWoopsaValue(property.PropertyInfo.GetValue(TargetObject), property.Type, GetTimeStamp()))
                );
            else
                new WoopsaProperty(this, property.PropertyInfo.Name, property.Type,
                    (sender) => (WoopsaValue.ToWoopsaValue(property.PropertyInfo.GetValue(TargetObject), property.Type, GetTimeStamp())),
                    (sender, value) => property.PropertyInfo.SetValue(TargetObject, value.ConvertTo(property.PropertyInfo.PropertyType))
                );
        }

        private void AddItemFromCache(ItemCache item)
        {
            try
            {
                object value = item.PropertyInfo.GetValue(TargetObject);
                // If an inner item is null, ignore it for the Woopsa hierarchy
                if (value != null)
                    new WoopsaObjectAdapter(this, item.PropertyInfo.Name, item.PropertyInfo.GetValue(TargetObject), 
                        Options, DefaultVisibility);
            }
            catch (Exception)
            {
                // Items from property getters that throw exceptions are not added to the object hierarchy
                // Ignore silently
            }
        }

        private void AddMethodFromCache(MethodCache method)
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

        public class ItemCache
        {
            public PropertyInfo PropertyInfo { get; set; }
        }

        public class PropertyCache
        {
            public WoopsaValueType Type { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public bool ReadOnly { get; set; }
        }

        public class MethodCache
        {
            public IList<ArgumentCache> Arguments { get; set; }
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

        public class ArgumentCache
        {
            public WoopsaMethodArgumentInfo ArgumentInfo { get; set; }
            public Type Type { get; set; }
        }

        public class TypeCache
        {
            public TypeCache()
            {
                Items = new List<ItemCache>();
                Properties = new List<PropertyCache>();
                Methods = new List<MethodCache>();
            }

            public IList<ItemCache> Items { get; private set; }
            public IList<PropertyCache> Properties { get; private set; }
            public IList<MethodCache> Methods { get; private set; }
        }
    }
}
