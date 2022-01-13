using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeySerializers
{
    public class UInt64BobKeySerializerTests : BobTestsBaseClass
    {
        public UInt64BobKeySerializerTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        private static BobKeySerializer<ulong> Serializer { get { return UInt64BobKeySerializer.Instance; } }

        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(sizeof(ulong), Serializer.SerializedSize);
        }

        [Theory]
        [InlineData(0, new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 })]
        [InlineData(100, new byte[8] { 100, 0, 0, 0, 0, 0, 0, 0 })]
        [InlineData(123121412412412, new byte[8] { 252, 19, 13, 112, 250, 111, 0, 0 })]
        [InlineData(uint.MaxValue, new byte[8] { 255, 255, 255, 255, 0, 0, 0, 0 })]
        [InlineData((ulong)uint.MaxValue + 1000, new byte[8] { 231, 3, 0, 0, 1, 0, 0, 0 })]
        [InlineData(ulong.MaxValue, new byte[8] { 255, 255, 255, 255, 255, 255, 255, 255 })]
        [InlineData(ulong.MaxValue - 999, new byte[8] { 24, 252, 255, 255, 255, 255, 255, 255 })]
        public void KeySerializationDeserializationTest(ulong key, byte[] expected)
        {
            var serKey = Serializer.SerializeToBobKey(key);
            Assert.Equal(Serializer.SerializedSize, serKey.Length);
            Assert.Equal(expected, serKey.GetKeyBytes());

            var deserKey = Serializer.DeserializeFromBobKey(serKey);
            Assert.Equal(key, deserKey);
        }
    }
}
