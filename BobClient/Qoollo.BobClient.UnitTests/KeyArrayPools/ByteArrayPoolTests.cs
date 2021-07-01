using Qoollo.BobClient.KeyArrayPools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeyArrayPools
{
    public class ByteArrayPoolTests
    {
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
        [InlineData(10, 20, 30)]
        [InlineData(300, 500, 600)]
        public void HeadTailIndicesStructUpdateTest(ushort head, ushort tail, int poolSize)
        {
            var data = new ByteArrayPool.HeadTailIndicesStruct(head, tail);
            Assert.Equal(head != tail, data.HasElements);

            Assert.Equal(head, data.MoveHeadForward(poolSize));
            Assert.Equal(head + 1, data.Head);

            while (data.HasElements)
                data.MoveHeadForward(poolSize);

            Assert.Equal(0, data.ElementCount(poolSize));

            Assert.Equal(data.Tail, data.Head);

            Assert.True(data.HasFreeSpace(poolSize));

            int count = 0;
            while (data.HasFreeSpace(poolSize))
            {
                data.MoveTailForward(poolSize);
                count++;
                Assert.Equal(count, data.ElementCount(poolSize));
            }

            Assert.True(data.HasElements);
            Assert.Equal(count, data.ElementCount(poolSize));
        }


        [Theory]
        [InlineData(0)]
        [InlineData(128)]
        [InlineData(36000)]
        public void TestRentReleaseGlobal(int poolSize)
        {
            using (var pool = new ByteArrayPool(sizeof(ulong), poolSize))
            {
                Assert.Equal(sizeof(ulong), pool.ByteArrayLength);
                Assert.Equal(poolSize, pool.MaxElementCount);

                Assert.Null(pool.TryRentGlobal());

                for (ulong i = 0; i < (ulong)poolSize; i++)
                {
                    Assert.True(pool.TryReleaseGlobal(BitConverter.GetBytes(i)));
                }

                Assert.False(pool.TryReleaseGlobal(BitConverter.GetBytes((ulong)0)));

                for (ulong i = 0; i < (ulong)poolSize; i++)
                {
                    Assert.Equal(BitConverter.GetBytes(i), pool.TryRentGlobal());
                }

                Assert.Null(pool.TryRentGlobal());

                if (poolSize > 0)
                {
                    for (ulong i = 0; i < 100; i++)
                    {
                        Assert.True(pool.TryReleaseGlobal(BitConverter.GetBytes(i)));
                        Assert.Equal(BitConverter.GetBytes(i), pool.TryRentGlobal());
                    }
                }
            }
        }
    }
}
