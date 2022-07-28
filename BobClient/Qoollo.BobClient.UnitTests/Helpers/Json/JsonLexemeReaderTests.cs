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

#if NET5_0_OR_GREATER
            var parsedStrAsSpan = JsonLexemeReader.ParseStringAsSpan(str, startIndex, endIndex);
            Assert.True(expected.AsSpan().Equals(parsedStrAsSpan, StringComparison.Ordinal));
#endif
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
        [InlineData("\"test string\"", 0, "test string")]
        [InlineData("{ abc: \"test string \", def: 123 }", 7, "test string ")]
        public void ReadThenParseStringTest(string str, int index, string expected)
        {
            var token = JsonLexemeReader.ReadString(str, index);
            var parsedStr = JsonLexemeReader.ParseString(str, token.Start, token.End);
            Assert.Equal(expected, parsedStr);
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
        [InlineData("Simple_identifier", 0, -1, "Simple_identifier")]
        [InlineData("_simple_identifier", 0, -1, "_simple_identifier")]
        [InlineData("Simple, other", 0, 6, "Simple")]
        public void ParseIdentifierTest(string str, int startIndex, int endIndex, string expected)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            var parsedStr = JsonLexemeReader.ParseIdentifier(str, startIndex, endIndex, validate: true);
            Assert.Equal(expected, parsedStr);

#if NET5_0_OR_GREATER
            var parsedStrAsSpan = JsonLexemeReader.ParseIdentifierAsSpan(str, startIndex, endIndex, validate: true);
            Assert.True(expected.AsSpan().Equals(parsedStrAsSpan, StringComparison.Ordinal));
#endif
        }


        [Theory]
        [InlineData("123", 0, -1)]
        [InlineData(" identifier", 0, -1)]
        [InlineData("identifier", 0, 3)]
        public void ParseIdentifierFailTest(string str, int startIndex, int endIndex)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            Assert.Throws<FormatException>(() =>
            {
                var parsedStr = JsonLexemeReader.ParseIdentifier(str, startIndex, endIndex, validate: true);
                Assert.Equal("", parsedStr);
            });
        }

        [Theory]
        [InlineData("Simple_identifier", 0, "Simple_identifier")]
        [InlineData("_simple_identifier", 0, "_simple_identifier")]
        [InlineData("Simple identifier", 0, "Simple")]
        public void ReadThenParseIdentifierTest(string str, int index, string expected)
        {
            var token = JsonLexemeReader.ReadIdentifier(str, index);
            var parsedStr = JsonLexemeReader.ParseIdentifier(str, token.Start, token.End, validate: true);
            Assert.Equal(expected, parsedStr);
        }


        [Theory]
        [InlineData("0", 0, "0")]
        [InlineData("124141", 0, "124141")]
        [InlineData("1234567890, something other", 0, "1234567890")]
        [InlineData("Start: 1234567890, Something after", 7, "1234567890")]
        [InlineData("0.14", 0, "0.14")]
        [InlineData("10.0", 0, "10.0")]
        [InlineData("1.", 0, "1.")]
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
        [InlineData("100", 0, -1, 100)]
        [InlineData("-1", 0, -1, -1)]
        [InlineData("{ abc: 0 }", 7, 8, 0)]
        public void ParseInt32Test(string str, int startIndex, int endIndex, int expected)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            var parsedNum = JsonLexemeReader.ParseInt32(str, startIndex, endIndex, validate: true);
            Assert.Equal(expected, parsedNum);
        }

        [Theory]
        [InlineData("", 0, -1)]
        [InlineData("abc", 0, -1)]
        [InlineData("+-12", 0, -1)]
        [InlineData("1.2", 0, -1)]
        [InlineData("{ abc: 1 }", 7, 10)]
        public void ParseInt32FailTest(string str, int startIndex, int endIndex)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            Assert.Throws<FormatException>(() =>
            {
                var parsedNum = JsonLexemeReader.ParseInt32(str, startIndex, endIndex, validate: true);
                Assert.Equal(0, parsedNum);
            });

            Assert.Throws<FormatException>(() =>
            {
                var parsedNum = JsonLexemeReader.ParseInt32(str, startIndex, endIndex, validate: false);
                Assert.Equal(0, parsedNum);
            });
        }

        [Theory]
        [InlineData("", 0, -1)]
        [InlineData("abc", 0, -1)]
        [InlineData("+-12", 0, -1)]
        [InlineData("1.2", 0, -1)]
        [InlineData("1 a", 0, -1)]
        public void ParseInt32NoValidationFailTest(string str, int startIndex, int endIndex)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            Assert.Throws<FormatException>(() =>
            {
                var parsedNum = JsonLexemeReader.ParseInt32(str, startIndex, endIndex, validate: false);
                Assert.Equal(0, parsedNum);
            });
        }

        [Theory]
        [InlineData("100", 0, -1, 100)]
        [InlineData("-1", 0, -1, -1)]
        [InlineData("100000000000", 0, -1, 100000000000)]
        [InlineData("{ abc: 0 }", 7, 8, 0)]
        public void ParseInt64Test(string str, int startIndex, int endIndex, long expected)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            var parsedNum = JsonLexemeReader.ParseInt64(str, startIndex, endIndex, validate: true);
            Assert.Equal(expected, parsedNum);
        }


        [Theory]
        [InlineData("100", 0, -1, 100.0)]
        [InlineData("-1", 0, -1, -1.0)]
        [InlineData("0.2", 0, -1, 0.2)]
        [InlineData("10.0", 0, -1, 10.0)]
        [InlineData("1.", 0, -1, 1.0)]
        [InlineData("123456789.123456789", 0, -1, 123456789.123456789)]
        [InlineData("-1.1", 0, -1, -1.1)]
        [InlineData("+1.1", 0, -1, +1.1)]
        [InlineData("1E-9", 0, -1, 1E-9)]
        [InlineData("1E1", 0, -1, 1E1)]
        [InlineData("1.0E-9", 0, -1, 1.0E-9)]
        [InlineData("1.1E+9", 0, -1, 1.1E+9)]
        [InlineData("1.1e+9", 0, -1, 1.1e+9)]
        [InlineData("-123.456e+7", 0, -1, -123.456e+7)]
        [InlineData("-123.456e-8, something after", 0, 11, -123.456e-8)]
        [InlineData("{ abc: 0 }", 7, 8, 0.0)]
        public void ParseDoubleTest(string str, int startIndex, int endIndex, double expected)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            var parsedNum = JsonLexemeReader.ParseDouble(str, startIndex, endIndex, validate: true);
            Assert.Equal(expected, parsedNum);
        }

     
        [Theory]
        [InlineData("", 0, -1)]
        [InlineData("abc", 0, -1)]
        [InlineData("+-12", 0, -1)]
        [InlineData("1.2a", 0, -1)]
        [InlineData("1.2 a", 0, -1)]
        [InlineData("1E-", 0, -1)]
        [InlineData("1.0E", 0, -1)]
        [InlineData("1.0E+a", 0, -1)]
        [InlineData("{ abc: 1 }", 7, 10)]
        public void ParseDoubleFailTest(string str, int startIndex, int endIndex)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            Assert.Throws<FormatException>(() =>
            {
                var parsedNum = JsonLexemeReader.ParseDouble(str, startIndex, endIndex, validate: true);
                Assert.Equal(0, parsedNum);
            });

            Assert.Throws<FormatException>(() =>
            {
                var parsedNum = JsonLexemeReader.ParseDouble(str, startIndex, endIndex, validate: false);
                Assert.Equal(0, parsedNum);
            });
        }

        [Theory]
        [InlineData("", 0, -1)]
        [InlineData("abc", 0, -1)]
        [InlineData("+-12", 0, -1)]
        [InlineData("1.2a", 0, -1)]
        [InlineData("1E-", 0, -1)]
        [InlineData("1.0E", 0, -1)]
        [InlineData("1.0E+a", 0, -1)]
        public void ParseDoubleNoValidationFailTest(string str, int startIndex, int endIndex)
        {
            if (endIndex == -1)
                endIndex = str.Length;

            Assert.Throws<FormatException>(() =>
            {
                var parsedNum = JsonLexemeReader.ParseDouble(str, startIndex, endIndex, validate: false);
                Assert.Equal(0, parsedNum);
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
        internal void ReadJsonTest(string str, JsonLexemeType[] sequence)
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
        public void ExtractLexemeSurroundingTextTest(string str, int lexemeIndex, string expected)
        {
            var reader = new JsonLexemeReader(str);
            var lexeme = new JsonLexemeInfo(JsonLexemeType.String, lexemeIndex, lexemeIndex);
            var surroundingText = reader.ExtractLexemeSurroundingText(lexeme);
            Assert.Equal(expected, surroundingText);
        }


        [Fact]
        public void IsValueNullTest()
        {
            var reader = new JsonLexemeReader("[123, null, \"abc\"]");

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartArray, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Number, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Null, reader.CurrentLexeme.Type);
            Assert.True(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndArray, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());

            Assert.False(reader.Read());
            Assert.Equal(JsonLexemeType.None, reader.CurrentLexeme.Type);
            Assert.False(reader.IsValueNull());
        }



        [Fact]
        public void GetRawStringTest()
        {
            var reader = new JsonLexemeReader("[123, null, \"abc\"]");

            Assert.True(reader.Read());
            Assert.Equal("[", reader.GetRawString());

            Assert.True(reader.Read());
            Assert.Equal("123", reader.GetRawString());

            Assert.True(reader.Read());
            Assert.Equal(",", reader.GetRawString());

            Assert.True(reader.Read());
            Assert.Equal("null", reader.GetRawString());

            Assert.True(reader.Read());
            Assert.Equal(",", reader.GetRawString());

            Assert.True(reader.Read());
            Assert.Equal("\"abc\"", reader.GetRawString());

            Assert.True(reader.Read());
            Assert.Equal("]", reader.GetRawString());

            Assert.False(reader.Read());
            Assert.Equal("", reader.GetRawString());
        }

        [Fact]
        public void GetValueStringTest()
        {
            var reader = new JsonLexemeReader("[123, null, \"abc\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Number, reader.CurrentLexeme.Type);
            Assert.Equal("123", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Null, reader.CurrentLexeme.Type);
            Assert.Null(reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Equal("abc", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Identifier, reader.CurrentLexeme.Type);
            Assert.Equal("x", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.KeyValueSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.False, reader.CurrentLexeme.Type);
            Assert.Equal("false", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.False(reader.Read());
            Assert.Equal(JsonLexemeType.None, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));
        }


        [Fact]
        public void GetValueInt32Test()
        {
            var reader = new JsonLexemeReader("[123, null, \"abc\", \"456\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Number, reader.CurrentLexeme.Type);
            Assert.Equal(123, reader.GetValueInt32());
            Assert.Equal(123, reader.GetValueInt32Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Null, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Null(reader.GetValueInt32Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Equal(456, reader.GetValueInt32());
            Assert.Equal(456, reader.GetValueInt32Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Identifier, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.KeyValueSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.False, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));

            Assert.False(reader.Read());
            Assert.Equal(JsonLexemeType.None, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
        }


        [Fact]
        public void GetValueInt64Test()
        {
            var reader = new JsonLexemeReader("[1230000000000, null, \"abc\", \"4560000000000\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Number, reader.CurrentLexeme.Type);
            Assert.Equal(1230000000000, reader.GetValueInt64());
            Assert.Equal(1230000000000, reader.GetValueInt64Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Null, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Null(reader.GetValueInt64Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Equal(4560000000000, reader.GetValueInt64());
            Assert.Equal(4560000000000, reader.GetValueInt64Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Identifier, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.KeyValueSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.False, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));

            Assert.False(reader.Read());
            Assert.Equal(JsonLexemeType.None, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
        }


        [Fact]
        public void GetValueDoubleTest()
        {
            var reader = new JsonLexemeReader("[1230, null, \"abc\", \"456.1\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Number, reader.CurrentLexeme.Type);
            Assert.Equal(1230, reader.GetValueDouble());
            Assert.Equal(1230, reader.GetValueDoubleNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Null, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Null(reader.GetValueDoubleNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Equal(456.1, reader.GetValueDouble());
            Assert.Equal(456.1, reader.GetValueDoubleNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Identifier, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.KeyValueSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.False, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));

            Assert.False(reader.Read());
            Assert.Equal(JsonLexemeType.None, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
        }


        [Fact]
        public void GetValueBoolTest()
        {
            var reader = new JsonLexemeReader("[1230, null, \"abc\", \"True\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Number, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Null, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Null(reader.GetValueBoolNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.String, reader.CurrentLexeme.Type);
            Assert.True(reader.GetValueBool());
            Assert.True(reader.GetValueBoolNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.ItemSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.StartObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.Identifier, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.KeyValueSeparator, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.False, reader.CurrentLexeme.Type);
            Assert.False(reader.GetValueBool());
            Assert.False(reader.GetValueBoolNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndObject, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.True(reader.Read());
            Assert.Equal(JsonLexemeType.EndArray, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));

            Assert.False(reader.Read());
            Assert.Equal(JsonLexemeType.None, reader.CurrentLexeme.Type);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
        }
    }
}
