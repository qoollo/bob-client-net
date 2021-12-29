using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Key serializer for <see cref="Guid"/>
    /// </summary>
    public sealed class GuidBobKeySerializer : BobKeySerializer<Guid>
    {
        /// <summary>
        /// sizeof(Guid)
        /// </summary>
        private const int GuidSize = 16;

        /// <summary>
        /// Singleton instance for <see cref="GuidBobKeySerializer"/>
        /// </summary>
        public static GuidBobKeySerializer Instance { get; } = new GuidBobKeySerializer();

        /// <summary>
        /// True if 'MemoryMarshal.TryWrite' can be used to serialize Guid
        /// </summary>
        private static readonly bool _canUseMemoryMarshal = CanUseMemoryMarshal();

        /// <summary>
        /// Detect whether 'MemoryMarshal.TryWrite' can be safely used on current framework to serialize Guid
        /// </summary>
        /// <returns>True if 'MemoryMarshal.TryWrite' can be used to serialize Guid</returns>
        private static bool CanUseMemoryMarshal()
        {
#if NET5_0_OR_GREATER
            return false;
#else
            try
            {
                var guid = Guid.Parse("c707a3dd-9a18-41da-96f8-9c96ba4e338f");
                byte[] guidBytes = new byte[GuidSize];
                if (!System.Runtime.InteropServices.MemoryMarshal.TryWrite<Guid>(guidBytes, ref Unsafe.AsRef(in guid)))
                    return false;

                if (BitConverter.IsLittleEndian)
                {
                    byte[] expectedBytes = new byte[] { 221, 163, 7, 199, 24, 154, 218, 65, 150, 248, 156, 150, 186, 78, 51, 143 };
                    return Enumerable.SequenceEqual(guidBytes, expectedBytes);
                }
                else
                {
                    byte[] expectedBytes = new byte[] { 199, 7, 163, 221, 154, 24, 65, 218, 150, 248, 156, 150, 186, 78, 51, 143 };
                    return Enumerable.SequenceEqual(guidBytes, expectedBytes);
                }
            }
            catch
            {
                return false;
            }
#endif
        }


        /// <summary>
        /// Swap bytes in array
        /// </summary>
        /// <param name="guidBytes">byte array</param>
        /// <param name="aIndex">First index</param>
        /// <param name="bIndex">Second index</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SwapBytes(byte[] guidBytes, int aIndex, int bIndex)
        {
            byte tmp = guidBytes[aIndex];
            guidBytes[aIndex] = guidBytes[bIndex];
            guidBytes[bIndex] = tmp;
        }
        /// <summary>
        /// Revere endianness int guid bytes (platform specific, onlt if <see cref="_canUseMemoryMarshal"/> is true)
        /// </summary>
        /// <param name="guidBytes">Guid bytes</param>
        private static void ReverseEndianness(byte[] guidBytes)
        {
            SwapBytes(guidBytes, 0, 3);
            SwapBytes(guidBytes, 1, 2);

            SwapBytes(guidBytes, 4, 5);

            SwapBytes(guidBytes, 6, 7);
        }


        /// <summary>
        /// Size of the key in bytes after serialization (16 bytes)
        /// </summary>
        public sealed override int SerializedSize { get { return GuidSize; } }

        /// <summary>
        /// Serialize <paramref name="key"/> into <paramref name="byteArray"/>
        /// </summary>
        /// <param name="key">Key to serialize</param>
        /// <param name="byteArray">Byte array to store serialized key (should have length == <see cref="SerializedSize"/>)</param>
        public override void SerializeInto(Guid key, byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != GuidSize)
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

#if NET5_0_OR_GREATER
            key.TryWriteBytes(byteArray);
#else
            if (_canUseMemoryMarshal)
            {
                System.Runtime.InteropServices.MemoryMarshal.TryWrite<Guid>(byteArray, ref Unsafe.AsRef(in key));
                if (!BitConverter.IsLittleEndian)
                    ReverseEndianness(byteArray);
            }
            else
            {
                byte[] tmpBytes = key.ToByteArray();
                Array.Copy(tmpBytes, byteArray, GuidSize);
            }
#endif
        }

        /// <summary>
        /// Deserialize <paramref name="byteArray"/> into key of type <see cref="ulong"/>
        /// </summary>
        /// <param name="byteArray">Source byte array (should have length == <see cref="SerializedSize"/>)</param>
        /// <returns>Deserialized key</returns>
        public override Guid Deserialize(byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != GuidSize)
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

#if NET5_0_OR_GREATER
            return new Guid(byteArray);
#else
            return new Guid(byteArray);
#endif
        }
    }
}
