using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NCoalescingRingBuffer
{
    public interface ICoalescingBuffer<in K, V>
    {

        /**
         * @return the current size of the buffer
         */
        int Size();

        /**
         * @return the maximum size of the buffer
         */
        int Capacity();

        bool IsEmpty();

        bool IsFull();

        /**
         * Add a value to be collapsed on the give key
         *
         * @param key the key on which to collapse the value
         *        equality is determined by the equals method
         * @return true if the value was added or false if the buffer was full
         */
        bool Offer(K key, V value);

        /**
         * Add a value that will never be collapsed
         *
         * @return true if the value was added or false if the buffer was full
         */
        bool Offer(V value);

        /**
         * add all available items to the given bucket
         *
         * @return the number of items added
         */
        int Poll(ICollection<V> bucket);

        /**
         * add a maximum number of items to the given bucket
         *
         * @return the number of items added
         */
        int Poll(ICollection<V> bucket, int maxItems);

    }
}
