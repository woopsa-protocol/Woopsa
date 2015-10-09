using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    public class OnlyWoopsaVisible : Attribute
    {
        public OnlyWoopsaVisible(bool value)
        {
            Value = value;
        }

        public bool Value { get; private set; }
    }

    public enum WoopsaObjectAdapterVisibility
    {
        All,
        Declared,
        WoopsaVisible
    }

	public class WoopsaObjectAdapter: WoopsaObject
	{
        public WoopsaObjectAdapter(WoopsaContainer container, string name, object targetObject,
            WoopsaObjectAdapterVisibility visibility = WoopsaObjectAdapterVisibility.All)
            : base(container, name)
        {
            TargetObject = targetObject;
            _visibility = visibility;
            if (targetObject.GetType().GetCustomAttribute<OnlyWoopsaVisible>() != null && targetObject.GetType().GetCustomAttribute<OnlyWoopsaVisible>().Value )
            {
                _visibility = WoopsaObjectAdapterVisibility.WoopsaVisible;
            }
        }

        public void ClearCache()
        {
            _typesCache = new Dictionary<Type, TypeCache>();
        }

        protected override void PopulateObject()
        {
            base.PopulateObject();
            if (_typesCache.ContainsKey(TargetObject.GetType()))
            {
                TypeCache cache = _typesCache[TargetObject.GetType()];
                foreach(var property in cache.Properties)
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
                _typesCache.Add(TargetObject.GetType(), newType);
                PopulateProperties(newType);
                PopulateMethods(newType);
            }
        }
        
        public object TargetObject { get; private set; } 

        #region private members

        //private bool _onlyWoopsaVisible;
        private WoopsaObjectAdapterVisibility _visibility;
        private static Dictionary<Type, TypeCache> _typesCache = new Dictionary<Type, TypeCache>();

        #endregion

        #region Private Methods

        private void PopulateProperties(TypeCache typeCache)
        {
            foreach (var propertyInfo in TargetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
                    else
                        newProperty.WoopsaVisible = false;
                    if ( TestVisibility(newProperty))
                        AddPropertyFromCache(newProperty);
                    typeCache.Properties.Add(newProperty);
                }
                else
                {
                    //This property is more likely just a random object. Treat it as a WoopsaObject!
                    ItemCache newItem = new ItemCache();
                    newItem.PropertyInfo = propertyInfo;
                    var woopsaVisible = propertyInfo.GetCustomAttribute<WoopsaVisible>();
                    if (woopsaVisible != null && woopsaVisible.Visible)
                        newItem.WoopsaVisible = true;
                    else
                        newItem.WoopsaVisible = false;
                    if (TestVisibility(newItem))
                        AddItemFromCache(newItem);
                    typeCache.Items.Add(newItem);
                }
            }
        }

        private void PopulateMethods(TypeCache typeCache)
        {
            foreach(var method in TargetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Where((m) => (m.IsSpecialName == false)))
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
                        else
                            newMethod.WoopsaVisible = false;
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
                    (sender) => (property.PropertyInfo.GetValue(TargetObject).ToWoopsaValue(property.Type))
                );
            else
                new WoopsaProperty(this, property.PropertyInfo.Name, property.Type,
                    (sender) => (property.PropertyInfo.GetValue(TargetObject).ToWoopsaValue(property.Type)),
                    (sender, value) => property.PropertyInfo.SetValue(TargetObject, ((WoopsaValue)value).ConvertTo(property.PropertyInfo.PropertyType))
                );
        }

        private void AddItemFromCache(ItemCache item)
        {
            new WoopsaObjectAdapter(this, item.PropertyInfo.Name, item.PropertyInfo.GetValue(TargetObject), _visibility);
        }

        private void AddMethodFromCache(MethodCache method)
        {
            new WoopsaMethod(this, method.MethodInfo.Name, method.ReturnType, method.WoopsaArguments, (args) =>
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
                {
                    return method.MethodInfo.Invoke(this.TargetObject, typedArguments.ToArray()).ToWoopsaValue(method.ReturnType);
                }
            });
        }

        private bool TestVisibility(PropertyCache property)
        {
            if (_visibility == WoopsaObjectAdapterVisibility.All)
                return true;
            else
                if (_visibility == WoopsaObjectAdapterVisibility.Declared && property.PropertyInfo.DeclaringType == TargetObject.GetType())
                    return true;
                else if (_visibility == WoopsaObjectAdapterVisibility.WoopsaVisible && property.WoopsaVisible)
                    return true;
                else
                    return false;
        }

        private bool TestVisibility(MethodCache method)
        {
            if (_visibility == WoopsaObjectAdapterVisibility.All)
                return true;
            else
                if (_visibility == WoopsaObjectAdapterVisibility.Declared && method.MethodInfo.DeclaringType == TargetObject.GetType())
                    return true;
                else if (_visibility == WoopsaObjectAdapterVisibility.WoopsaVisible && method.WoopsaVisible)
                    return true;
                else
                    return false;
        }

        private bool TestVisibility(ItemCache item)
        {
            if (_visibility == WoopsaObjectAdapterVisibility.All)
                return true;
            else
                if (_visibility == WoopsaObjectAdapterVisibility.Declared && item.PropertyInfo.DeclaringType == TargetObject.GetType())
                    return true;
                else if (_visibility == WoopsaObjectAdapterVisibility.WoopsaVisible && item.WoopsaVisible)
                    return true;
                else
                    return false;
        }

        #endregion

        private struct ItemCache
        {
            public PropertyInfo PropertyInfo { get; set; }
            public Boolean WoopsaVisible { get; set; }
        }

        private struct PropertyCache
        {
            public WoopsaValueType Type { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public Boolean ReadOnly { get; set; }
            public Boolean WoopsaVisible { get; set; }
        }

        private struct MethodCache
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

        private struct ArgumentCache
        {
            public WoopsaMethodArgumentInfo ArgumentInfo { get; set; }
            public Type Type { get; set; }
        }

        private class TypeCache
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
