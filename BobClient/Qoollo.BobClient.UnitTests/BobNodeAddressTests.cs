using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobNodeAddressTests
    {
        [Theory]
        [InlineData("host:123", "host", 123)]
        [InlineData("host", "host", BobNodeAddress.DefaultPort)]
        [InlineData("local_host:500", "local_host", 500)]
        [InlineData("local_host123:500", "local_host123", 500)]
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
        [InlineData(":::::")]
        public void ParseFormatExceptionTest(string addr)
        {
            Assert.Throws<FormatException>(() =>
            {
                BobNodeAddress.Parse(addr);
            });
        }
    }
}
