#nullable enable

namespace Dispatch
{
    public class ManagedThreadPool : IThreadPool
    {
        public ManagedThreadPool()
        {
        }

        public void QueueWorkItem(System.Threading.WaitCallback task, object? userData)
        {
            _ = System.Threading.ThreadPool.QueueUserWorkItem(task, userData);
        }
    }
}
