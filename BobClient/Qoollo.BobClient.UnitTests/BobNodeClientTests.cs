using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobNodeClientTests : BobTestsBaseClass
    {
        public BobNodeClientTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BasicStateTransitionTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour: null, stat: stat))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                client.Put(BobKey.FromUInt64(2), new byte[] { 1, 2, 3 });
                Assert.Equal(1, stat.PutRequestCount);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var testDataArray = client.Get(BobKey.FromUInt64(2));
                Assert.Equal(1, stat.GetRequestCount);
                Assert.Equal(new byte[] { 1, 2, 3 }, testDataArray);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult = client.Exists(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2), BobKey.FromUInt64(3) });
                Assert.Equal(1, stat.ExistsRequestCount);
                Assert.Equal(new bool[] { true, true, false }, existsResult);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult2 = client.Exists(new List<BobKey>() { BobKey.FromUInt64(1), BobKey.FromUInt64(2), BobKey.FromUInt64(3) });
                Assert.Equal(2, stat.ExistsRequestCount);
                Assert.Equal(new bool[] { true, true, false }, existsResult2);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                client.Close();
                Assert.Equal(BobNodeClientState.Shutdown, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }

        [Fact]
        public async Task BasicStateTransitionTestAsync()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour: null, stat: stat))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                await client.OpenAsync();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                await client.PutAsync(BobKey.FromUInt64(2), new byte[] { 1, 2, 3 });
                Assert.Equal(1, stat.PutRequestCount);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var testDataArray = await client.GetAsync(BobKey.FromUInt64(2));
                Assert.Equal(1, stat.GetRequestCount);
                Assert.Equal(new byte[] { 1, 2, 3 }, testDataArray);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult = await client.ExistsAsync(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2), BobKey.FromUInt64(3) });
                Assert.Equal(1, stat.ExistsRequestCount);
                Assert.Equal(new bool[] { true, true, false }, existsResult);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult2 = await client.ExistsAsync(new List<BobKey>() { BobKey.FromUInt64(1), BobKey.FromUInt64(2), BobKey.FromUInt64(3) });
                Assert.Equal(2, stat.ExistsRequestCount);
                Assert.Equal(new bool[] { true, true, false }, existsResult2);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                await client.CloseAsync();
                Assert.Equal(BobNodeClientState.Shutdown, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }

        [Fact]
        public void PutGetExistOperationTest()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };

            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = defaultData;
            data[BobKey.FromUInt64(ulong.MaxValue)] = defaultData;

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data))
            {
                Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(1)));
                Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(ulong.MaxValue)));
                Assert.Throws<BobKeyNotFoundException>(() => client.Get(BobKey.FromUInt64(2)));

                Assert.Equal(new bool[] { true, false }, client.Exists(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2) }));

                for (ulong i = 100; i < 10000; i++)
                {
                    client.Put(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(i)));
                }
                Assert.All(client.Exists(Enumerable.Range(100, 10000 - 100).Select(o => BobKey.FromUInt64((ulong)o)).ToArray()), res => Assert.True(res));
                Assert.All(client.Exists(Enumerable.Range(20000, 1000).Select(o => BobKey.FromUInt64((ulong)o)).ToArray()), res => Assert.False(res));


                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    client.Put(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, client.Get(BobKey.FromUInt64(i)));
                }
            }
        }


        [Fact]
        public async Task PutGetExistOperationTestAsync()
        {
            byte[] defaultData = new byte[] { 1, 2, 3 };
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = defaultData;
            data[BobKey.FromUInt64(ulong.MaxValue)] = defaultData;

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data))
            {
                Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(1)));
                Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(ulong.MaxValue)));
                Assert.Throws<BobKeyNotFoundException>(() => client.GetAsync(BobKey.FromUInt64(2)).GetAwaiter().GetResult());

                Assert.Equal(new bool[] { true, false }, await client.ExistsAsync(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2) }));

                for (ulong i = 100; i < 10000; i++)
                {
                    await client.PutAsync(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = 100; i < 10000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(i)));
                }
                Assert.All(await client.ExistsAsync(Enumerable.Range(100, 10000 - 100).Select(o => BobKey.FromUInt64((ulong)o)).ToArray()), res => Assert.True(res));
                Assert.All(await client.ExistsAsync(Enumerable.Range(20000, 1000).Select(o => BobKey.FromUInt64((ulong)o)).ToArray()), res => Assert.False(res));


                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    await client.PutAsync(BobKey.FromUInt64(i), defaultData);
                }
                for (ulong i = uint.MaxValue; i < (ulong)uint.MaxValue + 1000; i++)
                {
                    Assert.Equal(defaultData, await client.GetAsync(BobKey.FromUInt64(i)));
                }
            }
        }

        [Fact]
        public void ConnectingStateTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);

                behaviour.Pause.Reset();

                Task asyncOp = Task.Factory.StartNew(() => client.Open(), TaskCreationOptions.LongRunning);

                for (int i = 0; i < 1000 && client.State == BobNodeClientState.Idle; i++)
                    Thread.Sleep(10);

                Assert.Equal(BobNodeClientState.Connecting, client.State);

                behaviour.Pause.Set();
                asyncOp.Wait();

                Assert.Equal(BobNodeClientState.Ready, client.State);
            }
        }

        [Fact]
        public void ConnectingToFailedStateTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);

                behaviour.Pause.Reset();
                behaviour.ErrorStatus = new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "Internal error");

                Task asyncOp = Task.Factory.StartNew(() =>
                {
                    try { client.Open(); }
                    catch { }
                }, TaskCreationOptions.LongRunning);

                for (int i = 0; i < 1000 && client.State == BobNodeClientState.Idle; i++)
                    Thread.Sleep(10);

                Assert.Equal(BobNodeClientState.Connecting, client.State);

                behaviour.Pause.Set();
                asyncOp.Wait();

                Assert.Equal(BobNodeClientState.TransientFailure, client.State);
            }
        }

        [Fact]
        public void KeyNotFoundIsNotCountingAsErrors()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();
            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour, stat))
            {
                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                try
                {
                    client.Get(BobKey.FromUInt64(100));
                }
                catch (BobKeyNotFoundException)
                {
                }

                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }

        [Fact]
        public void CancellationIsNotCountingAsErrors()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();
            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour, stat))
            {
                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                try
                {
                    CancellationTokenSource cancelled = new CancellationTokenSource();
                    cancelled.Cancel();
                    client.Get(BobKey.FromUInt64(1), cancelled.Token);
                }
                catch (OperationCanceledException)
                {
                }

                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }

        [Fact]
        public void TimeoutIsNotCountingAsErrors()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();
            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour, stat))
            {
                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                try
                {
                    behaviour.ErrorStatus = new Grpc.Core.Status(Grpc.Core.StatusCode.DeadlineExceeded, "Deadline");
                    client.Get(BobKey.FromUInt64(1));
                }
                catch (TimeoutException)
                {
                }

                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }


        [Fact]
        public void RecoveryAfterFailTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();
            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour, stat))
            {
                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                client.Get(BobKey.FromUInt64(1));
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                try
                {
                    behaviour.ErrorStatus = new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "Internal error");
                    client.Get(BobKey.FromUInt64(1));
                }
                catch (BobOperationException)
                {
                }

                Assert.Equal(BobNodeClientState.TransientFailure, client.State);
                Assert.Equal(1, client.SequentialErrorCount);

                behaviour.ErrorStatus = Grpc.Core.Status.DefaultSuccess;

                client.Get(BobKey.FromUInt64(1));
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }


        [Fact]
        public void SequentialErrorCountTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();
            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour, stat))
            {
                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                behaviour.ErrorStatus = new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "Internal error");

                for (int i = 1; i <= 100; i++)
                {
                    try
                    {
                        client.Get(BobKey.FromUInt64(1));
                    }
                    catch (BobOperationException)
                    {
                    }

                    Assert.Equal(BobNodeClientState.TransientFailure, client.State);
                    Assert.Equal(i, client.SequentialErrorCount);
                }

                behaviour.ErrorStatus = Grpc.Core.Status.DefaultSuccess;

                client.Get(BobKey.FromUInt64(1));
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }


        [Fact]
        public void TimeSinceLastOperationTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            var behaviour = new BobNodeClientMockHelper.MockClientBehaviour();
            var stat = new BobNodeClientMockHelper.MockClientStat();

            using (var client = BobNodeClientMockHelper.CreateMockedClientWithData(data, behaviour, stat))
            {
                client.Open();

                client.Put(BobKey.FromUInt64(10), new byte[] { 1, 2, 3 });

                int startTick = Environment.TickCount;
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(10);
                    int elapsed = unchecked(Environment.TickCount - startTick);
                    Assert.InRange(client.TimeSinceLastOperationMs, elapsed - 10, elapsed + 100);
                }

                client.Get(BobKey.FromUInt64(10));
                Assert.True(client.TimeSinceLastOperationMs < 100);
            }
        }


        [Fact]
        public void ConnectionStringParametersPassedTest()
        {
            using (var client = new BobNodeClient("Address = 127.0.0.1; User = user; Password = pass; ConnectionTimeout = 00:00:33; OperationTimeout = 00:00:22"))
            {
                Assert.Equal("127.0.0.1", client.ConnectionParameters.Host);
                Assert.Equal("user", client.ConnectionParameters.User);
                Assert.Equal("pass", client.ConnectionParameters.Password);
                Assert.Equal(TimeSpan.Parse("00:00:33"), client.ConnectionParameters.ConnectionTimeout);
                Assert.Equal(TimeSpan.Parse("00:00:22"), client.ConnectionParameters.OperationTimeout);
            }
        }

        [Fact]
        public void ConnectionParametersPassedTest()
        {
            BobConnectionParametersBuilder brld = new BobConnectionParametersBuilder()
            {
                Host = "127.0.0.1",
                User = "user",
                Password = "pass",
                ConnectionTimeout = TimeSpan.Parse("00:00:33"),
                OperationTimeout = TimeSpan.Parse("00:00:22")
            };

            using (var client = new BobNodeClient(brld.Build()))
            {
                Assert.Equal("127.0.0.1", client.ConnectionParameters.Host);
                Assert.Equal("user", client.ConnectionParameters.User);
                Assert.Equal("pass", client.ConnectionParameters.Password);
                Assert.Equal(TimeSpan.Parse("00:00:33"), client.ConnectionParameters.ConnectionTimeout);
                Assert.Equal(TimeSpan.Parse("00:00:22"), client.ConnectionParameters.OperationTimeout);
            }
        }

        [Fact]
        public void OperationTimeoutIsUsedTest()
        {
            var data = new ConcurrentDictionary<BobKey, byte[]>();
            data[BobKey.FromUInt64(1)] = new byte[] { 1, 2, 3 };

            DateTime? deadline = null;

            var mock = BobNodeClientMockHelper.CreateDataAccessMockedBobApiClient(data, new BobNodeClientMockHelper.MockClientBehaviour(), new BobNodeClientMockHelper.MockClientStat(),
                getFunc: (req, opt) =>
                {
                    deadline = opt.Deadline;
                    return new BobStorage.Blob() { Data = Google.Protobuf.ByteString.CopyFrom(new byte[] { 1, 2, 3 }) };
                });

            using (var client = BobNodeClientMockHelper.CreateMockedClient("Address = 127.0.0.1; OperationTimeout = 01:00:00", mock))
            {
                var expectedDeadline = DateTime.UtcNow + TimeSpan.FromHours(1);
                client.Get(BobKey.FromUInt64(1));

                Assert.NotNull(deadline);
                var delta = (deadline.Value - expectedDeadline);
                if (delta < TimeSpan.Zero)
                    delta = -delta;
                Assert.True(delta < TimeSpan.FromMinutes(10));
            }
        }
    }
}
