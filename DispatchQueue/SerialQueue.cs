using System;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

namespace Dispatch
{
    /// <summary>
    /// <para> SerialQueue which takes work to be executed serially on a given threadpool </para>
    /// This implementation attempts to avoid allocations wherever possible 
    /// </summary>
    public class SerialQueue : IDispatchQueue
    {
        private struct WorkData
        {
            public WaitCallback Work;
            public object? Context;
        }

        private readonly IThreadPool mThreadPool;
        private readonly ConcurrentQueue<WorkData> mQueue = new ConcurrentQueue<WorkData>();

        // used only via interlocked exchange, thus it must be an int and not a bool
        private int mIsTaskRunning = 0;

        private WaitCallback mWaitCallback;

        // holding the next work and Context to avoid boxing the struct into the context param of mWaitCallback
        // This is OK because this is a SerialQueue which guarantees that exactly one task is executed at a time
        private WorkData mCurrentWork;


        public SerialQueue(IThreadPool threadPool)
        {
            mThreadPool = threadPool;
            mWaitCallback = OnExecuteWorkItem;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        public void DispatchAsync(object? context, WaitCallback work)
        {
            // can't allow nulls in our queue
            if (work == null)
            {
                return;
            }

            mQueue.Enqueue(new WorkData { Work = work, Context = context });

            // try to dequeue and run a task if there isn't one running already.
            AttemptDequeue();
        }

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
                        mThreadPool.QueueWorkItem(mWaitCallback, null);
                    }
                }
            }
        }

        private void OnExecuteWorkItem(object? context)
        {
            mCurrentWork.Work(mCurrentWork.Context);

            _ = Interlocked.Exchange(ref mIsTaskRunning, 0);

            AttemptDequeue();
        }
    }
}
