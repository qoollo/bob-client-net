﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobNodeAddressTests : BobTestsBaseClass
    {
        public BobNodeAddressTests(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("host:123", "host", 123)]
        [InlineData("host", "host", BobNodeAddress.DefaultPort)]
        [InlineData("local_host:500", "local_host", 500)]
        [InlineData("local_host123:500", "local_host123", 500)]
        [InlineData("[::ffff:192.0.2.1]:500", "[::ffff:192.0.2.1]", 500)]
        public void ParseTest(string addr, string host, int port)
        {
            BobNodeAddress parsed = BobNodeAddress.Parse(addr);
            Assert.Equal(host, parsed.Host);
            Assert.Equal(port, parsed.Port);
        }

        [Theory]
        [InlineData("host:")]
        [InlineData("host:-15")]
        [InlineData(":22")]
        [InlineData(":66666")]
        [InlineData(":::::")]
        public void ParseFormatExceptionTest(string addr)
        {
            Assert.False(BobNodeAddress.TryParse(addr, out BobNodeAddress _));

            Assert.Throws<FormatException>(() =>
            {
                BobNodeAddress.Parse(addr);
            });
        }
    }
}
