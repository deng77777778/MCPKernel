using System.Buffers;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Unity ArrayPool
    /// 减少数组分配
    /// </summary>
    internal static class UnityArrayPool
    {
        public static T[] Rent<T>(int minLength) => ArrayPool<T>.Shared.Rent(minLength);

        public static void Return<T>(T[] array)
        {
            if (array == null) return;
            ArrayPool<T>.Shared.Return(array);
        }

        public static void Return<T>(T[] array, bool clearArray)
        {
            if (array == null) return;
            ArrayPool<T>.Shared.Return(array, clearArray);
        }
    }
}
