using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobKeyTests : BobTestsBaseClass
    {
        public BobKeyTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(100, "0x6400000000000000")]
        [InlineData(100500, "0x9488010000000000")]
        [InlineData(500, "0xF401000000000000")]
        public void ToStringTest(ulong value, string expectedStr)
        {
            var key = BobKey.FromUInt64(value);
            string stringVal = key.ToString();
            Assert.Equal(expectedStr, stringVal);
        }

        [Theory]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }, true)]
        [InlineData(new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3 }, false)]
        [InlineData(new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3, 4 }, true)]
        public void EqualsTest(byte[] k1, byte[] k2, bool equals)
        {
            Assert.True(default(BobKey) == default(BobKey));
            Assert.False(default(BobKey) == new BobKey(k1));

            Assert.Equal(equals, new BobKey(k1) == new BobKey(k2));
            Assert.NotEqual(equals, new BobKey(k1) != new BobKey(k2));
            Assert.Equal(equals, new BobKey(k1).Equals(new BobKey(k2)));
            Assert.Equal(equals, object.Equals(new BobKey(k1), new BobKey(k2)));
        }

        [Theory]
        [InlineData(new byte[] { 1, 2, 3 })]
        [InlineData(new byte[] { 1, 2, 3, 4 })]
        [InlineData(new byte[] { 1, 2, 3, 4, 5 })]
        public void GetHashCodeTest(byte[] k1)
        {
            int hashCode = new BobKey(k1).GetHashCode();
            Assert.True(hashCode != 0);
        }

        [Fact]
        public void LengthTest()
        {
            Assert.Equal(4, new BobKey(new byte[] { 1, 2, 3, 4 }).Length);
            Assert.Equal(0, default(BobKey).Length);
        }

        [Theory]
        [InlineData(123, 3)]
        [InlineData(100500, 101)]
        [InlineData(100500, 100)]
        [InlineData(1457, -33)]
        [InlineData(10403248134, 54523)]
        [InlineData(96333534, 1241)]
        [InlineData(14124554, int.MaxValue)]
        public void RemainderTest(ulong key, int divisor)
        {
            Assert.Equal((long)key % divisor, BobKey.FromUInt64(key).Remainder(divisor));
        }

        [Theory]
        [InlineData(new byte[] { 1 }, 1u)]
        [InlineData(new byte[] { 1, 2, 3 }, 197121u)]
        [InlineData(new byte[] { 1, 2, 3, 4 }, 67305985u)]
        [InlineData(new byte[] { 1, 2, 3, 4, 5 }, 21542142465u)]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 578437695752307201u)]
        public void ToUInt64Test(byte[] k, ulong expected)
        {
            var bobKey = new BobKey(k);
            ulong convKey = bobKey.ToUInt64();
            Assert.Equal(expected, convKey);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(123)]
        [InlineData(100500)]
        [InlineData(96333534)]
        public void ToUInt64FromUInt64Test(ulong key)
        {
            var bobKey = BobKey.FromUInt64(key);
            ulong convKey = bobKey.ToUInt64();
            Assert.Equal(key, convKey);
        }
    }
}
