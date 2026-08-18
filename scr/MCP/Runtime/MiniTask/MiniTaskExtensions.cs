using System;
using System.Threading;
using UnityEngine;

namespace MCP.MiniTask
{
    // 扩展方法必须在非泛型静态类中定义
    public static class MiniTaskExtensions
    {
        // 1. Forget - 忽略任务结果
        public static void Forget(this MiniTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            else
            {
                awaiter.UnsafeOnCompleted(() =>
                {
                    try { awaiter.GetResult(); }
                    catch (Exception ex) { Debug.LogException(ex); }
                });
            }
        }

        // 2. 从 Unity 的异步操作转换
        public static MiniTask ToMiniTask(this UnityEngine.AsyncOperation asyncOp)
        {
            var tcs = new MiniTaskCompletionSource();
            asyncOp.completed += _ => tcs.TrySetResult();
            return tcs.Task;
        }

        // 3. 从 Unity 的异步操作转换（带进度）
        public static MiniTask ToMiniTask(this UnityEngine.AsyncOperation asyncOp,
            Action<float> onProgress = null)
        {
            var tcs = new MiniTaskCompletionSource();
            asyncOp.completed += _ => tcs.TrySetResult();

            if (onProgress != null)
            {
                // 使用协程或每帧检查进度（简化版本）
                // 实际使用时可以通过 PlayerLoop 实现
            }

            return tcs.Task;
        }

        // 4. 当所有任务完成
        public static MiniTask WhenAll(params MiniTask[] tasks)
        {
            var tcs = new MiniTaskCompletionSource();
            var remaining = tasks.Length;

            if (remaining == 0)
            {
                tcs.TrySetResult();
                return tcs.Task;
            }

            foreach (var task in tasks)
            {
                task.GetAwaiter().UnsafeOnCompleted(() =>
                {
                    try
                    {
                        task.GetResult();
                        if (Interlocked.Decrement(ref remaining) == 0)
                        {
                            tcs.TrySetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
            }

            return tcs.Task;
        }

        // 5. 当任意任务完成
        public static MiniTask WhenAny(params MiniTask[] tasks)
        {
            var tcs = new MiniTaskCompletionSource();
            var completed = false;

            foreach (var task in tasks)
            {
                task.GetAwaiter().UnsafeOnCompleted(() =>
                {
                    if (completed) return;
                    completed = true;
                    try
                    {
                        task.GetResult();
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
            }

            return tcs.Task;
        }
    }
}