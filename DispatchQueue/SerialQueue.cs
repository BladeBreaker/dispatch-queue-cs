﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Dispatch
{
    public class SerialQueue : IDispatchQueue
    {
        private readonly IThreadPool mThreadPool;
        private readonly ConcurrentQueue<Action> mQueue = new ConcurrentQueue<Action>();

        // used only via interlocked exchange, thus it must be an int and not a bool
        private int mIsTaskRunning = 0;

        private WaitCallback mWaitCallback;


        public SerialQueue(IThreadPool threadPool)
        {
            mThreadPool = threadPool;
            mWaitCallback = OnExecuteWorkItem;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        public void DispatchAsync(Action task)
        {
            if (task == null)
            {
                return;
            }
            
            mQueue.Enqueue(task);

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

                    bool wasTaskQueued = false;
                    while (!wasTaskQueued && !mQueue.IsEmpty)
                    {
                        if (mQueue.TryDequeue(out Action? task))
                        {
                            if (task != null)
                            {
                                wasTaskQueued = true;

                                mThreadPool.QueueWorkItem(mWaitCallback, task);
                            }
                        }
                    }

                    // if somehow we never got to queue a task, then set that we're not running.
                    if (!wasTaskQueued)
                    {
                        _ = Interlocked.Exchange(ref mIsTaskRunning, 0);
                    }
                }
            }
        }

        private void OnExecuteWorkItem(object userData)
        {
            Action task = (Action)userData;
            task();

            _ = Interlocked.Exchange(ref mIsTaskRunning, 0);

            AttemptDequeue();
        }
    }
}
