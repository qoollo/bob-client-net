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
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Incorrect timeout value</exception>
        public async Task OpenAsync(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            for (int i = 0; i < _clients.Length; i++)
                await _clients[i].OpenAsync(timeout);
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public async Task OpenAsync()
        {
            for (int i = 0; i < _clients.Length; i++)
                await _clients[i].OpenAsync();
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
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            for (int i = 0; i < _clients.Length; i++)
                _clients[i].Open(timeout);
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public void Open()
        {
            for (int i = 0; i < _clients.Length; i++)
                _clients[i].Open();
        }


        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public async Task CloseAsync()
        {
            for (int i = 0; i < _clients.Length; i++)
                await _clients[i].CloseAsync();
        }
        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public void Close()
        {
            for (int i = 0; i < _clients.Length; i++)
                _clients[i].Close();
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
        public void Put(ulong key, byte[] data)
        {
            SelectClient().Put(key, data);
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
        public Task PutAsync(ulong key, byte[] data, CancellationToken token)
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
        public Task PutAsync(ulong key, byte[] data)
        {
            return SelectClient().PutAsync(key, data);
        }


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
        protected internal byte[] Get(ulong key, bool fullGet, CancellationToken token)
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
        public byte[] Get(ulong key, CancellationToken token)
        {
            return SelectClient().Get(key, token);
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
            return SelectClient().Get(key);
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
        protected internal Task<byte[]> GetAsync(ulong key, bool fullGet, CancellationToken token)
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
        public Task<byte[]> GetAsync(ulong key, CancellationToken token)
        {
            return SelectClient().GetAsync(key, token);
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
            return SelectClient().GetAsync(key);
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
        protected internal bool[] Exists(ulong[] keys, bool fullGet, CancellationToken token)
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
        public bool[] Exists(ulong[] keys, CancellationToken token)
        {
            return SelectClient().Exists(keys, token);
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
            return SelectClient().Exists(keys);
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
        protected internal Task<bool[]> ExistsAsync(ulong[] keys, bool fullGet, CancellationToken token)
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
        public Task<bool[]> ExistsAsync(ulong[] keys, CancellationToken token)
        {
            return SelectClient().ExistsAsync(keys, token);
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
        public Task<bool[]> ExistsAsync(ulong[] keys)
        {
            return SelectClient().ExistsAsync(keys);
        }


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
