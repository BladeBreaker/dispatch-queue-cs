using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

using Dispatch;
using System.Diagnostics;


#nullable enable

namespace Tests
{
    [TestClass]
    public class SerialQueueTests
    {
        private TestContext testContextInstance;

        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

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

        [TestMethod]
        public void TimerQueueActuallyFires()
        {
            SerialQueue queue = new SerialQueue(new ManagedThreadPool());
            AutoResetEvent waitHandle = new AutoResetEvent(false);

            WaitCallback cb = (_) =>
            {
                waitHandle.Set();
            };

            queue.DispatchAfter(TimeSpan.FromMilliseconds(10), null, cb);

            Assert.IsTrue(waitHandle.WaitOne(TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public void TimerQueueFiresInProperOrder()
        {
            SerialQueue queue = new SerialQueue(new ManagedThreadPool());
            AutoResetEvent waitHandle = new AutoResetEvent(false);

            int numberOfExecutions = 0;

            bool firstSucceeded = false;
            bool secondSucceeded = false;
            bool thirdSucceeded = false;

            WaitCallback first = (_) =>
            {
                firstSucceeded = numberOfExecutions == 0;
                numberOfExecutions++;
            };

            WaitCallback second = (_) =>
            {
                secondSucceeded = numberOfExecutions == 1;
                numberOfExecutions++;
            };

            WaitCallback third = (_) =>
            {
                thirdSucceeded = numberOfExecutions == 2;
                numberOfExecutions++;
                waitHandle.Set();
            };

            queue.DispatchAfter(TimeSpan.FromMilliseconds(15), null, first);
            queue.DispatchAfter(TimeSpan.FromMilliseconds(30), null, second);
            queue.DispatchAfter(TimeSpan.FromMilliseconds(45), null, third);

            Assert.IsTrue(waitHandle.WaitOne(TimeSpan.FromSeconds(1)));

            Assert.IsTrue(firstSucceeded);
            Assert.IsTrue(secondSucceeded);
            Assert.IsTrue(thirdSucceeded);
        }
    }
}
