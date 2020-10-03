using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Dispatch
{
    public interface IDispatchQueue
    {
        public void DispatchAsync(object context, WaitCallback work);
    }


    public static class IDispatchQueueExtensionMethods
    {
        public static void DispatchSync(this IDispatchQueue queue, object context, WaitCallback? work)
        {
            TaskCompletionSource<object?> tcs = new TaskCompletionSource<object?>();

            queue.DispatchAsync(context, (context) =>
            {
                work?.Invoke(context);
                tcs.SetResult(null);
            });

            tcs.Task.Wait();
        }
    }
}
