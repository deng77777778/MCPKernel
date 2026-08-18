using System;
using System.Collections;
using System.Collections.Generic;

namespace MCP.AI
{
    internal sealed class EmptyReadOnlyList<T> : IReadOnlyList<T>, ICollection<T>
    {
        public static readonly EmptyReadOnlyList<T> Instance = new();
        private readonly Enumerator _enumerator = new();

        public IEnumerator<T> GetEnumerator() => _enumerator;
        IEnumerator IEnumerable.GetEnumerator() => _enumerator;
        public int Count => 0;
        public T this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            // nop
        }

        bool ICollection<T>.Contains(T item) => false;
        bool ICollection<T>.IsReadOnly => true;
        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        bool ICollection<T>.Remove(T item) => false;

        void ICollection<T>.Clear()
        {
            // nop
        }

        internal sealed class Enumerator : IEnumerator<T>
        {
            public void Dispose()
            {
                // nop
            }

            public void Reset()
            {
                // nop
            }

            public bool MoveNext() => false;
            public T Current => throw new InvalidOperationException();
            object IEnumerator.Current => throw new InvalidOperationException();
        }
    }
}
