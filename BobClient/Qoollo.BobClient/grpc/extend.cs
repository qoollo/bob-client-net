using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public GetRequest(ulong key, bool fullGet = false)
        {
            Key = new BlobKey
            {
                Key = key
            };
            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal,
            };
        }
    }

    public sealed partial class ExistRequest
    {
        public ExistRequest(IEnumerable<ulong> keys, bool fullGet = false)
        {
            keys_ = new Google.Protobuf.Collections.RepeatedField<BlobKey>();
            keys_.AddRange(keys.Select(k => new BlobKey() { Key = k } ));
            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            }; 
        }
    }
}