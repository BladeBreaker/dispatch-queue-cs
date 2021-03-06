﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

#nullable enable

namespace Dispatch
{
    // This is meant to encapsulate the implementation of Timers for any Dispatch Queue
    // This class is not meant to be used directly by the user of this lib
    internal class TimerQueue
    {
        #region Internal Declarations

        private struct TimerQueueData : IComparable<TimerQueueData>
        {
            public DateTime TargetTime;
            public WaitCallback Work;
            public object? Context;

            public int CompareTo(TimerQueueData other)
            {
                return TargetTime.CompareTo(other.TargetTime);
            }
        }

        private class TimerQueueDataComparer : IComparer<TimerQueueData>
        {
            public int Compare(TimerQueueData x, TimerQueueData y)
            {
                return y.TargetTime.CompareTo(x.TargetTime);
            }
        }

        #endregion


        #region Member Variables

        private readonly TimerQueueDataComparer mTimerQueueDataComparer = new TimerQueueDataComparer();

        private readonly IDispatchQueue mDispatchQueue;
        private readonly IDispatcher mDispatcher;

        //mTimerQueueData is used across multiple threads and must be locked
        // all TimerQueueData's added to this list will be ordered on insertion from oldest TargetTime to earliest
        private readonly List<TimerQueueData> mTimerQueueData = new List<TimerQueueData>();
        private readonly Timer mTimer;

        private readonly WaitCallback mScheduleWorkForExecutionCallback;

        #endregion


        #region Constructors

        public TimerQueue(IDispatchQueue dispatchQueue, IDispatcher dispatcher)
        {
            mDispatchQueue = dispatchQueue ?? throw new ArgumentNullException("dispatchQueue");
            mDispatcher = dispatcher ?? throw new ArgumentNullException("dispatcher");

            mTimer = new Timer(OnTimerExecute);

            mScheduleWorkForExecutionCallback = OnScheduleWorkForExecution;
        }

        #endregion


        #region Public Methods

        public void DispatchAfter(TimeSpan when, object? context, WaitCallback work)
        {
            DispatchAfter(DateTime.Now + when, context, work);
        }

        public void DispatchAfter(DateTime when, object? context, WaitCallback work)
        {
            if (work == null)
            {
                throw new ArgumentNullException("work");
            }

            TimerQueueData data = new TimerQueueData()
            {
                TargetTime = when,
                Work = work,
                Context = context
            };

            // boxed "data" but ... I don't see an alternative
            mDispatcher.QueueWorkItem(mScheduleWorkForExecutionCallback, data);
        }

        #endregion


        #region Private Methods

        private void OnScheduleWorkForExecution(object context)
        {
            TimerQueueData data = (TimerQueueData)context;

            // this list must never change or be made public
            // if that ever changes, then we'll need to make an object used for locks.
            lock (mTimerQueueData)
            {
                // insert into the list sorted by TargetTime (earlier TargetTime is at the end of the list)
                var index = mTimerQueueData.BinarySearch(data, mTimerQueueDataComparer);
                if (index < 0)
                {
                    index = ~index;
                }
                mTimerQueueData.Insert(index, data);

                DateTime earliestTarget = mTimerQueueData.Last().TargetTime;
                if (data.TargetTime < earliestTarget || mTimerQueueData.Count == 1)
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
                        mDispatchQueue.DispatchAsync(data.Context, data.Work);
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

        #endregion
    }
}
