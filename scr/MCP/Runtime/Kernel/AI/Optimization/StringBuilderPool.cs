using System;
using System.Collections.Concurrent;
using System.Text;

namespace MCP.AI
{
    /// <summary>
    /// 轻量级 StringBuilder 池（兼容 Unity）
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly ConcurrentStack<StringBuilder> _pool = new();
        private const int MaxPoolSize = 64;
        private const int DefaultCapacity = 256;
        private const int MaxCapacity = 4096;

        public static StringBuilder Rent()
        {
            return Rent(DefaultCapacity);
        }

        public static StringBuilder Rent(int minimumCapacity)
        {
            if (_pool.TryPop(out var sb))
            {
                sb.Clear();
                if (sb.Capacity < minimumCapacity)
                {
                    sb.Capacity = Math.Min(Math.Max(minimumCapacity, sb.Capacity * 2), MaxCapacity);
                }
                return sb;
            }
            return new StringBuilder(Math.Min(minimumCapacity, MaxCapacity));
        }

        public static void Return(StringBuilder sb)
        {
            if (sb == null) return;

            // 只回收较小容量的 StringBuilder，避免内存浪费
            if (_pool.Count < MaxPoolSize && sb.Capacity <= MaxCapacity)
            {
                // 如果容量太大但实际使用很少，收缩容量
                if (sb.Length < sb.Capacity / 4 && sb.Capacity > 1024)
                {
                    sb.Capacity = Math.Max(sb.Length * 2, 256);
                }
                sb.Clear();
                _pool.Push(sb);
            }
        }

        public static string ToStringAndReturn(StringBuilder sb)
        {
            if (sb == null) return string.Empty;
            var result = sb.ToString();
            Return(sb);
            return result;
        }

        public static void Clear() => _pool.Clear();
    }

}