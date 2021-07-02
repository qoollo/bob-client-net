using Qoollo.BobClient.KeySerializationArrayPools;
using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeyArrayPools
{
    public class SharedKeySerializationArrayPoolsTests
    {
        [Theory]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(long))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(int))]
        [InlineData(typeof(Guid))]
        public void GetOrCreateTest(Type keyType)
        {
            void CoreLogic<TKey>()
            {
                BobKeySerializer<TKey> keySerializer = null;
                Assert.True(BobDefaultKeySerializers.TryGetKeySerializer<TKey>(out keySerializer));

                ByteArrayPool pool = null;
                Assert.False(SharedKeySerializationArrayPools.TryGetSharedPool(keySerializer, out pool));

                pool = SharedKeySerializationArrayPools.GetOrCreateSharedPool(keySerializer);
                Assert.NotNull(pool);
                Assert.True(pool.MaxElementCount > 0);
                Assert.Equal(keySerializer.SerializedSize, pool.ByteArrayLength);

                var pool2 = SharedKeySerializationArrayPools.GetOrCreateSharedPool(keySerializer);
                Assert.True(object.ReferenceEquals(pool, pool2));

                Assert.True(SharedKeySerializationArrayPools.TryGetSharedPool(keySerializer, out pool2));
                Assert.True(object.ReferenceEquals(pool, pool2));
            }

            var referenceDelegate = new Action(CoreLogic<int>);
            var typedDelegate = (Action)Delegate.CreateDelegate(typeof(Action), referenceDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(keyType));
            typedDelegate();
        }
    }
}
