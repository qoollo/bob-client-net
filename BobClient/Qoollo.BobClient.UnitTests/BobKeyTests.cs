using System;
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
        [InlineData(100)]
        [InlineData(100500)]
        public void ToStringTest(ulong value)
        {
            var key = BobKey.FromUInt64(value);
            string stringVal = key.ToString();
            Assert.Equal("", stringVal);
        }
    }
}
