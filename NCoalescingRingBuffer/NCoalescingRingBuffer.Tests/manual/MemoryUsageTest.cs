using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace NCoalescingRingBuffer.Tests.manual
{
    public class MemoryUsageTest
    {

        public static long CalculateMemoryUsage(Func<Object> factory)
        {
            Object handle = factory();
            long memory = UsedMemory();
            handle = null;
            LotsOfGC();
            memory = UsedMemory();
            handle = factory();
            LotsOfGC();
            return UsedMemory() - memory;
        }

        private static long UsedMemory()
        {
            return GC.GetTotalMemory(false);
        }

        private static void LotsOfGC()
        {
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                Thread.Sleep(100);
            }
        }

        [Test]
        public void RunMemoryTest()
        {
            Func<Object> objFactory = () =>
                             {
                                 var buffers = new CoalescingRingBuffer<Object, Object>[100];
                                 for (int i = 0; i < buffers.Length; i++)
                                 {
                                     buffers[i] = new CoalescingRingBuffer<Object, Object>(4096);
                                 }

                                 return buffers;
                             };

            
            long mem = CalculateMemoryUsage(objFactory);
            Console.WriteLine("CoalescingRingBuffer takes " + mem + " bytes");
        }

    }

}
