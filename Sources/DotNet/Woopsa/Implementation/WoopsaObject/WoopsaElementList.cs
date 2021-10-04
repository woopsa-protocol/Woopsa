using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Woopsa
{
    public class WoopsaElementList<T> : IWoopsaElementReadOnlyList<T> where T : WoopsaElement
    {
        #region Constructors

        public WoopsaElementList()
        {
            _itemsByName = new Dictionary<string, T>();
        }

        #endregion

        #region Fields / Attributes

        private Dictionary<string, T> _itemsByName;

        #endregion

        #region Public Methods

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

        #endregion

        #region Private Methods

        IEnumerator IEnumerable.GetEnumerator()
        {
            // TODO : thread safety
            lock (_itemsByName)
                return _itemsByName.Values.GetEnumerator();
        }

        #endregion
    }
}