using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace MCP.MiniTask
{
    // ===== Awaitable 实现 =====

    // 1. Yield - 切换到线程池
    public struct YieldAwaitable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly YieldAwaiter GetAwaiter() => new();

        public struct YieldAwaiter : ICriticalNotifyCompletion
        {
            public readonly bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void OnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action)state)(), continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void UnsafeOnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action)state)(), continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void GetResult() { }
        }
    }

    // 2. SwitchToMainThread - 切换到主线程
    public struct SwitchToMainThreadAwaitable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SwitchToMainThreadAwaiter GetAwaiter() => new();

        public struct SwitchToMainThreadAwaiter : ICriticalNotifyCompletion
        {
            public readonly bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void OnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    PlayerLoopHelperRegistry.Current.PostToMainThread(continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void UnsafeOnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    PlayerLoopHelperRegistry.Current.PostToMainThread(continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void GetResult() { }
        }
    }

    // 3. MainThread - 确保在主线程
    public struct MainThreadAwaitable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly MainThreadAwaiter GetAwaiter() => new();

        public struct MainThreadAwaiter : ICriticalNotifyCompletion
        {
            public readonly bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void OnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    PlayerLoopHelperRegistry.Current.PostToMainThread(continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void UnsafeOnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    PlayerLoopHelperRegistry.Current.PostToMainThread(continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void GetResult() { }
        }
    }

    // 4. NextFrame - 等待下一帧
    public struct NextFrameAwaitable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NextFrameAwaiter GetAwaiter() => new();

        public struct NextFrameAwaiter : ICriticalNotifyCompletion
        {
            public readonly bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void OnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    PlayerLoopHelperRegistry.Current.PostToMainThread(continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void UnsafeOnCompleted(Action continuation)
            {
                if (continuation != null)
                {
                    PlayerLoopHelperRegistry.Current.PostToMainThread(continuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void GetResult() { }
        }
    }

    // 5. Delay - 延迟指定毫秒
    public readonly struct DelayAwaitable
    {
        private readonly int _millisecondsDelay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DelayAwaitable(int millisecondsDelay) => _millisecondsDelay = millisecondsDelay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DelayAwaiter GetAwaiter() => new(_millisecondsDelay);

        public readonly struct DelayAwaiter : ICriticalNotifyCompletion
        {
            private readonly int _millisecondsDelay;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DelayAwaiter(int millisecondsDelay) => _millisecondsDelay = millisecondsDelay;

            public bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                if (continuation == null) return;

                ThreadPool.UnsafeQueueUserWorkItem(static state =>
                {
                    var (delay, cont) = ((int, Action))state;
                    Thread.Sleep(delay);
                    PlayerLoopHelperRegistry.Current.PostToMainThread(cont);
                }, (_millisecondsDelay, continuation));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult() { }
        }
    }

    // 6. RunOnThreadPool - 在线程池执行操作
    public readonly struct RunOnThreadPoolAwaitable
    {
        private readonly Action _action;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RunOnThreadPoolAwaitable(Action action) => _action = action;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RunOnThreadPoolAwaiter GetAwaiter() => new(_action);

        public readonly struct RunOnThreadPoolAwaiter : ICriticalNotifyCompletion
        {
            private readonly Action _action;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RunOnThreadPoolAwaiter(Action action) => _action = action;

            public bool IsCompleted => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                var action = _action;
                var cont = continuation;

                ThreadPool.UnsafeQueueUserWorkItem(static state =>
                {
                    var (act, contAction) = ((Action, Action))state;
                    try
                    {
                        act?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                    finally
                    {
                        contAction?.Invoke();
                    }
                }, (action, cont));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult() { }
        }
    }
}