using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient
{
    /// <summary>
    /// State of the <see cref="BobNodeClient"/>
    /// </summary>
    public enum BobNodeClientState
    {
        /// <summary>
        /// Client is idle
        /// </summary>
        Idle = 0,
        /// <summary>
        /// Client is connecting
        /// </summary>
        Connecting = 1,
        /// <summary>
        /// Client is ready for work
        /// </summary>
        Ready = 2,
        /// <summary>
        /// Client has seen a failure but expects to recover
        /// </summary>
        TransientFailure = 3,
        /// <summary>
        /// Client closed or has seen a failure that it cannot recover from
        /// </summary>
        Shutdown = 4
    }


    public class BobNodeClient: IDisposable
    {
        private static DateTime? GetDeadline(int timeout)
        {
            if (timeout == Timeout.Infinite)
                return null;

            return DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout);
        }

        private static BobNodeClientState ConvertState(Grpc.Core.ChannelState state)
        {
            switch (state)
            {
                case Grpc.Core.ChannelState.Idle:
                    return BobNodeClientState.Idle;
                case Grpc.Core.ChannelState.Connecting:
                    return BobNodeClientState.Connecting;
                case Grpc.Core.ChannelState.Ready:
                    return BobNodeClientState.Ready;
                case Grpc.Core.ChannelState.TransientFailure:
                    return BobNodeClientState.TransientFailure;
                case Grpc.Core.ChannelState.Shutdown:
                    return BobNodeClientState.Shutdown;
                default:
                    throw new Exception($"Unexpected internal channel state: {state}");
            }
        }

        // =========

        private readonly NodeAddress _nodeAddress;
        private readonly TimeSpan _operationTimeout;

        private readonly Grpc.Core.Channel _rpcChannel;
        private readonly BobStorage.BobApi.BobApiClient _rpcClient;

        private bool _isDisposed;

        public BobNodeClient(NodeAddress nodeAddress, TimeSpan operationTimeout)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));
            if (operationTimeout < TimeSpan.Zero && operationTimeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(operationTimeout));

            _nodeAddress = nodeAddress;
            _operationTimeout = operationTimeout;
            _rpcChannel = new Grpc.Core.Channel(nodeAddress.Address, Grpc.Core.ChannelCredentials.Insecure);
            _rpcClient = new BobStorage.BobApi.BobApiClient(_rpcChannel);

            _isDisposed = false;
        }
        public BobNodeClient(string nodeAddress, TimeSpan operationTimeout)
            : this(new NodeAddress(nodeAddress), operationTimeout)
        {
        }
        public BobNodeClient(string nodeAddress)
            : this(nodeAddress, Timeout.InfiniteTimeSpan)
        {
        }

        public BobNodeClientState State { get { return ConvertState(_rpcChannel.State); } }

        public async Task OpenAsync(int timeoutMs)
        {
            if (timeoutMs < 0 && timeoutMs != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));

            try
            {
                await _rpcChannel.ConnectAsync(GetDeadline(timeoutMs));
            }
            catch (TaskCanceledException tce)
            {
                if (_rpcChannel.State == Grpc.Core.ChannelState.Shutdown)
                    throw new BobOperationException($"Connection failed to node {_nodeAddress}", tce);
                throw new TimeoutException($"Connection timeout reached (node: {_nodeAddress}, speciefied timeout: {timeoutMs}ms)", tce);
            }
            catch (Grpc.Core.RpcException rpce)
            {
                throw new BobOperationException($"Connection failed to node {_nodeAddress}", rpce);
            }
        }
        public Task OpenAsync()
        {
            return OpenAsync(Timeout.Infinite);
        }
        public void Open(int timeoutMs)
        {
            OpenAsync(timeoutMs).GetAwaiter().GetResult();
        }
        public void Open()
        {
            OpenAsync().GetAwaiter().GetResult();
        }


        public async Task CloseAsync()
        {
            try
            {
                await _rpcChannel.ShutdownAsync();
            }
            catch (Grpc.Core.RpcException rpce)
            {
                throw new BobOperationException($"Client closing failed for node {_nodeAddress}", rpce);
            }
        }
        public void Close()
        {
            CloseAsync().GetAwaiter().GetResult();
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
            return e.StatusCode == StatusCode.Unknown &&
                   (e.Status.Detail == "KeyNotFound" || e.Message == "DuplicateKey");
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

        public BobGetResult Get(ulong key, bool fullGet = false)
        {
            return Get(key, new CancellationToken(), fullGet);
        }

        public BobGetResult Get(ulong key, CancellationToken token, bool fullGet = false)
        {
            var client = GetClient();
            var request = new GetRequest(key, fullGet);

            BobGetResult result;
            try
            {
                var answer = client.Get(request, cancellationToken: token, deadline: Deadline());
                result = new BobGetResult(BobResult.Ok(), answer.Data.ToByteArray());
            }
            catch (RpcException e)
            {
                result = new BobGetResult(CanProcessRcpException(e)
                    ? BobResult.KeyNotFound()
                    : BobResult.Error(e.Message));
            }
            catch (OperationCanceledException e)
            {
                result = new BobGetResult(BobResult.Error(e.Message));
            }

            return result;
        }

        public async Task<BobGetResult> GetAsync(ulong key, bool fullGet = false)
        {
            return await GetAsync(key, new CancellationToken(), fullGet);
        }

        public async Task<BobGetResult> GetAsync(ulong key, CancellationToken token, bool fullGet = false)
        {
            var client = GetClient();
            var request = new GetRequest(key, fullGet);

            BobGetResult result;
            try
            {
                var answer = await client.GetAsync(request, cancellationToken: token, deadline: Deadline());
                result = new BobGetResult(BobResult.Ok(), answer.Data.ToByteArray());
            }
            catch (RpcException e)
            {
                result = new BobGetResult(CanProcessRcpException(e)
                    ? BobResult.KeyNotFound()
                    : BobResult.Error(e.Message));
            }
            catch (OperationCanceledException e)
            {
                result = new BobGetResult(BobResult.Error(e.Message));
            }

            return result;
        }



        protected virtual void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                try
                {
                    this.Close();
                }
                catch { }

                _isDisposed = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
