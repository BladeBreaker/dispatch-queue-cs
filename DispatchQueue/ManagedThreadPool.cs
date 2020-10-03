#nullable enable

namespace Dispatch
{
    public class ManagedThreadPool : IThreadPool
    {
        public ManagedThreadPool()
        {
        }

        public void QueueWorkItem(System.Threading.WaitCallback work, object? context)
        {
            _ = System.Threading.ThreadPool.QueueUserWorkItem(work, context);
        }
    }
}
