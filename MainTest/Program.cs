using System;

namespace MainTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Dispatch.SerialQueue queue = new Dispatch.SerialQueue(new Dispatch.ManagedThreadPool());

            queue.DispatchAsync(() =>
            {
            });

            queue.DispatchAsync(() =>
            {
            });
            queue.DispatchAsync(() =>
            {
            });
            queue.DispatchAsync(() =>
            {
            });
            queue.DispatchAsync(() =>
            {
            });

            queue.DispatchSync(null);
        }
    }
}
