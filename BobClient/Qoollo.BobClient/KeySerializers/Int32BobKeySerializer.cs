using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Key serializer for <see cref="int"/>
    /// </summary>
    public sealed class Int32BobKeySerializer : BobKeySerializer<int>
    {
        /// <summary>
        /// Singleton instance for <see cref="Int32BobKeySerializer"/>
        /// </summary>
        public static Int32BobKeySerializer Instance { get; } = new Int32BobKeySerializer();


        /// <summary>
        /// Size of the key in bytes after serialization (4 bytes)
        /// </summary>
        public sealed override int SerializedSize { get { return sizeof(int); } }

        /// <summary>
        /// Serialize <paramref name="key"/> into <paramref name="byteArray"/>
        /// </summary>
        /// <param name="key">Key to serialize</param>
        /// <param name="byteArray">Byte array to store serialized key (should have length == <see cref="SerializedSize"/>)</param>
        public override void SerializeInto(int key, byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != sizeof(int))
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

            System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref byteArray[0]) = key;

#if !NETFRAMEWORK
            // NETFRAMEWORK always runs on LittleEndian architecture

            if (!BitConverter.IsLittleEndian)
            {
                byte tmp = byteArray[0];
                byteArray[0] = byteArray[3];
                byteArray[3] = tmp;

                tmp = byteArray[1];
                byteArray[1] = byteArray[2];
                byteArray[2] = tmp;
            }
#endif
        }

        /// <summary>
        /// Deserialize <paramref name="byteArray"/> into key of type <see cref="int"/>
        /// </summary>
        /// <param name="byteArray">Source byte array (should have length == <see cref="SerializedSize"/>)</param>
        /// <returns>Deserialized key</returns>
        public override int Deserialize(byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != sizeof(int))
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

            if (!BitConverter.IsLittleEndian)
            {
                return unchecked((int)(((uint)byteArray[0] << 24) | ((uint)byteArray[1] << 16) | ((uint)byteArray[2] << 8) | ((uint)byteArray[3])));
            }

            return System.Runtime.CompilerServices.Unsafe.As<byte, int>(ref byteArray[0]);
        }
    }
}
