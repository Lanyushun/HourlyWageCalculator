using System;
using System.Threading.Tasks;

namespace demo2
{
    // 扩展Task类的功能
    public static class TaskExtensions
    {
        // 允许安全地执行异步操作而不阻塞UI线程，同时处理异常
        public static void SafeFireAndForget(this Task task, bool continueOnCapturedContext = false, Action<Exception> onException = null)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    // 如果有提供异常处理器，则调用它
                    onException?.Invoke(t.Exception.InnerException ?? t.Exception);

                    // 输出到控制台以便调试
                    Console.WriteLine($"任务执行失败: {t.Exception}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}