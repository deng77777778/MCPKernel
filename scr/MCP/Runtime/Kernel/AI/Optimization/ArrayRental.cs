#nullable enable
using System;
using System.Buffers;

namespace MCP.AI
{
    /// <summary>
    /// 带自动归还的数组租用器（使用 System.Buffers.ArrayPool）
    /// </summary>
    public struct ArrayRental<T> : IDisposable
    {
        private T[]? _array;
        private readonly ArrayPool<T> _pool;
        private readonly bool _clearOnReturn;

        public ArrayRental(int minimumLength, ArrayPool<T>? pool = null, bool clearOnReturn = true)
        {
            _pool = pool ?? ArrayPool<T>.Shared;
            _clearOnReturn = clearOnReturn;
            _array = _pool.Rent(minimumLength);
        }

        public T[] Array
        {
            get
            {
                return _array ?? System.Array.Empty<T>();
            }
        }

        public readonly int Length => _array?.Length ?? 0;

        public void Dispose()
        {
            if (_array != null)
            {
                if (_clearOnReturn)
                {
                    System.Array.Clear(_array, 0, _array.Length);
                }
                _pool.Return(_array);
                _array = null;
            }
        }

        public readonly Span<T> AsSpan()
        {
            return _array is not null ? _array.AsSpan(0, _array.Length) : Span<T>.Empty;
        }

        public readonly Memory<T> AsMemory() => _array?.AsMemory(0, _array.Length) ?? Memory<T>.Empty;
    }
}