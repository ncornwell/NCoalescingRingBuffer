using System;
using NCoalescingRingBuffer.Tests.automated;

namespace NCoalescingRingBuffer.Tests.manual.performance
{
    public class Producer
    {
        private readonly ICoalescingBuffer<long, MarketSnapshot> _buffer;
        private readonly long _numberOfUpdates;
        private readonly MarketSnapshot _poisonPill;
        private readonly StopWatch _stopWatch;
        private readonly int _numberOfInstruments;
        private readonly MarketSnapshot[] _snapshots;
        private int _nextSnapshot;

        public Producer(ICoalescingBuffer<long, MarketSnapshot> buffer, int numberOfInstruments, long numberOfUpdates, MarketSnapshot poisonPill, StopWatch stopWatch)
        {
            _buffer = buffer;
            _numberOfInstruments = numberOfInstruments;
            _numberOfUpdates = numberOfUpdates;
            _poisonPill = poisonPill;
            _stopWatch = stopWatch;
            _snapshots = CreateSnapshots(numberOfInstruments);
        }

        private static MarketSnapshot[] CreateSnapshots(int numberOfInstruments)
        {
            var snapshots = new MarketSnapshot[numberOfInstruments];

            for (int i = 0; i < numberOfInstruments; i++)
            {
                int bid = numberOfInstruments * i;
                int ask = numberOfInstruments * numberOfInstruments * i;

                snapshots[i] = MarketSnapshot.CreateMarketSnapshot(i, bid, ask);
            }

            return snapshots;
        }

        public void Run()
        {
            _stopWatch.producerIsReady();

            for (long i = 1; i <= _numberOfUpdates; i++)
            {
                Put(NextId(i), NextSnapshot());
            }

            Put(_poisonPill.GetInstrumentId(), _poisonPill);
        }

        /**
         * simulates some instruments update much more frequently than others
         */
        private long NextId(long counter)
        {
            var register = (int)counter;

            for (int i = 1; i < _numberOfInstruments; i++)
            {
                if ((register & 1) == 1)
                {
                    return i;
                }

                register >>= 1;
            }

            return _numberOfInstruments;
        }

        private MarketSnapshot NextSnapshot()
        {
            if (_nextSnapshot == _numberOfInstruments)
            {
                _nextSnapshot = 0;
            }

            return _snapshots[_nextSnapshot++];
        }

        private void Put(long id, MarketSnapshot snapshot)
        {
            bool success = _buffer.Offer(id, snapshot);

            if (!success)
            {
                throw new Exception("failed to add instrument id " + snapshot.GetInstrumentId());
            }
        }

    }
}
