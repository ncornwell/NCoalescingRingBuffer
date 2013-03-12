using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCoalescingRingBuffer.Tests.automated
{
    // deliberately mutable to make sure that thread safe does not depend on immutability
    public class MarketSnapshot
    {
        private long _instrumentId;
        private long _bestBid;
        private long _bestAsk;

        public static MarketSnapshot CreateMarketSnapshot(long instrumentId, long bestBid, long bestAsk)
        {
            var snapshot = new MarketSnapshot();
            snapshot.SetInstrumentId(instrumentId);
            snapshot.SetBestBid(bestBid);
            snapshot.SetBestAsk(bestAsk);
            return snapshot;
        }

        public long GetInstrumentId()
        {
            return _instrumentId;
        }

        public void SetInstrumentId(long instrumentId)
        {
            _instrumentId = instrumentId;
        }

        public long GetBid()
        {
            return _bestBid;
        }

        public void SetBestBid(long bestBid)
        {
            _bestBid = bestBid;
        }

        public long GetAsk()
        {
            return _bestAsk;
        }

        public void SetBestAsk(long bestAsk)
        {
            _bestAsk = bestAsk;
        }


        public override String ToString()
        {
            return _instrumentId + ": " + _bestBid + "/" + _bestAsk;
        }
    }
}
