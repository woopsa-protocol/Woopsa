using System;
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

    [AttributeUsage(AttributeTargets.Class)]
    public class WoopsaVisibility : Attribute
    {
        public WoopsaVisibility(WoopsaObjectAdapterVisibility visibility)
        {
            Value = visibility;
        }

        public WoopsaObjectAdapterVisibility Value { get; private set; }
    }

    public enum WoopsaObjectAdapterVisibility
    {
        All = 1,
        Declared = 2,
        WoopsaVisible = 4
    }

    [Flags]
    public enum WoopsaObjectAdapterOptions
    {
        None = 0,
        DisableClassesCaching = 1,
        DisableEnumerablesPublishing = 2,
        EnableSpecialMethodsPublishing = 4,
        SendTimestamps = 8
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

        static public void ClearTypeCache()
        {
            _typesCache = new Dictionary<Type, TypeCache>();
        }

        public WoopsaObjectAdapter(WoopsaContainer container, string name, object targetObject,
            WoopsaObjectAdapterVisibility visibility = WoopsaObjectAdapterVisibility.All,
            WoopsaObjectAdapterOptions options = WoopsaObjectAdapterOptions.None)
            : base(container, name)
        {
            TargetObject = targetObject;
            _visibility = visibility;
            Options = options;
            if (targetObject.GetType().GetCustomAttribute<WoopsaVisibility>() != null)
                _visibility = targetObject.GetType().GetCustomAttribute<WoopsaVisibility>().Value;
        }

        public void ClearCache()
        {
            // Clear all items and all the children of each item.
            base.Clear();

            // Clear the properties and the methods of the current object.
            ClearProperties();
            ClearMethods();

        }

        protected bool IsMemberWoopsaVisible(MemberInfo member)
        {
            EventArgsMemberVisibilityCheck e = new EventArgsMemberVisibilityCheck(member);

            if (!HasOption(WoopsaObjectAdapterOptions.EnableSpecialMethodsPublishing))
                if (member.DeclaringType == typeof(object))
                    e.IsVisible = false;
                else if (member is MethodInfo)
                    e.IsVisible = !(member as MethodInfo).IsSpecialName;
                else
                    e.IsVisible = true;
            else
                e.IsVisible= true;
            if (MemberWoopsaVisibilityCheck != null)
                MemberWoopsaVisibilityCheck(this, e);
            return e.IsVisible;
        }

        public event MemberVisibilityCheck MemberWoopsaVisibilityCheck;

        public object TargetObject { get; private set; }

        public WoopsaObjectAdapterOptions Options { get; private set; }

        #region private members

        private static Dictionary<Type, TypeCache> _typesCache = new Dictionary<Type, TypeCache>();

        private WoopsaObjectAdapterVisibility _visibility;

        #endregion

        #region Private/Protected Methods
        protected override void PopulateObject()
        {
            base.PopulateObject();
            if (!HasOption(WoopsaObjectAdapterOptions.DisableClassesCaching) && _typesCache.ContainsKey(TargetObject.GetType()))
            {
                TypeCache cache = _typesCache[TargetObject.GetType()];
                foreach (var property in cache.Properties)
                    if (TestVisibility(property))
                        AddPropertyFromCache(property);
                foreach (var item in cache.Items)
                    if (TestVisibility(item))
                        AddItemFromCache(item);
                foreach (var method in cache.Methods)
                    if (TestVisibility(method))
                        AddMethodFromCache(method);
            }
            else
            {
                TypeCache newType = new TypeCache();
                if (!HasOption(WoopsaObjectAdapterOptions.DisableClassesCaching))
                    _typesCache.Add(TargetObject.GetType(), newType);
                PopulateItems(newType);
                PopulateProperties(newType);
                PopulateMethods(newType);
            }
        }

        protected virtual void PopulateProperties(TypeCache typeCache)
        {
            foreach (var propertyInfo in TargetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (IsMemberWoopsaVisible(propertyInfo))
                {
                    WoopsaValueType woopsaType;
                    if (WoopsaTypeUtils.InferWoopsaType(propertyInfo.PropertyType, out woopsaType))
                    {
                        //This property is a C# property that's a basic Woopsa Type
                        PropertyCache newProperty = new PropertyCache();
                        newProperty.PropertyInfo = propertyInfo;
                        if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null)
                            newProperty.ReadOnly = false;
                        else
                            newProperty.ReadOnly = true;
                        newProperty.Type = woopsaType;
                        var woopsaVisible = propertyInfo.GetCustomAttribute<WoopsaVisible>();
                        if (woopsaVisible != null && woopsaVisible.Visible)
                            newProperty.WoopsaVisible = true;
                        else if (woopsaVisible != null && !woopsaVisible.Visible)
                            newProperty.WoopsaVisible = false;
                        else if (woopsaVisible == null)
                            newProperty.WoopsaVisible = true;
                        if (TestVisibility(newProperty))
                            AddPropertyFromCache(newProperty);
                        typeCache.Properties.Add(newProperty);
                    }
                }
        }

        protected virtual void PopulateItems(TypeCache typeCache)
        {
            if (TargetObject is IEnumerable<object> && !HasOption(WoopsaObjectAdapterOptions.DisableEnumerablesPublishing))
            {
                // TODO : investigate this
               // Options &= ~WoopsaObjectAdapterOptions.PreventClassesCaching;
                int i = 0;
                foreach (object elem in (TargetObject as IEnumerable<object>))
                {
                    string name = String.Format(IEnumerableIndexerFormat, elem.GetType().Name, i);
                    i++;
                    new WoopsaObjectAdapter(this, name, elem, _visibility);
                }
            }

            foreach (var propertyInfo in TargetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (IsMemberWoopsaVisible(propertyInfo))
                {
                    WoopsaValueType woopsaType;
                    if (!WoopsaTypeUtils.InferWoopsaType(propertyInfo.PropertyType, out woopsaType))
                    {
                        //This property is more likely just a random object. Treat it as a WoopsaObject!
                        ItemCache newItem = new ItemCache();
                        newItem.PropertyInfo = propertyInfo;
                        var woopsaVisible = propertyInfo.GetCustomAttribute<WoopsaVisible>();
                        if (woopsaVisible != null && woopsaVisible.Visible)
                            newItem.WoopsaVisible = true;
                        else if (woopsaVisible != null && !woopsaVisible.Visible)
                            newItem.WoopsaVisible = false;
                        else if (woopsaVisible == null)
                            newItem.WoopsaVisible = true;
                        if (TestVisibility(newItem))
                            AddItemFromCache(newItem);
                        typeCache.Items.Add(newItem);
                    }
                }
        }

        protected virtual void PopulateMethods(TypeCache typeCache)
        {
            foreach (var method in TargetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                if (IsMemberWoopsaVisible(method))
                {
                    // Check if return type is Woopsable
                    WoopsaValueType returnType;
                    if (WoopsaTypeUtils.InferWoopsaType(method.ReturnType, out returnType))
                    {
                        bool argumentsOk = true;
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
                                argumentsOk = false;
                                break;
                            }
                        }
                        if (argumentsOk)
                        {
                            MethodCache newMethod = new MethodCache();
                            newMethod.Arguments = arguments;
                            newMethod.ReturnType = returnType;
                            newMethod.MethodInfo = method;
                            var woopsaVisible = method.GetCustomAttribute<WoopsaVisible>();
                            if (woopsaVisible != null && woopsaVisible.Visible)
                                newMethod.WoopsaVisible = true;
                            else if (woopsaVisible != null && !woopsaVisible.Visible)
                                newMethod.WoopsaVisible = false;
                            else if (woopsaVisible == null)
                                newMethod.WoopsaVisible = true;
                            if (TestVisibility(newMethod))
                                AddMethodFromCache(newMethod);
                            typeCache.Methods.Add(newMethod);
                        }
                    }
                }
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

        private DateTime? GetTimeStamp()
        {
            if (HasOption(WoopsaObjectAdapterOptions.SendTimestamps))
                return DateTime.Now;
            else
                return null;
        }

        private void AddItemFromCache(ItemCache item)
        {
            try
            {
                object value = item.PropertyInfo.GetValue(TargetObject);
                // If an inner item is null, we ignore it from the Woopsa hierarchy
                if (value != null)
                    new WoopsaObjectAdapter(this, item.PropertyInfo.Name, item.PropertyInfo.GetValue(TargetObject), _visibility, Options);
            }
            catch (Exception) { } // Property getters that throw exceptions are not added to the object hierarchy
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

        private bool TestVisibility(PropertyCache property)
        {
            if (!property.WoopsaVisible)
                return false;
            if (_visibility == WoopsaObjectAdapterVisibility.All)
                if (property.WoopsaVisible)
                    return true;
                else
                    return false;
            else
                if (_visibility == WoopsaObjectAdapterVisibility.Declared && property.PropertyInfo.DeclaringType == TargetObject.GetType())
                return property.WoopsaVisible;
            else if (_visibility == WoopsaObjectAdapterVisibility.WoopsaVisible && property.WoopsaVisible)
                return true;
            else
                return false;
        }

        private bool TestVisibility(MethodCache method)
        {
            if (!method.WoopsaVisible)
                return false;
            if (_visibility == WoopsaObjectAdapterVisibility.All)
                if (method.WoopsaVisible)
                    return true;
                else
                    return false;
            else
                if (_visibility == WoopsaObjectAdapterVisibility.Declared && method.MethodInfo.DeclaringType == TargetObject.GetType())
                return method.WoopsaVisible;
            else if (_visibility == WoopsaObjectAdapterVisibility.WoopsaVisible && method.WoopsaVisible)
                return true;
            else
                return false;
        }

        private bool TestVisibility(ItemCache item)
        {
            if (!item.WoopsaVisible)
                return false;
            if (_visibility == WoopsaObjectAdapterVisibility.All)
                if (item.WoopsaVisible)
                    return true;
                else
                    return false;
            else
                if (_visibility == WoopsaObjectAdapterVisibility.Declared && item.PropertyInfo.DeclaringType == TargetObject.GetType())
                return item.WoopsaVisible;
            else if (_visibility == WoopsaObjectAdapterVisibility.WoopsaVisible && item.WoopsaVisible)
                return true;
            else
                return false;
        }

        private bool HasOption(WoopsaObjectAdapterOptions option)
        {
            return (Options & option) == option;
        }

        #endregion

        protected struct ItemCache
        {
            public PropertyInfo PropertyInfo { get; set; }
            public Boolean WoopsaVisible { get; set; }
        }

        protected struct PropertyCache
        {
            public WoopsaValueType Type { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public Boolean ReadOnly { get; set; }
            public Boolean WoopsaVisible { get; set; }
        }

        protected struct MethodCache
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
            public Boolean WoopsaVisible { get; set; }
        }

        protected struct ArgumentCache
        {
            public WoopsaMethodArgumentInfo ArgumentInfo { get; set; }
            public Type Type { get; set; }
        }

        protected class TypeCache
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
