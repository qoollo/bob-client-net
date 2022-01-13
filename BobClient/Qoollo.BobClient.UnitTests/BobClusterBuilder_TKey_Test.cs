using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobClusterBuilder_TKey_Test : BobTestsBaseClass
    {
        public BobClusterBuilder_TKey_Test(Xunit.Abstractions.ITestOutputHelper output) : base(output) { }

        [Fact]
        public void NodesPassedTest()
        {
            var builder = new BobClusterBuilder<ulong>();
            builder.WithAdditionalNode(new BobConnectionParameters("127.0.0.1"));
            builder.WithAdditionalNode("127.0.0.2");
            builder.WithAdditionalNodes(new string[] { "127.0.0.3", "127.0.0.4" });
            builder.WithAdditionalNodes(new BobConnectionParameters[] { new BobConnectionParameters("127.0.0.5"), new BobConnectionParameters("127.0.0.6") });

            using (var clusterClient = builder.Build())
            {
                Assert.Equal(6, clusterClient.ClientConnectionParameters.Count());
                var hosts = clusterClient.ClientConnectionParameters.Select(o => o.Host).ToList();
                var expectedHosts = new string[] { "127.0.0.1", "127.0.0.2", "127.0.0.3", "127.0.0.4", "127.0.0.5", "127.0.0.6" };
                Assert.Equal(expectedHosts, hosts);
            }
        }


        [Fact]
        public void NodesParametersPassedTest()
        {
            var builder = new BobClusterBuilder<ulong>("127.0.0.1", "127.0.0.2");
            builder.WithOperationTimeout(TimeSpan.FromSeconds(22))
                   .WithConnectionTimeout(TimeSpan.FromSeconds(33))
                   .WithAuthenticationData("user", "pass");

            using (var clusterClient = builder.Build())
            {
                Assert.All(clusterClient.ClientConnectionParameters, val =>
                {
                    Assert.Equal(TimeSpan.FromSeconds(22), val.OperationTimeout);
                    Assert.Equal(TimeSpan.FromSeconds(33), val.ConnectionTimeout);
                    Assert.Equal("user", val.User);
                    Assert.Equal("pass", val.Password);
                });
            }
        }
    }
}
