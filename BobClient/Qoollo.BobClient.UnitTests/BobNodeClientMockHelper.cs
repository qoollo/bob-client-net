using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.UnitTests
{
    internal class BobNodeClientMockHelper
    {
        public class MockClientBehaviour
        {
            public int DelayMs { get; set; } = 0;
            public ManualResetEventSlim Pause { get; } = new ManualResetEventSlim(true);
            public Grpc.Core.Status ErrorStatus { get; set; } = Grpc.Core.Status.DefaultSuccess;
            public string PutTextError { get; set; } = null;
        }

        public class MockClientStat
        {
            public int PingRequestCount { get; set; } = 0;
            public int GetRequestCount { get; set; } = 0;
            public int PutRequestCount { get; set; } = 0;
            public int ExistsRequestCount { get; set; } = 0;

            public int TotalRequestCount { get { return PingRequestCount + GetRequestCount + PutRequestCount + ExistsRequestCount; } }

            public int RequestsWithFullGet { get; set; } = 0;
        }

        public static BobNodeClient CreateMockedClient(string connectionString, Mock<BobStorage.BobApi.BobApiClient> rpcClientMock)
        {
            var result = new BobNodeClient(connectionString);
            var rpcClientField = result.GetType().GetField("_rpcClient", BindingFlags.NonPublic | BindingFlags.Instance);
            rpcClientField.SetValue(result, rpcClientMock.Object);
            return result;
        }
        public static Mock<BobStorage.BobApi.BobApiClient> CreateMockedBobApiClient(
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
        public static Mock<BobStorage.BobApi.BobApiClient> CreateDataAccessMockedBobApiClient(ConcurrentDictionary<BobKey, byte[]> data, MockClientBehaviour behaviour, MockClientStat stat,
            Func<BobStorage.Null, Grpc.Core.CallOptions, BobStorage.Null> pingFunc = null,
            Func<BobStorage.GetRequest, Grpc.Core.CallOptions, BobStorage.Blob> getFunc = null,
            Func<BobStorage.PutRequest, Grpc.Core.CallOptions, BobStorage.OpStatus> putFunc = null,
            Func<BobStorage.ExistRequest, Grpc.Core.CallOptions, BobStorage.ExistResponse> existsFunc = null)
        {
            pingFunc = pingFunc ?? ((request, callOptions) =>
            {
                stat.PingRequestCount++;

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
                stat.GetRequestCount++;
                if (request.Options.Source == BobStorage.GetSource.All)
                    stat.RequestsWithFullGet++;

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
                stat.PutRequestCount++;

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
                stat.ExistsRequestCount++;
                if (request.Options.Source == BobStorage.GetSource.All)
                    stat.RequestsWithFullGet++;

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
        public static BobNodeClient CreateMockedClientWithData(ConcurrentDictionary<BobKey, byte[]> data, MockClientBehaviour behaviour = null, MockClientStat stat = null)
        {
            return CreateMockedClient("Address = 127.0.0.1; OperationTimeout = 00:00:01; ConnectionTimeout = 00:00:01", CreateDataAccessMockedBobApiClient(data, behaviour ?? new MockClientBehaviour(), stat ?? new MockClientStat()));
        }
    }
}
