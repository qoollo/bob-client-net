using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Resources;
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


    /// <summary>
    /// Client for a single Bob node
    /// </summary>
    public class BobNodeClient: IBobApi, IDisposable
    {
        /// <summary>
        /// Default operation timeout
        /// </summary>
        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Calculates deadline value for GRPC
        /// </summary>
        private static DateTime? GetDeadline(TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return null;

            return DateTime.UtcNow + timeout;
        }

        /// <summary>
        /// Checks whether the exception is a KeyNotFound exception
        /// </summary>
        private static bool IsKeyNotFoundError(Grpc.Core.RpcException e)
        {
            return e.StatusCode == Grpc.Core.StatusCode.NotFound;
        }
        /// <summary>
        /// Checks whether the exception is a timeout
        /// </summary>
        private static bool IsOperationTimeoutError(Grpc.Core.RpcException e)
        {
            return e.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded;
        }
        /// <summary>
        /// Checks whether the exception is an operation cancelled exception
        /// </summary>
        private static bool IsOperationCancelledError(Grpc.Core.RpcException e, CancellationToken suppliedToken)
        {
            return e.StatusCode == Grpc.Core.StatusCode.Cancelled && suppliedToken.IsCancellationRequested;
        }

        // =========

        private readonly NodeAddress _nodeAddress;
        private readonly TimeSpan _operationTimeout;

#if GRPC_NET
        private readonly Grpc.Net.Client.GrpcChannel _rpcChannel;
#elif GRPC_LEGACY
        private readonly Grpc.Core.Channel _rpcChannel;
#endif
        private readonly BobStorage.BobApi.BobApiClient _rpcClient;

        private volatile int _state;
        private volatile bool _isDisposed;


        /// <summary>
        /// <see cref="BobNodeClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Address of a Bob node</param>
        /// <param name="operationTimeout">Timeout for every operation</param>
        public BobNodeClient(NodeAddress nodeAddress, TimeSpan operationTimeout)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));
            if (operationTimeout < TimeSpan.Zero && operationTimeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(operationTimeout));

            _nodeAddress = nodeAddress;
            _operationTimeout = operationTimeout;

#if GRPC_NET
            _rpcChannel = Grpc.Net.Client.GrpcChannel.ForAddress(nodeAddress.GetAddressAsUri(), 
                                                                 new Grpc.Net.Client.GrpcChannelOptions() 
                                                                 { 
                                                                    Credentials = Grpc.Core.ChannelCredentials.Insecure
                                                                 });
#elif GRPC_LEGACY
            _rpcChannel = new Grpc.Core.Channel(nodeAddress.Address, Grpc.Core.ChannelCredentials.Insecure);
#endif

            _rpcClient = new BobStorage.BobApi.BobApiClient(_rpcChannel);

            _state = (int)BobNodeClientState.Idle;
            _isDisposed = false;
        }
        /// <summary>
        /// <see cref="BobNodeClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Address of a Bob node</param>
        /// <param name="operationTimeout">Timeout for every operation</param>
        public BobNodeClient(string nodeAddress, TimeSpan operationTimeout)
            : this(new NodeAddress(nodeAddress), operationTimeout)
        {
        }
        /// <summary>
        /// <see cref="BobNodeClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Address of a Bob node</param>
        public BobNodeClient(string nodeAddress)
            : this(nodeAddress, DefaultOperationTimeout)
        {
        }

        /// <summary>
        /// Address of the Node
        /// </summary>
        public NodeAddress Address
        {
            get { return _nodeAddress; }
        }

        /// <summary>
        /// State of the client
        /// </summary>
        public BobNodeClientState State 
        {
            get { return (BobNodeClientState)_state; }
            private set { _state = (int)value; }
        }

        /// <summary>
        /// Attempts to atomically update State
        /// </summary>
        /// <param name="newState">New state</param>
        /// <param name="expectedState">Expected current state</param>
        /// <returns>Was state updated</returns>
        private bool TryUpdateStateAtomic(BobNodeClientState newState, BobNodeClientState expectedState)
        {
            return Interlocked.CompareExchange(ref _state, (int)newState, (int)expectedState) == (int)expectedState;
        }

        /// <summary>
        /// Notification about method running
        /// </summary>
        private void OnMethodRun()
        {
            SpinWait sw = new SpinWait();
            BobNodeClientState curState = State;
            while (curState == BobNodeClientState.Idle && !TryUpdateStateAtomic(BobNodeClientState.Connecting, curState))
            {
                sw.SpinOnce();
                curState = State;
            }
        }
        /// <summary>
        /// Notification about method successful completion
        /// </summary>
        private void OnMethodSuccess()
        {
            SpinWait sw = new SpinWait();
            BobNodeClientState curState = State;
            while ((curState == BobNodeClientState.Idle || curState == BobNodeClientState.Connecting || curState == BobNodeClientState.TransientFailure)
                    && !TryUpdateStateAtomic(BobNodeClientState.Ready, curState))
            {
                sw.SpinOnce();
                curState = State;
            }
        }
        /// <summary>
        /// Notification about method failure
        /// </summary>
        private void OnMethodFailure()
        {
            SpinWait sw = new SpinWait();
            BobNodeClientState curState = State;
            while ((curState == BobNodeClientState.Idle || curState == BobNodeClientState.Connecting || curState == BobNodeClientState.Ready)
                && !TryUpdateStateAtomic(BobNodeClientState.TransientFailure, curState))
            {
                sw.SpinOnce();
                curState = State;
            }
        }
        /// <summary>
        /// Notification about method cancelling or timeout
        /// </summary>
        private void OnMethodCancelledTimeouted()
        {
            SpinWait sw = new SpinWait();
            BobNodeClientState curState = State;
            while (curState == BobNodeClientState.Connecting && !TryUpdateStateAtomic(BobNodeClientState.TransientFailure, curState))
            {
                sw.SpinOnce();
                curState = State;
            }
        }
        /// <summary>
        /// Notification about client shoutdown
        /// </summary>
        private void OnShutdown()
        {
            State = BobNodeClientState.Shutdown;
        }



        /// <summary>
        /// Explicitly opens connection to the Bob node
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public async Task OpenAsync(TimeSpan timeout)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            try
            {
                OnMethodRun();
                await _rpcClient.PingAsync(new BobStorage.Null(), deadline: GetDeadline(timeout));
                OnMethodSuccess();
            }
            catch (TaskCanceledException tce)
            {
                OnMethodCancelledTimeouted();
                throw new TimeoutException($"Connection timeout reached (node: {_nodeAddress}, speciefied timeout: {timeout})", tce);
            }
            catch (Grpc.Core.RpcException rpce)
            {
                if (IsOperationTimeoutError(rpce))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Connection timeout reached (node: {_nodeAddress}, speciefied timeout: {timeout})", rpce);
                }

                OnMethodFailure();
                throw new BobOperationException($"Connection failed to node {_nodeAddress}", rpce);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }
        /// <summary>
        /// Explicitly opens connection to the Bob node
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public Task OpenAsync()
        {
            return OpenAsync(DefaultOperationTimeout);
        }
        /// <summary>
        /// Explicitly opens connection to the Bob node
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public void Open(TimeSpan timeout)
        {
            OpenAsync(timeout).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Explicitly opens connection to the Bob node
        /// </summary>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public void Open()
        {
            OpenAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Closes connection to the Bob node
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public async Task CloseAsync()
        {
            if (_isDisposed)
                return;

            try
            {
                await _rpcChannel.ShutdownAsync();
#if GRPC_NET
                _rpcChannel.Dispose();
#endif
                _isDisposed = true;
                OnShutdown();
            }
            catch (Grpc.Core.RpcException rpce)
            {
                OnMethodFailure();
                throw new BobOperationException($"Client closing failed for node {_nodeAddress}", rpce);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }
        /// <summary>
        /// Closes connection to the Bob node
        /// </summary>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public void Close()
        {
            CloseAsync().GetAwaiter().GetResult();
        }

        #region ============ Put ============

        /// <summary>
        /// Writes data to Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public void Put(ulong key, byte[] data, CancellationToken token)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.PutRequest(key, data);

            try
            {
                OnMethodRun();
                var answer = _rpcClient.Put(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                if (answer.Error != null)
                {
                    OnMethodFailure(); // Bob error is failure for the client too
                    throw new BobOperationException($"Put operation failed for key: {key} on node: {_nodeAddress}. Code: {answer.Error.Code}, Description: {answer.Error.Desc}");
                }

                OnMethodSuccess();
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Put operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Put operation failed for key: {key} on node: {_nodeAddress}", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Writes data to Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public void Put(ulong key, byte[] data)
        {
            Put(key, data, new CancellationToken());
        }

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public async Task PutAsync(ulong key, byte[] data, CancellationToken token)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.PutRequest(key, data);

            try
            {
                OnMethodRun();
                var answer = await _rpcClient.PutAsync(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                if (answer.Error != null)
                {
                    OnMethodFailure(); // Bob error is failure for the client too
                    throw new BobOperationException($"Put operation failed for key: {key} on node: {_nodeAddress}. Code: {answer.Error.Code}, Description: {answer.Error.Desc}");
                }

                OnMethodSuccess();
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Put operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Put operation failed for key: {key} on node: {_nodeAddress}", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public Task PutAsync(ulong key, byte[] data)
        {
            return PutAsync(key, data, new CancellationToken());
        }

        #endregion

        #region ============ Ping ============

        /// <summary>
        /// Sends Ping to Bob node
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected void Ping(CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            try
            {
                OnMethodRun();
                var answer = _rpcClient.Get(new BobStorage.GetRequest(), cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                OnMethodSuccess();
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Ping operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Ping operation failed on node: {_nodeAddress}", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Sends Ping to Bob node
        /// </summary>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected void Ping()
        {
            Ping(new CancellationToken());
        }


        /// <summary>
        /// Sends Ping to Bob node
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result Task</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected async Task PingAsync(CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            try
            {
                OnMethodRun();
                await _rpcClient.PingAsync(new BobStorage.Null(), cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                OnMethodSuccess();
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Ping operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Ping operation failed on node: {_nodeAddress}", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Sends Ping to Bob node
        /// </summary>
        /// <returns>Operation result Task</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected Task PingAsync()
        {
            return PingAsync(new CancellationToken());
        }

        #endregion

        #region ============ Get ============

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public byte[] Get(ulong key, bool fullGet, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.GetRequest(key, fullGet);

            try
            {
                OnMethodRun();
                var answer = _rpcClient.Get(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                var result = answer.Data.ToByteArray();
                OnMethodSuccess();
                return result;
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Get operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }
                if (IsKeyNotFoundError(e))
                {
                    OnMethodSuccess(); // Key not found is not the problem of the network channel or the Bob
                    throw new BobKeyNotFoundException($"Record for key = {key} is not found in Bob", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Get operation failed for key: {key} on node: {_nodeAddress}", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public byte[] Get(ulong key, CancellationToken token)
        {
            return Get(key, false, token);
        }

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public byte[] Get(ulong key, bool fullGet)
        {
            return Get(key, fullGet, new CancellationToken());
        }

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public byte[] Get(ulong key)
        {
            return Get(key, false, new CancellationToken());
        }


        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result with data</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public async Task<byte[]> GetAsync(ulong key, bool fullGet, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            var request = new BobStorage.GetRequest(key, fullGet);

            try
            {
                OnMethodRun();
                var answer = await _rpcClient.GetAsync(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                var result = answer.Data.ToByteArray();
                OnMethodSuccess();
                return result;
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Get operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }
                if (IsKeyNotFoundError(e))
                {
                    OnMethodSuccess(); // Key not found is not the problem of the network channel or the Bob
                    throw new BobKeyNotFoundException($"Record for key = {key} is not found in Bob", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Get operation failed for key: {key} on node: {_nodeAddress}", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result with data</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public Task<byte[]> GetAsync(ulong key, CancellationToken token)
        {
            return GetAsync(key, false, token);
        }

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result with data</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public Task<byte[]> GetAsync(ulong key, bool fullGet)
        {
            return GetAsync(key, fullGet, new CancellationToken());
        }

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Operation result with data</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public Task<byte[]> GetAsync(ulong key)
        {
            return GetAsync(key, false, new CancellationToken());
        }

        #endregion

        #region ============ Exists ============


        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="request">Prepared request</param>
        /// <param name="token">Cancellation token</param>
        /// /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        private bool[] Exists(BobStorage.ExistRequest request, CancellationToken token)
        {
            System.Diagnostics.Debug.Assert(request != null);
            System.Diagnostics.Debug.Assert(!_isDisposed);

            try
            {
                OnMethodRun();
                var answer = _rpcClient.Exist(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                var result = answer.Exist.ToArray();
                OnMethodSuccess();
                return result;
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Exists operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Exists operation failed on node: {_nodeAddress}.", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }


        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <param name="token">Cancellation token</param>
        /// /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(ulong[] keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys), "keys should not be null");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            return Exists(new BobStorage.ExistRequest(keys, fullGet), token);
        }

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(ulong[] keys, CancellationToken token)
        {
            return Exists(keys, false, token);
        }

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(ulong[] keys, bool fullGet)
        {
            return Exists(keys, fullGet, new CancellationToken());
        }

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(ulong[] keys)
        {
            return Exists(keys, false, new CancellationToken());
        }


        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <param name="token">Cancellation token</param>
        /// /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(IReadOnlyList<ulong> keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys), "keys should not be null");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            return Exists(new BobStorage.ExistRequest(keys, fullGet), token);
        }

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(IReadOnlyList<ulong> keys, CancellationToken token)
        {
            return Exists(keys, false, token);
        }

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(IReadOnlyList<ulong> keys, bool fullGet)
        {
            return Exists(keys, fullGet, new CancellationToken());
        }

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public bool[] Exists(IReadOnlyList<ulong> keys)
        {
            return Exists(keys, false, new CancellationToken());
        }



        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="request">Prepared request</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        private async Task<bool[]> ExistsAsync(BobStorage.ExistRequest request, CancellationToken token)
        {
            System.Diagnostics.Debug.Assert(request != null);
            System.Diagnostics.Debug.Assert(!_isDisposed);

            try
            {
                OnMethodRun();
                var answer = await _rpcClient.ExistAsync(request, cancellationToken: token, deadline: GetDeadline(_operationTimeout));
                var result = answer.Exist.ToArray();
                OnMethodSuccess();
                return result;
            }
            catch (Grpc.Core.RpcException e)
            {
                if (IsOperationCancelledError(e, token))
                {
                    OnMethodCancelledTimeouted();
                    throw new OperationCanceledException(token);
                }
                if (IsOperationTimeoutError(e))
                {
                    OnMethodCancelledTimeouted();
                    throw new TimeoutException($"Exists operation timeout reached (node: {_nodeAddress}, speciefied timeout: {_operationTimeout})", e);
                }

                OnMethodFailure();
                throw new BobOperationException($"Exists operation failed on node: {_nodeAddress}.", e);
            }
            catch
            {
                OnMethodFailure();
                throw;
            }
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public async Task<bool[]> ExistsAsync(ulong[] keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys), "keys should not be null");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            return await ExistsAsync(new BobStorage.ExistRequest(keys, fullGet), token);
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(ulong[] keys, CancellationToken token)
        {
            return ExistsAsync(keys, false, token);
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(ulong[] keys, bool fullGet)
        {
            return ExistsAsync(keys, fullGet, new CancellationToken());
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(ulong[] keys)
        {
            return ExistsAsync(keys, false, new CancellationToken());
        }


        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public async Task<bool[]> ExistsAsync(IReadOnlyList<ulong> keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys), "keys should not be null");
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            return await ExistsAsync(new BobStorage.ExistRequest(keys, fullGet), token);
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(IReadOnlyList<ulong> keys, CancellationToken token)
        {
            return ExistsAsync(keys, false, token);
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(IReadOnlyList<ulong> keys, bool fullGet)
        {
            return ExistsAsync(keys, fullGet, new CancellationToken());
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(IReadOnlyList<ulong> keys)
        {
            return ExistsAsync(keys, false, new CancellationToken());
        }

        #endregion


        /// <summary>
        ///  Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
#if GRPC_NET
                _rpcChannel.Dispose();
#else
                try
                {
                    this.Close();
                }
                catch { }
#endif
                OnShutdown();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
