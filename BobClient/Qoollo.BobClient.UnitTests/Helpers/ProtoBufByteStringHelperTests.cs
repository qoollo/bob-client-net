using Qoollo.BobClient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.Helpers
{
    public class ProtoBufByteStringHelperTests
    {
        [Fact]
        public void AttachBytesIsAvailableTest()
        {
            Assert.True(ProtoBufByteStringHelper.CanCreateFromByteArrayOptimized());
        }


        [Fact]
        public void CreateFromByteArrayOptimizedTest()
        {
            byte[] data = Enumerable.Range(0, 1000).Select(o => (byte)(o % 256)).ToArray();

            var btArr = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(data);
            Assert.NotNull(btArr);

            Assert.Equal(data, btArr.ToByteArray());
        }




        [Fact]
        public void CanPerformByteArrayExtractionTest()
        {
            Assert.True(ProtoBufByteStringHelper.CanExtractByteArrayOptimized());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(32)]
        [InlineData(1024)]
        [InlineData(1024 * 1024)]
        public void ExtractByteArrayOptimizedTest(int size)
        {
            byte[] data = Enumerable.Range(0, size).Select(o => (byte)(o % 256)).ToArray();

            Google.Protobuf.ByteString btStr = ProtoBufByteStringHelper.CreateFromByteArrayOptimized(data);
            var extractedArray = ProtoBufByteStringHelper.ExtractByteArrayOptimized(btStr);

            Assert.Equal(data, extractedArray);

            if (ProtoBufByteStringHelper.CanExtractByteArrayOptimized() && ProtoBufByteStringHelper.CanCreateFromByteArrayOptimized() && data.Length >= ProtoBufByteStringHelper.ExtractObjectIndexFromMemoryWithReflectionThreshold)
                Assert.True(object.ReferenceEquals(data, extractedArray));
        }
    }
}
