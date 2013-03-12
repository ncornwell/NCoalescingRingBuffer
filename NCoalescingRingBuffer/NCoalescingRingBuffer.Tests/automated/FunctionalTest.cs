using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NCoalescingRingBuffer.Tests.automated
{
    public class FunctionalTest
    {
        private static readonly MarketSnapshot VodSnapshot1 = MarketSnapshot.CreateMarketSnapshot(1, 3, 4);
        private static readonly MarketSnapshot VodSnapshot2 = MarketSnapshot.CreateMarketSnapshot(1, 5, 6);
        private static readonly MarketSnapshot BpSnapshot = MarketSnapshot.CreateMarketSnapshot(2, 7, 8);

        protected ICoalescingBuffer<long, MarketSnapshot> Buffer;

        [SetUp]
        public void BeforeEveryTest()
        {
            Buffer = CreateBuffer(16);
        }

        public ICoalescingBuffer<long, MarketSnapshot> CreateBuffer(int capacity)
        {
            return new CoalescingRingBuffer<long, MarketSnapshot>(capacity);
        }

        [Test]
        public void ShouldCorrectlyReportSize()
        {
            ICollection<MarketSnapshot> snapshots = new List<MarketSnapshot>();

            Buffer = CreateBuffer(2);
            Assert.AreEqual(0, Buffer.Size());
            Assert.True(Buffer.IsEmpty());
            Assert.False(Buffer.IsFull());

            Buffer.Offer(BpSnapshot);
            Assert.AreEqual(1, Buffer.Size());
            Assert.False(Buffer.IsEmpty());
            Assert.False(Buffer.IsFull());

            Buffer.Offer(VodSnapshot1.GetInstrumentId(), VodSnapshot1);
            Assert.AreEqual(2, Buffer.Size());
            Assert.False(Buffer.IsEmpty());
            Assert.True(Buffer.IsFull());

            Buffer.Poll(snapshots, 1);
            Assert.AreEqual(1, Buffer.Size());
            Assert.False(Buffer.IsEmpty());
            Assert.False(Buffer.IsFull());

            Buffer.Poll(snapshots, 1);
            Assert.AreEqual(0, Buffer.Size());
            Assert.True(Buffer.IsEmpty());
            Assert.False(Buffer.IsFull());
        }

        [Test]
        public void ShouldRejectNonCollapsibleValueWhenFull()
        {
            Buffer = CreateBuffer(2);
            Buffer.Offer(BpSnapshot);
            Buffer.Offer(BpSnapshot);

            Assert.False(Buffer.Offer(BpSnapshot));
            Assert.AreEqual(2, Buffer.Size());
        }

        [Test]
        public void ShouldRejectNewCollapsibleValueWhenFull()
        {
            Buffer = CreateBuffer(2);
            Buffer.Offer(1L, BpSnapshot);
            Buffer.Offer(2L, VodSnapshot1);

            Assert.False(Buffer.Offer(4L, VodSnapshot2));
            Assert.AreEqual(2, Buffer.Size());
        }

        [Test]
        public void ShouldAcceptNewCollapsibleValueWhenFull()
        {
            Buffer = CreateBuffer(2);
            Buffer.Offer(1L, BpSnapshot);
            Buffer.Offer(2L, BpSnapshot);

            Assert.True(Buffer.Offer(2L, BpSnapshot));
            Assert.AreEqual(2, Buffer.Size());
        }

        [Test]
        public void ShouldReturnOneUpdate()
        {
            AddCollapsibleValue(BpSnapshot);
            AssertContains(BpSnapshot);
        }

        [Test]
        public void ShouldReturnTwoDifferentUpdates()
        {
            AddCollapsibleValue(BpSnapshot);
            AddCollapsibleValue(VodSnapshot1);

            AssertContains(BpSnapshot, VodSnapshot1);
        }

        [Test]
        public void ShouldCollapseTwoCollapsibleUpdatesOnSameTopic()
        {
            AddCollapsibleValue(VodSnapshot1);
            AddCollapsibleValue(VodSnapshot2);

            AssertContains(VodSnapshot2);
        }

        [Test]
        public void ShouldNotCollapseTwoNonCollapsibleUpdatesOnSameTopic()
        {
            AddNonCollapsibleValue(VodSnapshot1);
            AddNonCollapsibleValue(VodSnapshot2);

            AssertContains(VodSnapshot1, VodSnapshot2);
        }

        [Test]
        public void ShouldCollapseTwoUpdatesOnSameTopicAndPreserveOrdering()
        {
            AddCollapsibleValue(VodSnapshot1);
            AddCollapsibleValue(BpSnapshot);
            AddCollapsibleValue(VodSnapshot2);

            AssertContains(VodSnapshot2, BpSnapshot);
        }

        [Test]
        public void ShouldNotCollapseValuesIfReadFastEnough()
        {
            AddCollapsibleValue(VodSnapshot1);
            AssertContains(VodSnapshot1);

            AddCollapsibleValue(VodSnapshot2);
            AssertContains(VodSnapshot2);
        }

        [Test]
        public void ShouldReturnOnlyTheMaximumNumberOfRequestedItems()
        {
            AddNonCollapsibleValue(BpSnapshot);
            AddNonCollapsibleValue(VodSnapshot1);
            AddNonCollapsibleValue(VodSnapshot2);

            var snapshots = new List<MarketSnapshot>();
            Assert.AreEqual(2, Buffer.Poll(snapshots, 2));
            Assert.AreEqual(2, snapshots.Count);
            Assert.AreSame(BpSnapshot, snapshots[0]);
            Assert.AreSame(VodSnapshot1, snapshots[1]);

            snapshots.Clear();
            Assert.AreEqual(1, Buffer.Poll(snapshots, 1));
            Assert.AreEqual(1, snapshots.Count);
            Assert.AreSame(VodSnapshot2, snapshots[0]);

            AssertIsEmpty();
        }

        [Test]
        public void ShouldReturnAllItemsWithoutRequestLimit()
        {
            AddNonCollapsibleValue(BpSnapshot);
            AddCollapsibleValue(VodSnapshot1);
            AddCollapsibleValue(VodSnapshot2);

            var snapshots = new List<MarketSnapshot>();
            Assert.AreEqual(2, Buffer.Poll(snapshots));
            Assert.AreEqual(2, snapshots.Count);

            Assert.AreSame(BpSnapshot, snapshots[0]);
            Assert.AreSame(VodSnapshot2, snapshots[1]);

            AssertIsEmpty();
        }

        [Test]
        public void ShouldCountRejections()
        {
            var buffer2 = new CoalescingRingBuffer<int, Object>(2);
            Assert.AreEqual(0, buffer2.RejectionCount());

            buffer2.Offer(new Object());
            Assert.AreEqual(0, buffer2.RejectionCount());

            buffer2.Offer(1, new Object());
            Assert.AreEqual(0, buffer2.RejectionCount());

            buffer2.Offer(1, new Object());
            Assert.AreEqual(0, buffer2.RejectionCount());

            buffer2.Offer(new Object());
            Assert.AreEqual(1, buffer2.RejectionCount());

            buffer2.Offer(2, new Object());
            Assert.AreEqual(2, buffer2.RejectionCount());
        }

        [Test]
        public void ShouldUseObjectEqualityToCompareKeys()
        {
            var buffer = new CoalescingRingBuffer<String, Object>(2);

            buffer.Offer("boo", new Object());
            buffer.Offer("boo", new Object());

            Assert.AreEqual(1, buffer.Size());
        }

        private void AddCollapsibleValue(MarketSnapshot snapshot)
        {
            Assert.True(Buffer.Offer(snapshot.GetInstrumentId(), snapshot));
        }

        private void AddNonCollapsibleValue(MarketSnapshot snapshot)
        {
            Assert.True(Buffer.Offer(snapshot));
        }

        private void AssertContains(params MarketSnapshot[] expected)
        {
            var actualSnapshots = new List<MarketSnapshot>(expected.Length);

            int readCount = Buffer.Poll(actualSnapshots);
            Assert.AreEqual(expected.Length, readCount);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreSame(expected[i], actualSnapshots[i]);
            }

            Assert.True(Buffer.IsEmpty(), "buffer should now be empty");
        }

        private void AssertIsEmpty()
        {
            AssertContains();
        }

    }
}
