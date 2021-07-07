using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob client for whole cluster (switch nodes according to the policy)
    /// </summary>
    public class BobClusterClient: IBobApi, IDisposable
    {
        private readonly BobNodeClient[] _clients;
        private readonly BobNodeSelectionPolicy _selectionPolicy;

        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="clients">List of clients for every bob node</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        public BobClusterClient(IEnumerable<BobNodeClient> clients, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory)
        {
            if (clients == null)
                throw new ArgumentNullException(nameof(clients));

            _clients = clients.ToArray();

            if (_clients.Length == 0)
                throw new ArgumentException("Clients list cannot be empty", nameof(clients));
            for (int i = 0; i < _clients.Length; i++)
                if (_clients[i] == null)
                    throw new ArgumentNullException($"{nameof(clients)}[{i}]", "Client inside clients array cannot be null");

            if (nodeSelectionPolicyFactory == null)
            {
                if (_clients.Length == 1)
                    _selectionPolicy = new FirstNodeSelectionPolicy(_clients);
                else
                    _selectionPolicy = new SequentialNodeSelectionPolicy(_clients);
            }
            else
            {
                _selectionPolicy = nodeSelectionPolicyFactory.Create(_clients);
            }
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="clients">List of clients for every bob node</param>
        public BobClusterClient(IEnumerable<BobNodeClient> clients)
            : this(clients, (BobNodeSelectionPolicyFactory)null)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        /// <param name="operationTimeout">Operation timeout for every created node client</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        public BobClusterClient(IEnumerable<NodeAddress> nodeAddress, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, TimeSpan operationTimeout)
            : this(nodeAddress.Select(o => new BobNodeClient(o, operationTimeout)).ToList(), nodeSelectionPolicyFactory)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        public BobClusterClient(IEnumerable<NodeAddress> nodeAddress)
            : this(nodeAddress, (BobNodeSelectionPolicyFactory)null, BobNodeClient.DefaultOperationTimeout)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        /// <param name="operationTimeout">Operation timeout for every created node client</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        public BobClusterClient(IEnumerable<string> nodeAddress, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, TimeSpan operationTimeout)
            : this(nodeAddress.Select(o => new BobNodeClient(o, operationTimeout)).ToList(), nodeSelectionPolicyFactory)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        public BobClusterClient(IEnumerable<string> nodeAddress)
            : this(nodeAddress, (BobNodeSelectionPolicyFactory)null, BobNodeClient.DefaultOperationTimeout)
        {
        }


        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <param name="mode">Mode that contols open error handling</param>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public async Task OpenAsync(TimeSpan timeout, BobClusterOpenCloseMode mode)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    await _clients[i].OpenAsync(timeout);
                }
                catch (BobOperationException)
                {
                    if (mode == BobClusterOpenCloseMode.ThrowOnFirstError)
                        throw;
                }
            }
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public Task OpenAsync(TimeSpan timeout)
        {
            return OpenAsync(timeout, BobClusterOpenCloseMode.ThrowOnFirstError);
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <param name="mode">Mode that contols open error handling</param>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public async Task OpenAsync(BobClusterOpenCloseMode mode)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    await _clients[i].OpenAsync();
                }
                catch (BobOperationException)
                {
                    if (mode == BobClusterOpenCloseMode.ThrowOnFirstError)
                        throw;
                }
            }
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public Task OpenAsync()
        {
            return OpenAsync(BobClusterOpenCloseMode.ThrowOnFirstError);
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <param name="mode">Mode that contols open error handling</param>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public void Open(TimeSpan timeout, BobClusterOpenCloseMode mode)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    _clients[i].Open(timeout);
                }
                catch (BobOperationException)
                {
                    if (mode == BobClusterOpenCloseMode.ThrowOnFirstError)
                        throw;
                }
            }
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public void Open(TimeSpan timeout)
        {
            Open(timeout, BobClusterOpenCloseMode.ThrowOnFirstError);
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <param name="mode">Mode that contols open error handling</param>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public void Open(BobClusterOpenCloseMode mode)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    _clients[i].Open();
                }
                catch (BobOperationException)
                {
                    if (mode == BobClusterOpenCloseMode.ThrowOnFirstError)
                        throw;
                }
            }
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public void Open()
        {
            Open(BobClusterOpenCloseMode.ThrowOnFirstError);
        }


        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <param name="mode">Mode that contols close error handling</param>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public async Task CloseAsync(BobClusterOpenCloseMode mode)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    await _clients[i].CloseAsync();
                }
                catch (BobOperationException)
                {
                    if (mode == BobClusterOpenCloseMode.ThrowOnFirstError)
                        throw;
                }
            }
        }
        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public Task CloseAsync()
        {
            return CloseAsync(BobClusterOpenCloseMode.ThrowOnFirstError);
        }
        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <param name="mode">Mode that contols close error handling</param>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public void Close(BobClusterOpenCloseMode mode)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                try
                {
                    _clients[i].Close();
                }
                catch (BobOperationException)
                {
                    if (mode == BobClusterOpenCloseMode.ThrowOnFirstError)
                        throw;
                }
            }
        }
        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public void Close()
        {
            Close(BobClusterOpenCloseMode.ThrowOnFirstError);
        }

        /// <summary>
        /// Throws IndexOutOfRangeException when client index is incorrect
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSelectedIndexOutOfRange(int index, int clientsCount)
        {
            throw new IndexOutOfRangeException($"NodeSelectionPolicy returned node index that is out of range (index: {index}, number of clients: {clientsCount}");
        }

        /// <summary>
        /// Selects next client
        /// </summary>
        /// <returns>Selected client</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BobNodeClient SelectClient()
        {
            int clientIndex = _selectionPolicy.SelectNextNodeIndex();
            if (clientIndex < 0 || clientIndex > _clients.Length)
                ThrowSelectedIndexOutOfRange(clientIndex, _clients.Length);
            return _clients[clientIndex];
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
        public void Put(BobKey key, byte[] data, CancellationToken token)
        {
            SelectClient().Put(key, data, token);
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
        public void Put(BobKey key, byte[] data)
        {
            Put(key, data, default(CancellationToken));
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
        public Task PutAsync(BobKey key, byte[] data, CancellationToken token)
        {
            return SelectClient().PutAsync(key, data, token);
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
        public Task PutAsync(BobKey key, byte[] data)
        {
            return PutAsync(key, data, default(CancellationToken));
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
        protected internal byte[] Get(BobKey key, bool fullGet, CancellationToken token)
        {
            return SelectClient().Get(key, fullGet, token);
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
        public byte[] Get(BobKey key, CancellationToken token)
        {
            return Get(key, false, token);
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
        public byte[] Get(BobKey key)
        {
            return Get(key, false, default(CancellationToken));
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
        protected internal Task<byte[]> GetAsync(BobKey key, bool fullGet, CancellationToken token)
        {
            return SelectClient().GetAsync(key, fullGet, token);
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
        public Task<byte[]> GetAsync(BobKey key, CancellationToken token)
        {
            return GetAsync(key, false, token);
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
        public Task<byte[]> GetAsync(BobKey key)
        {
            return GetAsync(key, false, default(CancellationToken));
        }

        #endregion

        #region ============ Exists =========

        /// <summary>
        /// Checks data presented in Bob
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
        protected internal bool[] Exists(BobKey[] keys, bool fullGet, CancellationToken token)
        {
            return SelectClient().Exists(keys, fullGet, token);
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
        public bool[] Exists(BobKey[] keys, CancellationToken token)
        {
            return Exists(keys, false, token);
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
        public bool[] Exists(BobKey[] keys)
        {
            return Exists(keys, false, default(CancellationToken));
        }


        /// <summary>
        /// Checks data presented in Bob
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
        protected internal bool[] Exists(IReadOnlyList<BobKey> keys, bool fullGet, CancellationToken token)
        {
            return SelectClient().Exists(keys, fullGet, token);
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
        public bool[] Exists(IReadOnlyList<BobKey> keys, CancellationToken token)
        {
            return Exists(keys, false, token);
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
        public bool[] Exists(IReadOnlyList<BobKey> keys)
        {
            return Exists(keys, false, default(CancellationToken));
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
        protected internal Task<bool[]> ExistsAsync(BobKey[] keys, bool fullGet, CancellationToken token)
        {
            return SelectClient().ExistsAsync(keys, fullGet, token);
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
        public Task<bool[]> ExistsAsync(BobKey[] keys, CancellationToken token)
        {
            return ExistsAsync(keys, false, token);
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(BobKey[] keys)
        {
            return ExistsAsync(keys, false, default(CancellationToken));
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
        protected internal Task<bool[]> ExistsAsync(IReadOnlyList<BobKey> keys, bool fullGet, CancellationToken token)
        {
            return SelectClient().ExistsAsync(keys, fullGet, token);
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
        public Task<bool[]> ExistsAsync(IReadOnlyList<BobKey> keys, CancellationToken token)
        {
            return ExistsAsync(keys, false, token);
        }

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        public Task<bool[]> ExistsAsync(IReadOnlyList<BobKey> keys)
        {
            return ExistsAsync(keys, false, default(CancellationToken));
        }

        #endregion

        /// <summary>
        ///  Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected virtual void Dispose(bool isUserCall)
        {
            for (int i = 0; i < _clients.Length; i++)
                _clients[i].Dispose();
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
