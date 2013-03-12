NCoalescingRingBuffer
=====================
This is a port of LMAX's CoalescingRingBuffer which you can find here:
https://github.com/LMAX-Exchange/LMAXCollections

Limitations
===========
Due to the differences in .NET and java, the buffer can only use classes as items in the buffer (no ints, longs, etc). 
It also relies on a volatile package imported from the .NET Disruptor port which can be found here:
https://github.com/odeheurles/Disruptor-net

If you find any bugs please send a pull request my way.