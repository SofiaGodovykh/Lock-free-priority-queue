namespace Kontur.Cache.ConcurrentPriorityQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class PriorityQueue<T>
    {
        public SkipList<T> skipList;

        private int length;

        public int Length
        {
            get { return length; }
        }

        public PriorityQueue()
        {
            length = 0;
            skipList = new SkipList<T>();   
        }

        public bool Add(T value, int score)
        {
            var node = new SkipList<T>.Node<T>(value, score);
            var res = skipList.Add(node);
            if (res)
            {
                Interlocked.Increment(ref length);
                return true;
            }

            return false;
        }

        public T Dequeue()
        {
            var node = skipList.FindAndMarkMin();
            if (node != null)
            {
                skipList.Remove(node);
                Interlocked.Decrement(ref length);
                return node.NodeValue.Value;     
            }
            return default(T);
        }
    }
}
