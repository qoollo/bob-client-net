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
        /// <summary>
        /// Attempts to get OperationRetryCount value from ConnectionParameters of clients.
        /// If some clients have OperationRetryCount in their connection strings and its values are the same, then method returns that value. Otherwise it returns 'null'
        /// </summary>
        /// <param name="clients">Clients collection</param>
        /// <returns>OperationRetryCount value</returns>
        private static int? TryGetOperationRetryCountFromNodesConnectionParameters(BobNodeClient[] clients)
        {
            if (clients == null)
                throw new ArgumentNullException(nameof(clients));

            return BobConnectionParameters.TryExtractValueFromMultipleParameters(clients.Select(o => o.ConnectionParameters), p => p.OperationRetryCount);
        }
        /// <summary>
        /// Attempts to get NodeSelectionPolicyFactory value from ConnectionParameters of clients.
        /// If some clients have NodeSelectionPolicyFactory in their connection strings and its values are the same, then method returns that value. Otherwise it returns 'null'
        /// </summary>
        /// <param name="clients">Clients collection</param>
        /// <returns>NodeSelectionPolicyFactory value</returns>
        private static BobNodeSelectionPolicyFactory TryGetNodeSelectionPolicyFactoryFromNodesConnectionParameters(BobNodeClient[] clients)
        {
            if (clients == null)
                throw new ArgumentNullException(nameof(clients));

            KnownBobNodeSelectionPolicies? result = BobConnectionParameters.TryExtractValueFromMultipleParameters(clients.Select(o => o.ConnectionParameters), p => p.NodeSelectionPolicy);
            return result != null ? BobNodeSelectionPolicyFactory.FromKnownNodeSelectionPolicy(result.Value) : null;
        }


        // ===========

        private readonly BobNodeClient[] _clients;
        private readonly BobNodeSelectionPolicy _selectionPolicy;
        private readonly int _operationRetryCount;

        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="clients">List of clients for every bob node</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        /// <param name="operationRetryCount">The number of times the operation retries in case of failure (null - default value (no retries), 0 - no retries, >= 1 - number of retries after failure, -1 - number of retries is equal to number of nodes)</param>
        public BobClusterClient(IEnumerable<BobNodeClient> clients, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, int? operationRetryCount)
        {
            if (clients == null)
                throw new ArgumentNullException(nameof(clients));

            _clients = clients.ToArray();

            if (_clients.Length == 0)
                throw new ArgumentException("Clients list cannot be empty", nameof(clients));
            for (int i = 0; i < _clients.Length; i++)
                if (_clients[i] == null)
                    throw new ArgumentNullException($"{nameof(clients)}[{i}]", "Client inside clients array cannot be null");

            nodeSelectionPolicyFactory = nodeSelectionPolicyFactory ?? TryGetNodeSelectionPolicyFactoryFromNodesConnectionParameters(_clients);
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

            operationRetryCount = operationRetryCount ?? TryGetOperationRetryCountFromNodesConnectionParameters(_clients);
            if (operationRetryCount == null)
                _operationRetryCount = 0;
            else if (operationRetryCount.Value < 0)
                _operationRetryCount = _clients.Length - 1;
            else
                _operationRetryCount = operationRetryCount.Value;
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="clients">List of clients for every bob node</param>
        public BobClusterClient(IEnumerable<BobNodeClient> clients)
            : this(clients, (BobNodeSelectionPolicyFactory)null, (int?)null)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeConnectionParameters">List of nodes connection parameters</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        /// <param name="operationRetryCount">The number of times the operation retries in case of failure (null - default value (no retries), 0 - no retries, >= 1 - number of retries after failure, -1 - number of retries is equal to number of nodes)</param>
        public BobClusterClient(IEnumerable<BobConnectionParameters> nodeConnectionParameters, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, int? operationRetryCount)
            : this(nodeConnectionParameters.Select(o => new BobNodeClient(o)).ToList(), nodeSelectionPolicyFactory, operationRetryCount)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeConnectionParameters">List of nodes connection parameters</param>
        public BobClusterClient(IEnumerable<BobConnectionParameters> nodeConnectionParameters)
            : this(nodeConnectionParameters, (BobNodeSelectionPolicyFactory)null, (int?)null)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeConnectionStrings">List of connection strings to nodes</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        /// <param name="operationRetryCount">The number of times the operation retries in case of failure (null - default value (no retries), 0 - no retries, >= 1 - number of retries after failure, -1 - number of retries is equal to number of nodes)</param>
        public BobClusterClient(IEnumerable<string> nodeConnectionStrings, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, int? operationRetryCount)
            : this(nodeConnectionStrings.Select(o => new BobNodeClient(o)).ToList(), nodeSelectionPolicyFactory, operationRetryCount)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeConnectionStrings">List of connection strings to nodes</param>
        public BobClusterClient(IEnumerable<string> nodeConnectionStrings)
            : this(nodeConnectionStrings, (BobNodeSelectionPolicyFactory)null, (int?)null)
        {
        }

        /// <summary>
        /// The number of times the operation retries in case of failure
        /// </summary>
        internal int OperationRetryCount { get { return _operationRetryCount; } }
        /// <summary>
        /// Connection parameters of all registered clients
        /// </summary>
        internal IEnumerable<BobConnectionParameters> ClientConnectionParameters { get { return _clients.Select(o => o.ConnectionParameters); } }


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
                catch (TimeoutException)
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
                catch (TimeoutException)
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
        /// <param name="operation">Operation for which the node selection is performing</param>
        /// <param name="key">Key for which the node selection is performing (can be empty)</param>
        /// <returns>Selected client</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SelectClientIndex(BobOperationKind operation, BobKey key)
        {
            int clientIndex = _selectionPolicy.SelectNodeIndex(operation, key);
            if (clientIndex < 0 || clientIndex > _clients.Length)
                ThrowSelectedIndexOutOfRange(clientIndex, _clients.Length);
            return clientIndex;
        }

        /// <summary>
        /// Selects next client for retry
        /// </summary>
        /// <param name="clientIndex">Node index</param>
        /// <param name="operation">Operation for which the node selection is performing</param>
        /// <param name="key">Key for which the node selection is performing (can be empty)</param>
        /// <returns>Selected client</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TrySelectClientIndexOnRetry(ref int clientIndex, BobOperationKind operation, BobKey key)
        {
            clientIndex = _selectionPolicy.SelectNodeIndexOnRetry(clientIndex, operation, key);
            if (clientIndex > _clients.Length)
                ThrowSelectedIndexOutOfRange(clientIndex, _clients.Length);
            return clientIndex >= 0;
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
            int clientIndex = SelectClientIndex(BobOperationKind.Put, key);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    _clients[clientIndex].Put(key, data, token);
                    return;
                }
                catch (BobOperationException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Put, key))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Put, key))
                        throw;
                }
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
        public async Task PutAsync(BobKey key, byte[] data, CancellationToken token)
        {
            int clientIndex = SelectClientIndex(BobOperationKind.Put, key);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    await _clients[clientIndex].PutAsync(key, data, token);
                    return;
                }
                catch (BobOperationException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Put, key))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Put, key))
                        throw;
                }
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
            int clientIndex = SelectClientIndex(BobOperationKind.Get, key);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    return _clients[clientIndex].Get(key, fullGet, token);
                }
                catch (BobOperationException ex) when (!(ex is BobKeyNotFoundException) && retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Get, key))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Get, key))
                        throw;
                }
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
        protected internal async Task<byte[]> GetAsync(BobKey key, bool fullGet, CancellationToken token)
        {
            int clientIndex = SelectClientIndex(BobOperationKind.Get, key);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    return await _clients[clientIndex].GetAsync(key, fullGet, token);
                }
                catch (BobOperationException ex) when (!(ex is BobKeyNotFoundException) && retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Get, key))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Get, key))
                        throw;
                }
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
            BobKey firstOrDefaultKey = keys.Length > 0 ? keys[0] : default(BobKey);
            int clientIndex = SelectClientIndex(BobOperationKind.Exist, firstOrDefaultKey);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    return _clients[clientIndex].Exists(keys, fullGet, token);
                }
                catch (BobOperationException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
            }
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
            BobKey firstOrDefaultKey = keys.Count > 0 ? keys[0] : default(BobKey);
            int clientIndex = SelectClientIndex(BobOperationKind.Exist, firstOrDefaultKey);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    return _clients[clientIndex].Exists(keys, fullGet, token);
                }
                catch (BobOperationException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
            }
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
        protected internal async Task<bool[]> ExistsAsync(BobKey[] keys, bool fullGet, CancellationToken token)
        {
            BobKey firstOrDefaultKey = keys.Length > 0 ? keys[0] : default(BobKey);
            int clientIndex = SelectClientIndex(BobOperationKind.Exist, firstOrDefaultKey);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    return await _clients[clientIndex].ExistsAsync(keys, fullGet, token);
                }
                catch (BobOperationException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
            }
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
        protected internal async Task<bool[]> ExistsAsync(IReadOnlyList<BobKey> keys, bool fullGet, CancellationToken token)
        {
            BobKey firstOrDefaultKey = keys.Count > 0 ? keys[0] : default(BobKey);
            int clientIndex = SelectClientIndex(BobOperationKind.Exist, firstOrDefaultKey);

            int retryCount = 0;
            while (true)
            {
                retryCount++;

                try
                {
                    return await _clients[clientIndex].ExistsAsync(keys, fullGet, token);
                }
                catch (BobOperationException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
                catch (TimeoutException) when (retryCount <= _operationRetryCount)
                {
                    if (!TrySelectClientIndexOnRetry(ref clientIndex, BobOperationKind.Exist, firstOrDefaultKey))
                        throw;
                }
            }
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
