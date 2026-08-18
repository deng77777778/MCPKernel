using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine.LowLevel;

namespace MCP.MiniTask
{
    public class RuntimePlayerLoopRunner : PlayerLoopRunnerBase
    {
        private readonly int _mainThreadId;
        private static bool _isInjected;
        private static RuntimePlayerLoopRunner _instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RuntimePlayerLoopRunner()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _instance = this;  // 保存实例引用
            Initialize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Initialize()
        {
            if (_isInjected) return;

            lock (typeof(RuntimePlayerLoopRunner))
            {
                if (_isInjected) return;

                var playerLoop = PlayerLoop.GetDefaultPlayerLoop();
                InjectPlayerLoop(ref playerLoop, typeof(UnityEngine.PlayerLoop.Update));
                PlayerLoop.SetPlayerLoop(playerLoop);
                _isInjected = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InjectPlayerLoop(ref PlayerLoopSystem playerLoop, Type systemType)
        {
            var subSystems = playerLoop.subSystemList;
            for (int i = 0; i < subSystems.Length; i++)
            {
                if (subSystems[i].type != systemType) continue;

                var subSystem = subSystems[i];
                var original = subSystem.updateDelegate;
                subSystem.updateDelegate = () =>
                {
                    original?.Invoke();
                    _instance?.Run();  // 使用实例引用
                };
                subSystems[i] = subSystem;
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void RequestRun()
        {
            // Runtime 通过 PlayerLoop 自动调用 Run
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMainThread() => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
    }
}