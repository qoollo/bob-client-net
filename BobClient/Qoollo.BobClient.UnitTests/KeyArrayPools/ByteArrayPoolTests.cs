using Qoollo.BobClient.KeySerializationArrayPools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Qoollo.BobClient.UnitTests.KeyArrayPools
{
    public class ByteArrayPoolTests : BobTestsBaseClass
    {
        public ByteArrayPoolTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }


        [Theory]
        [InlineData(10, 20)]
        [InlineData(300, 500)]
        public void HeadTailIndicesStructCreationFromValuesTest(ushort head, ushort tail)
        {
            var data = new ByteArrayPool.HeadTailIndicesStruct(head, tail);
            Assert.Equal(head, data.Head);
            Assert.Equal(tail, data.Tail);

            Assert.Equal((int)(((uint)head << 16) | tail), data.Pack());
        }

        [Theory]
        [InlineData(10, 20)]
        [InlineData(300, 500)]
        public void HeadTailIndicesStructCreationPackkedValueTest(ushort head, ushort tail)
        {
            var data = new ByteArrayPool.HeadTailIndicesStruct((int)(((uint)head << 16) | tail));
            Assert.Equal(head, data.Head);
            Assert.Equal(tail, data.Tail);

            Assert.Equal((int)(((uint)head << 16) | tail), data.Pack());
        }

        [Theory]
        [InlineData(10, 20, 30 + ByteArrayPool.GapSize * 2)]
        [InlineData(300, 500, 600 + ByteArrayPool.GapSize * 2)]
        public void HeadTailIndicesStructUpdateTest(ushort head, ushort tail, int poolSize)
        {
            var data = new ByteArrayPool.HeadTailIndicesStruct(head, tail);
            Assert.Equal((tail - head + poolSize) % poolSize, data.ElementCount(poolSize));
            Assert.Equal(data.ElementCount(poolSize) > ByteArrayPool.GapSize, data.HasAvailableElements(poolSize));

            Assert.Equal(head, data.MoveHeadForward(poolSize));
            Assert.Equal(head + 1, data.Head);

            while (data.HasAvailableElements(poolSize))
                data.MoveHeadForward(poolSize);

            Assert.True(data.ElementCount(poolSize) <= ByteArrayPool.GapSize);
            while (!data.HasAvailableElements(poolSize))
                data.MoveTailForward(poolSize);

            data.MoveHeadForward(poolSize);

            Assert.Equal((data.Tail - data.Head + poolSize) % poolSize, ByteArrayPool.GapSize);
            Assert.Equal(ByteArrayPool.GapSize, data.ElementCount(poolSize));

            Assert.True(data.HasFreeSpace(poolSize));

            int count = ByteArrayPool.GapSize;
            while (data.HasFreeSpace(poolSize))
            {
                data.MoveTailForward(poolSize);
                count++;
                Assert.Equal(count, data.ElementCount(poolSize));
            }

            Assert.True(data.HasAvailableElements(poolSize));
            Assert.Equal(count, data.ElementCount(poolSize));
        }


        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(128)]
        [InlineData(36000)]
        public void InnerLogicTest(int poolSize)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);
                Assert.Equal(poolSize + ByteArrayPool.GapSize, pool.FullCapacity);

                Assert.Null(pool.TryRentGlobal());

                for (ulong i = 0; i < (ulong)pool.FullCapacity; i++)
                {
                    Assert.True(pool.TryReleaseGlobal(BitConverter.GetBytes(i)));
                }

                Assert.False(pool.TryReleaseGlobal(BitConverter.GetBytes((ulong)0)));

                for (ulong i = 0; i < (ulong)poolSize; i++)
                {
                    Assert.Equal(BitConverter.GetBytes(i), pool.TryRentGlobal());
                }

                Assert.Null(pool.TryRentGlobal());


                ulong releaseStart = (ulong)pool.FullCapacity;
                ulong rentStart = (ulong)poolSize;
                for (int repeat = 0; repeat < 4; repeat++)
                {
                    Assert.Null(pool.TryRentGlobal());

                    for (ulong i = releaseStart; i < releaseStart + (ulong)poolSize; i++)
                    {
                        Assert.True(pool.TryReleaseGlobal(BitConverter.GetBytes(i)));
                    }

                    Assert.False(pool.TryReleaseGlobal(BitConverter.GetBytes((ulong)0)));

                    for (ulong i = rentStart; i < rentStart + (ulong)poolSize; i++)
                    {
                        Assert.Equal(BitConverter.GetBytes(i), pool.TryRentGlobal());
                    }

                    Assert.Null(pool.TryRentGlobal());

                    releaseStart += (ulong)poolSize;
                    rentStart += (ulong)poolSize;
                }
            }
        }


        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(128, true)]
        [InlineData(128, false)]
        [InlineData(36000, true)]
        [InlineData(36000, false)]
        public void RentReleaseTest(int poolSize, bool skipLocal)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);

                HashSet<byte[]> uniqArrays = new HashSet<byte[]>();

                for (int i = 0; i < Math.Max(poolSize * 2 + 101, 100000); i++)
                {
                    byte[] array = pool.Rent(skipLocal: skipLocal);
                    Assert.NotNull(array);
                    uniqArrays.Add(array);
                    pool.Release(array, skipLocal: skipLocal);
                }

                if (poolSize > 0)
                {
                    Assert.True(uniqArrays.Count <= pool.FullCapacity);
                    Assert.True(uniqArrays.Count <= ByteArrayPool.GapSize + 1);
                }

                if (poolSize > 0 && !skipLocal)
                    Assert.Single(uniqArrays);
            }
        }


        [Theory]
        [InlineData(128, true)]
        [InlineData(128, false)]
        [InlineData(256, true)]
        [InlineData(256, false)]
        public void RentReleasePerfTest(int poolSize, bool skipLocal)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);

                Stopwatch sw = Stopwatch.StartNew();

                const int iterations = 1000000;
                for (int i = 0; i < iterations; i++)
                {
                    byte[] array = pool.Rent(skipLocal: skipLocal);
                    pool.Release(array, skipLocal: skipLocal);
                }

                sw.Stop();
                Output?.WriteLine($"Perf: {iterations * 1000 / sw.ElapsedMilliseconds} op/sec ({sw.ElapsedMilliseconds} ms for {iterations})");
            }
        }


        [Theory]
        [InlineData(1, true, 2)]
        [InlineData(1, true, 8)]
        [InlineData(1, false, 4)]
        [InlineData(128, true, 2)]
        [InlineData(128, true, 8)]
        [InlineData(128, false, 4)]
        [InlineData(36000, true, 8)]
        [InlineData(36000, false, 8)]
        public void RentReleaseConcurrentTest(int poolSize, bool skipLocal, int threadCount)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);

                HashSet<byte[]> uniqArrays = new HashSet<byte[]>();
                Barrier bar = new Barrier(threadCount + 1);

                void act()
                {
                    HashSet<byte[]> uniqArraysLocal = new HashSet<byte[]>();

                    bar.SignalAndWait(10000);

                    for (int i = 0; i < Math.Max(poolSize * 2 + 101, 100000); i++)
                    {
                        byte[] array = pool.Rent(skipLocal: skipLocal);
                        Assert.NotNull(array);
                        Assert.Equal(0, array[0]);
                        array[0] = 1;
                        uniqArraysLocal.Add(array);
                        array[0] = 0;
                        pool.Release(array, skipLocal: skipLocal);
                    }

                    lock (uniqArrays)
                    {
                        foreach (var item in uniqArraysLocal)
                            uniqArrays.Add(item);
                    }
                }


                Task[] tasks = new Task[threadCount];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(act);
                }

                bar.SignalAndWait(10000);

                Task.WaitAll(tasks);

                if (poolSize > threadCount)
                    Assert.True(uniqArrays.Count <= pool.FullCapacity + threadCount);
            }
        }



        [Theory]
        [InlineData(1, true, 2)]
        [InlineData(1, true, 8)]
        [InlineData(1, false, 4)]
        [InlineData(128, true, 2)]
        [InlineData(128, true, 8)]
        [InlineData(128, false, 4)]
        [InlineData(36000, true, 8)]
        [InlineData(36000, false, 8)]
        public void RentReleaseConcurrentAsyncTest(int poolSize, bool skipLocal, int threadCount)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);

                HashSet<byte[]> uniqArrays = new HashSet<byte[]>();
                Barrier bar = new Barrier(threadCount + 1);

                async Task act()
                {
                    HashSet<byte[]> uniqArraysLocal = new HashSet<byte[]>();

                    bar.SignalAndWait(10000);

                    for (int i = 0; i < Math.Max(poolSize * 2 + 101, 100000); i++)
                    {
                        byte[] array = pool.Rent(skipLocal: skipLocal);
                        Assert.NotNull(array);
                        Assert.Equal(0, array[0]);
                        array[0] = 1;
                        await Task.Yield();
                        uniqArraysLocal.Add(array);
                        array[0] = 0;
                        pool.Release(array, skipLocal: skipLocal);
                    }

                    lock (uniqArrays)
                    {
                        foreach (var item in uniqArraysLocal)
                            uniqArrays.Add(item);
                    }
                }


                Task[] tasks = new Task[threadCount];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(() => act().GetAwaiter().GetResult());
                }

                bar.SignalAndWait(10000);

                Task.WaitAll(tasks);

                if (poolSize > threadCount)
                    Assert.True(uniqArrays.Count <= pool.FullCapacity + threadCount);
            }
        }



        [Theory]
        [InlineData(64, true, 2)]
        [InlineData(64, false, 4)]
        [InlineData(64, false, 8)]
        [InlineData(64, false, 16)]
        public void NotReturnArrayInUseConcurrentTestTest(int poolSize, bool skipLocal, int threadCount)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);

                Barrier bar = new Barrier(threadCount + 1);

                void act()
                {
                    bar.SignalAndWait(10000);

                    for (int i = 0; i < Math.Max(poolSize * 2 + 101, 100000); i++)
                    {
                        byte[] array = pool.Rent(skipLocal: skipLocal);
                        Assert.NotNull(array);
                        Assert.Equal(0, Volatile.Read(ref array[0]));
                        Volatile.Write(ref array[0], 1);
                        Thread.SpinWait(1000);
                        Volatile.Write(ref array[0], 0);
                        pool.Release(array, skipLocal: skipLocal);
                    }
                }


                Task[] tasks = new Task[threadCount];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(act);
                }

                bar.SignalAndWait(10000);

                Task.WaitAll(tasks);
            }
        }
    }
}
