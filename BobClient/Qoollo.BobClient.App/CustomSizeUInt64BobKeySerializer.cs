using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.App
{
    public class CustomSizeUInt64BobKeySerializer : KeySerializers.BobKeySerializer<ulong>
    {
        private readonly int _serializedSize;

        public CustomSizeUInt64BobKeySerializer(int targetKeySize)
        {
            if (targetKeySize <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetKeySize));

            _serializedSize = targetKeySize;
        }

        public override int SerializedSize { get { return _serializedSize; } }

        public override void SerializeInto(ulong key, byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != _serializedSize)
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

            if (byteArray.Length >= sizeof(ulong))
            {
                System.Runtime.CompilerServices.Unsafe.As<byte, ulong>(ref byteArray[0]) = key;
                for (int i = sizeof(ulong); i < byteArray.Length; i++)
                    byteArray[i] = 0;
            }
            else
            {
                for (int i = 0; i < byteArray.Length; i++)
                    byteArray[i] = (byte)(key >> (i * 8));
            }
        }

        public override ulong Deserialize(byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray));
            if (byteArray.Length != _serializedSize)
                throw new ArgumentException("Passed byte array has incorrect length", nameof(byteArray));

            if (byteArray.Length >= sizeof(ulong))
            {
                return System.Runtime.CompilerServices.Unsafe.As<byte, ulong>(ref byteArray[0]);
            }
            else
            {
                ulong result = 0;
                for (int i = 0; i < byteArray.Length; i++)
                    result |= ((ulong)byteArray[i]) << (i * 8);
                return result;
            }
        }
    }
}
