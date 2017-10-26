using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public interface IWoopsaElementReadOnlyList<T> : IEnumerable, IEnumerable<T>
    {
        T this[string name] { get; }

        T ByName(string name);

        T ByNameOrNull(string name);

        bool Contains(string Name);

        bool Contains(T element);

        int Count { get; }

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

        public bool IsDisposed { get { return _isDisposed; } }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            _isDisposed = true;
            Owner = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        protected void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetPath());
        }

        private bool _isDisposed;
    }

    public class WoopsaContainer : WoopsaElement, IWoopsaContainer
    {
        #region Constructors

        public WoopsaContainer(WoopsaContainer container, string name)
            : base(container, name)
        {
            Lock = new object();
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

        public WoopsaContainer ByNameOrNull(string name)
        {
            return Items.ByNameOrNull(name);
        }

        public WoopsaContainer ByName(string name)
        {
            WoopsaContainer result;
            lock (Lock)
                result = ByNameOrNull(name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa element not found : {0}", name));
        }

        #endregion

        protected virtual void PopulateContainer(WoopsaElementList<WoopsaContainer> items)
        { }

        protected void DoPopulate()
        {
            CheckDisposed();
            lock (Lock)
            {
                if (!_populated)
                {
                    PopulateContainer(_items);
                    _populated = true;
                }
                else
                    UpdateItems();
            }
        }

        /// <summary>
        /// This method give the opportunity to update items, when the list of contained Items may change dynamically
        /// Typical usage is when publishing a list.
        /// This method is called every time Items are requested
        /// </summary>
        protected virtual void UpdateItems()
        {
        }

        #region Items Management Add / Remove / Clear

        internal void Add(WoopsaContainer item)
        {
            lock (Lock)
            {
                if (_items.ByNameOrNull(item.Name) != null)
                    throw new WoopsaException("Tried to add an item with duplicate name '" + item.Name + "' to WoopsaContainer '" + Name + "'");
                _items.Add(item);
            }
        }

        internal void Remove(WoopsaContainer item)
        {
            lock (Lock)
                _items.Remove(item);
        }

        protected virtual void Clear()
        {
            lock (Lock)
            {
                DisposeWoopsaElements(_items);
                _populated = false;
            }
        }

        internal void DisposeWoopsaElements(IEnumerable<WoopsaElement> elements)
        {
            var items = elements.ToArray();
            foreach (var item in items)
                item.Dispose();
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Clear();
                if (Owner != null)
                    Owner.Remove(this);
            }
            base.Dispose(disposing);
        }

        #endregion

        internal object Lock { get; private set; }

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
            get
            {
                CheckDisposed();
                return _get(this);
            }
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
                CheckDisposed();
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
            CheckDisposed();
            return _methodInvoke(arguments);
        }
        public WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments)
        {
            CheckDisposed();
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
                lock (Lock)
                {
                    DoPopulate();
                    return _properties;
                }
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
                lock (Lock)
                {
                    DoPopulate();
                    return _methods;
                }
            }
        }

        IEnumerable<IWoopsaMethod> IWoopsaObject.Methods
        {
            get { return Methods; }
        }

        #endregion

        public new WoopsaElement ByNameOrNull(string name)
        {
            WoopsaElement result;
            lock (Lock)
            {
                result = base.ByNameOrNull(name);
                if (result == null)
                    result = Properties.ByNameOrNull(name);
                if (result == null)
                    result = Methods.ByNameOrNull(name);
            }
            return result;
        }

        public new WoopsaElement ByName(string name)
        {
            WoopsaElement result = ByNameOrNull(name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa element not found : {0}", name));
        }

        protected override void PopulateContainer(WoopsaElementList<WoopsaContainer> items)
        {
            base.PopulateContainer(items);
            PopulateObject();
        }

        protected virtual void PopulateObject()
        {
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
            lock (Lock)
            {
                if (_methods.ByNameOrNull(item.Name) != null)
                    throw new WoopsaException("Tried to add a method with duplicate name '" + item.Name + "' to WoopsaObject '" + Name + "'");
                _methods.Add(item);
            }
        }

        internal void Remove(WoopsaMethod item)
        {
            lock (Lock)
                _methods.Remove(item);
        }

        protected override void Clear()
        {
            lock (Lock)
            {
                DisposeWoopsaElements(_properties);
                DisposeWoopsaElements(_methods);
                base.Clear();
            }
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

    public class WoopsaElementList<T> : IWoopsaElementReadOnlyList<T> where T : WoopsaElement
    {
        public WoopsaElementList()
        {
            _itemsByName = new Dictionary<string, T>();
        }

        public T this[string name]
        {
            get
            {
                lock (_itemsByName)
                    return _itemsByName[name];
            }
        }

        public int Count
        {
            get
            {
                lock (_itemsByName)
                    return _itemsByName.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_itemsByName)
            {
                // TODO : thread safety
                return _itemsByName.Values.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // TODO : thread safety
            lock (_itemsByName)
                return _itemsByName.Values.GetEnumerator();
        }

        public void Add(T item)
        {
            lock (_itemsByName)
                _itemsByName.Add(item.Name, item);
        }

        public void Remove(T item)
        {
            lock (_itemsByName)
                _itemsByName.Remove(item.Name);
        }

        public bool Contains(string key)
        {
            lock (_itemsByName)
                return _itemsByName.ContainsKey(key);
        }

        public bool Contains(T element)
        {
            lock (_itemsByName)
                return _itemsByName.ContainsValue(element);
        }

        public void Clear()
        {
            T[] items;
            lock (_itemsByName)
                items = _itemsByName.Values.ToArray();
            foreach (var item in items)
                item.Dispose();
        }

        public T ByName(string name)
        {
            lock (_itemsByName)
                return _itemsByName[name];
        }

        public T ByNameOrNull(string name)
        {
            lock (_itemsByName)
            {
                if (Contains(name))
                    return ByName(name);
                else
                    return null;
            }
        }

        private Dictionary<string, T> _itemsByName;

    }
}