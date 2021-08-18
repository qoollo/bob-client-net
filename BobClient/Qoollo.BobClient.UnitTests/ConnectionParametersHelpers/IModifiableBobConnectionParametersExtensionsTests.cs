﻿using Qoollo.BobClient.ConnectionParametersHelpers;
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

        [InlineData(nameof(IModifiableBobConnectionParameters.MaxReceiveMessageLength), "MaxReceiveMessageLength", "65535", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.MaxReceiveMessageLength), "MaxReceiveMessageLength", "  ", "")]

        [InlineData(nameof(IModifiableBobConnectionParameters.MaxSendMessageLength), "MaxSendMessageLength", "65535", null)]
        [InlineData(nameof(IModifiableBobConnectionParameters.MaxSendMessageLength), "MaxSendMessageLength", "  ", "")]

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

        [InlineData("MaxReceiveMessageLength", "-10")]
        [InlineData("MaxReceiveMessageLength", "abc")]

        [InlineData("MaxSendMessageLength", "-10")]
        [InlineData("MaxSendMessageLength", "abc")]

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
    }
}
