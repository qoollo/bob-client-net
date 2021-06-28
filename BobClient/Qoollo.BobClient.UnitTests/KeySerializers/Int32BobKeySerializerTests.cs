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
        [InlineData(0, new byte[4] { 0, 0, 0, 0 })]
        [InlineData(100, new byte[4] { 100, 0, 0, 0 })]
        [InlineData(1245556, new byte[4] { 116, 1, 19, 0 })]
        [InlineData(-567547645, new byte[4] { 3, 233, 43, 222 })]
        [InlineData(int.MaxValue, new byte[4] { 255, 255, 255, 127 })]
        [InlineData(int.MinValue, new byte[4] { 0, 0, 0, 128 })]
        [InlineData(int.MaxValue - 432, new byte[4] { 79, 254, 255, 127 })]
        [InlineData(int.MinValue + 432, new byte[4] { 176, 1, 0, 128 })]
        public void KeySerializationDeserializationTest(int key, byte[] expected)
        {
            var serKey = Serializer.SerializeToBobKey(key);
            Assert.Equal(Serializer.SerializedSize, serKey.Length);
            Assert.Equal(expected, serKey.GetKeyBytes());

            var deserKey = Serializer.DeserializeFromBobKey(serKey);
            Assert.Equal(key, deserKey);
        }
    }
}
