﻿using Qoollo.BobClient.KeySerializers;
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
        private static BobKeySerializer<ulong> Serializer { get { return UInt64BobKeySerializer.Instance; } }

        [Fact]
        public void SerializedSizeTest()
        {
            Assert.Equal(sizeof(ulong), Serializer.SerializedSize);
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