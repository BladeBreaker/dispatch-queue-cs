using System;

#nullable enable

namespace Dispatch
{
    public interface IDispatchQueue
    {
        public void DispatchAsync(Action task);
        public void DispatchSync(Action? task);
    }
}
