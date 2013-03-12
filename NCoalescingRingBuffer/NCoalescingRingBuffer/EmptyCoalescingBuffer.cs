using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCoalescingRingBuffer
{
    public class EmptyCoalescingBuffer<K, V> : ICoalescingBuffer<K, V>
    {
        public static ICoalescingBuffer<K, V> EmptyBuffer<K, V>()
        {
            return new EmptyCoalescingBuffer<K, V>();
        }

        private EmptyCoalescingBuffer() { }

        public int Size()
        {
            return 0;
        }

        public int Capacity()
        {
            return 0;
        }

        public bool IsEmpty()
        {
            return true;
        }

        public bool IsFull()
        {
            return false;
        }

        public bool Offer(K key, V value)
        {
            return false;
        }

        public bool Offer(V value)
        {
            return false;
        }

        public int Poll(ICollection<V> bucket)
        {
            return 0;
        }

        public int Poll(ICollection<V> bucket, int maxItems)
        {
            return 0;
        }

    }
}
