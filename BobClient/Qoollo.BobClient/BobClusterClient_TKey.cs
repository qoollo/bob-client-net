﻿using Qoollo.BobClient.KeySerializers;
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
    /// <typeparam name="TKey">Type of the Key</typeparam>
    public class BobClusterClient<TKey>: IBobApi<TKey>, IDisposable
    {
        private readonly BobClusterClient _innerCluster;
        private readonly BobKeySerializer<TKey> _keySerializer;

        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="innerCluster">Low-level BobClusterClient</param>
        /// <param name="keySerializer">Serializer for <typeparamref name="TKey"/> (null for default serializer)</param>
        protected internal BobClusterClient(BobClusterClient innerCluster, BobKeySerializer<TKey> keySerializer)
        {
            if (innerCluster == null)
                throw new ArgumentNullException(nameof(innerCluster));

            _innerCluster = innerCluster;
            _keySerializer = keySerializer;

            if (keySerializer == null && !BobDefaultKeySerializers.TryGetKeySerializer<TKey>(out _keySerializer))
                throw new ArgumentException($"KeySerializer is null and no default key serializer found for key type '{typeof(TKey).Name}'", nameof(keySerializer));
        }

        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="clients">List of clients for every bob node</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        /// <param name="keySerializer">Serializer for <typeparamref name="TKey"/> (null for default serializer)</param>
        public BobClusterClient(IEnumerable<BobNodeClient> clients, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, BobKeySerializer<TKey> keySerializer)
            : this(new BobClusterClient(clients, nodeSelectionPolicyFactory), keySerializer)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="clients">List of clients for every bob node</param>
        public BobClusterClient(IEnumerable<BobNodeClient> clients)
            : this(clients, (BobNodeSelectionPolicyFactory)null, (BobKeySerializer<TKey>)null)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        /// <param name="operationTimeout">Operation timeout for every created node client</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        /// <param name="keySerializer">Serializer for <typeparamref name="TKey"/> (null for default serializer)</param>
        public BobClusterClient(IEnumerable<NodeAddress> nodeAddress, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, BobKeySerializer<TKey> keySerializer, TimeSpan operationTimeout)
            : this(nodeAddress.Select(o => new BobNodeClient(o, operationTimeout)).ToList(), nodeSelectionPolicyFactory, keySerializer)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        public BobClusterClient(IEnumerable<NodeAddress> nodeAddress)
            : this(nodeAddress, (BobNodeSelectionPolicyFactory)null, (BobKeySerializer<TKey>)null, BobNodeClient.DefaultOperationTimeout)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        /// <param name="operationTimeout">Operation timeout for every created node client</param>
        /// <param name="nodeSelectionPolicyFactory">Factory to create node selection policy (null for <see cref="SequentialNodeSelectionPolicy"/>)</param>
        /// <param name="keySerializer">Serializer for <typeparamref name="TKey"/> (null for default serializer)</param>
        public BobClusterClient(IEnumerable<string> nodeAddress, BobNodeSelectionPolicyFactory nodeSelectionPolicyFactory, BobKeySerializer<TKey> keySerializer, TimeSpan operationTimeout)
            : this(nodeAddress.Select(o => new BobNodeClient(o, operationTimeout)).ToList(), nodeSelectionPolicyFactory, keySerializer)
        {
        }
        /// <summary>
        /// <see cref="BobClusterClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">List of nodes addresses</param>
        public BobClusterClient(IEnumerable<string> nodeAddress)
            : this(nodeAddress, (BobNodeSelectionPolicyFactory)null, (BobKeySerializer<TKey>)null, BobNodeClient.DefaultOperationTimeout)
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
        public Task OpenAsync(TimeSpan timeout)
        {
            return _innerCluster.OpenAsync(timeout);
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
            return _innerCluster.OpenAsync();
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
            _innerCluster.Open(timeout);
        }
        /// <summary>
        /// Explicitly opens connection to every Bob node in cluster
        /// </summary>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="TimeoutException">Specified timeout reached</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public void Open()
        {
            _innerCluster.Open();
        }


        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public Task CloseAsync()
        {
            return _innerCluster.CloseAsync();
        }
        /// <summary>
        /// Closes connections to every Bob node in cluster
        /// </summary>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public void Close()
        {
            _innerCluster.Close();
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
        public void Put(TKey key, byte[] data, CancellationToken token)
        {
            _innerCluster.Put(_keySerializer.SerializeToBobKey(key), data, token);
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
        public void Put(TKey key, byte[] data)
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
        public Task PutAsync(TKey key, byte[] data, CancellationToken token)
        {
            return _innerCluster.PutAsync(_keySerializer.SerializeToBobKey(key), data, token);
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
        public Task PutAsync(TKey key, byte[] data)
        {
            return PutAsync(key, data, default(CancellationToken));
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
        protected internal byte[] Get(TKey key, bool fullGet, CancellationToken token)
        {
            return _innerCluster.Get(_keySerializer.SerializeToBobKey(key), fullGet, token);
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
        public byte[] Get(TKey key, CancellationToken token)
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
        public byte[] Get(TKey key)
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
        protected internal Task<byte[]> GetAsync(TKey key, bool fullGet, CancellationToken token)
        {
            return _innerCluster.GetAsync(_keySerializer.SerializeToBobKey(key), fullGet, token);
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
        public Task<byte[]> GetAsync(TKey key, CancellationToken token)
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
        public Task<byte[]> GetAsync(TKey key)
        {
            return GetAsync(key, false, default(CancellationToken));
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
        protected internal bool[] Exists(TKey[] keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            BobKey[] bobKeyArray = new BobKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                bobKeyArray[i] = _keySerializer.SerializeToBobKey(keys[i]);

            return _innerCluster.Exists(bobKeyArray, fullGet, token);
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
        public bool[] Exists(TKey[] keys, CancellationToken token)
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
        public bool[] Exists(TKey[] keys)
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
        protected internal async Task<bool[]> ExistsAsync(TKey[] keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            BobKey[] bobKeyArray = new BobKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                bobKeyArray[i] = _keySerializer.SerializeToBobKey(keys[i]);

            return await _innerCluster.ExistsAsync(bobKeyArray, fullGet, token);
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
        public Task<bool[]> ExistsAsync(TKey[] keys, CancellationToken token)
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
        public Task<bool[]> ExistsAsync(TKey[] keys)
        {
            return ExistsAsync(keys, false, default(CancellationToken));
        }


        /// <summary>
        ///  Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">Was called by user</param>
        protected virtual void Dispose(bool isUserCall)
        {
            _innerCluster.Dispose();
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