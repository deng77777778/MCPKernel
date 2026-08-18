using System;
using UnityEngine;

namespace MCP.MiniTask
{
    public enum PlayerLoopTiming
    {
        Update = 0,
    }

    public class RuntimePlayerLoopHelper : IPlayerLoopHelper
    {
        private static RuntimePlayerLoopHelper _instance;
        private readonly RuntimePlayerLoopRunner _runner;

        public static RuntimePlayerLoopHelper Instance
        {
            get
            {
                _instance ??= new RuntimePlayerLoopHelper();
                return _instance;
            }
        }

        private RuntimePlayerLoopHelper()
        {
            _runner = new RuntimePlayerLoopRunner();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            var instance = Instance;
            PlayerLoopHelperRegistry.Register(instance);
        }

        public bool IsMainThread()
        {
            return _runner.IsMainThread();
        }

        public void PostToMainThread(Action continuation)
        {
            if (continuation == null) return;

            if (IsMainThread())
            {
                continuation.Invoke();
                return;
            }

            _runner.Add(continuation);
        }

        public int GetPendingActionCount() => _runner.ActionCount;
        public bool IsRunning => _runner.IsRunning;
    }
}