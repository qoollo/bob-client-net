﻿using Qoollo.BobClient.UnitTests.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests
{
    public class BobConnectionParametersTests
    {
        [Fact]
        public void SimpleConstructorTest()
        {
            var bobParams = new BobConnectionParameters(new BobNodeAddress("127.0.0.1", 12000));
            Assert.Equal("127.0.0.1", bobParams.Host);
            Assert.Equal("127.0.0.1", bobParams.NodeAddress.Host);
            Assert.Equal(12000, bobParams.Port);
            Assert.Equal(12000, bobParams.NodeAddress.Port);

            Assert.Null(bobParams.User);
            Assert.Null(bobParams.Password);
            Assert.Null(bobParams.MaxSendMessageLength);
            Assert.Null(bobParams.MaxReceiveMessageLength);
            Assert.Null(bobParams.OperationTimeout);
            Assert.Null(bobParams.ConnectionTimeout);

            Assert.NotNull(bobParams.CustomParameters);


            var bobParams2 = new BobConnectionParameters(new BobNodeAddress("127.0.0.1", 12000), "user", "pass");
            Assert.Equal("user", bobParams2.User);
            Assert.Equal("pass", bobParams2.Password);
        }


        public static IEnumerable<object[]> ParseConnectionStringTestData
        {
            get { return ConnectionParametersHelpers.BobConnectionStringParserTests.ParseConnectionStringIntoTestData; }
        }

        [Theory]
        [MemberData(nameof(ParseConnectionStringTestData))]
        public void ConnectionStringParsingTest(string connectionString, ModifiableBobConnectionParametersMock expected)
        {
            var bobParams = new BobConnectionParameters(connectionString);
            Assert.Equal(expected, bobParams, ModifiableBobConnectionParametersEqualityComparer.Instance);
        }


        public static IEnumerable<object[]> ParseConnectionStringFormatException
        {
            get { return ConnectionParametersHelpers.BobConnectionStringParserTests.ParseConnectionStringIntoFormatException; }
        }

        [Theory]
        [MemberData(nameof(ParseConnectionStringFormatException))]
        public void ConnectionStringParsingWithFormatExceptionTest(string connectionString)
        {
            Assert.Throws<FormatException>(() =>
            {
                var bobParams = new BobConnectionParameters(connectionString);
            });
        }


        [Fact]
        public void ConnectionStringParsingExtDataTest()
        {
            var bobParams = new BobConnectionParameters("Address = 127.0.0.1; KeySerializationPoolSize = 100; OperationRetryCount = 2; NodeSelectionPolicy = FirstWorking");
            Assert.Equal(100, bobParams.KeySerializationPoolSize);
            Assert.Equal(2, bobParams.OperationRetryCount);
            Assert.Equal(BobClient.NodeSelectionPolicies.KnownBobNodeSelectionPolicies.FirstWorking, bobParams.NodeSelectionPolicy);
        }
    }
}