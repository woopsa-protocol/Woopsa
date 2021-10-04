using System.Collections;
using System.Collections.Generic;

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
}