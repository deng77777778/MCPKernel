using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MCP.MiniTask
{
    public static class PlayerLoopHelperRegistry
    {
        // 使用 ConcurrentStack 替代手动锁
        private static readonly ConcurrentStack<IPlayerLoopHelper> _helpers = new();
        private static IPlayerLoopHelper _currentHelper;
        private static int _initialized;

        public static IPlayerLoopHelper Current
        {
            get
            {
                if (_currentHelper == null)
                {
                    Interlocked.CompareExchange(ref _initialized, 1, 0);
                    _currentHelper = ResolveHelper();
                }
                return _currentHelper;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(IPlayerLoopHelper helper)
        {
            if (helper == null) return;

            _helpers.Push(helper);
            _currentHelper = ResolveHelper();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unregister(IPlayerLoopHelper helper)
        {
            if (helper == null) return;

            var tempStack = new ConcurrentStack<IPlayerLoopHelper>();

            // 重新构建栈
            while (_helpers.TryPop(out var item))
            {
                if (item != helper)
                {
                    tempStack.Push(item);
                }
            }

            // 恢复
            while (tempStack.TryPop(out var item))
            {
                _helpers.Push(item);
            }

            _currentHelper = ResolveHelper();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IPlayerLoopHelper ResolveHelper()
        {
            if (_helpers.TryPeek(out var top))
            {
                return top;
            }

            return RuntimePlayerLoopHelper.Instance;
        }

        public static int HelperCount => _helpers.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRegistered(IPlayerLoopHelper helper)
        {
            return helper != null && _helpers.Contains(helper);
        }
    }
}