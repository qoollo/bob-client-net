using Google.Protobuf;
using Qoollo.BobClient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BobStorage
{
    internal sealed partial class PutRequest
    {
        public PutRequest(Qoollo.BobClient.BobKey key, byte[] data)
        {
            Key = new BlobKey
            {
                //Key = ByteString.CopyFrom(key.GetKeyBytes())
                Key = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(key.GetKeyBytes())
            };
            Data = new Blob
            {
                //Data = ByteString.CopyFrom(data),
                Data = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(data),
                Meta = new BlobMeta { Timestamp = unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()) }
            };
        }
    }

    internal sealed partial class GetRequest
    {
        public GetRequest(Qoollo.BobClient.BobKey key, bool fullGet = false)
        {
            Key = new BlobKey
            {
                //Key = ByteString.CopyFrom(key.GetKeyBytes())
                Key = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(key.GetKeyBytes())
            };
            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal,
            };
        }
    }

    internal sealed partial class Blob
    {
        public byte[] ExtractData()
        {
            //return this.Data.ToByteArray();
            return ProtoBufByteStringHelper.ExtractByteArrayOptimized(this.Data);
        }
    }

    internal sealed partial class ExistRequest
    {
        public ExistRequest(IEnumerable<Qoollo.BobClient.BobKey> keys, bool fullGet)
        {
            foreach (var k in keys)
            {
                Keys.Add(new BlobKey()
                {
                    //Key = ByteString.CopyFrom(k.GetKeyBytes())
                    Key = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(k.GetKeyBytes())
                });
            }

            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            }; 
        }
        public ExistRequest(Qoollo.BobClient.BobKey[] keys, bool fullGet)
        {
            Keys.Capacity = keys.Length;
            for (int i = 0; i < keys.Length; i++)
            {
                Keys.Add(new BlobKey() 
                { 
                    //Key = ByteString.CopyFrom(keys[i].GetKeyBytes())
                    Key = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(keys[i].GetKeyBytes())
                });
            }

            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            };
        }
        public ExistRequest(IReadOnlyList<Qoollo.BobClient.BobKey> keys, bool fullGet)
        {
            Keys.Capacity = keys.Count;
            for (int i = 0; i < keys.Count; i++)
            {
                Keys.Add(new BlobKey() 
                { 
                    //Key = ByteString.CopyFrom(keys[i].GetKeyBytes())
                    Key = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(keys[i].GetKeyBytes())
                });
            }

            Options = new GetOptions
            {
                Source = fullGet ? GetSource.All : GetSource.Normal
            };
        }
    }

    internal sealed partial class ExistResponse
    {
        public bool[] ExtractExistResults()
        {
            bool[] result = new bool[this.Exist.Count];
            this.Exist.CopyTo(result, 0);
            return result;
        }
    }
}