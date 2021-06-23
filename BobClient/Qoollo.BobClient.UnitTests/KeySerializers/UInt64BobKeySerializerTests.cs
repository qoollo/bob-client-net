using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeySerializers
{
    public class UInt64BobKeySerializerTests
    {
        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(sizeof(ulong), UInt64BobKeySerializer.Instance.SerializedSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(123121412412412)]
        [InlineData(uint.MaxValue)]
        [InlineData((ulong)uint.MaxValue + 1000)]
        [InlineData(ulong.MaxValue)]
        [InlineData(ulong.MaxValue - 999)]
        public void KeySerializationDeserializationTest(ulong key)
        {
            var serKey = UInt64BobKeySerializer.Instance.SerializeToBobKey(key);
            Assert.Equal(UInt64BobKeySerializer.Instance.SerializedSize, serKey.Length);

            if (BitConverter.IsLittleEndian)
            {
                Assert.Equal(BitConverter.GetBytes(key), serKey.GetKeyBytes());
            }

            ulong deserKey = UInt64BobKeySerializer.Instance.DeserializeFromBobKey(serKey);
            Assert.Equal(key, deserKey);
        }
    }
}
