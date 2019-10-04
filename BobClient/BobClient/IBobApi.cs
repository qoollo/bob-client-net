using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BobStorage;
using Google.Protobuf;
using Grpc.Core;

namespace BobClient
{
    public class PutResult
    {
        public string Message { get; }
        public int ErrorCode { get; }
        public bool IsError { get; }

        private PutResult(bool isError, string message, int errorCode)
        {
            IsError = isError;
            Message = message;
            ErrorCode = errorCode;
        }

        internal PutResult(string message, int errorCode)
            : this(false, message, errorCode)
        {

        }

        internal PutResult()
            : this(false, string.Empty, 0)
        {

        }

        internal static PutResult Create(OpStatus status)
        {
            return status.Error is null ? new PutResult() : new PutResult(status.Error.Desc, status.Error.Code);
        }

        public override string ToString()
        {
            return $"code: {ErrorCode}, message: {Message}";
        }
    }

    public interface IBobApi
    {
        PutResult Put(ulong key, byte[] data, CancellationToken token);
        //Task PutAsync(ulong key, byte[] data);

        //byte[] Get(ulong key);
        //Task<byte[]> GetAsync(ulong key);
    }

    internal class BobApi : IBobApi
    {
        private readonly List<BobStorage.BobApi.BobApiClient> _clients;
        private readonly Random _random;

        private readonly TimeSpan _timeout;

        public BobApi(List<BobStorage.BobApi.BobApiClient> clients, TimeSpan timeout)
        {
            _clients = clients;
            _timeout = timeout;
            _random = new Random(DateTime.Now.Millisecond);
        }

        private BobStorage.BobApi.BobApiClient GetClient()
        {
            var number = _random.Next(_clients.Count);
            return _clients[number];
        }

        private static PutRequest CreatePutRequest(ulong key, byte[] data)
        {
            return new PutRequest
            {
                Key = new BlobKey
                {
                    Key = key
                },
                Data = new Blob
                {
                    Data = ByteString.CopyFrom(data),
                    Meta = new BlobMeta {Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()}
                }
            };
        }

        public PutResult Put(ulong key, byte[] data, CancellationToken token)
        {
            var client = GetClient();
            var request = CreatePutRequest(key, data);

            PutResult result;
            try
            {
                var answer = client.Put(request, cancellationToken: token, deadline: DateTime.Now + _timeout);
                result = PutResult.Create(answer);
            }
            catch (RpcException e)
            {
                result = new PutResult(e.Message, -1); //TODO
            }
            catch (OperationCanceledException e)
            {
                result = new PutResult(e.Message, -1); //TODO
            }

            return result;
        }
    }
}
