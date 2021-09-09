using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.ConnectionParametersHelpers
{
    public class IModifiableBobConnectionParametersExtensionsTests
    {
        [Theory]
        [InlineData(nameof(IModifiableBobConnectionParameters.Host), "host", "localhost", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.Host), "HOST", "HO$$$t", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.Host), "Host", "node.com", null)]

        [InlineData(nameof(IModifiableBobConnectionParameters.Port), "Port", "1322", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.Port), "PorT", "", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.Port), "port", "  ", "")]

        [InlineData(nameof(IModifiableBobConnectionParameters.Host), "Address", "node1.bob.com", "node1.bob.com")]
        [InlineData(nameof(IModifiableBobConnectionParameters.Host), "Address", "node1.bob.com:1322", "node1.bob.com")]
        [InlineData(nameof(IModifiableBobConnectionParameters.Host), "SERVER", "node2.bob.com:1322", "node2.bob.com")]

        [InlineData(nameof(IModifiableBobConnectionParameters.Port), "Address", "node1.bob.com", "")]
        [InlineData(nameof(IModifiableBobConnectionParameters.Port), "Address", "node1.bob.com:1322", "1322")]
        [InlineData(nameof(IModifiableBobConnectionParameters.Port), "SERVER", "node2.bob.com:1322", "1322")]

        [InlineData(nameof(IModifiableBobConnectionParameters.User), "user", "bob_user", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.User), "User ID", "", null)]

        [InlineData(nameof(IModifiableBobConnectionParameters.Password), "password", "Password@#$%", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.Password), "Password", "", null)]

        [InlineData(nameof(IModifiableBobConnectionParameters.MaxReceiveMessageSize), "MaxReceiveMessageSize", "65535", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.MaxReceiveMessageSize), "MaxReceiveMessageSize", "  ", "")]
        [InlineData(nameof(IModifiableBobConnectionParameters.MaxReceiveMessageSize), "MaxReceiveMessageLength", "500000", null)]

        [InlineData(nameof(IModifiableBobConnectionParameters.MaxSendMessageSize), "MaxSendMessageSize", "65535", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.MaxSendMessageSize), "MaxSendMessageSize", "  ", "")]
        [InlineData(nameof(IModifiableBobConnectionParameters.MaxSendMessageSize), "MaxSendMessageLength", "500000", null)]

        [InlineData(nameof(IModifiableBobConnectionParameters.OperationTimeout), "OperationTimeout", "10000", "00:00:10")]
        [InlineData(nameof(IModifiableBobConnectionParameters.OperationTimeout), "OperationTimeout", "00:00:13", "00:00:13")]
        [InlineData(nameof(IModifiableBobConnectionParameters.OperationTimeout), "OperationTimeout", "", "")]

        [InlineData(nameof(IModifiableBobConnectionParameters.ConnectionTimeout), "ConnectionTimeout", "10000", "00:00:10")]
        [InlineData(nameof(IModifiableBobConnectionParameters.ConnectionTimeout), "ConnectionTimeout", "00:00:13", "00:00:13")]
        [InlineData(nameof(IModifiableBobConnectionParameters.ConnectionTimeout), "ConnectionTimeout", "", "")]
        [InlineData(nameof(IModifiableBobConnectionParameters.ConnectionTimeout), "Connect Timeout", "00:30:30", "00:30:30")]

        [InlineData(nameof(IModifiableBobConnectionParameters.OperationTimeout), "Timeout", "10000", "00:00:10")]
        [InlineData(nameof(IModifiableBobConnectionParameters.OperationTimeout), "Timeout", " ", "")]

        [InlineData(nameof(IModifiableBobConnectionParameters.ConnectionTimeout), "Timeout", "10000", "00:00:10")]
        [InlineData(nameof(IModifiableBobConnectionParameters.ConnectionTimeout), "Timeout", " ", "")]
        public void SetValueTest(string property, string key, string value, string expected = null)
        {
            ModifiableBobConnectionParametersMock target = new ModifiableBobConnectionParametersMock()
            {
                Host = "------",
                User = "------",
                Password = "-----"
            };
            target.SetValue(key, value, allowCustomParameters: false);

            var propVal = target.GetType().GetProperty(property, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(target);

            expected = expected ?? value;

            if (propVal == null)
                Assert.True(string.IsNullOrEmpty(expected));
            else
                Assert.Equal(expected, propVal.ToString());          
        }

        [Fact]
        public void SetValueToCustomParamsTest()
        {
            ModifiableBobConnectionParametersMock target = new ModifiableBobConnectionParametersMock();

            target.SetValue("Abba", "Abba", allowCustomParameters: true);
            Assert.Single(target.CustomParameters);
            Assert.True(target.CustomParameters.ContainsKey("Abba"));
            Assert.Equal("Abba", target.CustomParameters["Abba"]);

            target.SetValue("AbbA", "Bob", allowCustomParameters: true);
            Assert.Single(target.CustomParameters);
            Assert.True(target.CustomParameters.ContainsKey("abba"));
            Assert.Equal("Bob", target.CustomParameters["Abba"]);

            target.SetValue("Mamba", "Bob", allowCustomParameters: true);
            Assert.Equal(2, target.CustomParameters.Count);
            Assert.True(target.CustomParameters.ContainsKey("Mamba"));
            Assert.Equal("Bob", target.CustomParameters["mamba"]);


            Assert.Throws<ArgumentException>(() =>
            {
                target.SetValue("Vombat", "111", allowCustomParameters: false);
            });
        }


        [Theory]
        [InlineData("host", "")]
        [InlineData("HOST", "   ")]

        [InlineData("Port", "-1")]
        [InlineData("Port", "65600")]
        [InlineData("Port", "asdasdasd")]

        [InlineData("Address", "")]
        [InlineData("Address", "localhost:")]
        [InlineData("Address", ":22")]
        [InlineData("Address", "node1.bob.com:-1")]
        [InlineData("Address", "node1.bob.com:65600")]

        [InlineData("MaxReceiveMessageSize", "-10")]
        [InlineData("MaxReceiveMessageSize", "abc")]

        [InlineData("MaxSendMessageSize", "-10")]
        [InlineData("MaxSendMessageSize", "abc")]

        [InlineData("OperationTimeout", "-1")]
        [InlineData("OperationTimeout", "abc")]
        [InlineData("OperationTimeout", "00:13:--")]
        [InlineData("OperationTimeout", "-00:13:00")]

        [InlineData("ConnectionTimeout", "-1")]
        [InlineData("ConnectionTimeout", "abc")]
        [InlineData("ConnectionTimeout", "00:13:--")]
        [InlineData("ConnectionTimeout", "-00:13:00")]
        public void SetValueThrowsTest(string key, string value)
        {
            ModifiableBobConnectionParametersMock target = new ModifiableBobConnectionParametersMock();

            Assert.Throws<FormatException>(() =>
            {
                target.SetValue(key, value, allowCustomParameters: false);
            });
        }


        [Fact]
        public void GetValueTest()
        {
            ModifiableBobConnectionParametersMock source = new ModifiableBobConnectionParametersMock()
            {
                Host = "localhost",
                Port = null,
                User = "user",
                Password = null,
                MaxReceiveMessageSize = 100500,
                MaxSendMessageSize = null,
                ConnectionTimeout = TimeSpan.Parse("12:00:00"),
                OperationTimeout = null
            }
            .WithCustomParam("Custom1", "Value1");

            Assert.Equal("localhost", source.GetValue("Host", allowCustomParameters: true));
            Assert.Null(source.GetValue("Port", allowCustomParameters: true));
            Assert.Equal("localhost", source.GetValue("Address", allowCustomParameters: true));
            Assert.Equal("localhost", source.GetValue("Server", allowCustomParameters: true));
            Assert.Equal("user", source.GetValue("USER", allowCustomParameters: true));
            Assert.Equal("user", source.GetValue("USER ID", allowCustomParameters: true));
            Assert.Null(source.GetValue("Password", allowCustomParameters: true));
            Assert.Equal("100500", source.GetValue("MaxReceiveMessageSize", allowCustomParameters: true));
            Assert.Equal("100500", source.GetValue("MaxReceiveMessageLength", allowCustomParameters: true));
            Assert.Null(source.GetValue("MaxSendMessageSize", allowCustomParameters: true));
            Assert.Null(source.GetValue("MaxSendMessageLength", allowCustomParameters: true));
            Assert.Equal("12:00:00", source.GetValue("ConnectionTimeout", allowCustomParameters: true));
            Assert.Equal("12:00:00", source.GetValue("Connect Timeout", allowCustomParameters: true));
            Assert.Null(source.GetValue("OperationTimeout", allowCustomParameters: true));

            Assert.Equal("Value1", source.GetValue("Custom1", allowCustomParameters: true));

            Assert.Throws<ArgumentException>(() => source.GetValue("Custom2", allowCustomParameters: true));


            source.Port = 1000;

            Assert.Equal("localhost", source.GetValue("Host", allowCustomParameters: true));
            Assert.Equal("1000", source.GetValue("Port", allowCustomParameters: true));
            Assert.Equal("localhost:1000", source.GetValue("Address", allowCustomParameters: true));
        }


        [Theory]
        [InlineData("host", "localhost", false)]
        [InlineData("HOST", "node1.bob.com", false)]

        [InlineData("Port", "", false)]
        [InlineData("Port", "32132", false)]
        public void SetGetValueRoundtripTest(string key, string value, bool allowCustom, string expected = null)
        {
            ModifiableBobConnectionParametersMock target = new ModifiableBobConnectionParametersMock()
            {
                Host = "------",
                User = "------",
                Password = "-----"
            };

            target.SetValue(key, value, allowCustom);
            var result = target.GetValue(key, allowCustom);

            Assert.Equal(expected ?? value, result ?? "");
        }
    }
}
