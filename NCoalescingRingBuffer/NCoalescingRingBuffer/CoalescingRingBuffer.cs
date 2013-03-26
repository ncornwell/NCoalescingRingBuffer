using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace NCoalescingRingBuffer
{
    public class CoalescingRingBuffer<K, V> : ICoalescingBuffer<K, V> where V : class
    {
        private Volatile.Long _nextWrite = new Volatile.Long(1); // the next write index
        private long _lastCleaned = 0; // the last index that was nulled out by the producer
        private Volatile.Long _rejectionCount = new Volatile.Long(0);
        private readonly K[] _keys;
        private readonly Volatile.ReferenceArray<V> _values;

        private readonly K _nonCollapsibleKey;
        private readonly int _mask;
        private readonly int _capacity;

        private Volatile.Long _nextRead = new Volatile.Long(1); // the oldest slot that is is safe to write to
        private Volatile.Long _lastRead = new Volatile.Long(0); // the newest slot that it is safe to overwrite

        public CoalescingRingBuffer(int capacity)
        {
            _capacity = NextPowerOfTwo(capacity);
            _mask = _capacity - 1;

            _keys = new K[_capacity];

            _values = new Volatile.ReferenceArray<V>(_capacity);
        }

        private static int NextPowerOfTwo(int value)
        {
            int power = 1;
            while (power < value)
                power *= 2;

            return power;
        }

        public int Size()
        {
            return (int)(_nextWrite.ReadFullFence() - _lastRead.ReadFullFence() - 1);
        }

        public int Capacity()
        {
            return _capacity;
        }

        public long RejectionCount()
        {
            return _rejectionCount.ReadFullFence();
        }

        public long NextWrite()
        {
            return _nextWrite.ReadFullFence();
        }

        public long NextRead()
        {
            return _nextRead.ReadFullFence();
        }

        public bool IsEmpty()
        {
            return _nextRead.ReadFullFence() == _nextWrite.ReadFullFence();
        }

        public bool IsFull()
        {
            return Size() == _capacity;
        }

        public bool Offer(K key, V value)
        {
            long nextWrite = _nextWrite.ReadFullFence();

            for (long readPosition = _nextRead.ReadFullFence(); readPosition < nextWrite; readPosition++)
            {
                int index = Mask(readPosition);

                if (key.Equals(_keys[index]))
                {
                    _values.AtomicExchange(index, value);

                    if (_nextRead.ReadFullFence() <= readPosition)
                    {  // check that the reader has not read it yet
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return Add(key, value);
        }

        public bool Offer(V value)
        {
            return Add(_nonCollapsibleKey, value);
        }

        private bool Add(K key, V value)
        {
            if (IsFull())
            {
                _rejectionCount.AtomicIncrementAndGet();
                return false;
            }

            CleanUp();
            Store(key, value);
            return true;
        }

        private void CleanUp()
        {
            var lastRead = _lastRead.ReadFullFence();
            var lastCleaned = _lastCleaned;

            if (lastRead == lastCleaned)
            {
                return;
            }

            while (lastCleaned < lastRead)
            {
                var index = Mask(++lastCleaned);
                _keys[index] = default(K);
                _values.WriteCompilerOnlyFence(index, null);
            }
            
            _lastCleaned = lastRead;
        }

        private void Store(K key, V value)
        {
            long nextWrite = _nextWrite.ReadFullFence();
            int index = Mask(nextWrite);

            _keys[index] = key;
            _values.WriteFullFence(index, value);

            _nextWrite.WriteFullFence(nextWrite + 1);
        }

        public int Poll(ICollection<V> bucket)
        {
            return Fill(bucket, _nextWrite.ReadFullFence());
        }

        public int Poll(ICollection<V> bucket, int maxItems)
        {
            var claimUpTo = Min(_nextRead.ReadFullFence() + maxItems, _nextWrite.ReadFullFence());
            return Fill(bucket, claimUpTo);
        }

        public static long Min(long a, long b)
        {
            return (a <= b) ? a : b;
        }
        
        private int Fill(ICollection<V> bucket, long claimUpTo)
        {
            _nextRead.WriteFullFence(claimUpTo);
            var lastRead = _lastRead.ReadFullFence();

            for (long readIndex = lastRead + 1; readIndex < claimUpTo; readIndex++)
            {
                int index = Mask(readIndex);
                bucket.Add(_values.ReadFullFence(index));
            }


            _lastRead.WriteCompilerOnlyFence(claimUpTo - 1);

            return (int)(claimUpTo - lastRead - 1);
        }

        private int Mask(long value)
        {
            return ((int)value) & _mask;
        }

    }
}
