using System;

namespace MCP.MiniTask
{
    public interface IPlayerLoopHelper
    {
        bool IsMainThread();
        void PostToMainThread(Action continuation);
    }
}