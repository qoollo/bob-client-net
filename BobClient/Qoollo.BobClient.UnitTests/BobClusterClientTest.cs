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
    public class BobClusterClientTest
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

            using (var client = new BobClusterClient(clients, SequentialNodeSelectionPolicy.Factory))
            {
                client.Put(BobKey.FromUInt64(1), defaultData);
                client.Put(BobKey.FromUInt64(ulong.MaxValue), defaultData);
                Assert.Equal(2, stat1.PutRequestCount + stat2.PutRequestCount);

                Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(1)));
                Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(ulong.MaxValue)));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(BobKey.FromUInt64(2)));
                Assert.Equal(3, stat1.GetRequestCount + stat2.GetRequestCount);

                Assert.Equal(new bool[] { true, false }, client.Exists(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2) }));
                Assert.Equal(1, stat1.ExistsRequestCount + stat2.ExistsRequestCount);

                for (ulong i = 100; i < 10000; i++)
                {
                    client.Put(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(i)));
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(i), fullGet: true, new System.Threading.CancellationToken()));
                }
                Assert.Equal(10000 - 100, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                Assert.All(client.Exists(Enumerable.Range(100, 10000 - 100).Select(o => BobKey.FromUInt64((ulong)o)).ToArray()), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(20000, 1000).Select(o => BobKey.FromUInt64((ulong)o)).ToArray(), fullGet: true, new CancellationToken()), res => Assert.False(res));
                Assert.Equal(10000 - 100 + 1, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    client.Put(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(i)));
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

            using (var client = new BobClusterClient(clients, SequentialNodeSelectionPolicy.Factory))
            {
                await client.PutAsync(BobKey.FromUInt64(1), defaultData);
                await client.PutAsync(BobKey.FromUInt64(ulong.MaxValue), defaultData);
                Assert.Equal(2, stat1.PutRequestCount + stat2.PutRequestCount);

                Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(1)));
                Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(ulong.MaxValue)));
                Assert.Throws<BobKeyNotFoundException>(() => client.GetAsync(BobKey.FromUInt64(2)).GetAwaiter().GetResult());
                Assert.Equal(3, stat1.GetRequestCount + stat2.GetRequestCount);

                Assert.Equal(new bool[] { true, false }, await client.ExistsAsync(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2) }));
                Assert.Equal(1, stat1.ExistsRequestCount + stat2.ExistsRequestCount);

                for (ulong i = 100; i < 10000; i++)
                {
                    await client.PutAsync(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(i)));
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, await client .GetAsync(BobKey.FromUInt64(i), fullGet: true, new CancellationToken()));
                }
                Assert.Equal(10000 - 100, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                Assert.All(await client.ExistsAsync(Enumerable.Range(100, 10000 - 100).Select(o => BobKey.FromUInt64((ulong)o)).ToArray()), res => Assert.True(res));
                Assert.All(await client.ExistsAsync(Enumerable.Range(20000, 1000).Select(o => BobKey.FromUInt64((ulong)o)).ToArray(), fullGet: true, new CancellationToken()), res => Assert.False(res));
                Assert.Equal(10000 - 100 + 1, stat1.RequestsWithFullGet + stat2.RequestsWithFullGet);

                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    await client.PutAsync(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(i)));
                }

                Assert.True(stat1.RequestsWithFullGet + stat2.RequestsWithFullGet > 0);
                Assert.True(stat1.TotalRequestCount > 0);
                Assert.True(stat2.TotalRequestCount > 0);
                Assert.True(Math.Abs(stat1.TotalRequestCount - stat2.TotalRequestCount) <= 1);
            }
        }
    }
}
