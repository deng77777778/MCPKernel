using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;

namespace MCP.MiniTask.Editor
{
    public class EditorPlayerLoopRunner : PlayerLoopRunnerBase
    {
        private readonly int _mainThreadId;
        private int _subscribed;
        private readonly EditorApplication.CallbackFunction _updateCallback;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EditorPlayerLoopRunner()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _updateCallback = Run;
            AssemblyReloadEvents.beforeAssemblyReload += Unsubscribe;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void RequestRun()
        {
            if (Interlocked.Exchange(ref _subscribed, 1) == 0)
            {
                EditorApplication.update += _updateCallback;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unsubscribe()
        {
            if (Interlocked.Exchange(ref _subscribed, 0) == 1)
            {
                EditorApplication.update -= _updateCallback;
            }
            AssemblyReloadEvents.beforeAssemblyReload -= Unsubscribe;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMainThread() => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush() => Run();
    }
}