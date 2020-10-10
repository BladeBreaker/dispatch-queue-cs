# DispatchQueue

A simple implementation of Apple's Grand Central Dispatch in C# with an interest in creating as few allocations as possible.
This implementation also avoids locks where possible.

# Example Code

```C#
// create a serial queue and give it a Thread pool (anything inheriting from IThreadPool, ManagedThreadPool is provided as a default implementation)
SerialQueue queue = new SerialQueue(new ManagedThreadPool());

// push some work to the queue
queue.DispatchAsync(null, (_) =>
{
    Console.WriteLine("This is executed on a worker thread");
});

queue.DispatchAsync(null, (_) =>
{
    Console.WriteLine("This is also executed on a worker thread");
});
```
