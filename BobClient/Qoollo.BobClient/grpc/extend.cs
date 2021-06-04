using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;

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
                Source = fullGet ? GetSource.All : GetSource.Normal,
            };
        }
    }

    internal sealed partial class ExistRequest
    {
        public ExistRequest(IEnumerable<ulong> keys, bool fullGet = false)
        {
            keys_ = new Google.Protobuf.Collections.RepeatedField<BlobKey>();
            foreach (var k in keys)
                keys_.Add(new BlobKey() { Key = k });

            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            }; 
        }
        public ExistRequest(ulong[] keys, bool fullGet = false)
        {
            keys_ = new Google.Protobuf.Collections.RepeatedField<BlobKey>();
            for (int i = 0; i < keys.Length; i++)
                keys_.Add(new BlobKey() { Key = keys[i] });

            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            };
        }
        public ExistRequest(IReadOnlyList<ulong> keys, bool fullGet = false)
        {
            keys_ = new Google.Protobuf.Collections.RepeatedField<BlobKey>();
            for (int i = 0; i < keys.Count; i++)
                keys_.Add(new BlobKey() { Key = keys[i] });

            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            };
        }
    }
}