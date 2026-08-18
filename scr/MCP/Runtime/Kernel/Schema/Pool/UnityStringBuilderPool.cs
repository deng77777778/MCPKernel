using System.Text;
using UnityEngine.Pool;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Unity StringBuilder 池
    /// 使用 UnityEngine.Pool.ObjectPool
    /// </summary>
    internal static class UnityStringBuilderPool
    {
        private static readonly ObjectPool<StringBuilder> _pool = new(
            createFunc: () => new StringBuilder(128),
            actionOnGet: (sb) => sb.Clear(),
            actionOnRelease: (sb) => { if (sb.Length > 1024) sb.Capacity = 1024; sb.Clear(); },
            actionOnDestroy: (sb) => { },
            collectionCheck: false,
            defaultCapacity: 64,
            maxSize: 128
        );

        public static StringBuilder Get() => _pool.Get();

        public static void Release(StringBuilder sb)
        {
            if (sb == null) return;
            _pool.Release(sb);
        }

        public static string BuildAndRelease(StringBuilder sb)
        {
            try
            {
                return sb.ToString();
            }
            finally
            {
                Release(sb);
            }
        }

        public static void Clear()
        {
            _pool.Clear();
        }
    }
}
