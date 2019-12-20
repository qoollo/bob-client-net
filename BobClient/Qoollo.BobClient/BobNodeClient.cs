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


    public class BobNodeClient: IBobApi, IDisposable
    {
        private static DateTime? GetDeadline(TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return null;

            return DateTime.UtcNow + timeout;
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

        private static bool IsKeyNotFoundError(Grpc.Core.RpcException e)
        {
            return e.StatusCode == Grpc.Core.StatusCode.Unknown && (e.Status.Detail == "KeyNotFound" || e.Message == "DuplicateKey");
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

        public async Task OpenAsync(TimeSpan timeout)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            try
            {
                await _rpcChannel.ConnectAsync(GetDeadline(timeout));
            }
            catch (TaskCanceledException tce)
            {
                if (_rpcChannel.State == Grpc.Core.ChannelState.Shutdown)
                    throw new BobOperationException($"Connection failed to node {_nodeAddress}", tce);
                throw new TimeoutException($"Connection timeout reached (node: {_nodeAddress}, speciefied timeout: {timeout}ms)", tce);
            }
            catch (Grpc.Core.RpcException rpce)
            {
                throw new BobOperationException($"Connection failed to node {_nodeAddress}", rpce);
            }
        }
        public Task OpenAsync()
        {
            return OpenAsync(Timeout.InfiniteTimeSpan);
        }
        public void Open(TimeSpan timeout)
        {
            OpenAsync(timeout).GetAwaiter().GetResult();
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


        public BobResult Put(ulong key, byte[] data, CancellationToken token)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.PutRequest(key, data);

            BobResult result;
            try
            {
                var answer = _rpcClient.Put(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                result = BobResult.FromOp(answer);
            }
            catch (Grpc.Core.RpcException e)
            {
                throw new BobOperationException($"Put operation failed for key: {key}", e);
            }

            return result;
        }

        public BobResult Put(ulong key, byte[] data)
        {
            return Put(key, data, new CancellationToken());
        }


        public async Task<BobResult> PutAsync(ulong key, byte[] data, CancellationToken token)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.PutRequest(key, data);

            BobResult result;
            try
            {
                var answer = await _rpcClient.PutAsync(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                result = BobResult.FromOp(answer);
            }
            catch (Grpc.Core.RpcException e)
            {
                throw new BobOperationException($"Put operation failed for key: {key}", e);
            }

            return result;
        }

        public Task<BobResult> PutAsync(ulong key, byte[] data)
        {
            return PutAsync(key, data, new CancellationToken());
        }


        public BobGetResult Get(ulong key, bool fullGet, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.GetRequest(key, fullGet);

            BobGetResult result;
            try
            {
                var answer = _rpcClient.Get(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                result = new BobGetResult(BobResult.Ok(), answer.Data.ToByteArray());
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsKeyNotFoundError(e))
                    throw new BobKeyNotFoundException($"Record for key = {key} is not found in Bob", e);

                throw new BobOperationException($"Get operation failed for key: {key}", e);
            }

            return result;
        }
        public BobGetResult Get(ulong key, CancellationToken token)
        {
            return Get(key, false, token);
        }
        public BobGetResult Get(ulong key, bool fullGet)
        {
            return Get(key, fullGet, new CancellationToken());
        }
        public BobGetResult Get(ulong key)
        {
            return Get(key, false, new CancellationToken());
        }

        public async Task<BobGetResult> GetAsync(ulong key, bool fullGet, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.GetRequest(key, fullGet);

            BobGetResult result;
            try
            {
                var answer = await _rpcClient.GetAsync(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                result = new BobGetResult(BobResult.Ok(), answer.Data.ToByteArray());
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsKeyNotFoundError(e))
                    throw new BobKeyNotFoundException($"Record for key = {key} is not found in Bob", e);

                throw new BobOperationException($"Get operation failed for key: {key}", e);
            }

            return result;
        }

        public Task<BobGetResult> GetAsync(ulong key, CancellationToken token)
        {
            return GetAsync(key, false, token);
        }
        public Task<BobGetResult> GetAsync(ulong key, bool fullGet)
        {
            return GetAsync(key, fullGet, new CancellationToken());
        }
        public Task<BobGetResult> GetAsync(ulong key)
        {
            return GetAsync(key, false, new CancellationToken());
        }



        protected virtual void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                try
                {
                    this.Close();
                }
                catch { }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
