using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Key serializer for <see cref="ulong"/>
    /// </summary>
    public sealed class UInt64BobKeySerializer : BobKeySerializer<ulong>
    {
        /// <summary>
        /// Singleton instance for <see cref="UInt64BobKeySerializer"/>
        /// </summary>
        public static UInt64BobKeySerializer Instance { get; } = new UInt64BobKeySerializer();


        /// <summary>
        /// Size of the key in bytes after serialization (8 bytes)
        /// </summary>
        public sealed override int SerializedSize { get { return sizeof(ulong); } }

        /// <summary>
        /// Serialize <paramref name="key"/> into <paramref name="byteArray"/>
        /// </summary>
        /// <param name="key">Key to serialize</param>
        /// <param name="byteArray">Byte array to store serialized key (should have length == <see cref="SerializedSize"/>)</param>
        public override void SerializeInto(ulong key, byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != sizeof(ulong))
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

            System.Runtime.CompilerServices.Unsafe.As<byte, ulong>(ref byteArray[0]) = key;

            if (!BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 4; i++)
                {
                    byte tmp = byteArray[i];
                    byteArray[i] = byteArray[7 - i];
                    byteArray[7 - i] = tmp;
                }
            }
        }

        /// <summary>
        /// Deserialize <paramref name="byteArray"/> into key of type <see cref="ulong"/>
        /// </summary>
        /// <param name="byteArray">Source byte array (should have length == <see cref="SerializedSize"/>)</param>
        /// <returns>Deserialized key</returns>
        public override ulong Deserialize(byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != sizeof(ulong))
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

            if (!BitConverter.IsLittleEndian)
            {
                uint a = ((uint)byteArray[0] << 24) | ((uint)byteArray[1] << 16) | ((uint)byteArray[2] << 8) | ((uint)byteArray[3]);
                uint b = ((uint)byteArray[4] << 24) | ((uint)byteArray[5] << 16) | ((uint)byteArray[6] << 8) | ((uint)byteArray[7]);
                return ((ulong)a << 32) | ((ulong)b);
            }

            return System.Runtime.CompilerServices.Unsafe.As<byte, ulong>(ref byteArray[0]);
        }
    }
}
