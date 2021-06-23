using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobNodeClient_TKey_Tests
    {
        [Fact]
        public void PutGetExistOperationTest()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), defaultData },
                { BobKey.FromUInt64(ulong.MaxValue), defaultData }
            };

            using (var client = new BobNodeClient<ulong>(BobNodeClientMockHelper.CreateMockedClientWithData(data), null))
            {
                Assert.Equal(defaultData, client.Get(1));
                Assert.Equal(defaultData, client.Get(ulong.MaxValue));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(2));

                Assert.Equal(new bool[] { true, false }, client.Exists(new ulong[] { 1, 2 }));

                for (ulong i = 100; i < 10000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
                Assert.All(client.Exists(Enumerable.Range(100, 10000 - 100).Select(o => (ulong)o).ToArray()), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(20000, 1000).Select(o => (ulong)o).ToArray()), res => Assert.False(res));


                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
            }
        }


        [Fact]
        public async Task PutGetExistOperationTestAsync()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), defaultData },
                 { BobKey.FromUInt64(ulong.MaxValue), defaultData }
            };

            using (var client = new BobNodeClient<ulong>(BobNodeClientMockHelper.CreateMockedClientWithData(data), null))
            {
                Assert.Equal(defaultData, await client.GetAsync(1));
                Assert.Equal(defaultData, await client.GetAsync(ulong.MaxValue));
                Assert.Throws<BobKeyNotFoundException>(() => client.GetAsync(2).GetAwaiter().GetResult());

                Assert.Equal(new bool[] { true, false }, await client.ExistsAsync(new ulong[] { 1, 2 }));

                for (ulong i = 100; i < 10000; i++)
                {
                    await client.PutAsync(i, defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(i));
                }
                Assert.All(await client.ExistsAsync(Enumerable.Range(100, 10000 - 100).Select(o => (ulong)o).ToArray()), res => Assert.True(res));
                Assert.All(await client.ExistsAsync(Enumerable.Range(20000, 1000).Select(o => (ulong)o).ToArray()), res => Assert.False(res));


                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    await client.PutAsync(i, defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(i));
                }
            }
        }
    }
}
