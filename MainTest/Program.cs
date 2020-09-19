using System;

namespace MainTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            string null_str = null;
            int? null_int = null;
            int? real_int = 5;

            bool test = null_str is string;          // false
            bool test2 = null_int is int?;           // false
            bool test3 = real_int is int?;           // true
            bool test4 = real_int is int;            // true
            bool test5 = real_int is Nullable<int>;  // true

            Console.WriteLine("test");

            /*
            Dispatch.SerialQueue queue = new Dispatch.SerialQueue(new Dispatch.ManagedThreadPool());

            queue.DispatchAsync(() =>
            {
            });

            queue.DispatchAsync(() =>
            {
            });
            queue.DispatchAsync(() =>
            {
            });
            queue.DispatchAsync(() =>
            {
            });
            queue.DispatchAsync(() =>
            {
            });

            queue.DispatchSync(null);
            */
        }
    }
}
