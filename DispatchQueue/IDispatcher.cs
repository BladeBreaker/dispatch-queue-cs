#nullable enable

namespace Dispatch
{
    public interface IDispatcher
    {
        public void QueueWorkItem(System.Threading.WaitCallback work, object? context);
    }
}
