#nullable enable

namespace Dispatch
{
    public class ManagedThreadPool : IThreadPool
    {
        public ManagedThreadPool()
        {
        }

        public void QueueWorkItem(System.Threading.WaitCallback work, object? userData)
        {
            _ = System.Threading.ThreadPool.QueueUserWorkItem(work, userData);
        }
    }
}
