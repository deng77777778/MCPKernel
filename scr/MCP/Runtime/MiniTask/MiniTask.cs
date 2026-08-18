using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace MCP.MiniTask
{
    // 使用静态池避免重复分配
    internal static class MiniTaskPool
    {
        private static readonly object _lock = new object();
        private static MiniTaskCompletionSource _root;
        private static int _poolSize;

        public static MiniTaskCompletionSource Rent()
        {
            lock (_lock)
            {
                if (_root == null) return new MiniTaskCompletionSource();

                var node = _root;
                _root = node.Next;
                node.Next = null;
                _poolSize--;
                return node;
            }
        }

        public static void Return(MiniTaskCompletionSource node)
        {
            node.Reset();
            lock (_lock)
            {
                node.Next = _root;
                _root = node;
                _poolSize++;
            }
        }
    }

    [AsyncMethodBuilder(typeof(AsyncMiniTaskMethodBuilder))]
    public readonly struct MiniTask : ICriticalNotifyCompletion
    {
        private readonly int _token;
        private readonly object _state;

        public MiniTask(int token, object state)
        {
            _token = token;
            _state = state;
        }

        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var status = GetStatus();
                return status != MiniTaskStatus.Pending;
            }
        }

        public bool IsCompletedSuccessfully
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetStatus() == MiniTaskStatus.Succeeded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MiniTaskStatus GetStatus()
        {
            if (_state == null) return MiniTaskStatus.Succeeded;

            if (_state is ExceptionDispatchInfo edi)
            {
                return edi.SourceException is OperationCanceledException
                    ? MiniTaskStatus.Canceled
                    : MiniTaskStatus.Faulted;
            }

            return MiniTaskStatus.Pending;
        }

        public MiniTaskAwaiter GetAwaiter() => new(this);

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation?.Invoke();
                return;
            }

            if (_state is MiniTaskCompletionSource source)
            {
                source.OnCompleted(continuation);
            }
            else
            {
                continuation?.Invoke();
            }
        }

        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            if (_state is ExceptionDispatchInfo edi)
            {
                edi.Throw();
            }
        }

        // ===== 静态 API 方法 - 类似 UniTask =====

        // 1. Yield - 切换到线程池
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YieldAwaitable Yield() => new();

        // 2. SwitchToThreadPool - 切换到线程池
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YieldAwaitable SwitchToThreadPool() => new();

        // 3. SwitchToMainThread - 切换到主线程
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SwitchToMainThreadAwaitable SwitchToMainThread() => new();

        // 4. MainThread - 确保在主线程
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MainThreadAwaitable MainThread() => new();

        // 5. NextFrame - 等待下一帧
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NextFrameAwaitable NextFrame() => new();

        // 6. Delay - 延迟指定毫秒
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelayAwaitable Delay(int millisecondsDelay) => new(millisecondsDelay);

        // 7. RunOnThreadPool - 在线程池执行操作
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RunOnThreadPoolAwaitable RunOnThreadPool(Action action) => new(action);
    }

    public enum MiniTaskStatus
    {
        Pending,
        Succeeded,
        Faulted,
        Canceled
    }

    public readonly struct MiniTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly MiniTask _task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MiniTaskAwaiter(MiniTask task) => _task = task;

        public bool IsCompleted => _task.IsCompleted;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => _task.OnCompleted(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => _task.UnsafeOnCompleted(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() => _task.GetResult();
    }

    public struct AsyncMiniTaskMethodBuilder
    {
        private MiniTaskCompletionSource _core;
        private bool _haveCore;
        private bool _started;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncMiniTaskMethodBuilder Create() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (_started) return;
            _started = true;
            stateMachine.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (_haveCore)
            {
                _core.TrySetResult();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_haveCore)
            {
                _core.TrySetException(exception);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (!_haveCore)
            {
                _core = MiniTaskPool.Rent();
                _haveCore = true;
            }
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (!_haveCore)
            {
                _core = MiniTaskPool.Rent();
                _haveCore = true;
            }
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        public MiniTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _haveCore ? _core.Task : new MiniTask(0, null);
        }
    }

    public class MiniTaskCompletionSource
    {
        private Action _continuation;
        private ExceptionDispatchInfo _exception;
        private volatile int _completed;
        private int _version;
        private static int _versionCounter;

        internal MiniTaskCompletionSource Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MiniTaskCompletionSource()
        {
            _version = Interlocked.Increment(ref _versionCounter);
        }

        public MiniTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_version, _exception ?? (object)_continuation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrySetResult()
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;

            var c = Interlocked.Exchange(ref _continuation, null);
            _exception = null;
            c?.Invoke();

            ReturnToPool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrySetException(Exception exception)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;

            var c = Interlocked.Exchange(ref _continuation, null);
            _exception = ExceptionDispatchInfo.Capture(exception);
            c?.Invoke();

            ReturnToPool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrySetCanceled()
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;

            var c = Interlocked.Exchange(ref _continuation, null);
            _exception = ExceptionDispatchInfo.Capture(new OperationCanceledException());
            c?.Invoke();

            ReturnToPool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            if (_completed != 0)
            {
                continuation?.Invoke();
                return;
            }

            var prev = Interlocked.CompareExchange(ref _continuation,
                (Action)Delegate.Combine(_continuation, continuation),
                _continuation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            _continuation = null;
            _exception = null;
            _completed = 0;
            _version = Interlocked.Increment(ref _versionCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnToPool()
        {
            if (_version % 2 == 0)
            {
                MiniTaskPool.Return(this);
            }
        }
    }
}