using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#nullable enable

namespace Dispatch
{
    /// <summary>
    /// SerialQueue which takes work to be executed serially on a given thread pool
    /// </summary>
    public class SerialQueue : IDispatchQueue
    {
        #region Internal Declarations

        /// <summary>
        /// Struct to hold all the data required to perform work
        /// </summary>
        private struct WorkData
        {
            public WaitCallback Work;
            public object? Context;
        }

        #endregion


        #region Member Variables

        /// <summary>
        /// TimerQueue to assist with the DispatchAfter functionality
        /// </summary>
        private readonly TimerQueue mTimerQueue;


        /// <summary>
        /// Thread pool interface to submit work to
        /// </summary>
        private readonly IDispatcher mDispatcher;


        /// <summary>
        /// Queue to serialize and hold the work to perform which also avoids locking
        /// </summary>
        private readonly ConcurrentQueue<WorkData> mQueue = new ConcurrentQueue<WorkData>();


        /// <summary>
        /// Flag to indicate if we are running a task currently
        /// This flag will only be modified via Interlocked style Atomic functions
        /// </summary>
        private int mIsTaskRunning = 0;


        /// <summary>
        /// Delegate constructed and held in a member variable to avoid any potential allocations
        /// </summary>
        private readonly WaitCallback mOnExecuteWorkItemFunction;


        /// <summary>
        /// This is a field to hold the current work that we need to perform
        /// holding the next work and Context to avoid boxing the struct into the context param of a callback
        /// This is OK because this is a SerialQueue which guarantees that exactly one task is executed at a time
        /// </summary>
        private WorkData mCurrentWork;

        #endregion


        #region Constructors

        /// <summary>
        /// Constructs a SerialQueue which will schedule work onto the passed thread pool
        /// </summary>
        /// <param name="threadPool">The thread pool that this SerialQueue will post work to. Must not be null</param>
        public SerialQueue(IDispatcher dispatcher)
        {
            mDispatcher = dispatcher ?? throw new ArgumentNullException("dispatcher");
            mTimerQueue = new TimerQueue(this, dispatcher);
            mOnExecuteWorkItemFunction = OnExecuteWorkItem;
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


        /// <summary>
        /// Will dispatch the work delegate to the thread pool once all the previously dispatched work is completed
        /// </summary>
        /// <param name="context">User data to pass to the work delegate</param>
        /// <param name="work">Delegate which will perform the work. Must not be null</param>
        public void DispatchAsync(object? context, WaitCallback work)
        {
            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            mQueue.Enqueue(new WorkData { Work = work, Context = context });

            // try to dequeue and run a task if there isn't one running already.
            AttemptDequeue();
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Will attempt to Dequeue some work if there is none running
        /// </summary>
        private void AttemptDequeue()
        {
            if (!mQueue.IsEmpty)
            {
                // only dequeue and run the next task in the queue if there are no running tasks already...

                int previousValue = Interlocked.CompareExchange(ref mIsTaskRunning, 1, 0);
                if (previousValue == 0)
                {
                    // nothing was running, and we have some tasks in the queue.
                    // Lets grab the next task and schedule it

                    if (mQueue.TryDequeue(out WorkData work))
                    {
                        mCurrentWork = work;
                        mDispatcher.QueueWorkItem(mOnExecuteWorkItemFunction, null);
                    }
                }
            }
        }


        /// <summary>
        /// Internal callback from the thread pool to perform some work
        /// </summary>
        /// <param name="context">User data to pass to the work delegate</param>
        private void OnExecuteWorkItem(object? context)
        {
            mCurrentWork.Work(mCurrentWork.Context);

            _ = Interlocked.Exchange(ref mIsTaskRunning, 0);

            AttemptDequeue();
        }

        #endregion
    }
}
