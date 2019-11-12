using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BobStorage
{
    internal sealed partial class PutRequest
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

    internal sealed partial class GetRequest
    {
        public GetRequest(ulong key, bool fullGet = false)
        {
            Key = new BlobKey
            {
                Key = key
            };
            Options = new GetOptions
            {
                FullGet = fullGet
            };
        }
    }
}