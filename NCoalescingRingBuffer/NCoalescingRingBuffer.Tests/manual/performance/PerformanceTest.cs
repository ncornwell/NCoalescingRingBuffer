using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NCoalescingRingBuffer;
using NCoalescingRingBuffer.Tests.automated;
using NUnit.Framework;

namespace NCoalescingRingBuffer.Tests.manual.performance
{

    public class PerformanceTest
    {
        private const long BILLION = 1000L*1000L*1000L;
        private static readonly MarketSnapshot PoisonPill = MarketSnapshot.CreateMarketSnapshot(-1, -1, -1);
        private const int NumberOfInstruments = 10;
        private const int SECONDS = 1000;

        private readonly ICoalescingBuffer<long, MarketSnapshot> _buffer;
        private readonly long _numberOfUpdates;

        public PerformanceTest(ICoalescingBuffer<long, MarketSnapshot> buffer, long numberOfUpdates)
        {
            _buffer = buffer;
            _numberOfUpdates = numberOfUpdates;
        }

        public long Run()
        {
            gc();
            Console.WriteLine("testing " + _buffer.GetType() + " with " + _numberOfUpdates + " updates...");
            var stopWatch = new StopWatch();

            var producer = new Producer(_buffer, NumberOfInstruments, _numberOfUpdates, PoisonPill, stopWatch);
            var consumer = new Consumer(_buffer, NumberOfInstruments, PoisonPill, stopWatch);

            var producerThread = new Thread(producer.Run);
            var consumerThread = new Thread(consumer.Run);

            producerThread.Start();
            consumerThread.Start();

            consumerThread.Join();

            return ComputeAndPrintResults(consumer, stopWatch.nanosTaken());
        }

        private void gc()
        {
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                Thread.Sleep(100);
            }
        }

        private long ComputeAndPrintResults(Consumer consumer, long nanosTaken)
        {
            for (int i = 0; i < consumer.LatestSnapshots.Length; i++)
            {
                Console.WriteLine(consumer.LatestSnapshots[i]);
            }

            Console.WriteLine(String.Format("\ntime {0}", nanosTaken / 1000000000.0));

            double compressionRatio = (1.0 * _numberOfUpdates) / consumer.ReadCounter;
            Console.WriteLine(String.Format("compression ratio = {0}", compressionRatio));

            double megaOpsPerSecond = (1000.0 * _numberOfUpdates) / nanosTaken;
            Console.WriteLine(String.Format("mops = {0}", megaOpsPerSecond));

            return Convert.ToInt64(megaOpsPerSecond);
        }

        public static void RunPerfTestMain()
        {
            var results = new long[3];
            var runNumber = 1;

            do
            {
                var result = RunTest(runNumber++, 2 * BILLION);
                Update(results, result);
                Thread.Sleep(5 * SECONDS);

            } while (!AreAllResultsTheSame(results));
        }

        private static long RunTest(int runNumber, long numberOfUpdates)
        {
            var buffer = new CoalescingRingBuffer<long, MarketSnapshot>(1 << 20);
            var test = new PerformanceTest(buffer, numberOfUpdates);

            Console.WriteLine("\n======================================= run " + runNumber + " =======================================\n");
            return test.Run();
        }

        private static void Update(long[] results, long result)
        {
            Buffer.BlockCopy(results, 1, results, 0, results.Length - 1);
            results[results.Length - 1] = result;
        }

        private static bool AreAllResultsTheSame(long[] results)
        {
            var oldestResult = results[0];

            for (int i = 1; i < results.Length; i++)
            {
                var result = results[i];

                if (result != oldestResult)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
