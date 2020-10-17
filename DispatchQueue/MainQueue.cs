using System;
using System.Threading;

#nullable enable

namespace Dispatch
{
    /// <summary>
    /// SerialQueue which takes work to be executed serially.
    /// Work is executed when calling TryDoWork
    /// </summary>
    public class MainQueue : IDispatchQueue
    {
        #region Internal Declarations

        /// <summary>
        /// Class used to intercept the Dispatch calls from the internal Serial Queue
        /// </summary>
        private class WorkHolder : IDispatcher
        {
            WaitCallback? mCurrentWork = null;
            object? mCurrentContext;

            public void QueueWorkItem(WaitCallback work, object? context)
            {
                if (mCurrentWork != null)
                {
                    throw new InvalidOperationException("Queueing work but previous work is not complete");
                }

                mCurrentWork = work;
                mCurrentContext = context;
            }

            public bool TryDoWork()
            {
                if (mCurrentWork == null)
                {
                    return false;
                }

                // make a copy because executing the work will likely cause QueueWorkItem to be called
                WaitCallback workCopy = mCurrentWork;
                mCurrentWork = null;

                workCopy.Invoke(mCurrentContext);

                return true;
            }
        }

        #endregion


        #region Member Variables

        /// <summary>
        /// SerialQueue which will hold all our work to do
        /// </summary>
        private readonly SerialQueue mQueue;


        /// <summary>
        /// WorkHolder that will intercept the Dispatch calls from mQueue
        /// </summary>
        private readonly WorkHolder mWorkHolder = new WorkHolder();

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor for MainQueue
        /// </summary>
        public MainQueue() 
        {
            mQueue = new SerialQueue(mWorkHolder);
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Will dispatch the work delegate to this queue once at least the specified amount of time has passed
        /// </summary>
        /// <param name="when">Amount of time to wait before submitting the work to this queue</param>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAfter(TimeSpan when, object? context, WaitCallback work)
        {
            mQueue.DispatchAfter(when, context, work);
        }


        /// <summary>
        /// Will dispatch the work delegate to this queue after the current time has passed the specified date
        /// </summary>
        /// <param name="when">Date after which the work will be submitted to this queue</param>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAfter(DateTime when, object? context, WaitCallback work)
        {
            mQueue.DispatchAfter(when, context, work);
        }


        /// <summary>
        /// Will dispatch the work delegate to the thread pool once all the previously dispatched work is completed
        /// </summary>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAsync(object? context, WaitCallback work)
        {
            mQueue.DispatchAsync(context, work);
        }


        /// <summary>
        /// Will attempt to dequeue and execute work which has been queued
        /// </summary>
        /// <returns>true if work has been dequeued and done, otherwise false</returns>
        public bool TryDoWork()
        {
            return mWorkHolder.TryDoWork();
        }

        #endregion
    }
}
