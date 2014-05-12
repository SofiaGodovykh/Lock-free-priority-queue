namespace Kontur.Cache.ConcurrentPriorityQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class SkipList<T>
    {
        private const int MAX_LEVEL = 14;

        private static uint randomSeed;

        private static Random random;

        public class Node<T>
        {
            private MarkedAtomicReference<T> nodeValue;

            public MarkedAtomicReference<T> NodeValue
            {
                get
                {
                    return nodeValue;
                }
            }

            private int nodeKey;

            public int NodeKey
            {
                get
                {
                    return nodeKey;
                }
            }

            private MarkedAtomicReference<Node<T>>[] next;

            public MarkedAtomicReference<Node<T>>[] Next
            {
                get
                {
                    return next;
                }
            }

            private int topLevel;

            public int TopLevel
            {
                get
                {
                    return topLevel;
                }
            }

            public Node(int key)
            {
                nodeValue = new MarkedAtomicReference<T>(default(T), false);
                this.nodeKey = key;
                next = new MarkedAtomicReference<Node<T>>[MAX_LEVEL + 1];
                for (int i = 0; i < next.Length; ++i)
                {
                    next[i] = new MarkedAtomicReference<Node<T>>(null, false);
                }
                topLevel = MAX_LEVEL;
            }

            public Node(T value, int key)
            {
                nodeValue = new MarkedAtomicReference<T>(value, false);
                this.nodeKey = key;
                var height = RandomLevel();
                next = new MarkedAtomicReference<Node<T>>[height + 1];
                for (int i = 0; i < next.Length; ++i)
                {
                    next[i] = new MarkedAtomicReference<Node<T>>(null, false);
                }
                topLevel = height;
            }
        }

        private readonly Node<T> head = new Node<T>(int.MinValue);

        private readonly Node<T> tail = new Node<T>(int.MaxValue);

        public SkipList()
        {
            random = new Random();

            randomSeed = (uint)(DateTime.Now.Millisecond) | 0x0100;

            for (int i = 0; i < head.Next.Length; ++i)
            {
                head.Next[i] = new MarkedAtomicReference<Node<T>>(tail, false);
            }
        }

        public void ToString()
        {
            var temp = this.head;
            while (temp.Next[0].Value != null)
            {
                Console.WriteLine(temp.Next[0].Value.NodeKey);
                temp = temp.Next[0].Value;
            }
        }

        public bool Add(Node<T> node)
        {
            var preds = new Node<T>[MAX_LEVEL + 1];
            var succs = new Node<T>[MAX_LEVEL + 1];

            while (true)
            {
                var found = this.Find(node, ref preds, ref succs);
                //if (found)
                //{
                //   return false;
                //}
                //else
                {
                    var topLevel = node.TopLevel;
                    var bottomLevel = 0;

                    for (var level = bottomLevel; level <= topLevel; ++level)
                    {
                        var tempSucc = succs[level];
                        node.Next[level] = new MarkedAtomicReference<Node<T>>(tempSucc, false); // todo check if this operation is equal to set in Java
                    }

                    var pred = preds[bottomLevel];
                    var succ = succs[bottomLevel];

                    node.Next[bottomLevel] = new MarkedAtomicReference<Node<T>>(succ, false);

                    if (!pred.Next[bottomLevel].CompareAndExchange(node, false, succ, false))
                    {
                        continue;
                    }

                    for (int level = bottomLevel + 1; level <= topLevel; level++)
                    {
                        while (true)
                        {
                            pred = preds[level];
                            succ = succs[level];

                            if (pred.Next[level].CompareAndExchange(node, false, succ, false))
                            {
                                break;
                            }

                            this.Find(node, ref preds, ref succs);
                        }
                    }
                    return true;
                }
            }
        }

        public bool Remove(Node<T> node)
        {
            int bottomLevel = 0;
            var preds = new Node<T>[MAX_LEVEL + 1];
            var succs = new Node<T>[MAX_LEVEL + 1];
            Node<T> succ;

            while (true)
            {
                bool found = this.Find(node, ref preds, ref succs);
                if (!found)
                {
                    return false;
                }

                else
                {
                    for (int level = node.TopLevel; level > bottomLevel; level--)
                    {
                        bool isMarked = false;
                        succ = node.Next[level].Get(ref isMarked);

                        while (!isMarked)
                        {
                            node.Next[level].CompareAndExchange(succ, true, succ, false);
                            succ = node.Next[level].Get(ref isMarked);
                        }
                    }

                    bool marked = false;
                    succ = node.Next[bottomLevel].Get(ref marked);

                    while (true)
                    {
                        bool iMarkedIt = node.Next[bottomLevel].CompareAndExchange(succ, true, succ, false);
                        succ = succs[bottomLevel].Next[bottomLevel].Get(ref marked);

                        if (iMarkedIt)
                        {
                            this.Find(node, ref preds, ref succs);
                            return true;
                        }
                        else
                        {
                            if (marked)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
        }

        public Node<T> FindAndMarkMin()
        {
            var curr = this.head.Next[0].Value;

            while (curr != tail)
            {
                if (!curr.NodeValue.Marked.Value)
                {
                    //if (curr.Marked.CompareAndSet(false, true))
                    if (curr.NodeValue.Marked.CompareAndSet(false, true))
                    {
                        return curr;
                    }
                    else
                    {
                        curr = curr.Next[0].Value;
                    }
                }
            }
            return null;
        }

        private bool Find(Node<T> node, ref Node<T>[] preds, ref Node<T>[] succs)
        {
            int bottomLevel = 0;
            bool marked = false;
            bool snip = false;
            Node<T> pred = null;
            Node<T> curr = null;
            Node<T> succ = null;
        retry: //TODO: remove goto
            while (true)
            {
                pred = head;
                for (int level = MAX_LEVEL; level >= bottomLevel; level--)
                {
                    curr = pred.Next[level].Value;
                    while (true)
                    {
                        succ = curr.Next[level].Get(ref marked);
                        while (marked)
                        {
                            snip = pred.Next[level].CompareAndExchange(succ, false, curr, false);
                            if (!snip)
                            {
                                goto retry;
                            }

                            curr = pred.Next[level].Value;
                            succ = curr.Next[level].Get(ref marked);
                        }

                        if (curr.NodeKey < node.NodeKey)
                        {
                            pred = curr;
                            curr = succ;
                        }

                        else
                        {
                            break;
                        }
                    }

                    preds[level] = pred;
                    succs[level] = curr;
                }
                return (curr.NodeKey == node.NodeKey);
            }
        }

        private static int RandomLevel()
        {
            uint x = randomSeed;
            x ^= x << 13;
            x ^= x >> 17;
            randomSeed = x ^= x << 5;
            if ((x & 0x80000001) != 0)
            {
                return 0;
            }

            int level = 1;
            while (((x >>= 1) & 1) != 0)
            {
                level++;
            }

            return Math.Min(level, MAX_LEVEL);
        }

        /*public bool Contains(T value)
        {
            int bottomLevel = 0;
            int key = value.GetHashCode();
            bool marked = false;
            Node<T> pred = head;
            Node<T> curr = null;
            Node<T> succ = null;

            for (int level = MAX_LEVEL; level >= bottomLevel; level--)
            {
                curr = pred.Next[level].Value;
                while (true)
                {
                    succ = curr.Next[level].Get(ref marked);
                    while (marked)
                    {
                        curr = pred.Next[level].Value;
                        succ = curr.Next[level].Get(ref marked);
                    }

                    if (curr.NodeKey < key)
                    {
                        pred = curr;
                        curr = succ;
                    }

                    else
                    {
                        break;
                    }
                }
            }
            return (curr.NodeKey == key);
        }*/
    }
}
    
