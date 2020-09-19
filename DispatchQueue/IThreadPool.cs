#nullable enable

namespace Dispatch
{
    public interface IThreadPool
    {
        public void QueueWorkItem(System.Threading.WaitCallback task, object? userdata);
    }
}
