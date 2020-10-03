﻿using System;
using System.Threading.Tasks;

#nullable enable

namespace Dispatch
{
    public interface IDispatchQueue
    {
        public void DispatchAsync(Action work);
    }


    public static class IDispatchQueueExtensionMethods
    {
        public static void DispatchSync(this IDispatchQueue queue, Action? work)
        {
            TaskCompletionSource<object?> tcs = new TaskCompletionSource<object?>();

            queue.DispatchAsync(() =>
            {
                work?.Invoke();
                tcs.SetResult(null);
            });

            tcs.Task.Wait();
        }
    }
}
