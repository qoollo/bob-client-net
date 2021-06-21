﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobKeyTests
    {
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
    }
}