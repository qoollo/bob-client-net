using Qoollo.BobClient.UnitTests.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobConnectionParametersBuilderTests
    {
        [Fact]
        public void SimpleConstructorTest()
        {
            var bobParams = new BobConnectionParametersBuilder(new BobNodeAddress("127.0.0.1", 12000));
            Assert.Equal("127.0.0.1", bobParams.Host);
            Assert.Equal("127.0.0.1", bobParams.NodeAddress.Host);
            Assert.Equal(12000, bobParams.Port);
            Assert.Equal(12000, bobParams.NodeAddress.Port);

            Assert.Null(bobParams.User);
            Assert.Null(bobParams.Password);
            Assert.Null(bobParams.MaxSendMessageSize);
            Assert.Null(bobParams.MaxReceiveMessageSize);
            Assert.Null(bobParams.OperationTimeout);
            Assert.Null(bobParams.ConnectionTimeout);

            Assert.NotNull(bobParams.CustomParameters);


            var bobParams2 = new BobConnectionParametersBuilder(new BobNodeAddress("127.0.0.1", 12000), "user", "pass");
            Assert.Equal("user", bobParams2.User);
            Assert.Equal("pass", bobParams2.Password);
        }

        [Fact]
        public void SimpleConnectionStringParsingTest()
        {
            var bobParams = new BobConnectionParametersBuilder("Address = 127.0.0.1:12000; User = user; Password = pass; OperationTimeout = 00:00:10");
            Assert.Equal("127.0.0.1", bobParams.Host);
            Assert.Equal("127.0.0.1", bobParams.NodeAddress.Host);
            Assert.Equal(12000, bobParams.Port);
            Assert.Equal(12000, bobParams.NodeAddress.Port);

            Assert.Equal("user", bobParams.User);
            Assert.Equal("pass", bobParams.Password);

            Assert.Null(bobParams.MaxSendMessageSize);
            Assert.Null(bobParams.MaxReceiveMessageSize);

            Assert.Equal(TimeSpan.FromSeconds(10), bobParams.OperationTimeout);
            Assert.Null(bobParams.ConnectionTimeout);
        }

        [Fact]
        public void SimpleBuildTest()
        {
            var bobParams = new BobConnectionParametersBuilder()
            {
                Host = "127.0.0.1",
                Port = 12000,

                User = "user",
                Password = "pass",

                OperationTimeout = TimeSpan.FromSeconds(10)
            }
            .WithCustomParameter("Custom1", "value")
            .Build();

            Assert.Equal("127.0.0.1", bobParams.Host);
            Assert.Equal("127.0.0.1", bobParams.NodeAddress.Host);
            Assert.Equal(12000, bobParams.Port);
            Assert.Equal(12000, bobParams.NodeAddress.Port);

            Assert.Equal("user", bobParams.User);
            Assert.Equal("pass", bobParams.Password);

            Assert.Null(bobParams.MaxSendMessageSize);
            Assert.Null(bobParams.MaxReceiveMessageSize);

            Assert.Equal(TimeSpan.FromSeconds(10), bobParams.OperationTimeout);
            Assert.Null(bobParams.ConnectionTimeout);

            Assert.Contains("Custom1", bobParams.CustomParameters);
            Assert.Equal("value", bobParams.CustomParameters["Custom1"]);
        }

        [Fact]
        public void SimpleIsValidTest()
        {
            var bobParams = new BobConnectionParametersBuilder()
            {
                Host = "127.0.0.1",
                Port = 12000,

                User = "user",
                Password = "pass",

                OperationTimeout = TimeSpan.FromSeconds(10)
            };

            Assert.True(bobParams.IsValid);

            bobParams.Port = -1;
            Assert.False(bobParams.IsValid);

            bobParams.Port = 10;
            bobParams.Host = null;
            Assert.False(bobParams.IsValid);
        }

        [Fact]
        public void SimpleGetSetTest()
        {
            var bobParams = new BobConnectionParametersBuilder();

            bobParams.SetValue("Address", "127.0.0.1:12000");
            bobParams.SetValue("User", "user");
            bobParams.SetValue("Password", "pass");
            bobParams.SetValue("OperationTimeout", "00:00:10");
            bobParams.SetValue("Custom1", "value");

            Assert.Equal("127.0.0.1", bobParams.Host);
            Assert.Equal("127.0.0.1", bobParams.NodeAddress.Host);
            Assert.Equal("127.0.0.1", bobParams.GetValue("host"));
            Assert.Equal(12000, bobParams.Port);
            Assert.Equal(12000, bobParams.NodeAddress.Port);
            Assert.Equal("12000", bobParams.GetValue("port"));

            Assert.Equal("user", bobParams.User);
            Assert.Equal("user", bobParams.GetValue("user"));
            Assert.Equal("pass", bobParams.Password);
            Assert.Equal("pass", bobParams.GetValue("password"));

            Assert.Null(bobParams.MaxSendMessageSize);
            Assert.Null(bobParams.GetValue("MaxSendMessageSize"));
            Assert.Null(bobParams.MaxReceiveMessageSize);
            Assert.Null(bobParams.GetValue("MaxReceiveMessageSize"));

            Assert.Equal(TimeSpan.FromSeconds(10), bobParams.OperationTimeout);
            Assert.Equal("00:00:10", bobParams.GetValue("OperationTimeout"));
            Assert.Null(bobParams.ConnectionTimeout);
            Assert.Null(bobParams.GetValue("ConnectionTimeout"));

            Assert.Contains("Custom1", (IDictionary<string, string>)bobParams.CustomParameters);
            Assert.Equal("value", bobParams.CustomParameters["Custom1"]);

            Assert.Equal("value", bobParams.GetValue("Custom1"));
        }

        [Fact]
        public void SimpleToStringTest()
        {
            var bobParams = new BobConnectionParametersBuilder()
            {
                Host = "127.0.0.1",
                Port = 12000,

                User = "user",
                Password = "pass",

                OperationTimeout = TimeSpan.FromSeconds(10)
            };


            Assert.Equal("Address = 127.0.0.1:12000; User = user; OperationTimeout = 00:00:10", bobParams.ToString());
            Assert.Equal("Address = 127.0.0.1:12000; User = user; Password = pass; OperationTimeout = 00:00:10", bobParams.ToString(includePassword: true));
        }


        [Fact]
        public void SimpleCopyConstructorTest()
        {
            var bobParams = new BobConnectionParameters("Address = 127.0.0.1:12000; User = user; Password = pass; OperationTimeout = 00:00:10");

            var bobBuilder = new BobConnectionParametersBuilder(bobParams);
            Assert.Equal(bobParams, bobBuilder, ModifiableBobConnectionParametersEqualityComparer.Instance);

            var bobBuilder2 = new BobConnectionParametersBuilder(bobBuilder);
            Assert.Equal(bobParams, bobBuilder2, ModifiableBobConnectionParametersEqualityComparer.Instance);
        }
    }
}
