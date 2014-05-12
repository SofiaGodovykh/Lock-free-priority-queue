namespace Kontur.Cache.ConcurrentPriorityQueue
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static Cache.ConcurrentPriorityQueue.PriorityQueue<int> concurrentQueue = new Cache.ConcurrentPriorityQueue.PriorityQueue<Int32>();
        static int count = 500000;
        static int threadcount = 16;
        static int counter = 0;
        static ConcurrentQueue<int> keys = new ConcurrentQueue<int>();
        static Random rand = new Random();
        static List<Thread> threads = new List<Thread>();
        static ConcurrentStack<int> check = new ConcurrentStack<int>();
        static object locking = new object();
        

        static void Add()
        {
            int i;
            while (counter < count - 1)
            {
                Interlocked.Increment(ref counter);
                if (keys.TryDequeue(out i))
                {
                    concurrentQueue.Add(i, i);
                }
                else
                {
                    Console.WriteLine("Something goes wrong");
                }
                //Console.WriteLine("thread {0}, counter {1}", Thread.CurrentThread.Name, counter);
            }
        }

        static void Deq()
        {
            while (concurrentQueue.Length != 0)
            {
                lock (locking)
                {
                    if (concurrentQueue.Length != 0)
                    {
                        check.Push(concurrentQueue.Dequeue());
                    }
                }
            }

            Console.WriteLine("Thread" + Thread.CurrentThread.Name + " is done");
        }

        static void Main(string[] args)
        {
            for (int i = 0; i < threadcount; i++)
            {
                var t = new Thread(Add);
                t.Name = i.ToString();
                threads.Add(t);
            }

            int r;

            while(keys.Count != count)
            {
                r = rand.Next();
                {
                    keys.Enqueue(r);
                }
            }

            foreach (var v in threads)
            {
                v.Start();
            }

            foreach (var v in threads)
            {
                v.Join();
            }

            Console.WriteLine("done " + concurrentQueue.Length);

            threads = new List<Thread>();
            for (int i = 0; i < threadcount; i++)
            {
                var t = new Thread(Deq);
                t.Name = i.ToString();
                threads.Add(t);
            }

            foreach (var v in threads)
            {
                v.Start();
            }

            foreach (var v in threads)
            {
                v.Join();
            }

            var arr = check.ToArray();
            for (int i = 0; i < arr.Length - 1; i++)
            {
                if (arr[i] < arr[i + 1])
                {
                    Console.WriteLine("Wrong order");
                    Console.ReadLine();
                    return;
                }
            }
            Console.WriteLine("OK");
            Console.ReadLine();
        }
    }
}
