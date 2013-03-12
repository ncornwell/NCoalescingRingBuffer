using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NCoalescingRingBuffer.Tests.automated;

namespace NCoalescingRingBuffer.Tests.manual.performance
{
    public class Consumer
    {
        private readonly ICoalescingBuffer<long, MarketSnapshot> _buffer;
        private readonly int _numberOfInstruments;
        private readonly MarketSnapshot _poisonPill;
        private readonly StopWatch _stopWatch;

        public readonly MarketSnapshot[] LatestSnapshots;

        public Consumer(ICoalescingBuffer<long, MarketSnapshot> buffer, int numberOfInstruments, MarketSnapshot poisonPill, StopWatch stopWatch)
        {
            _buffer = buffer;
            _numberOfInstruments = numberOfInstruments;
            _poisonPill = poisonPill;
            _stopWatch = stopWatch;
            LatestSnapshots = new MarketSnapshot[numberOfInstruments];
        }

        public long ReadCounter { get; private set; }

        public void Run()
        {
            var bucket = new List<MarketSnapshot>(_numberOfInstruments);
            _stopWatch.consumerIsReady();

            while (true)
            {
                _buffer.Poll(bucket);

                for (int i = 0, snapshotsSize = bucket.Count; i < snapshotsSize; i++)
                {
                    ReadCounter++;

                    MarketSnapshot snapshot = bucket[i];
                    if (snapshot == _poisonPill)
                    {
                        _stopWatch.consumerIsDone();
                        return;
                    }

                    LatestSnapshots[((int)snapshot.GetInstrumentId())] = snapshot;
                }

                SimulateProcessing();
                bucket.Clear();
            }
        }

        private static void SimulateProcessing()
        {
            var sleepUntil = DateTime.Now.ToFileTime() + 10 * 1000;
            while (DateTime.Now.ToFileTime() < sleepUntil)
            {
                // busy spin to simulate processing
            }
        }

    }

}
