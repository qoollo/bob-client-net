using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Serializer, that reverse byte order in key (for test purposes)
    /// </summary>
    internal sealed class ReverseByteOrderBobKeySerializer<TKey> : BobKeySerializer<TKey>
    {
        private readonly BobKeySerializer<TKey> _innerSerializer;

        /// <summary>
        /// <see cref="ReverseByteOrderBobKeySerializer{TKey}"/> constructor
        /// </summary>
        /// <param name="innerSerializer">Inner serializer</param>
        public ReverseByteOrderBobKeySerializer(BobKeySerializer<TKey> innerSerializer)
        {
            if (innerSerializer == null)
                throw new ArgumentNullException(nameof(innerSerializer));

            _innerSerializer = innerSerializer;
        }

        /// <inheritdoc/>
        public override int SerializedSize { get { return _innerSerializer.SerializedSize; } }

        /// <inheritdoc/>
        public override void SerializeInto(TKey key, byte[] byteArray)
        {
            _innerSerializer.SerializeInto(key, byteArray);

            for (int i = 0; i < byteArray.Length / 2; i++)
            {
                byte tmp = byteArray[i];
                byteArray[i] = byteArray[byteArray.Length - i - 1];
                byteArray[byteArray.Length - i - 1] = tmp;
            }
        }

        /// <inheritdoc/>
        public override TKey Deserialize(byte[] byteArray)
        {
            byte[] byteArrayCopy = new byte[byteArray.Length];
            for (int i = 0; i < byteArray.Length; i++)
                byteArrayCopy[byteArrayCopy.Length - i - 1] = byteArray[i];

            return _innerSerializer.Deserialize(byteArrayCopy);
        }
    }
}
