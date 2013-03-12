using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace NCoalescingRingBuffer.Tests.automated
{

    public class ThreadSafetyTest
    {
        private const int NumberOfInstruments = 5000000;
        private const long PoisonPill = -1;

        private const long FirstBid = 3;
        private const long SecondBid = 4;

        private const long FirstAsk = 5;
        private const long SecondAsk = 6;

        private class Producer
        {
            private readonly ICoalescingBuffer<long, MarketSnapshot> _snapshotBuffer;

            public Producer(ICoalescingBuffer<long, MarketSnapshot> snapshotBuffer)
            {
                _snapshotBuffer = snapshotBuffer;
            }

            public void Run()
            {
                for (long key = 0; key < NumberOfInstruments; key++)
                {
                    Put(key, FirstBid, FirstAsk);
                    Put(key, SecondBid, SecondAsk);
                }

                Put(PoisonPill, PoisonPill, PoisonPill);
            }

            private void Put(long key, long bid, long ask)
            {
                var success = _snapshotBuffer.Offer(key, MarketSnapshot.CreateMarketSnapshot(key, bid, ask));
                if (!success)
                {
                    throw new Exception("adding of key " + key + " failed");
                }
            }
        }

        private class Consumer
        {
            private readonly MarketSnapshot[] _snapshots = new MarketSnapshot[NumberOfInstruments];
            private readonly ICoalescingBuffer<long, MarketSnapshot> _snapshotBuffer;
            private bool _useLimitedRead;

            public Consumer(ICoalescingBuffer<long, MarketSnapshot> snapshotBuffer)
            {
                _snapshotBuffer = snapshotBuffer;
            }

            public MarketSnapshot[] Snapshots
            {
                get { return _snapshots; }
            }

            public void Run()
            {
                var bucket = new List<MarketSnapshot>();

                while (true)
                {
                    Fill(bucket);

                    foreach (var snapshot in bucket)
                    {
                        if (snapshot.GetInstrumentId() == PoisonPill)
                        {
                            return;
                        }

                        Snapshots[IndexOf(snapshot)] = snapshot;
                    }

                    bucket.Clear();
                }
            }

            private static int IndexOf(MarketSnapshot snapshot)
            {
                return (int)snapshot.GetInstrumentId();
            }

            private void Fill(ICollection<MarketSnapshot> bucket)
            {
                if (_useLimitedRead)
                {
                    _snapshotBuffer.Poll(bucket, 1);
                }
                else
                {
                    _snapshotBuffer.Poll(bucket);
                }
                _useLimitedRead = !_useLimitedRead;
            }
        }

        [Test]
        public void ShouldSeeLastPrices()
        {
            ICoalescingBuffer<long, MarketSnapshot> buffer = new CoalescingRingBuffer<long, MarketSnapshot>(1 << 20);

            var consumer = new Consumer(buffer);
            var consumerThread = new Thread(consumer.Run);


            new Thread(() => new Producer(buffer).Run()).Start();
            consumerThread.Start();

            consumerThread.Join();

            for (int instrument = 0; instrument < NumberOfInstruments; instrument++)
            {
                MarketSnapshot snapshot = consumer.Snapshots[instrument];

                Assert.AreEqual(SecondBid, snapshot.GetBid(), "bid for instrument " + instrument + ":");
                Assert.AreEqual(SecondAsk, snapshot.GetAsk(), "ask for instrument " + instrument + ":");
            }
        }

    }

}
