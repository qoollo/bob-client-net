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
    public interface IBobApi
    {
        BobResult Put(ulong key, byte[] data);
        BobResult Put(ulong key, byte[] data, CancellationToken token);

        Task<BobResult> PutAsync(ulong key, byte[] data);
        Task<BobResult> PutAsync(ulong key, byte[] data, CancellationToken token);

        BobResult Get(ulong key, out byte[] data);
        BobResult Get(ulong key, out byte[] data, CancellationToken token);


        Task<(BobResult, byte[])> GetAsync(ulong key);
        Task<(BobResult, byte[])> GetAsync(ulong key, CancellationToken token);
    }


    internal class BobApi : IBobApi
    {
        private readonly List<BobStorage.BobApi.BobApiClient> _clients;
        private readonly Random _random;

        private readonly TimeSpan? _timeout;

        public BobApi(List<BobStorage.BobApi.BobApiClient> clients, TimeSpan? timeout)
        {
            _clients = clients;
            _timeout = timeout;
            _random = new Random(DateTime.Now.Millisecond);
        }

        private DateTime? Deadline()
        {
            return _timeout is null ? DateTime.UtcNow + _timeout : null;
        }
        private BobStorage.BobApi.BobApiClient GetClient()
        {
            var number = _random.Next(_clients.Count);
            return _clients[number];
        }

        private bool CanProcessRcpException(RpcException e)
        {
            return e.StatusCode == StatusCode.Unknown && (e.Status.Detail == "KeyNotFound" || e.Message == "DuplicateKey");
        }

        public BobResult Put(ulong key, byte[] data)
        {
            return Put(key, data, new CancellationToken());
        }

        public BobResult Put(ulong key, byte[] data, CancellationToken token)
        {
            var client = GetClient();
            var request = new PutRequest(key, data);

            BobResult result;
            try
            {
                var answer = client.Put(request, cancellationToken: token, deadline: Deadline());
                result = BobResult.FromOp(answer);
            }
            catch (RpcException e)
            {
                result = BobResult.Error(e.Message);
            }
            catch (OperationCanceledException e)
            {
                result = BobResult.Error(e.Message);
            }

            return result;
        }

        public async Task<BobResult> PutAsync(ulong key, byte[] data)
        {
            return await PutAsync(key, data, new CancellationToken());
        }

        public async Task<BobResult> PutAsync(ulong key, byte[] data, CancellationToken token)
        {
            var client = GetClient();
            var request = new PutRequest(key, data);

            BobResult result;
            try
            {
                var answer = await client.PutAsync(request, cancellationToken: token, deadline: Deadline());
                result = BobResult.FromOp(answer);
            }
            catch (RpcException e)
            {
                result = BobResult.Error(e.Message);
            }
            catch (OperationCanceledException e)
            {
                result = BobResult.Error(e.Message);
            }

            return result;
        }

        public BobResult Get(ulong key, out byte[] data)
        {
            return Get(key, out data, new CancellationToken());
        }
        public BobResult Get(ulong key, out byte[] data, CancellationToken token)
        {
            var client = GetClient();
            var request = new GetRequest(key);

            BobResult result;
            data = new byte[0];
            try
            {
                var answer = client.Get(request, cancellationToken: token, deadline: Deadline());

                data = answer.Data.ToByteArray();
                result = BobResult.Ok();
            }
            catch (RpcException e)
            {
                result = CanProcessRcpException(e) ? BobResult.KeyNotFound() : BobResult.Error(e.Message);
            }
            catch (OperationCanceledException e)
            {
                result = BobResult.Error(e.Message);
            }
            return result;
        }

        public async Task<(BobResult, byte[])> GetAsync(ulong key)
        {
            return await GetAsync(key, new CancellationToken());
        }

        public async Task<(BobResult, byte[])> GetAsync(ulong key, CancellationToken token)
        {
            var client = GetClient();
            var request = new GetRequest(key);

            BobResult result;
            var data = new byte[0];
            try
            {
                var answer = await client.GetAsync(request, cancellationToken: token, deadline: Deadline());

                data = answer.Data.ToByteArray();
                result = BobResult.Ok();
            }
            catch (RpcException e)
            {
                result = CanProcessRcpException(e) ? BobResult.KeyNotFound() : BobResult.Error(e.Message);
            }
            catch (OperationCanceledException e)
            {
                result = BobResult.Error(e.Message);
            }
            return (result, data);
        }
    }
}
