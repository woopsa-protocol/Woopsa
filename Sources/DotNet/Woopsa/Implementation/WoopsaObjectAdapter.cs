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
        ObjectClassMembers = 16,
        /// <summary>
        /// Publish all
        /// </summary>
        All = DefaultIsVisible | MethodSpecialName | Inherited | IEnumerableObject | ObjectClassMembers
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

        #region static

        static WoopsaObjectAdapter()
        {
            _typesCache = new TypeDescriptions(WoopsaVisibility.All);
        }

        private static TypeDescriptions _typesCache;

        #endregion


        public const string IEnumerableIndexerFormat = "{0}[{1}]";

        public const string IEnumerableItemBaseName = "Item";

        public WoopsaObjectAdapter(WoopsaContainer container, string name, object targetObject,
                WoopsaObjectAdapterOptions options = WoopsaObjectAdapterOptions.None,
                WoopsaVisibility defaultVisibility = WoopsaReflection.DefaultVisibility) :
            this(container, name, () => targetObject, options, defaultVisibility)
        {

        }
        protected WoopsaObjectAdapter(WoopsaContainer container, string name, Func<object> targetObjectGetter,
            WoopsaObjectAdapterOptions options = WoopsaObjectAdapterOptions.None,
            WoopsaVisibility defaultVisibility = WoopsaReflection.DefaultVisibility)
            : base(container, name)
        {
            TargetObjectGetter = targetObjectGetter;
            DefaultVisibility = defaultVisibility;
            Options = options;
        }

        /// <summary>
        /// To customize the woopsa visibility of a member. 
        /// This event is triggered for every member, including the members of the inner items.
        /// It can be used to force the visibility of any member to true or false.
        /// </summary>
        public event EventHandler<EventArgsMemberVisibilityCheck> MemberWoopsaVisibilityCheck;

        public object TargetObject { get; private set; }
        public Func<object> TargetObjectGetter { get; private set; }
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

            RefreshTargetObject();
            if (TargetObject != null)
            {
                WoopsaVisibilityAttribute woopsaVisibilityAttribute =
                    WoopsaReflection.GetCustomAttribute<WoopsaVisibilityAttribute>(TargetObject.GetType());
                if (woopsaVisibilityAttribute != null)
                    Visibility = woopsaVisibilityAttribute.Visibility;
                else
                    Visibility = DefaultVisibility;
            }
            else
                Visibility = DefaultVisibility;
            base.PopulateObject();
            if (TargetObject != null)
            {
                if (!Options.HasFlag(WoopsaObjectAdapterOptions.DisableClassesCaching))
                    lock (_typesCache)
                        typeDescription = _typesCache.GetTypeDescription(TargetObject.GetType());
                else
                    typeDescription = WoopsaReflection.ReflectType(TargetObject.GetType(),
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
        }

        protected virtual void PopulateProperties(IEnumerable<PropertyDescription> properties)
        {
            foreach (var property in properties)
                if (IsMemberWoopsaVisible(property.PropertyInfo))
                    AddWoopsaProperty(property);
        }
        protected virtual void PopulateMethods(IEnumerable<MethodDescription> methods)
        {
            foreach (var method in methods)
                if (IsMemberWoopsaVisible(method.MethodInfo))
                    AddWoopsaMethod(method);
        }

        protected virtual void PopulateItems(IEnumerable<ItemDescription> items)
        {
            foreach (var item in items)
                if (IsMemberWoopsaVisible(item.PropertyInfo))
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

        protected override void UpdateItems()
        {
            base.UpdateItems();
            object lastTargetObjectType = TargetObject != null ? TargetObject.GetType() : null;
            RefreshTargetObject();
            object newTargetObjectType = TargetObject != null ? TargetObject.GetType() : null;
            if (newTargetObjectType != lastTargetObjectType)
            {
                Clear();
                DoPopulate();
            }
        }

        protected virtual bool IsMemberWoopsaVisible(MemberInfo memberInfo)
        {
            return WoopsaReflection.IsMemberWoopsaVisible(TargetObject.GetType(),
                memberInfo, DefaultVisibility, OnMemberWoopsaVisibilityCheck);
        }

        private void RefreshTargetObject()
        {
            try
            {
                TargetObject = TargetObjectGetter();
            }
            catch (Exception)
            {
                TargetObject = null;
                // Items from property getters that throw exceptions are not added to the object hierarchy
                // Ignore silently
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
                new WoopsaProperty(this, property.PropertyInfo.Name, property.WoopsaType,
                    (sender) => (WoopsaValue.ToWoopsaValue(property.PropertyInfo.GetValue(TargetObject, EmptyParameters), property.WoopsaType, GetTimeStamp()))
                );
            else
                new WoopsaProperty(this, property.PropertyInfo.Name, property.WoopsaType,
                    (sender) => (WoopsaValue.ToWoopsaValue(property.PropertyInfo.GetValue(TargetObject, EmptyParameters), property.WoopsaType, GetTimeStamp())),
                    (sender, value) => property.PropertyInfo.SetValue(TargetObject, value.ConvertTo(property.PropertyInfo.PropertyType), null)
                );
        }

        protected void AddWoopsaItem(ItemDescription item)
        {
                    CreateItemWoopsaAdapter(item, () =>
                        item.PropertyInfo.GetValue(TargetObject, EmptyParameters));
        }

        protected virtual WoopsaObjectAdapter CreateItemWoopsaAdapter(
            ItemDescription itemDescription, Func<object> itemGetter)
        {
            return new WoopsaObjectAdapter(this, itemDescription.Name, itemGetter, Options, DefaultVisibility);
        }

        protected void AddWoopsaMethod(MethodDescription method)
        {
            try
            {
                new WoopsaMethod(this, method.MethodInfo.Name, method.WoopsaReturnType, method.WoopsaArguments, (args) =>
                {
                    try
                    {
                        var typedArguments = new object[method.Arguments.Count];
                        for (var i = 0; i < method.Arguments.Count; i++)
                            typedArguments[i] = ((WoopsaValue)args[i]).ConvertTo(method.Arguments[i].Type);
                        if (method.MethodInfo.ReturnType == typeof(void))
                        {
                            method.MethodInfo.Invoke(TargetObject, typedArguments);
                            return null;
                        }
                        else
                            return WoopsaValue.ToWoopsaValue(
                                method.MethodInfo.Invoke(TargetObject, typedArguments.ToArray()), method.WoopsaReturnType,
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
