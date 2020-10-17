#nullable enable

namespace Dispatch
{
    public class ManagedThreadPoolDispatcher : IDispatcher
    {
        public ManagedThreadPoolDispatcher()
        {
        }

        public void QueueWorkItem(System.Threading.WaitCallback work, object? context)
        {
            _ = System.Threading.ThreadPool.QueueUserWorkItem(work, context);
        }
    }
}
