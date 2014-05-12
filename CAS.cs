namespace Kontur.Cache.ConcurrentPriorityQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;

    public class CAS
    {
        //private unsafe class Node<T>
        //{
        //    private int key;
        //    private int level;
        //    private int validLevel;
        //    private T value;
        //    private Node<T>* []next;
        //    private Node<T>* prev;
        //}

        private int[] array;

        private List<Thread> threads;

        private const int NUMBER = 8;

        public CAS(int arraySize, int threadCount)
        {
            array = new int[arraySize];

            threads = new List<Thread>();

            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(this.DoSomething);
                thread.Name = i.ToString();
                threads.Add(thread);
            }

            var t = new Thread(DoSomethingCAS);
            t.Name = "_";
            threads.Add(t);

            foreach (var thread in threads)
            {
                thread.Start();
            }
        }

        private void DoSomething()
        {
            Random rand = new Random(System.DateTime.Now.Millisecond);
            while(true)
            {
                var index = rand.Next(array.Length);
                if (Interlocked.CompareExchange(ref array[index], NUMBER, 0) == 0)
                {
                    Interlocked.CompareExchange(ref array[index], 0, NUMBER);
                }
              //  array[index] = NUMBER;
             //   array[index] = 0;
            }
        }

        private void DoSomethingCAS()
        {
            Random rand = new Random(System.DateTime.Now.Millisecond);
            while (true)
            {
                var index = rand.Next(array.Length);
                if (Interlocked.CompareExchange(ref array[index], NUMBER, NUMBER) == NUMBER)
                {
                    var value = Thread.VolatileRead(ref array[index]);
                    Console.WriteLine(value);
                    Console.WriteLine("CAS is succeed, the change was found at {0} index, the name of thread is {1}", index, Thread.CurrentThread.Name);
                }
            }
        }
    }
}
