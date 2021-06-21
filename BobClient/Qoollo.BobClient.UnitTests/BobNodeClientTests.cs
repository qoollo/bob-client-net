﻿using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobNodeClientTests
    {
        private class MockClientBehaviour
        {
            public int DelayMs { get; set; } = 0;
            public ManualResetEventSlim Pause { get; } = new ManualResetEventSlim(true);
            public Grpc.Core.Status ErrorStatus { get; set; } = Grpc.Core.Status.DefaultSuccess;
            public string PutTextError { get; set; } = null;
        }

        private BobNodeClient CreateMockedClient(Mock<BobStorage.BobApi.BobApiClient> rpcClientMock, TimeSpan? timeout = null)
        {
            var result = new BobNodeClient("127.0.0.1", timeout ?? TimeSpan.FromSeconds(16));
            var rpcClientField = result.GetType().GetField("_rpcClient", BindingFlags.NonPublic | BindingFlags.Instance);
            rpcClientField.SetValue(result, rpcClientMock.Object);
            return result;
        }
        private Mock<BobStorage.BobApi.BobApiClient> CreateMockedBobApiClient(
            Func<BobStorage.Null, Grpc.Core.CallOptions, BobStorage.Null> pingFunc,
            Func<BobStorage.GetRequest, Grpc.Core.CallOptions, BobStorage.Blob> getFunc,
            Func<BobStorage.PutRequest, Grpc.Core.CallOptions, BobStorage.OpStatus> putFunc,
            Func<BobStorage.ExistRequest, Grpc.Core.CallOptions, BobStorage.ExistResponse> existsFunc)
        {
            var mock = new Mock<BobStorage.BobApi.BobApiClient>()
            {
                CallBase = true
            };

            Func<T1, T2, Grpc.Core.AsyncUnaryCall<TRes>> WrapToAsync<T1, T2, TRes>(Func<T1, T2, TRes> sourceFunc)
            {
                return (p1, p2) => new Grpc.Core.AsyncUnaryCall<TRes>(Task.FromResult(sourceFunc(p1, p2)), Task.FromResult<Grpc.Core.Metadata>(null), null, null, null);
            }

            mock.Setup(o => o.Ping(It.IsAny<BobStorage.Null>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(pingFunc);
            mock.Setup(o => o.PingAsync(It.IsAny<BobStorage.Null>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(WrapToAsync(pingFunc));

            mock.Setup(o => o.Exist(It.IsAny<BobStorage.ExistRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(existsFunc);
            mock.Setup(o => o.ExistAsync(It.IsAny<BobStorage.ExistRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(WrapToAsync(existsFunc));

            mock.Setup(o => o.Get(It.IsAny<BobStorage.GetRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(getFunc);
            mock.Setup(o => o.GetAsync(It.IsAny<BobStorage.GetRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(WrapToAsync(getFunc));

            mock.Setup(o => o.Put(It.IsAny<BobStorage.PutRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(putFunc);
            mock.Setup(o => o.PutAsync(It.IsAny<BobStorage.PutRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(WrapToAsync(putFunc));

            return mock;
        }
        private Mock<BobStorage.BobApi.BobApiClient> CreateDataAccessMockedBobApiClient(Dictionary<BobKey, byte[]> data, MockClientBehaviour behaviour,
            Func<BobStorage.Null, Grpc.Core.CallOptions, BobStorage.Null> pingFunc = null,
            Func<BobStorage.GetRequest, Grpc.Core.CallOptions, BobStorage.Blob> getFunc = null,
            Func<BobStorage.PutRequest, Grpc.Core.CallOptions, BobStorage.OpStatus> putFunc = null,
            Func<BobStorage.ExistRequest, Grpc.Core.CallOptions, BobStorage.ExistResponse> existsFunc = null)
        {
            pingFunc = pingFunc ?? ((request, callOptions) =>
            {
                if (callOptions.CancellationToken.IsCancellationRequested)
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Cancelled, "Cancelled"));
                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                return new BobStorage.Null();
            });

            getFunc = getFunc ?? ((request, callOptions) =>
            {
                if (callOptions.CancellationToken.IsCancellationRequested)
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Cancelled, "Cancelled"));
                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                if (data.TryGetValue(new BobKey(request.Key.Key.ToByteArray()), out byte[] val))
                {
                    return new BobStorage.Blob()
                    {
                        Data = Google.Protobuf.ByteString.CopyFrom(val),
                        Meta = new BobStorage.BlobMeta()
                        {
                            Timestamp = 100
                        }
                    };
                }
                else
                {
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.NotFound, "key not found"));
                }
            });

            putFunc = putFunc ?? new Func<BobStorage.PutRequest, Grpc.Core.CallOptions, BobStorage.OpStatus>((request, callOptions) =>
            {
                if (callOptions.CancellationToken.IsCancellationRequested)
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Cancelled, "Cancelled"));
                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                BobStorage.OpStatus result = null;

                if (!string.IsNullOrEmpty(behaviour.PutTextError))
                {
                    result = new BobStorage.OpStatus() { Error = new BobStorage.BobError() { Code = -1, Desc = behaviour.PutTextError } };
                }
                else
                {
                    data[new BobKey(request.Key.Key.ToByteArray())] = request.Data.Data.ToByteArray();
                    result = new BobStorage.OpStatus() { Error = null };
                }

                return result;
            });

            existsFunc = existsFunc ?? ((request, callOptions) =>
            {
                if (callOptions.CancellationToken.IsCancellationRequested)
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Cancelled, "Cancelled"));
                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                var result = new BobStorage.ExistResponse();
                result.Exist.AddRange(request.Keys.Select(o => data.ContainsKey(new BobKey(o.Key.ToByteArray()))));
                return result;
            });


            return CreateMockedBobApiClient(pingFunc, getFunc, putFunc, existsFunc);
        }
        private BobNodeClient CreateMockedClientWithData(Dictionary<BobKey, byte[]> data, MockClientBehaviour behaviour = null, TimeSpan? timeout = null)
        {
            return CreateMockedClient(CreateDataAccessMockedBobApiClient(data, behaviour ?? new MockClientBehaviour()), timeout);
        }

        // ==================

        [Fact]
        public void BasicStateTransitionTest()
        {
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };

            using (var client = CreateMockedClientWithData(data))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                client.Put(BobKey.FromUInt64(2), new byte[] { 1, 2, 3 });
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var testDataArray = client.Get(BobKey.FromUInt64(2));
                Assert.Equal(new byte[] { 1, 2, 3 }, testDataArray);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult = client.Exists(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2), BobKey.FromUInt64(3) });
                Assert.Equal(new bool[] { true, true, false }, existsResult);
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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };

            using (var client = CreateMockedClientWithData(data))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                await client.OpenAsync();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                await client.PutAsync(BobKey.FromUInt64(2), new byte[] { 1, 2, 3 });
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var testDataArray = await client.GetAsync(BobKey.FromUInt64(2));
                Assert.Equal(new byte[] { 1, 2, 3 }, testDataArray);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult = await client.ExistsAsync(new BobKey[] { BobKey.FromUInt64(1), BobKey.FromUInt64(2), BobKey.FromUInt64(3) });
                Assert.Equal(new bool[] { true, true, false }, existsResult);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                await client.CloseAsync();
                Assert.Equal(BobNodeClientState.Shutdown, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
            }
        }

        [Fact]
        public void ConnectingStateTest()
        {
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);

                behaviour.Pause.Reset();

                Task asyncOp = Task.Run(() => client.Open());

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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);

                behaviour.Pause.Reset();
                behaviour.ErrorStatus = new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "Internal error");

                Task asyncOp = Task.Run(() =>
                {
                    try { client.Open(); }
                    catch { }
                });

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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour, TimeSpan.FromSeconds(1)))
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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour, TimeSpan.FromSeconds(1)))
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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour, TimeSpan.FromSeconds(1)))
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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour, TimeSpan.FromSeconds(1)))
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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour, TimeSpan.FromSeconds(1)))
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
            var data = new Dictionary<BobKey, byte[]>
            {
                { BobKey.FromUInt64(1), new byte[] { 1, 2, 3 } }
            };
            var behaviour = new MockClientBehaviour();

            using (var client = CreateMockedClientWithData(data, behaviour, TimeSpan.FromSeconds(1)))
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
    }
}