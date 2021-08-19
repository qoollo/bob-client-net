using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.ConnectionParametersHelpers
{
    public class BobConnectionStringParserTests
    {
        public class ExpectedParsingResults
        {
            public static ExpectedParsingResults WithFormatException() { return new ExpectedParsingResults() { FormatException = true }; }
            public static ExpectedParsingResults WithResult() { return new ExpectedParsingResults() { Pairs = new List<BobConnectionStringParser.KeyValuePair>() }; }
            public static ExpectedParsingResults WithResult(string key, string value) { return WithResult().Add(key, value); }

            internal List<BobConnectionStringParser.KeyValuePair> Pairs { get; set; }
            public bool FormatException { get; set; }

            public ExpectedParsingResults Add(string key, string value)
            {
                this.Pairs.Add(new BobConnectionStringParser.KeyValuePair(key, value));
                return this;
            }
        }

        public static IEnumerable<object[]> ParsingTestData
        {
            get
            {
                yield return new object[] { "Key=Value", ExpectedParsingResults.WithResult("Key", "Value") };
                yield return new object[] { "Key=Value;", ExpectedParsingResults.WithResult("Key", "Value") };
                yield return new object[] { "  Key  = Value    ", ExpectedParsingResults.WithResult("Key", "Value") };
                yield return new object[] { "'Key'  = 'Value'    ;", ExpectedParsingResults.WithResult("Key", "Value") };
                yield return new object[] { " \"Key\"=\"Value\";", ExpectedParsingResults.WithResult("Key", "Value") };
                yield return new object[] { "' Key ' = ' Value ' ", ExpectedParsingResults.WithResult(" Key ", " Value ") };
                yield return new object[] { " Key =   ", ExpectedParsingResults.WithResult("Key", "") };
                yield return new object[] { " Key =   ;", ExpectedParsingResults.WithResult("Key", "") };
                yield return new object[] { " Key'1 = Value'2;", ExpectedParsingResults.WithResult("Key'1", "Value'2") };
                yield return new object[] { "Key=value;;;", ExpectedParsingResults.WithResult("Key", "value") };
                yield return new object[] { " Key with spaces  = value with spaces ", ExpectedParsingResults.WithResult("Key with spaces", "value with spaces") };
                yield return new object[] { "User = admin; Password = '$$$! 11'; Address=127.0.0.1:22;", 
                                            ExpectedParsingResults.WithResult("User", "admin").Add("Password", "$$$! 11").Add("Address", "127.0.0.1:22") };
                yield return new object[] { "User = admin; Password = '$$$!11'; Address=127.0.0.1:22; User = user; number = 123",
                                            ExpectedParsingResults.WithResult("User", "admin").Add("Password", "$$$!11").Add("Address", "127.0.0.1:22").Add("User", "user").Add("number", "123") };


                yield return new object[] { "Key", ExpectedParsingResults.WithFormatException() };
                yield return new object[] { " Key   ", ExpectedParsingResults.WithFormatException() };
                yield return new object[] { " \"Key ", ExpectedParsingResults.WithFormatException() };
                yield return new object[] { " 'Key ", ExpectedParsingResults.WithFormatException() };
                yield return new object[] { " 'Key ' s = text  ", ExpectedParsingResults.WithFormatException() };
                yield return new object[] { " \"Key ' = text  ", ExpectedParsingResults.WithFormatException() };
                yield return new object[] { "= text  ", ExpectedParsingResults.WithFormatException() };
                
            }
        }


        [Theory]
        [MemberData(nameof(ParsingTestData))]
        public void BobConnectionStringParsingTest(string connectionString, ExpectedParsingResults parsingData)
        {
            if (parsingData.FormatException)
            {
                Assert.Throws<FormatException>(() =>
                {
                    BobConnectionStringParser.ParseConnectionStringIntoKeyValues(connectionString);
                });
            }
            else
            {
                var parsedRes = BobConnectionStringParser.ParseConnectionStringIntoKeyValues(connectionString);
                Assert.Equal(parsingData.Pairs, parsedRes);
            }
        }



        public static IEnumerable<object[]> ParseConnectionStringIntoTestData
        {
            get
            {
                yield return new object[] 
                {
                    "Host = node1.bob.com; ",  
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com"
                    }
                };
                yield return new object[] 
                {
                    "Host=node1.bob.com; Port=20001",  
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 20001
                    }
                };
                yield return new object[]
                {
                    " Address=node1.bob.com:20001;",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 20001
                    }
                };
                yield return new object[]
                {
                    "'Address'='node2.bob.com'",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node2.bob.com"
                    }
                };
                yield return new object[]
                {
                    "Address = node2.bob.com; Port = 20001",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node2.bob.com",
                        Port = 20001
                    }
                };
                yield return new object[]
                {
                    "address = node2.bob.com:19000 ; Port = 22000",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node2.bob.com",
                        Port = 22000
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000 ; 'User' = 'user'; PASSWORD = '!@#$%=;'",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                        User = "user",
                        Password = "!@#$%=;"
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000; User = 'user'; Password = '!@#$%=;'; MaxSendMessageLength = 1024; MaxReceiveMessageLength = 2048",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                        User = "user",
                        Password = "!@#$%=;",
                        MaxSendMessageLength = 1024,
                        MaxReceiveMessageLength = 2048
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000; User ID = 'user'; OperationTimeout=00:12:00; ConnectionTimeout = 10000",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                        User = "user",
                        OperationTimeout = TimeSpan.Parse("00:12:00"),
                        ConnectionTimeout = TimeSpan.FromMilliseconds(10000)
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000; User ID = 'user'; Timeout=1000;",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                        User = "user",
                        OperationTimeout = TimeSpan.FromMilliseconds(1000),
                        ConnectionTimeout = TimeSpan.FromMilliseconds(1000)
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000; Timeout=1000; Connect Timeout = 2000",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                        OperationTimeout = TimeSpan.FromMilliseconds(1000),
                        ConnectionTimeout = TimeSpan.FromMilliseconds(2000)
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000; Timeout=1000; Connect Timeout = 2000; Timeout=1000;",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                        OperationTimeout = TimeSpan.FromMilliseconds(1000),
                        ConnectionTimeout = TimeSpan.FromMilliseconds(1000)
                    }
                };
                yield return new object[]
                {
                    "address = node1.bob.com:19000; Custom1 = Value1; Custom2 = 'Value2'; \"Custom3\"=\"VALUE3\"",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                    }
                    .WithCustomParam("Custom1", "Value1")
                    .WithCustomParam("Custom2", "Value2")
                    .WithCustomParam("Custom3", "VALUE3")
                };

                yield return new object[]
                {
                    "node1.bob.com:19000",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node1.bob.com",
                        Port = 19000,
                    }
                };
                yield return new object[]
                {
                    "node2.bob.com",
                    new ModifiableBobConnectionParametersMock()
                    {
                        Host = "node2.bob.com"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ParseConnectionStringIntoTestData))]
        public void ParseConnectionStringIntoTest(string connectionString, ModifiableBobConnectionParametersMock expected)
        {
            ModifiableBobConnectionParametersMock target = new ModifiableBobConnectionParametersMock();
            BobConnectionStringParser.ParseConnectionStringInto(connectionString, target);

            Assert.Equal(expected, target);
        }
    }
}
