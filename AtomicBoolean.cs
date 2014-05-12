using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.Cache.ConcurrentPriorityQueue
{
    using System.Threading;

    public class AtomicBoolean
    {
        private const int VALUE_TRUE = 1;
        private const int VALUE_FALSE = 0;

        private int _currentValue;

        public AtomicBoolean(bool initialValue)
        {
            _currentValue = BoolToInt(initialValue);
        }

        private int BoolToInt(bool value)
        {
            return value ? VALUE_TRUE : VALUE_FALSE;
        }

        private bool IntToBool(int value)
        {
            return value == VALUE_TRUE;
        }

        public bool Value
        {
            get
            {
                return IntToBool(Interlocked.Add(
                ref _currentValue, 0));
            }
        }

        public bool SetValue(bool newValue)
        {
            return IntToBool(
            Interlocked.Exchange(ref _currentValue,
            BoolToInt(newValue)));
        }

        public bool CompareAndSet(bool expectedValue,
            bool newValue)
        {
            int expectedVal = BoolToInt(expectedValue);
            int newVal = BoolToInt(newValue);
            return (Interlocked.CompareExchange(
            ref _currentValue, newVal, expectedVal) == expectedVal);
        }
    }
}
