using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.KeySerializers
{
    /// <summary>
    /// Base non-generic interface for Bob Key serializer
    /// </summary>
    internal interface IBobKeySerializer
    {
        /// <summary>
        /// Size of the key in bytes after serialization
        /// </summary>
        int SerializedSize { get; }
        /// <summary>
        /// Type of the key
        /// </summary>
        Type KeyType { get; }
    }
}
