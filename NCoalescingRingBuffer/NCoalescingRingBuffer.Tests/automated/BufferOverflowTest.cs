using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace NCoalescingRingBuffer.Tests.automated
{
    [TestFixture]
    public class BufferOverflowTest
    {
        private const String PoisonPill = "-1";
        public static int CAPACITY = 100;

        private bool _hasOverflowed;

        [Test]
        public void ShouldBeAbleToReuseCapacity()
        {
            ICoalescingBuffer<int, String> buffer = new CoalescingRingBuffer<int, String>(32);

            var producer = new Thread(
                () =>
                {
                    for (var run = 0; run < 1000000; run++)
                    {
                        for (var message = 0; message < 10; message++)
                        {
                            var success = buffer.Offer(message, message.ToString());

                            if (!success)
                            {
                                _hasOverflowed = true;
                                buffer.Offer(PoisonPill);
                                return;
                            }
                        }
                    }

                    buffer.Offer(PoisonPill);
                });


            var consumer = new Thread(
                () =>
                    {
                        var values = new List<String>(CAPACITY);
                        while (true)
                        {
                            buffer.Poll(values, CAPACITY);
                            if (values.Contains(PoisonPill))
                            {
                                return;
                            }
                        }
                    });

            producer.Start();
            consumer.Start();

            producer.Join();
            Assert.IsFalse(_hasOverflowed, "ring buffer has overflowed");
        }
    }
}
