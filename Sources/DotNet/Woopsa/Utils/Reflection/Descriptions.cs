using System.Collections;
using System.Collections.Generic;

namespace Woopsa
{
    public class Descriptions<T> : IEnumerable<T> where T : Description
    {
        public Descriptions()
        {
            _items = new List<T>();
            _itemsByName = new Dictionary<string, T>();
        }

        public T this[int index] => _items[index];

        public bool Contains(string name) => _itemsByName.ContainsKey(name);

        public T this[string name] => _itemsByName[name];

        public bool TryGetValue(string name, out T value) => _itemsByName.TryGetValue(name, out value);

        public int Count => _items.Count;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        internal void Add(T item)
        {
            _items.Add(item);
            _itemsByName[item.Name] = item;
        }

        private List<T> _items;
        private Dictionary<string, T> _itemsByName;
    }

}
