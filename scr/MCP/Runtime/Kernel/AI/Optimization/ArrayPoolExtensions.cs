#nullable enable
using System;
using System.Buffers;

namespace MCP.AI
{
    /// <summary>
    /// ArrayPool 扩展方法，提供更便捷的使用方式
    /// </summary>
    public static class ArrayPoolExtensions
    {
        /// <summary>
        /// 租用数组并自动清空
        /// </summary>
        public static T[] RentCleared<T>(this ArrayPool<T> pool, int minimumLength)
        {
            var array = pool.Rent(minimumLength);
            // 清空数组以避免引用残留
            Array.Clear(array, 0, Math.Min(array.Length, minimumLength));
            return array;
        }

        /// <summary>
        /// 安全归还数组（处理null和空数组）
        /// </summary>
        public static void SafeReturn<T>(this ArrayPool<T> pool, T[]? array, bool clearArray = true)
        {
            if (array == null || array.Length == 0) return;
            if (clearArray)
            {
                Array.Clear(array, 0, array.Length);
            }
            pool.Return(array);
        }
    }

}