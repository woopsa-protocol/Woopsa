using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public interface IWoopsaElementReadOnlyList<T>: IReadOnlyCollection<T>
    {
        T this[string name] { get; }

        T ByName(string name);

        T ByNameOrNull(string name);

        bool Contains(string Name);

        bool Contains(T element);
    }

    public abstract class WoopsaElement : IWoopsaElement, IDisposable
    {
        #region Constructors

        protected WoopsaElement(WoopsaContainer owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        #endregion

        #region IWoopsaElement

        public WoopsaContainer Owner { get; private set; }

        IWoopsaContainer IWoopsaElement.Owner
        {
            get { return Owner; }
        }

        public string Name { get; private set; }

        #endregion IWoopsaElement

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            Owner = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }

    public class WoopsaContainer : WoopsaElement, IWoopsaContainer
    {
        #region Constructors

        public WoopsaContainer(WoopsaContainer container, string name)
            : base(container, name)
        {
            _items = new WoopsaElementList<WoopsaContainer>();
            if (Owner != null)
                Owner.Add(this);
        }

        #endregion

        #region Public 

        public IWoopsaElementReadOnlyList<WoopsaContainer> Items
        {
            get
            {
                DoPopulate();
                return _items;
            }
        }
        IEnumerable<IWoopsaContainer> IWoopsaContainer.Items
        {
            get { return Items; }
        }

        /// <summary>
        /// Calls refresh recursively on all the WoopsaContainers hierarchy.
        /// Objects containing dynamic structure should refresh their content following this call.
        /// </summary>
        public virtual void Refresh()
        {
            foreach (var item in _items)
                item.Refresh();
            // Refresh must not call clear, as WoopsaContainer and WoopsaObject can be used to create
            // static hierarchy.
            // Inheriting classes containing dynamic hierarchy should call clear in their Refresh method implementation
        }

        #endregion

        protected virtual void PopulateContainer(WoopsaElementList<WoopsaContainer> items)
        { }

        protected void DoPopulate()
        {
            if (!_populated)
            {
                PopulateContainer(_items);
                _populated = true;
            }
        }

        #region Items Management Add / Remove / Clear

        internal void Add(WoopsaContainer item)
        {
            if (_items.ByNameOrNull(item.Name) != null)
                throw new WoopsaException("Tried to add an item with duplicate name '" + item.Name + "' to WoopsaContainer '" + Name + "'");
            _items.Add(item);
        }

        internal void Remove(WoopsaContainer item)
        {
            _items.Remove(item);
        }

        protected virtual void Clear()
        {
            _items.Clear();
            _populated = false;
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (Owner != null)
                Owner.Remove(this);
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private readonly WoopsaElementList<WoopsaContainer> _items;
        private bool _populated;

        #endregion
    }

    public delegate WoopsaValue WoopsaPropertyGet(IWoopsaProperty property);
    public delegate void WoopsaPropertySet(IWoopsaProperty property, IWoopsaValue value);

    public class WoopsaProperty : WoopsaElement, IWoopsaProperty
    {
        #region Constructors

        public WoopsaProperty(WoopsaObject container, string name, WoopsaValueType type, WoopsaPropertyGet get, WoopsaPropertySet set)
            : base(container, name)
        {
            Type = type;
            _get = get;
            IsReadOnly = set == null;
            if (!IsReadOnly)
                _set = set;
            if (container != null)
                container.Add(this);
        }

        public WoopsaProperty(WoopsaObject container, string name, WoopsaValueType type, WoopsaPropertyGet get)
            : this(container, name, type, get, null)
        {
        }

        #endregion

        #region IWoopsaProperty

        public bool IsReadOnly { get; private set; }

        public WoopsaValue Value
        {
            get { return _get(this); }
            set
            {
                ((IWoopsaProperty)this).Value = value;
            }
        }

        IWoopsaValue IWoopsaProperty.Value
        {
            get { return Value; }
            set
            {
                if (!IsReadOnly)
                    _set(this, value);
                else
                    throw new WoopsaException(string.Format("Cannot set read-only property {0}", Name));
            }
        }

        public WoopsaValueType Type { get; private set; }

        #endregion IWoopsaProperty

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (Owner != null)
                ((WoopsaObject)Owner).Remove(this);
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private readonly WoopsaPropertyGet _get;
        private readonly WoopsaPropertySet _set;

        #endregion
    }

    public class WoopsaMethodArgumentInfo : IWoopsaMethodArgumentInfo
    {
        #region Constructor

        public WoopsaMethodArgumentInfo(string name, WoopsaValueType type)
        {
            Name = name;
            Type = type;
        }

        #endregion

        #region Public Properties

        public string Name { get; private set; }

        public WoopsaValueType Type { get; private set; }

        #endregion
    }

    public delegate WoopsaValue WoopsaMethodInvoke(IWoopsaValue[] arguments);

    public class WoopsaMethod : WoopsaElement, IWoopsaMethod
    {
        #region Constructors

        public WoopsaMethod(WoopsaObject container, string name, WoopsaValueType returnType, IEnumerable<WoopsaMethodArgumentInfo> argumentInfos, WoopsaMethodInvoke methodInvoke)
            : base(container, name)
        {
            ReturnType = returnType;
            ArgumentInfos = argumentInfos;
            _methodInvoke = methodInvoke;
            if (container != null)
                container.Add(this);
        }

        #endregion

        #region IWoopsaMethod

        public WoopsaValue Invoke(params WoopsaValue[] arguments)
        {
            return _methodInvoke(arguments);
        }
        public WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments)
        {
            return _methodInvoke(arguments.ToArray());
        }
        
        IWoopsaValue IWoopsaMethod.Invoke(IWoopsaValue[] arguments)
        {
            return Invoke(arguments);
        }

        public WoopsaValueType ReturnType { get; private set; }

        public IEnumerable<IWoopsaMethodArgumentInfo> ArgumentInfos { get; private set; }

        #endregion IWoopsaMethod

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (Owner != null)
                ((WoopsaObject)Owner).Remove(this);
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private readonly WoopsaMethodInvoke _methodInvoke;

        #endregion
    }

    public class WoopsaObject : WoopsaContainer, IWoopsaObject
    {
        #region Constructors

        public WoopsaObject(WoopsaContainer container, string name)
            : base(container, name)
        {
            _properties = new WoopsaElementList<WoopsaProperty>();
            _methods = new WoopsaElementList<WoopsaMethod>();
        }

        #endregion

        #region IWoopsaObject Properties

        public IWoopsaElementReadOnlyList<WoopsaProperty> Properties
        {
            get
            {
                DoPopulate();
                return _properties;
            }
        }

        IEnumerable<IWoopsaProperty> IWoopsaObject.Properties
        {
            get { return Properties; }
        }

        public IWoopsaElementReadOnlyList<WoopsaMethod> Methods
        {
            get
            {
                DoPopulate();
                return _methods;
            }
        }

        IEnumerable<IWoopsaMethod> IWoopsaObject.Methods
        {
            get { return Methods; }
        }

        #endregion

        protected override void PopulateContainer(WoopsaElementList<WoopsaContainer> items)
        {
            base.PopulateContainer(items);
            PopulateObject();
        }

        protected virtual void PopulateObject()
        {
        }

        protected override void Clear()
        {
            _properties.Clear();
            _methods.Clear();
            base.Clear();
        }

        #region Properties Management Add / Remove / Clear

        internal void Add(WoopsaProperty item)
        {
            if (_properties.ByNameOrNull(item.Name) != null)
                throw new WoopsaException("Tried to add a property with duplicate name '" + item.Name + "' to WoopsaObject '" + Name + "'");
            _properties.Add(item);
        }

        internal void Remove(WoopsaProperty item)
        {
            _properties.Remove(item);
        }

        #endregion

        #region Methods Management Add / Remove / Clear

        internal void Add(WoopsaMethod item)
        {
            if (_methods.ByNameOrNull(item.Name) != null)
                throw new WoopsaException("Tried to add a method with duplicate name '" + item.Name + "' to WoopsaObject '" + Name + "'");
            _methods.Add(item);
        }

        internal void Remove(WoopsaMethod item)
        {
            _methods.Remove(item);
        }

        #endregion

        #region Private Members

        private readonly WoopsaElementList<WoopsaProperty> _properties;
        private readonly WoopsaElementList<WoopsaMethod> _methods;

        #endregion
    }

    public class WoopsaRoot : WoopsaContainer
    {
        public WoopsaRoot()
            : base(null, string.Empty)
        {
        }
    }

    public class WoopsaElementList<T> : IWoopsaElementReadOnlyList<T> where T :WoopsaElement
    {
        public WoopsaElementList()
        {
            _items = new Dictionary<string, T>();
        }

        public T this[string name]
        {
            get
            {
                return _items[name];
            }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        public void Add(T item)
        {
            _items.Add(item.Name, item);
        }

        public void Remove(T item)
        {
            _items.Remove(item.Name);
        }

        public bool Contains(string key)
        {
            return _items.ContainsKey(key);
        }

        public bool Contains(T element)
        {
            return _items.ContainsValue(element);
        }

        public void Clear()
        {
            T[] items = _items.Values.ToArray();
            foreach (var item in items)
                item.Dispose();
        }

        public T ByName(string name)
        {
            return _items[name];
        }

        public T ByNameOrNull(string name)
        {
            if (Contains(name))
                return ByName(name);
            else
                return null;
        }

        private Dictionary<string, T> _items;

    }
}