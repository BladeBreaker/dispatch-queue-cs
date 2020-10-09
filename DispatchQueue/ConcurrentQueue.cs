using System;
using System.Threading;

#nullable enable

namespace Dispatch
{
    public class ConcurrentQueue : IDispatchQueue
    {
        IThreadPool mThreadPool;

        public ConcurrentQueue(IThreadPool threadPool)
        {
            if (threadPool == null)
            {
                throw new ArgumentNullException("threadPool", "Threadpool parameter must not be null");
            }

            mThreadPool = threadPool;
        }

        public void DispatchAsync(object? context, WaitCallback work)
        {
            mThreadPool.QueueWorkItem(work, context);
        }

        public void DispatchAfter(TimeSpan when, object? context, WaitCallback work)
        {

        }
    }
}
