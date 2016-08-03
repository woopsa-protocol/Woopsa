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
                    WoopsaReflection.GetCustomAttribute<WoopsaVisibilityAttribute>(targetObject.GetType());
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

        protected virtual void OnMemberWoopsaVisibilityCheck(EventArgsMemberVisibilityCheck e)
        {
            if (MemberWoopsaVisibilityCheck != null)
                MemberWoopsaVisibilityCheck(this, e);
            else if (Owner is WoopsaObjectAdapter)
                ((WoopsaObjectAdapter)Owner).OnMemberWoopsaVisibilityCheck(e);
        }

        protected override void PopulateObject()
        {
            TypeDescription typeDescription = null;

            base.PopulateObject();
            if (!Options.HasFlag(WoopsaObjectAdapterOptions.DisableClassesCaching))
                lock (_typesCache)
                {
                    _typesCache.TryGetValue(TargetObject.GetType(), out typeDescription);
                    if (typeDescription == null)
                        typeDescription = WoopsaReflection.ReflectObject(TargetObject.GetType(),
                            Visibility,
                            OnMemberWoopsaVisibilityCheck);
                }
            else
                typeDescription = WoopsaReflection.ReflectObject(TargetObject.GetType(), 
                    Visibility, OnMemberWoopsaVisibilityCheck);

            PopulateProperties(typeDescription.Properties);
            PopulateMethods(typeDescription.Methods);
            PopulateItems(typeDescription.Items);
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

        private DateTime? GetTimeStamp()
        {
            if (Options.HasFlag(WoopsaObjectAdapterOptions.SendTimestamps))
                return DateTime.Now;
            else
                return null;
        }

        protected void AddWoopsaProperty(PropertyDescription property)
        {
            if (property.IsReadOnly)
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
                            typedArguments.Add(((WoopsaValue)args[i]).ConvertTo(method.Arguments[i].Type));
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

        private static readonly object[] EmptyParameters = new object[] { };

    }
}
