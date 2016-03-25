using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public abstract class WoopsaElement : IWoopsaElement, IDisposable
    {
        #region Constructors

        protected WoopsaElement(WoopsaContainer container, string name)
        {
            Container = container;
            Name = name;
        }

        #endregion

        #region Public Properties

        public WoopsaContainer Container { get; private set; }

        #endregion

        #region IWoopsaElement

        public IWoopsaContainer Owner
        {
            get { return Container; }
        }

        public string Name { get; private set; }

        #endregion IWoopsaElement

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            Container = null;
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
            _items = new List<WoopsaContainer>();
            if (Container != null)
                Container.Add(this);
        }

        #endregion

        #region Public Properties

        public IEnumerable<IWoopsaContainer> Items
        {
            get
            {
                DoPopulate();
                return _items;
            }
        }

        #endregion

        protected virtual void PopulateContainer(IList<WoopsaContainer> items)
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

        internal void Clear()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
                _items[i].Dispose();
            _populated = false;
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (Container != null)
                Container.Remove(this);
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private readonly List<WoopsaContainer> _items;
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

        public IWoopsaValue Value
        {
            get { return _get(this); }
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
            if (Container != null)
                ((WoopsaObject)Container).Remove(this);
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

    public delegate IWoopsaValue WoopsaMethodInvoke(IEnumerable<IWoopsaValue> arguments);

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

        public IWoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments)
        {
            return _methodInvoke(arguments);
        }

        public WoopsaValueType ReturnType { get; private set; }

        public IEnumerable<IWoopsaMethodArgumentInfo> ArgumentInfos { get; private set; }

        #endregion IWoopsaMethod

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (Container != null)
                ((WoopsaObject)Container).Remove(this);
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
            _properties = new List<WoopsaProperty>();
            _methods = new List<WoopsaMethod>();
        }

        #endregion

        #region Public Properties

        public IEnumerable<IWoopsaProperty> Properties
        {
            get
            {
                DoPopulate();
                return _properties;
            }
        }

        public IEnumerable<IWoopsaMethod> Methods
        {
            get
            {
                DoPopulate();
                return _methods;
            }
        }

        #endregion

        protected override void PopulateContainer(IList<WoopsaContainer> items)
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

        internal void ClearProperties()
        {
            for (int i = _properties.Count - 1; i >= 0; i--)
                _properties[i].Dispose();
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

        internal void ClearMethods()
        {
            for (int i = _methods.Count - 1; i >= 0; i--)
                _methods[i].Dispose();
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            ClearProperties();
            ClearMethods();
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private readonly List<WoopsaProperty> _properties;
        private readonly List<WoopsaMethod> _methods;

        #endregion
    }

    public class WoopsaRoot : WoopsaContainer
    {
        public WoopsaRoot()
            : base(null, string.Empty)
        {
        }
    }
}
