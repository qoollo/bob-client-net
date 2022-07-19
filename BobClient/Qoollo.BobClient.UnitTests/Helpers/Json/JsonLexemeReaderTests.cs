using Qoollo.BobClient.Helpers.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.Helpers.Json
{
    public class JsonLexemeReaderTests
    {
        [Theory]
        [InlineData("\"" + "\"", 0, "\"" + "\"")]
        [InlineData("\"" + @"Simple test string." + "\"", 0, "\"" + @"Simple test string." + "\"")]
        [InlineData("\"" + @"Simple test string." + "\"" + ", Something other", 0, "\"" + @"Simple test string." + "\"")]
        [InlineData("Start: " + "\"" + @"Simple test string." + "\"" + " Something after", 7, "\"" + @"Simple test string." + "\"")]
        [InlineData("\"" + @"Esc \"" \t \r \n \b \f \/ \u123A" + "\"", 0, "\"" + @"Esc \"" \t \r \n \b \f \/ \u123A" + "\"")]
        public void ReadStringTest(string str, int index, string expected)
        {
            var token = JsonLexemeReader.ReadString(str, index);
            Assert.Equal(expected, token.RawLexemeString(str));
        }

        [Theory]
        [InlineData("\"" + @"Non start" + "\"", 2)]
        [InlineData("Start: " + "\"" + @"Non start" + "\"" + " Something after", 0)]
        [InlineData("\"" + @"Bad esc \X" + "\"", 0)]
        [InlineData("\"" + @"Bad esc \ux" + "\"", 0)]
        [InlineData("\"" + @"Bad esc \u123" + "\"", 0)]
        [InlineData("\"" + @"Bad esc \uA33Q" + "\"", 0)]
        [InlineData("\"" + @"No end", 0)]
        public void ReadStringFailTest(string str, int index)
        {
            Assert.Throws<JsonParsingException>(() =>
            {
                var token = JsonLexemeReader.ReadString(str, index);
                Assert.Equal("", token.RawLexemeString(str));
            });
        }

        [Theory]
        [InlineData("\"" + "\"", 0, -1, "")]
        [InlineData("\"" + @"Simple test string." + "\"", 0, -1, "Simple test string.")]
        [InlineData("Start: " + "\"" + @"Simple test string." + "\"" + " Something after", 7, 28, "Simple test string.")]
        [InlineData("\"" + @"Esc \"" \t \r \n \b \f \/ \u123A text" + "\"", 0, -1, "Esc \" \t \r \n \b \f / \u123A text")]
        public void ParseStringTest(string str, int startIndex, int endIndex, string expected)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            var parsedStr = JsonLexemeReader.ParseString(str, startIndex, endIndex);
            Assert.Equal(expected, parsedStr);
        }

        [Theory]
        [InlineData("\"" + @"Non start" + "\"", 2, -1)]
        [InlineData(@"Bad start" + "\"", 0, -1)]
        [InlineData("\"" + @"Bad end" + "\"", 0, 4)]
        [InlineData("\"" + @"Bad end" + "\", something other", 0, 11)]
        [InlineData("\"" + @"Bad esc \X" + "\"", 0, -1)]
        [InlineData("\"" + @"Bad esc \ux" + "\"", 0, -1)]
        [InlineData("\"" + @"Bad esc \u123" + "\"", 0, -1)]
        [InlineData("\"" + @"Bad esc \uA33Q" + "\"", 0, -1)]
        [InlineData("\"" + @"No end", 0, -1)]
        public void ParseStringFailTest(string str, int startIndex, int endIndex)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            Assert.Throws<FormatException>(() =>
            {
                var parsedStr = JsonLexemeReader.ParseString(str, startIndex, endIndex);
                Assert.Equal("", parsedStr);
            });
        }



        [Theory]
        [InlineData("Simple_identifier", 0, "Simple_identifier")]
        [InlineData("_simple_identifier", 0, "_simple_identifier")]
        [InlineData("Simple identifier", 0, "Simple")]
        [InlineData("true", 0, "true")]
        [InlineData("false", 0, "false")]
        [InlineData("null", 0, "null")]
        public void ReadIdentifierTest(string str, int index, string expected)
        {
            var token = JsonLexemeReader.ReadIdentifier(str, index);
            Assert.Equal(expected, token.RawLexemeString(str));
        }

        [Theory]
        [InlineData(" Bad start symbol", 0)]
        [InlineData(": Bad start symbol", 0)]
        [InlineData("1 Bad start symbol", 0)]
        public void ReadIdentifierFailTest(string str, int index)
        {
            Assert.Throws<JsonParsingException>(() =>
            {
                var token = JsonLexemeReader.ReadIdentifier(str, index);
                Assert.Equal("", token.RawLexemeString(str));
            });
        }


        [Theory]
        [InlineData("0", 0, "0")]
        [InlineData("124141", 0, "124141")]
        [InlineData("1234567890, something other", 0, "1234567890")]
        [InlineData("Start: 1234567890, Something after", 7, "1234567890")]
        [InlineData("0.14", 0, "0.14")]
        [InlineData("10.0", 0, "10.0")]
        [InlineData("123456789.123456789", 0, "123456789.123456789")]
        [InlineData("-1.1", 0, "-1.1")]
        [InlineData("+1.1", 0, "+1.1")]
        [InlineData("1E-9", 0, "1E-9")]
        [InlineData("1.0E-9", 0, "1.0E-9")]
        [InlineData("1.1E+9", 0, "1.1E+9")]
        [InlineData("1.1e+9", 0, "1.1e+9")]
        [InlineData("-123.456e+789", 0, "-123.456e+789")]
        [InlineData("-123.456e+789, something after", 0, "-123.456e+789")]
        public void ReadNumberTest(string str, int index, string expected)
        {
            var token = JsonLexemeReader.ReadNumber(str, index);
            Assert.Equal(expected, token.RawLexemeString(str));
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("abc", 0)]
        [InlineData("+", 0)]
        [InlineData("-", 0)]
        [InlineData("-x", 0)]
        [InlineData("1e", 0)]
        [InlineData("1eabc", 0)]
        [InlineData("1e+", 0)]
        [InlineData("1e-", 0)]
        [InlineData("1e-a", 0)]
        public void ReadNumberFailTest(string str, int index)
        {
            Assert.Throws<JsonParsingException>(() =>
            {
                var token = JsonLexemeReader.ReadNumber(str, index);
                Assert.Equal("", token.RawLexemeString(str));
            });
        }


        [Theory]
        [InlineData("", 0, "", JsonLexemeType.None)]
        [InlineData("0", 0, "0", JsonLexemeType.Number)]
        [InlineData("123", 0, "123", JsonLexemeType.Number)]
        [InlineData("+123", 0, "+123", JsonLexemeType.Number)]
        [InlineData("-0", 0, "-0", JsonLexemeType.Number)]
        [InlineData("\"abcd\"", 0, "\"abcd\"", JsonLexemeType.String)]
        [InlineData("\"abcd\", after", 0, "\"abcd\"", JsonLexemeType.String)]
        [InlineData("{", 0, "{", JsonLexemeType.StartObject)]
        [InlineData("[", 0, "[", JsonLexemeType.StartArray)]
        [InlineData("}", 0, "}", JsonLexemeType.EndObject)]
        [InlineData("]", 0, "]", JsonLexemeType.EndArray)]
        [InlineData(":", 0, ":", JsonLexemeType.KeyValueSeparator)]
        [InlineData(",", 0, ",", JsonLexemeType.ItemSeparator)]
        [InlineData("false", 0, "false", JsonLexemeType.False)]
        [InlineData("true", 0, "true", JsonLexemeType.True)]
        [InlineData("null", 0, "null", JsonLexemeType.Null)]
        [InlineData("identifier", 0, "identifier", JsonLexemeType.Identifier)]
        internal void ReadLexemeTest(string str, int index, string expected, JsonLexemeType tokenType)
        {
            var token = JsonLexemeReader.ReadLexeme(str, index);
            Assert.Equal(tokenType, token.Type);
            Assert.Equal(expected, token.RawLexemeString(str));
        }


        [Theory]
        [InlineData("@", 0)]
        [InlineData("-x", 0)]
        [InlineData("!!", 0)]
        [InlineData("\"" + @"No end", 0)]
        public void ReadLexemeFailTest(string str, int index)
        {
            Assert.Throws<JsonParsingException>(() =>
            {
                var token = JsonLexemeReader.ReadLexeme(str, index);
                Assert.Equal("", token.RawLexemeString(str));
            });
        }


        [Theory]
        [InlineData("256", new JsonLexemeType[] { JsonLexemeType.Number })]
        [InlineData("{\"abc\": 123}", new JsonLexemeType[] { JsonLexemeType.StartObject, JsonLexemeType.String, JsonLexemeType.KeyValueSeparator, JsonLexemeType.Number, JsonLexemeType.EndObject })]
        [InlineData("[\"abc\", false, true, null, 123.3, { bcd: [] }]", new JsonLexemeType[] { JsonLexemeType.StartArray, JsonLexemeType.String, JsonLexemeType.ItemSeparator, JsonLexemeType.False, JsonLexemeType.ItemSeparator, JsonLexemeType.True, JsonLexemeType.ItemSeparator, JsonLexemeType.Null, JsonLexemeType.ItemSeparator, JsonLexemeType.Number, JsonLexemeType.ItemSeparator, JsonLexemeType.StartObject, JsonLexemeType.Identifier, JsonLexemeType.KeyValueSeparator, JsonLexemeType.StartArray, JsonLexemeType.EndArray, JsonLexemeType.EndObject, JsonLexemeType.EndArray })]
        internal void ReadJson(string str, JsonLexemeType[] sequence)
        {
            var reader = new JsonLexemeReader(str);
            int seqIndex = 0;
            while (reader.Read())
            {
                Assert.True(seqIndex < sequence.Length, "seqIndex < sequence");
                Assert.Equal(sequence[seqIndex], reader.CurrentLexeme.Type);
                seqIndex++;
            }

            Assert.True(seqIndex == sequence.Length, "seqIndex == sequence");
        }


        [Theory]
        [InlineData("[abc, 123, null]", 6, "123, null]")]
        [InlineData("[abc, 123, null, true, false]", 6, "123, null, true,...")]
        [InlineData("[abc, 123, null]", 16, "[abc, 123, null]")]
        [InlineData("[abc, 123, null]  ", 16, "bc, 123, null]  ")]
        public void ExtractLexemeSurroundingText(string str, int lexemeIndex, string expected)
        {
            var reader = new JsonLexemeReader(str);
            var lexeme = new JsonLexemeInfo(JsonLexemeType.String, lexemeIndex, lexemeIndex);
            var surroundingText = reader.ExtractLexemeSurroundingText(lexeme);
            Assert.Equal(expected, surroundingText);
        }
    }
}
