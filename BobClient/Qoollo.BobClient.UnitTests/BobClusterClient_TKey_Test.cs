using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobClusterClient_TKey_Test
    {
        [Fact]
        public void PutGetExistOperationTest()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new ConcurrentDictionary<BobKey, byte[]>();

            var stat1 = new BobNodeClientMockHelper.MockClientStat();
            var stat2 = new BobNodeClientMockHelper.MockClientStat();

            BobNodeClient[] clients = new BobNodeClient[]
            {
                BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour: null, stat: stat1),
                BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour: null, stat: stat2)
            };

            using (var client = new BobClusterClient<ulong>(clients, SequentialNodeSelectionPolicy.Factory, keySerializer: null))
            {
                client.Put(1, defaultData);
                client.Put(ulong.MaxValue, defaultData);
                Assert.Equal(2, stat1.PutRequestCount + stat2.PutRequestCount);

                Assert.Equal(defaultData, client.Get(1));
                Assert.Equal(defaultData, client.Get(ulong.MaxValue));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(2));
                Assert.Equal(3, stat1.GetRequestCount + stat2.GetRequestCount);

                Assert.Equal(new bool[] { true, false }, client.Exists(new ulong[] { 1, 2 }));
                Assert.Equal(1, stat1.ExistsRequestCount + stat2.ExistsRequestCount);

                for (ulong i = 100; i < 10000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i, fullGet: true, new CancellationToken()));
                }
                Assert.Equal(10000 - 100, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                Assert.All(client.Exists(Enumerable.Range(100, 10000 - 100).Select(o => (ulong)o).ToArray()), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(20000, 1000).Select(o => (ulong)o).ToArray(), fullGet: true, new CancellationToken()), res => Assert.False(res));
                Assert.Equal(10000 - 100 + 1, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }

                Assert.True(stat1.RequestsWithFullGet + stat2.RequestsWithFullGet > 0);
                Assert.True(stat1.TotalRequestCount > 0);
                Assert.True(stat2.TotalRequestCount > 0);
                Assert.True(Math.Abs(stat1.TotalRequestCount - stat2.TotalRequestCount) <= 1);
            }
        }

        [Fact]
        public async Task PutGetExistOperationTestAsync()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new ConcurrentDictionary<BobKey, byte[]>();

            var stat1 = new BobNodeClientMockHelper.MockClientStat();
            var stat2 = new BobNodeClientMockHelper.MockClientStat();

            BobNodeClient[] clients = new BobNodeClient[]
            {
                BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour: null, stat: stat1),
                BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour: null, stat: stat2)
            };
            
            using (var client = new BobClusterClient<uint>(clients, SequentialNodeSelectionPolicy.Factory, keySerializer: null))
            {
                await client.PutAsync(1, defaultData);
                await client.PutAsync(uint.MaxValue, defaultData);
                Assert.Equal(2, stat1.PutRequestCount + stat2.PutRequestCount);

                Assert.Equal(defaultData, await client.GetAsync(1));
                Assert.Equal(defaultData, await client.GetAsync(uint.MaxValue));
                Assert.Throws<BobKeyNotFoundException>(() => client.GetAsync(2).GetAwaiter().GetResult());
                Assert.Equal(3, stat1.GetRequestCount + stat2.GetRequestCount);

                Assert.Equal(new bool[] { true, false }, await client.ExistsAsync(new uint[] { 1, 2 }));
                Assert.Equal(1, stat1.ExistsRequestCount + stat2.ExistsRequestCount);

                for (uint i = 100; i < 10000; i++)
                {
                    await client.PutAsync(i, defaultData);
                }
                for (uint i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(i));
                }
                for (uint i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(i, fullGet: true, new CancellationToken()));
                }
                Assert.Equal(10000 - 100, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                Assert.All(await client.ExistsAsync(Enumerable.Range(100, 10000 - 100).Select(o => (uint)o).ToArray()), res => Assert.True(res));
                Assert.All(await client.ExistsAsync(Enumerable.Range(20000, 1000).Select(o => (uint)o).ToArray(), fullGet: true, new CancellationToken()), res => Assert.False(res));
                Assert.Equal(10000 - 100 + 1, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                for (uint i = ushort.MaxValue; i < (uint)ushort.MaxValue + 1000; i++)
                {
                    await client.PutAsync(i, defaultData);
                }
                for (uint i = ushort.MaxValue; i < (uint)ushort.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(i));
                }

                Assert.True(stat1.RequestsWithFullGet + stat2.RequestsWithFullGet > 0);
                Assert.True(stat1.TotalRequestCount > 0);
                Assert.True(stat2.TotalRequestCount > 0);
                Assert.True(Math.Abs(stat1.TotalRequestCount - stat2.TotalRequestCount) <= 1);
            }
        }
    }
}
