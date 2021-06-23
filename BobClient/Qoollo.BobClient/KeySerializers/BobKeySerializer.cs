using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Key serializer. Converts Key of type <typeparamref name="TKey"/> to its byte array representation
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract class BobKeySerializer<TKey>
    {
        /// <summary>
        /// Size of the key in bytes after serialization
        /// </summary>
        public abstract int SerializedSize { get; }

        /// <summary>
        /// Serialize <paramref name="key"/> into <paramref name="byteArray"/>
        /// </summary>
        /// <param name="key">Key to serialize</param>
        /// <param name="byteArray">Byte array to store serialized key (should have length == <see cref="SerializedSize"/>)</param>
        public abstract void SerializeInto(TKey key, byte[] byteArray);

        /// <summary>
        /// Deserialize <paramref name="byteArray"/> into key of type <typeparamref name="TKey"/>
        /// </summary>
        /// <param name="byteArray">Source byte array (should have length == <see cref="SerializedSize"/>)</param>
        /// <returns>Deserialized key</returns>
        public abstract TKey Deserialize(byte[] byteArray);


        /// <summary>
        /// Serialize <paramref name="key"/> into <see cref="BobKey"/>
        /// </summary>
        /// <param name="key">Key to serialize</param>
        /// <returns>BobKey</returns>
        internal BobKey SerializeToBobKey(TKey key)
        {
            byte[] array = new byte[SerializedSize];
            SerializeInto(key, array);
            return new BobKey(array);
        }
        /// <summary>
        /// Deserialize <see cref="BobKey"/> to key of type <typeparamref name="TKey"/>
        /// </summary>
        /// <param name="bobKey">Bob key</param>
        /// <returns>Deserialized key</returns>
        internal TKey DeserializeFromBobKey(BobKey bobKey)
        {
            return Deserialize(bobKey.GetKeyBytes());
        }
    }
}
