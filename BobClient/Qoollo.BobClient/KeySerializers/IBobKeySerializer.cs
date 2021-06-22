using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    public interface IBobKeySerializer<TKey>
    {
        int SerializedSize { get; }

        void SerializeInto(TKey key, byte[] targetArray);
        TKey Deserialize(byte[] byteArray);
    }
}
