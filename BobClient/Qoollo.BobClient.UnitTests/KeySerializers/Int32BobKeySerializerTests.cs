using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeySerializers
{
    public class Int32BobKeySerializerTests
    {
        private static BobKeySerializer<int> Serializer { get { return Int32BobKeySerializer.Instance; } }

        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(sizeof(int), Serializer.SerializedSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(1245556)]
        [InlineData(-567547645)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue - 432)]
        [InlineData(int.MinValue + 432)]
        public void KeySerializationDeserializationTest(int key)
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
