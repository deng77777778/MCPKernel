using MCP.Result;
using System;
using System.Collections.Generic;

namespace MCP.Kernel.Registry
{
    public class BaseRegistry<TKey, TValue> : IRegistry<TKey, TValue>
            where TValue : class
    {
        private readonly Dictionary<TKey, TValue> storage = new();
        public int Count { get => storage.Count; }

        public IReadOnlyCollection<TValue> Values => storage.Values;

        public void Register(TKey key, TValue value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            storage[key] = value;
        }

        public IResult<TValue> Resolve(TKey key)
        {
            if (storage.TryGetValue(key, out var val))
            {
                return Results.Ok(val);
            }
            return Results.Bad<TValue>();
        }

        public bool UnRegister(TKey key) => storage.Remove(key);
        public void Clear() => storage.Clear();

    }
}
