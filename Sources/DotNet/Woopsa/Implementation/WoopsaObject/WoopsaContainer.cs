using System.Collections.Generic;
using System.Linq;

namespace Woopsa
{
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

        #region Internal Properties

        internal object Lock { get; }

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
        IEnumerable<IWoopsaContainer> IWoopsaContainer.Items => Items;

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

        #region Protected Methods

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

        #endregion

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

        #region Private Members

        private readonly WoopsaElementList<WoopsaContainer> _items;
        private bool _populated;

        #endregion
    }
}