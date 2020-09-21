using System;
using System.Threading.Tasks;

#nullable enable

namespace Dispatch
{
    public interface IDispatchQueue
    {
        public void DispatchAsync(Action task);
    }


    public static class IDispatchQueueExtensionMethods
    {
        public static void DispatchSync(this IDispatchQueue queue, Action? task)
        {
            TaskCompletionSource<object?> tcs = new TaskCompletionSource<object?>();

            queue.DispatchAsync(() =>
            {
                task?.Invoke();
                tcs.SetResult(null);
            });

            tcs.Task.Wait();
        }
    }
}
