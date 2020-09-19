using System;

namespace Dispatch
{
    public class ManagedThreadPool : IThreadPool
    {
        public ManagedThreadPool()
        {
        }

        public void QueueWorkItem(Action task)
        {
            _ = System.Threading.ThreadPool.QueueUserWorkItem((_) =>
            {
                task();
            });
        }
    }
}
