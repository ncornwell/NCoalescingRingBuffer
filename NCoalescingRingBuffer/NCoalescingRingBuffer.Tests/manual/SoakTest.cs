using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NCoalescingRingBuffer.Tests.manual;
using NUnit.Framework;

namespace NCoalescingRingBuffer.Tests.manual
{

    public class SoakTest
    {
        private const int TimeUpdate = 1;
        private const int SizeUpdate = 2;

        private static ICoalescingBuffer<int, String> _buffer = new CoalescingRingBuffer<int, String>(8);

        private class Producer
        {
            public void Run()
            {
                long messagesSent = 0;
                long lastCountTime = DateTime.Now.ToFileTime();

                while (true)
                {
                    put(TimeUpdate, DateTime.Now.ToString());
                    put(SizeUpdate, "buffer size = " + _buffer.Size());
                    messagesSent += 2;

                    long now = DateTime.Now.ToFileTime();
                    if (now > lastCountTime + 10000)
                    {
                        lastCountTime = now;
                        put("sent " + ++messagesSent + " messages");
                    }
                }
            }

            private void put(String message)
            {
                bool success = _buffer.Offer(message);
                if (!success)
                {
                    throw new Exception("offer of " + message + " failed");
                }
            }

            private void put(int key, String value)
            {
                bool success = _buffer.Offer(key, value);
                if (!success)
                {
                    throw new Exception("offer of " + key + " = " + value + " + failed");
                }
            }
        }

        private class Consumer
        {
            public void Run()
            {
                var messages = new List<String>();

                while (true)
                {
                    _buffer.Poll(messages, 10);
                    foreach (String message in messages)
                    {
                        Console.WriteLine(message);
                    }
                    messages.Clear();
                    Console.WriteLine("-----------------");

                    Thread.Sleep(1000);
                }
            }

        }

        [Test]
        public void RunSoakTest()
        {
            var producer = new Producer();
            var consumer = new Consumer();

            var producerThread = new Thread(producer.Run);
            var consumerThread = new Thread(consumer.Run);

            producerThread.Start();
            consumerThread.Start();

            consumerThread.Join();
        }


    }
}
