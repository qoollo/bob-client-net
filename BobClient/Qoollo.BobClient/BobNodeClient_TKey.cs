using Qoollo.BobClient.KeySerializationArrayPools;
using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Client for a single Bob node
    /// </summary>
    /// <typeparam name="TKey">Type of the Key</typeparam>
    [System.Diagnostics.DebuggerDisplay("[Bob Node: {Address.Address}, State: {State}]")]
    public class BobNodeClient<TKey>: IBobApi<TKey>, IBobNodeClientStatus, IDisposable
    {
        /// <summary>
        /// Default operation timeout
        /// </summary>
        public static readonly TimeSpan DefaultOperationTimeout = BobNodeClient.DefaultOperationTimeout;


        private readonly BobNodeClient _innerClient;
        private readonly BobKeySerializer<TKey> _keySerializer;
        private readonly ByteArrayPool _keySerializationPool;

        /// <summary>
        /// <see cref="BobNodeClient"/> constructor
        /// </summary>
        /// <param name="innerClient">Low-level BobNodeClient</param>
        /// <param name="keySerializer">Serializer for <typeparamref name="TKey"/>  (null for default serializer)</param>
        /// <param name="keySerializationPoolSize">Size of the Key serialization pool (null - shared pool, 0 or less - pool is disabled, 1 or greater - custom pool with specified size)</param>
        protected internal BobNodeClient(BobNodeClient innerClient, BobKeySerializer<TKey> keySerializer, int? keySerializationPoolSize)
        {
            if (innerClient == null)
                throw new ArgumentNullException(nameof(innerClient));

            _innerClient = innerClient;
            _keySerializer = keySerializer;

            if (keySerializer == null && !BobDefaultKeySerializers.TryGetKeySerializer<TKey>(out _keySerializer))
                throw new ArgumentException($"KeySerializer is null and no default key serializer found for key type '{typeof(TKey).Name}'", nameof(keySerializer));

            if (keySerializationPoolSize == null)
                _keySerializationPool = SharedKeySerializationArrayPools.GetOrCreateSharedPool(_keySerializer);
            else if (keySerializationPoolSize.Value > 0)
                _keySerializationPool = new ByteArrayPool(_keySerializer.SerializedSize, keySerializationPoolSize.Value);
            else
                _keySerializationPool = null;
        }

        /// <summary>
        /// <see cref="BobNodeClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Address of a Bob node</param>
        /// <param name="operationTimeout">Timeout for every operation</param>
        /// <param name="keySerializer">Serializer for <typeparamref name="TKey"/> (null for default serializer)</param>
        /// <param name="keySerializationPoolSize">Size of the Key serialization pool (null - shared pool, 0 or less - pool is disabled, 1 or greater - custom pool with specified size)</param>
        public BobNodeClient(NodeAddress nodeAddress, TimeSpan operationTimeout, BobKeySerializer<TKey> keySerializer, int? keySerializationPoolSize)
            : this(new BobNodeClient(nodeAddress, operationTimeout), keySerializer, keySerializationPoolSize)
        {
        }
        /// <summary>
        /// <see cref="BobNodeClient"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Address of a Bob node</param>
        /// <param name="operationTimeout">Timeout for every operation</param>
        public BobNodeClient(NodeAddress nodeAddress, TimeSpan operationTimeout)
            : this(new BobNodeClient(nodeAddress, operationTimeout), null, null)
        {
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
            get { return _innerClient.Address; }
        }

        /// <summary>
        /// State of the client
        /// </summary>
        public BobNodeClientState State 
        {
            get { return _innerClient.State; }
        }

        /// <summary>
        /// Number of sequential errors
        /// </summary>
        public int SequentialErrorCount
        {
            get { return _innerClient.SequentialErrorCount; }
        }

        /// <summary>
        /// Time elapsed since the last operation started (in milliseconds)
        /// </summary>
        public int TimeSinceLastOperationMs
        {
            get { return _innerClient.TimeSinceLastOperationMs; }
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
        public Task OpenAsync(TimeSpan timeout)
        {
            return _innerClient.OpenAsync(timeout);
        }
        /// <summary>
        /// Explicitly opens connection to the Bob node
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public Task OpenAsync()
        {
            return _innerClient.OpenAsync();
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
            _innerClient.Open(timeout);
        }
        /// <summary>
        /// Explicitly opens connection to the Bob node
        /// </summary>
        /// <exception cref="BobOperationException">Connection was not opened</exception>
        /// <exception cref="ObjectDisposedException">Client was disposed</exception>
        public void Open()
        {
            _innerClient.Open();
        }

        /// <summary>
        /// Closes connection to the Bob node
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public Task CloseAsync()
        {
            return _innerClient.CloseAsync();
        }
        /// <summary>
        /// Closes connection to the Bob node
        /// </summary>
        /// <exception cref="BobOperationException">Error during connection shutdown</exception>
        public void Close()
        {
            _innerClient.Close();
        }

        #region ============ Key serialization helpers ============

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BobKey SerializeToBobKeyFromPool(TKey key, bool skipLocalInPool)
        {
            byte[] array = _keySerializationPool?.Rent(skipLocalInPool) ?? new byte[_keySerializer.SerializedSize];
            _keySerializer.SerializeInto(key, array);
            return new BobKey(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseBobKeyToPool(BobKey key, bool skipLocalInPool)
        {
            _keySerializationPool?.Release(key.GetKeyBytes(), skipLocalInPool);
        }

        #endregion

        #region ============ Put ============

        /// <summary>
        /// Writes data to Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public void Put(TKey key, byte[] data, CancellationToken token)
        {
            BobKey bobKey = SerializeToBobKeyFromPool(key, skipLocalInPool: false);
            try
            {
                _innerClient.Put(bobKey, data, token);
            }
            finally
            {
                ReleaseBobKeyToPool(bobKey, skipLocalInPool: false);
            }
        }

        /// <summary>
        /// Writes data to Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public void Put(TKey key, byte[] data)
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
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public async Task PutAsync(TKey key, byte[] data, CancellationToken token)
        {
            var bobKey = SerializeToBobKeyFromPool(key, skipLocalInPool: true);
            try
            {
                await _innerClient.PutAsync(bobKey, data, token);
            }
            finally
            {
                ReleaseBobKeyToPool(bobKey, skipLocalInPool: true);
            }
        }

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ArgumentNullException">Data is null</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public Task PutAsync(TKey key, byte[] data)
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
        protected internal void Ping(CancellationToken token)
        {
            _innerClient.Ping(token);
        }

        /// <summary>
        /// Sends Ping to Bob node
        /// </summary>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected internal void Ping()
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
        protected internal Task PingAsync(CancellationToken token)
        {
            return _innerClient.PingAsync(token);
        }

        /// <summary>
        /// Sends Ping to Bob node
        /// </summary>
        /// <returns>Operation result Task</returns>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected internal Task PingAsync()
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
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected internal byte[] Get(TKey key, bool fullGet, CancellationToken token)
        {
            BobKey bobKey = SerializeToBobKeyFromPool(key, skipLocalInPool: false);
            try
            {
                return _innerClient.Get(bobKey, fullGet, token);
            }
            finally
            {
                ReleaseBobKeyToPool(bobKey, skipLocalInPool: false);
            }
        }

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        /// <exception cref="ArgumentException">key is not specified</exception>
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
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public byte[] Get(TKey key)
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
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        protected internal async Task<byte[]> GetAsync(TKey key, bool fullGet, CancellationToken token)
        {
            BobKey bobKey = SerializeToBobKeyFromPool(key, skipLocalInPool: true);
            try
            {
                return await _innerClient.GetAsync(bobKey, fullGet, token);
            }
            finally
            {
                ReleaseBobKeyToPool(bobKey, skipLocalInPool: true);
            }
        }

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result with data</returns>
        /// <exception cref="ArgumentException">key is not specified</exception>
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
        /// <exception cref="ArgumentException">key is not specified</exception>
        /// <exception cref="ObjectDisposedException">Client was closed</exception>
        /// <exception cref="TimeoutException">Timeout reached</exception>
        /// <exception cref="BobKeyNotFoundException">Specified key was not found</exception>
        /// <exception cref="BobOperationException">Other operation errors</exception>
        public Task<byte[]> GetAsync(TKey key)
        {
            return GetAsync(key, false, new CancellationToken());
        }

        #endregion

        #region ============ Exists ============


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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        protected internal bool[] Exists(TKey[] keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            BobKey[] bobKeyArray = new BobKey[keys.Length];
            try
            {
                for (int i = 0; i < keys.Length; i++)
                    bobKeyArray[i] = SerializeToBobKeyFromPool(keys[i], skipLocalInPool: i >= 1);

                return _innerClient.Exists(bobKeyArray, fullGet, token);
            }
            finally
            {
                for (int i = 0; i < bobKeyArray.Length; i++)
                    ReleaseBobKeyToPool(bobKeyArray[i], skipLocalInPool: i >= 1);
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        public bool[] Exists(TKey[] keys)
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        protected internal bool[] Exists(IReadOnlyList<TKey> keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            BobKey[] bobKeyArray = new BobKey[keys.Count];
            try
            {
                for (int i = 0; i < keys.Count; i++)
                    bobKeyArray[i] = SerializeToBobKeyFromPool(keys[i], skipLocalInPool: i >= 1);

                return _innerClient.Exists(bobKeyArray, fullGet, token);
            }
            finally
            {
                for (int i = 0; i < bobKeyArray.Length; i++)
                    ReleaseBobKeyToPool(bobKeyArray[i], skipLocalInPool: i >= 1);
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        public bool[] Exists(IReadOnlyList<TKey> keys, CancellationToken token)
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        public bool[] Exists(IReadOnlyList<TKey> keys)
        {
            return Exists(keys, false, new CancellationToken());
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        protected internal async Task<bool[]> ExistsAsync(TKey[] keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            BobKey[] bobKeyArray = new BobKey[keys.Length];
            try
            {
                for (int i = 0; i < keys.Length; i++)
                    bobKeyArray[i] = SerializeToBobKeyFromPool(keys[i], skipLocalInPool: true);

                return await _innerClient.ExistsAsync(bobKeyArray, fullGet, token);
            }
            finally
            {
                for (int i = 0; i < bobKeyArray.Length; i++)
                    ReleaseBobKeyToPool(bobKeyArray[i], skipLocalInPool: true);
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
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
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        public Task<bool[]> ExistsAsync(TKey[] keys)
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        protected internal async Task<bool[]> ExistsAsync(IReadOnlyList<TKey> keys, bool fullGet, CancellationToken token)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            BobKey[] bobKeyArray = new BobKey[keys.Count];
            try
            {
                for (int i = 0; i < keys.Count; i++)
                    bobKeyArray[i] = SerializeToBobKeyFromPool(keys[i], skipLocalInPool: true);

                return await _innerClient.ExistsAsync(bobKeyArray, fullGet, token);
            }
            finally
            {
                for (int i = 0; i < bobKeyArray.Length; i++)
                    ReleaseBobKeyToPool(bobKeyArray[i], skipLocalInPool: true);
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
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        public Task<bool[]> ExistsAsync(IReadOnlyList<TKey> keys, CancellationToken token)
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
        /// <exception cref="BobOperationException">Other operation errors</exception>
        /// <exception cref="ArgumentNullException">keys is null</exception>
        /// <exception cref="ArgumentException">At least one key in <paramref name="keys"/> array is not specified</exception>
        public Task<bool[]> ExistsAsync(IReadOnlyList<TKey> keys)
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
            _innerClient.Dispose();
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
