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
        public void PutGetExistOperationUInt64Test()
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
        public async Task PutGetExistOperationUInt64TestAsync()
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


        [Fact]
        public void PutGetExistOperationInt64Test()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new Dictionary<BobKey, byte[]>
            {
            };

            using (var client = new BobNodeClient<long>(BobNodeClientMockHelper.CreateMockedClientWithData(data), null))
            {
                client.Put(1, defaultData);
                client.Put(long.MaxValue, defaultData);

                Assert.Equal(defaultData, client.Get(1));
                Assert.Equal(defaultData, client.Get(long.MaxValue));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(2));

                Assert.Equal(new bool[] { true, false }, client.Exists(new long[] { 1, 2 }));

                for (long i = 100; i < 10000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (long i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
                Assert.All(client.Exists(Enumerable.Range(100, 10000 - 100).Select(o => (long)o).ToArray()), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(20000, 1000).Select(o => (long)o).ToArray()), res => Assert.False(res));


                for (long i = uint.MaxValue; i < (long)uint.MaxValue + 1000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (long i = uint.MaxValue; i < (long)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
            }
        }


        [Fact]
        public void PutGetExistOperationInt32Test()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new Dictionary<BobKey, byte[]>
            {
            };

            using (var client = new BobNodeClient<int>(BobNodeClientMockHelper.CreateMockedClientWithData(data), null))
            {
                client.Put(1, defaultData);
                client.Put(int.MaxValue, defaultData);

                Assert.Equal(defaultData, client.Get(1));
                Assert.Equal(defaultData, client.Get(int.MaxValue));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(2));

                Assert.Equal(new bool[] { true, false }, client.Exists(new int[] { 1, 2 }));

                for (int i = 100; i < 10000; i++)
                {
                    client.Put(i, defaultData);
                }
                for (int i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
                Assert.All(client.Exists(Enumerable.Range(100, 10000 - 100).Select(o => (int)o).ToArray()), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(20000, 1000).Select(o => (int)o).ToArray()), res => Assert.False(res));


                for (int i = int.MaxValue - 1000; i < int.MaxValue - 1; i++)
                {
                    client.Put(i, defaultData);
                }
                for (int i = int.MaxValue - 1000; i < int.MaxValue - 1; i++)
                {
                    Assert.Equal(defaultData, client.Get(i));
                }
            }
        }


        [Fact]
        public void PutGetExistOperationGuidTest()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new Dictionary<BobKey, byte[]>
            {
            };

            Guid guid1 = Guid.NewGuid();
            Guid guid2 = Guid.NewGuid();

            Guid[] guidArray = Enumerable.Range(0, 1000).Select(o => Guid.NewGuid()).ToArray();

            using (var client = new BobNodeClient<Guid>(BobNodeClientMockHelper.CreateMockedClientWithData(data), null))
            {
                client.Put(guid1, defaultData);

                Assert.Equal(defaultData, client.Get(guid1));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(guid2));

                Assert.Equal(new bool[] { true, false }, client.Exists(new Guid[] { guid1, guid2 }));

                for (int i = 0; i < guidArray.Length; i++)
                {
                    client.Put(guidArray[i], defaultData);
                }
                for (int i = 0; i < guidArray.Length; i++)
                {
                    Assert.Equal(defaultData, client.Get(guidArray[i]));
                }
                Assert.All(client.Exists(guidArray), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(0, 100).Select(o => Guid.NewGuid()).ToArray()), res => Assert.False(res));
            }
        }
    }
}
