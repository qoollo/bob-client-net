using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeySerializers
{
    public class Int64BobKeySerializerTests
    {
        private static BobKeySerializer<long> Serializer { get { return Int64BobKeySerializer.Instance; } }

        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(sizeof(long), Serializer.SerializedSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(123121412412412)]
        [InlineData(-42534523134556)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData((long)uint.MaxValue + 1000)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MaxValue - 999)]
        [InlineData(long.MinValue)]
        [InlineData(long.MinValue + 999)]
        public void KeySerializationDeserializationTest(long key)
        {
            var serKey = Serializer.SerializeToBobKey(key);
            Assert.Equal(Serializer.SerializedSize, serKey.Length);

            if (BitConverter.IsLittleEndian)
            {
                Assert.Equal(BitConverter.GetBytes(key), serKey.GetKeyBytes());
            }

            var deserKey = Serializer.DeserializeFromBobKey(serKey);
            Assert.Equal(key, deserKey);
        }
    }
}
