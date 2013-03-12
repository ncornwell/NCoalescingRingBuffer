using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NCoalescingRingBuffer.Tests.manual.performance
{
    public class StopWatch
    {
        private readonly CountdownEvent _startingGate = new CountdownEvent(2);
        private Volatile.Long _startTime;
        private Volatile.Long _endTime;

        public void consumerIsReady()
        {
            awaitStart();
        }

        private void awaitStart()
        {
            _startingGate.Signal();

            _startingGate.Wait();
        }

        public void producerIsReady()
        {
            awaitStart();
            _startTime.WriteFullFence(DateTime.Now.ToFileTime());
        }

        public void consumerIsDone()
        {
            _endTime.WriteFullFence(DateTime.Now.ToFileTime());
        }

        public long nanosTaken()
        {
            return _endTime.ReadFullFence() - _startTime.ReadFullFence();
        }
    }

}
