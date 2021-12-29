using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializationArrayPools
{
    /// <summary>
    /// Container for shared ByteArrayPool's that can be used for key serialization
    /// </summary>
    internal static class SharedKeySerializationArrayPools
    {
        /// <summary>
        /// Optimal size of the pool in bytes (~ memory page size)
        /// </summary>
        private const int OptimalPoolSizeInBytes = 4 * 1024;
        /// <summary>
        /// Minimal pool size to handle concurrent requests
        /// </summary>
        private static readonly int MinPoolSize = Environment.ProcessorCount * 2;

        /// <summary>
        /// Key for inner dictionary to store shared pools.
        /// Combines type of the serializer and size of the key
        /// </summary>
        private struct DictionaryKey : IEquatable<DictionaryKey>
        {
            public DictionaryKey(IBobKeySerializer keySerializer)
            {
                if (keySerializer == null)
                    throw new ArgumentNullException(nameof(keySerializer));

                SerializerType = keySerializer.GetType();
                KeySize = keySerializer.SerializedSize;
            }

            public readonly Type SerializerType;
            public readonly int KeySize;

            public bool Equals(DictionaryKey other)
            {
                return KeySize == other.KeySize && SerializerType == other.SerializerType;
            }
            public override bool Equals(object obj)
            {
                if (obj is DictionaryKey other)
                    return Equals(other);

                return false;
            }
            public override int GetHashCode()
            {
                return SerializerType.GetHashCode() ^ KeySize.GetHashCode();
            }
            public override string ToString()
            {
                return $"[{SerializerType}({KeySize})]";
            }
        }

        private static readonly Dictionary<DictionaryKey, ByteArrayPool> _sharedPools = new Dictionary<DictionaryKey, ByteArrayPool>();
        private static readonly object _syncObject = new object();


        /// <summary>
        /// Create ByteArrayPool with optimal size for specified key serializer
        /// </summary>
        /// <param name="keySerializer">Key serializer</param>
        /// <returns>Created ByteArrayPool</returns>
        private static ByteArrayPool CreateSharedPool(IBobKeySerializer keySerializer)
        {
            return new ByteArrayPool(keySerializer.SerializedSize, Math.Max(MinPoolSize, OptimalPoolSizeInBytes / keySerializer.SerializedSize));
        }
        /// <summary>
        /// Attempts to get shared pool for specified key serializer from inner storage
        /// </summary>
        /// <param name="keySerializer">Key serializer</param>
        /// <param name="pool">Extracted pool on success</param>
        /// <returns>True if pool for specific serializer is found</returns>
        public static bool TryGetSharedPool(IBobKeySerializer keySerializer, out ByteArrayPool pool)
        {
            if (keySerializer == null)
                throw new ArgumentNullException(nameof(keySerializer));

            lock (_syncObject)
            {
                return _sharedPools.TryGetValue(new DictionaryKey(keySerializer), out pool);
            }
        }
        /// <summary>
        /// Gets or creates shared pool for specified key serializer
        /// </summary>
        /// <param name="keySerializer">Key serializer</param>
        /// <returns>Create pool</returns>
        public static ByteArrayPool GetOrCreateSharedPool(IBobKeySerializer keySerializer)
        {
            if (keySerializer == null)
                throw new ArgumentNullException(nameof(keySerializer));

            lock (_syncObject)
            {
                ByteArrayPool result = null;
                if (_sharedPools.TryGetValue(new DictionaryKey(keySerializer), out result))
                    return result;

                result = CreateSharedPool(keySerializer);
                _sharedPools[new DictionaryKey(keySerializer)] = result;
                return result;
            }
        }
    }
}
