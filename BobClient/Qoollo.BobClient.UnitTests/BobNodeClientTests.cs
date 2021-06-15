using Moq;
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

        private BobNodeClient CreateMockedClient(Mock<BobStorage.BobApi.BobApiClient> rpcClientMock)
        {
            var result = new BobNodeClient("127.0.0.1", TimeSpan.FromSeconds(16));
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
        private Mock<BobStorage.BobApi.BobApiClient> CreateDataAccessMockedBobApiClient(Dictionary<ulong, byte[]> data, MockClientBehaviour behaviour,
            Func<BobStorage.Null, Grpc.Core.CallOptions, BobStorage.Null> pingFunc = null,
            Func<BobStorage.GetRequest, Grpc.Core.CallOptions, BobStorage.Blob> getFunc = null,
            Func<BobStorage.PutRequest, Grpc.Core.CallOptions, BobStorage.OpStatus> putFunc = null,
            Func<BobStorage.ExistRequest, Grpc.Core.CallOptions, BobStorage.ExistResponse> existsFunc = null)
        {
            pingFunc = pingFunc ?? ((request, callOptions) =>
            {
                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                return new BobStorage.Null();
            });

            getFunc = getFunc ?? ((request, callOptions) =>
            {
                callOptions.CancellationToken.ThrowIfCancellationRequested();

                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                if (data.TryGetValue(request.Key.Key, out byte[] val))
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
                callOptions.CancellationToken.ThrowIfCancellationRequested();

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
                    data[request.Key.Key] = request.Data.Data.ToByteArray();
                    result = new BobStorage.OpStatus() { Error = null };
                }

                return result;
            });

            existsFunc = existsFunc ?? ((request, callOptions) =>
            {
                callOptions.CancellationToken.ThrowIfCancellationRequested();

                if (behaviour.DelayMs > 0)
                    Thread.Sleep(behaviour.DelayMs);
                behaviour.Pause.Wait(TimeSpan.FromMinutes(15));
                if (behaviour.ErrorStatus.StatusCode != Grpc.Core.StatusCode.OK)
                    throw new Grpc.Core.RpcException(behaviour.ErrorStatus);

                var result = new BobStorage.ExistResponse();
                result.Exist.AddRange(request.Keys.Select(o => data.ContainsKey(o.Key)));
                return result;
            });


            return CreateMockedBobApiClient(pingFunc, getFunc, putFunc, existsFunc);
        }
        private BobNodeClient CreateMockedClientWithData(Dictionary<ulong, byte[]> data, MockClientBehaviour behaviour = null)
        {
            return CreateMockedClient(CreateDataAccessMockedBobApiClient(data, behaviour ?? new MockClientBehaviour()));
        }

        // ==================

        [Fact]
        public void BasicStateTransitionTest()
        {
            var data = new Dictionary<ulong, byte[]>
            {
                { 1, new byte[] { 1, 2, 3 } }
            };

            using (var client = CreateMockedClientWithData(data))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                client.Open();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                client.Put(2, new byte[] { 1, 2, 3 });
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var testDataArray = client.Get(2);
                Assert.Equal(new byte[] { 1, 2, 3 }, testDataArray);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult = client.Exists(new ulong[] { 1, 2, 3 });
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
            var data = new Dictionary<ulong, byte[]>
            {
                { 1, new byte[] { 1, 2, 3 } }
            };

            using (var client = CreateMockedClientWithData(data))
            {
                Assert.Equal(BobNodeClientState.Idle, client.State);
                Assert.Equal(0, client.SequentialErrorCount);

                await client.OpenAsync();
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                await client.PutAsync(2, new byte[] { 1, 2, 3 });
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var testDataArray = await client.GetAsync(2);
                Assert.Equal(new byte[] { 1, 2, 3 }, testDataArray);
                Assert.Equal(BobNodeClientState.Ready, client.State);
                Assert.Equal(0, client.SequentialErrorCount);
                Assert.True(client.TimeSinceLastOperationMs < 10000);

                var existsResult = await client.ExistsAsync(new ulong[] { 1, 2, 3 });
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
            var data = new Dictionary<ulong, byte[]>
            {
                { 1, new byte[] { 1, 2, 3 } }
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
    }
}
