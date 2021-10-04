using System.Collections.Generic;
using System.Threading.Tasks;

namespace Woopsa
{
    public delegate WoopsaValue WoopsaPropertyGet(IWoopsaProperty property);

    public delegate void WoopsaPropertySet(IWoopsaProperty property, IWoopsaValue value);

    public delegate WoopsaValue WoopsaMethodInvoke(IWoopsaValue[] arguments);

    public delegate Task<WoopsaValue> WoopsaMethodInvokeAsync(IWoopsaValue[] arguments);


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

        IEnumerable<IWoopsaProperty> IWoopsaObject.Properties => Properties;

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

        IEnumerable<IWoopsaMethod> IWoopsaObject.Methods => Methods;

        #endregion

        #region Public Methods

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

        #endregion

        #region Protected Methods

        protected override void PopulateContainer(WoopsaElementList<WoopsaContainer> items)
        {
            base.PopulateContainer(items);
            PopulateObject();
        }

        protected virtual void PopulateObject()
        {
        }

        #endregion

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
}