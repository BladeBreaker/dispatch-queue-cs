using System;
using System.Threading;

#nullable enable

namespace Dispatch
{
    public class ConcurrentQueue : IDispatchQueue
    {
        private readonly IThreadPool mThreadPool;
        private readonly TimerQueue mTimerQueue;

        public ConcurrentQueue(IThreadPool threadPool)
        {
            mThreadPool = threadPool ?? throw new ArgumentNullException("threadPool");
            mTimerQueue = new TimerQueue(this, threadPool);
        }

        public void DispatchAsync(object? context, WaitCallback work)
        {
            mThreadPool.QueueWorkItem(work, context);
        }

        public void DispatchAfter(TimeSpan when, object? context, WaitCallback work)
        {
            mTimerQueue.DispatchAfter(when, context, work);
        }
    }
}
