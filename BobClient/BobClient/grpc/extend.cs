using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BobStorage
{
    public sealed partial class PutRequest
    {
        public PutRequest(ulong key, byte[] data)
        {
            Key = new BlobKey
            {
                Key = key
            };
            Data = new Blob
            {
                Data = ByteString.CopyFrom(data),
                Meta = new BlobMeta { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };
        }
    }

    public sealed partial class GetRequest
    {
        public GetRequest(ulong key)
        {
            Key = new BlobKey
            {
                Key = key
            };
        }
    }
}