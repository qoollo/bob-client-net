using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeySerializers
{
    public class UInt32BobKeySerializerTests
    {
        private static BobKeySerializer<uint> Serializer { get { return UInt32BobKeySerializer.Instance; } }

        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(sizeof(uint), Serializer.SerializedSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(12414124)]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MaxValue - 999)]
        public void KeySerializationDeserializationTest(uint key)
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
