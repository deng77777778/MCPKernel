using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace MCP.MiniTask
{
    public abstract class PlayerLoopRunnerBase
    {
        private readonly ConcurrentQueue<Action> _actions = new();
        private int _isRunning;
        private int _actionCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Action action)
        {
            if (action == null) return;

            _actions.Enqueue(action);
            Interlocked.Increment(ref _actionCount);

            // 使用 Interlocked 避免锁
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
            {
                RequestRun();
            }
        }

        public void Run()
        {
            // 处理所有积压的操作
            while (true)
            {
                if (!_actions.TryDequeue(out var action))
                {
                    // 检查是否还有更多操作
                    if (Interlocked.Decrement(ref _actionCount) <= 0)
                    {
                        Interlocked.Exchange(ref _isRunning, 0);
                        // 二次检查防止遗漏
                        if (_actionCount > 0 && Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0)
                        {
                            continue; // 有新的操作，重新开始
                        }
                        return;
                    }
                    continue;
                }

                try
                {
                    // 执行回调
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public bool IsRunning => _isRunning != 0;
        public int ActionCount => _actionCount;

        public void Clear()
        {
            while (_actions.TryDequeue(out _)) { }
            Interlocked.Exchange(ref _actionCount, 0);
            Interlocked.Exchange(ref _isRunning, 0);
        }

        protected abstract void RequestRun();
    }
}