using System;
using System.Threading;

#nullable enable

namespace Dispatch
{
    public class ConcurrentQueue : IDispatchQueue
    {
        #region Member Variables

        /// <summary>
        /// TimerQueue to assist with the DispatchAfter functionality
        /// </summary>
        private readonly TimerQueue mTimerQueue;


        /// <summary>
        /// Thread pool interface to submit work to
        /// </summary>
        private readonly IThreadPool mThreadPool;

        #endregion


        #region Constructors

        /// <summary>
        /// Constructs a concurrent queue which will schedule work onto the passed thread pool
        /// </summary>
        /// <param name="threadPool">The thread pool that this SerialQueue will post work to. Must not be null</param>
        public ConcurrentQueue(IThreadPool threadPool)
        {
            mThreadPool = threadPool ?? throw new ArgumentNullException("threadPool");
            mTimerQueue = new TimerQueue(this, threadPool);
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Will dispatch the work delegate to the thread pool immediately
        /// </summary>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAsync(object? context, WaitCallback work)
        {
            mThreadPool.QueueWorkItem(work, context);
        }


        /// <summary>
        /// Will dispatch the work delegate to this queue once at least the specified amount of time has passed
        /// </summary>
        /// <param name="when">Amount of time to wait before submitting the work to this queue</param>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAfter(TimeSpan when, object? context, WaitCallback work)
        {
            mTimerQueue.DispatchAfter(when, context, work);
        }


        /// <summary>
        /// Will dispatch the work delegate to this queue after the current time has passed the specified date
        /// </summary>
        /// <param name="when">Date after which the work will be submitted to this queue</param>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAfter(DateTime when, object? context, WaitCallback work)
        {
            mTimerQueue.DispatchAfter(when, context, work);
        }

        #endregion
    }
}
