namespace Woopsa
{
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

        public bool IsReadOnly { get; }

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
            get => Value;
            set
            {
                CheckDisposed();
                if (!IsReadOnly)
                    _set(this, value);
                else
                    throw new WoopsaException(string.Format("Cannot set read-only property {0}", Name));
            }
        }

        public WoopsaValueType Type { get; }

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
}