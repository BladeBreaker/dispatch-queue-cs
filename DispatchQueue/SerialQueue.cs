using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        private WaitCallback mExecuteCurrentWorkItem;

        // holding the next work and Context to avoid boxing the struct into the context param of mWaitCallback
        // This is OK because this is a SerialQueue which guarantees that exactly one task is executed at a time
        private WorkData mCurrentWork;


        public SerialQueue(IThreadPool threadPool)
        {
            if (threadPool == null)
            {
                throw new ArgumentNullException("threadPool", "Threadpool parameter must not be null");
            }

            mThreadPool = threadPool;
            mExecuteCurrentWorkItem = OnExecuteWorkItem;
            mTimer = new Timer(OnTimerExecute);
            mScheduleWorkForExecutionCallback = OnScheduleWorkForExecution;
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
                        mThreadPool.QueueWorkItem(mExecuteCurrentWorkItem, null);
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




        private struct TimerQueueData
        {
            public DateTime TargetTime;
            public WaitCallback Work;
            public object? Context;
        }

        //mTimerQueueData is used across multiple threads and must be locked
        // all TimerQueueData's added to this list will be ordered on insertion from oldest TargetTime to earliest
        List<TimerQueueData> mTimerQueueData = new List<TimerQueueData>();
        Timer mTimer;

        WaitCallback mScheduleWorkForExecutionCallback;

        public void DispatchAfter(TimeSpan when, object? context, WaitCallback work)
        {
            if (work == null)
            {
                return;
            }

            TimerQueueData data = new TimerQueueData()
            {
                TargetTime = DateTime.Now + when,
                Work = work,
                Context = context
            };

            // boxed "data" but ... I don't see an alternative
            mThreadPool.QueueWorkItem(mScheduleWorkForExecutionCallback, data); 

            //idea: whenever we dispatch after... we need to then try to reschedule the timer
            // if our current task needs to execute before the task that the timer is currently waiting for
            // to do any of these checks... we would probably need to lock a mutex... I want to avoid that
            // at all costs on the "API" function as it's likely to be the main thread.
            // therefor: we can submit tasks to the threadpool (assume concurrent) to run a check whenever
            // we add new tasks to the timer queue.
            // This can at least guarantee that we check to see if we need to reschedule the timer every time
            // we add a new task

            // unfortunately we will need to box the TimerQueueData when we submit it to the ThreadPool... no way around it.
        }

        class TimerQueueDataComparer : IComparer<TimerQueueData>
        {
            public int Compare(TimerQueueData x, TimerQueueData y)
            {
                return x.TargetTime.CompareTo(y.TargetTime);
            }
        }
        TimerQueueDataComparer mTimerQueueDataComparer = new TimerQueueDataComparer();

        private void OnScheduleWorkForExecution(object context)
        {
            TimerQueueData data = (TimerQueueData)context;

            // this list must never change or be made public
            // if that ever changes, then we'll need to make an object used for locks.
            lock(mTimerQueueData)
            {
                // insert into the list sorted by TargetTime (earlier TargetTime is at the start of the list)
                var index = mTimerQueueData.BinarySearch(data, mTimerQueueDataComparer);
                if (index < 0)
                {
                    index = ~index;
                }
                mTimerQueueData.Insert(index, data);

                DateTime earliestTarget = mTimerQueueData.Last().TargetTime;

                if (data.TargetTime < earliestTarget)
                {
                    mTimer.Change(earliestTarget - DateTime.Now, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private void OnTimerExecute(object context)
        {
            // this list must never change or be made public
            // if that ever changes, then we'll need to make an object used for locks.
            lock (mTimerQueueData)
            {
                // loop through the list and find the timers data that all need to fire now
                // schedule them in order

                for (int i = mTimerQueueData.Count - 1; i >= 0; --i)
                {
                    TimerQueueData data = mTimerQueueData[i];
                    DateTime now = DateTime.Now;
                    if (data.TargetTime <= now)
                    {
                        DispatchAsync(data.Context, data.Work);
                        mTimerQueueData.RemoveAt(i);
                    }
                    else
                    {
                        mTimer.Change(data.TargetTime - now, Timeout.InfiniteTimeSpan);
                        break;
                    }
                }
            }
        }
    }
}
