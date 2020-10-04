using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

using Dispatch;


#nullable enable

namespace Tests
{
    [TestClass]
    public class SerialQueueTests
    {
        [TestMethod]
        public void AsyncRunsOnSeparateThreads()
        {
            SerialQueue queue = new SerialQueue(new ManagedThreadPool());
            
            int threadId = Thread.CurrentThread.ManagedThreadId;

            for (int i = 0; i < 10; ++i)
            {
                queue.DispatchAsync(null, (_) =>
                {
                    Assert.AreNotEqual(threadId, Thread.CurrentThread.ManagedThreadId);
                });
            }
        }

        [TestMethod]
        public void SyncRunsOnSeparateThreads()
        {
            SerialQueue queue = new SerialQueue(new ManagedThreadPool());

            int threadId = Thread.CurrentThread.ManagedThreadId;

            for (int i = 0; i < 10; ++i)
            {
                queue.DispatchSync(null, (_) =>
                {
                    Assert.AreNotEqual(threadId, Thread.CurrentThread.ManagedThreadId);
                });
            }
        }

        [TestMethod]
        public void SyncRunsSynchronously()
        {
            SerialQueue queue = new SerialQueue(new ManagedThreadPool());

            int numberTest = 0;

            for (int i = 0; i < 100; ++i)
            {
                queue.DispatchSync(null, (_) =>
                {
                    Assert.AreEqual(numberTest++, i);
                });
            }
        }

        // This test ensures that all async calls are executed one at a time
        [TestMethod]
        public void TasksRunOneAtATime()
        {
            SerialQueue queue = new SerialQueue(new ManagedThreadPool());

            bool taskRunning = false;

            for (int i = 0; i < 10; ++i)
            {
                queue.DispatchAsync(null, (_) =>
                {
                    Assert.IsFalse(taskRunning);
                    taskRunning = true;

                    Thread.Sleep(TimeSpan.FromMilliseconds(30));

                    Assert.IsTrue(taskRunning);
                    taskRunning = false;
                });
            }

            queue.DispatchSync(null, (_) =>
            {
                // just to ensure that the tests don't finish prematurely.
            });
        }

        // This test makes sure that all async calls are executed in series
        [TestMethod]
        public void TasksCompletedInProperSequence()
        {
            int numberTest = 0;

            SerialQueue queue = new SerialQueue(new ManagedThreadPool());

            for (int i = 0; i < 100; ++i)
            {
                int localValue = i;
                queue.DispatchAsync(null, (_) =>
                {
                    Assert.AreEqual(numberTest++, localValue);
                });
            }

            queue.DispatchSync(null, (_) =>
            {
                Assert.AreEqual(numberTest, 100);
            });
        }
    }
}
