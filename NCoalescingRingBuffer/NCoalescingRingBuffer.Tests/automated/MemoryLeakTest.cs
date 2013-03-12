using System;
using System.Threading;
using NUnit.Framework;
using System.Collections.Generic;

namespace NCoalescingRingBuffer.Tests.automated
{
    [TestFixture]
    public class MemoryLeakTest
    {
        private static Volatile.Integer _counter = new Volatile.Integer(0);

        private class CountingKey
        {
            private readonly int _id;

            public CountingKey(int id)
            {
                _id = id;
            }

            public override bool Equals(Object obj)
            {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;

                var other = (CountingKey)obj;
                return _id == other._id;
            }

            public override int GetHashCode()
            {
                return _id;
            }

            ~CountingKey()
            {
                _counter.AtomicIncrementAndGet();
            }
        }

        private class CountingValue
        {
            
            ~CountingValue()
            {
                _counter.AtomicIncrementAndGet();
            }
        }

        [Test]
        public void ShouldNotHaveMemoryLeaks()
        {
            //Have to run outside test method for objects to be claimed by GC
            RunMemoryLeakTest();

            Thread.Sleep(100);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.AreEqual(8, _counter.ReadFullFence());
        }

        public void RunMemoryLeakTest()
        {

            ICoalescingBuffer<CountingKey, CountingValue> buffer = new CoalescingRingBuffer<CountingKey, CountingValue>(16);
            buffer.Offer(new CountingValue());

            buffer.Offer(new CountingKey(1), new CountingValue());
            buffer.Offer(new CountingKey(2), new CountingValue());
            buffer.Offer(new CountingKey(1), new CountingValue());

            buffer.Offer(new CountingValue());

            Assert.AreEqual(4, buffer.Size());
            buffer.Poll(new List<CountingValue>(), 1);
            buffer.Poll(new List<CountingValue>());
            Assert.True(buffer.IsEmpty());

            buffer.Offer(null); // to trigger the clean

        }
    }
}
