namespace Kontur.Cache.ConcurrentPriorityQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class AtomicReference<T> where T : class
    {
        private T value;

        public AtomicReference()
        {
            this.value = default(T);
        }

        public AtomicReference(T value)
        {
            this.value = value;
        }

        public T Value
        {
            get
            {
                object obj = value;
                return (T)Thread.VolatileRead(ref obj);
            }
        }

        public bool CompareAndExchange(T newValue, T oldValue)
        {
            return object.ReferenceEquals(Interlocked.CompareExchange(ref value, newValue, oldValue), oldValue);
        }
    }

    public class MarkedAtomicReference<T>
    {
        private readonly AtomicReference<Reference> reference;

        public MarkedAtomicReference(T value, bool marked)
        {
            this.reference = new AtomicReference<Reference>(new Reference(value, marked));
        }

        public T Get(ref bool marked)
        {
            marked = this.Marked.Value;
            return this.Value;
        }

        public T Value
        {
            get { return reference.Value.value; }
        }

        public AtomicBoolean Marked
        {
            get { return reference.Value.marked; }
        }

        public bool CompareAndExchange(T newValue, bool newMarked, T oldValue, bool oldMarked)
        {
            var oldReference = reference.Value;

            if (!object.ReferenceEquals(oldReference.value, oldValue))
            { 
                return false;
            }

            if (oldReference.marked.Value != oldMarked) // todo check if it is correct to compare by value
            {
                return false;
            }

            return reference.CompareAndExchange(new Reference(newValue, newMarked), oldReference);
        }

        private class Reference
        {
            public T value;
            public AtomicBoolean marked;

            public Reference(T value, bool marked)
            {
                this.value = value;
                this.marked = new AtomicBoolean(marked);
            }
        }
    }
}
