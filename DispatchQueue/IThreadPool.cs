using System;
namespace Dispatch
{
    public interface IThreadPool
    {
        public void QueueWorkItem(Action task);
    }
}
