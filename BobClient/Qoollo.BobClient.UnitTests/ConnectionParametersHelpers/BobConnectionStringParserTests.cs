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

    }
}
