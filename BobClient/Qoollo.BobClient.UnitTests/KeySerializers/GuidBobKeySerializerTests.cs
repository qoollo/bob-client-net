using Qoollo.BobClient.KeySerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.KeySerializers
{
    public class GuidBobKeySerializerTests : BobTestsBaseClass
    {
        public GuidBobKeySerializerTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        private static BobKeySerializer<Guid> Serializer { get { return GuidBobKeySerializer.Instance; } }

        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(16, Serializer.SerializedSize);
        }

        [Theory]
        [InlineData("c707a3dd-9a18-41da-96f8-9c96ba4e338f", new byte[16] { 221, 163, 7, 199, 24, 154, 218, 65, 150, 248, 156, 150, 186, 78, 51, 143 })]
        [InlineData("e33c5321-aa55-4b80-a80d-b6c9030e2238", new byte[16] { 33, 83, 60, 227, 85, 170, 128, 75, 168, 13, 182, 201, 3, 14, 34, 56 })]
        [InlineData("79903edb-a9ab-40c7-b1e5-3ead698695a5", new byte[16] { 219, 62, 144, 121, 171, 169, 199, 64, 177, 229, 62, 173, 105, 134, 149, 165 })]
        public void KeySerializationDeserializationTest(string guid, byte[] expectedGuidBytes)
        {
            var guidKey = Guid.Parse(guid);

            var serKey = Serializer.SerializeToBobKey(guidKey);
            Assert.Equal(Serializer.SerializedSize, serKey.Length);

            Assert.Equal(expectedGuidBytes, serKey.GetKeyBytes());

            var deserKey = Serializer.DeserializeFromBobKey(serKey);
            Assert.Equal(guidKey, deserKey);
        }
    }
}
