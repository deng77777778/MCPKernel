using System;
using UnityEditor;

namespace MCP.MiniTask.Editor
{
    public class EditorPlayerLoopHelper : IPlayerLoopHelper
    {
        private static EditorPlayerLoopHelper _instance;
        private readonly EditorPlayerLoopRunner _runner;

        public static EditorPlayerLoopHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EditorPlayerLoopHelper();
                }
                return _instance;
            }
        }

        private EditorPlayerLoopHelper()
        {
            _runner = new EditorPlayerLoopRunner();
        }

        [InitializeOnLoadMethod]
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
                continuation();
                return;
            }

            _runner.Add(continuation);
        }

        public int GetPendingActionCount() => _runner.ActionCount;
        public bool IsRunning => _runner.IsRunning;
        public void Flush() => _runner.Flush();
        public void Clear() => _runner.Clear();
    }
}
