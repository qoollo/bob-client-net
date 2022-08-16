using Qoollo.BobClient.Helpers.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.Helpers.Json
{
    public class JsonReaderTests
    {
        private static JsonLexemeType IntoJsonLexemeType(JsonElementType elementType)
        {
            switch (elementType)
            {
                case JsonElementType.None:
                    return JsonLexemeType.None;
                case JsonElementType.StartObject:
                    return JsonLexemeType.StartObject;
                case JsonElementType.EndObject:
                    return JsonLexemeType.EndObject;
                case JsonElementType.StartArray:
                    return JsonLexemeType.StartArray;
                case JsonElementType.EndArray:
                    return JsonLexemeType.EndArray;
                case JsonElementType.PropertyName:
                    return JsonLexemeType.Identifier;
                case JsonElementType.Null:
                    return JsonLexemeType.Null;
                case JsonElementType.True:
                    return JsonLexemeType.True;
                case JsonElementType.False:
                    return JsonLexemeType.False;
                case JsonElementType.Number:
                    return JsonLexemeType.Number;
                case JsonElementType.String:
                    return JsonLexemeType.String;
                default:
                    throw new Exception("Unknown: " + elementType.ToString());
            }
        }


        [Fact]
        public void ReaderContextInitializationTest()
        {
            var context = new JsonReader.ReaderContext("{}");
            Assert.Equal(JsonElementType.None, context.LastElement.Type);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);
            Assert.Equal(JsonScopeElement.None, context.CurrentScope);
            Assert.Null(context.PropertyName);

            Assert.True(context.IsScopeStackEmpty);
            Assert.NotNull(context.ScopeStack);

            Assert.Equal("", context.GetScopeStackNotClosedSequence());
        }


        [Theory]
        [InlineData("\"str\"", new JsonElementType[] { JsonElementType.String, JsonElementType.None })]
        [InlineData("{}", new JsonElementType[] { JsonElementType.StartObject, JsonElementType.EndObject, JsonElementType.None })]
        [InlineData("[null]", new JsonElementType[] { JsonElementType.StartArray, JsonElementType.Null, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("[{abc: false}, {abc: true}, {}, []]", 
            new JsonElementType[] { JsonElementType.StartArray, JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.False, JsonElementType.EndObject, JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.True, JsonElementType.EndObject, JsonElementType.StartObject, JsonElementType.EndObject, JsonElementType.StartArray, JsonElementType.EndArray, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("{abc: 123, def: [456, 789, false, true]}", 
            new JsonElementType[] { JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.Number, JsonElementType.PropertyName, JsonElementType.StartArray, JsonElementType.Number, JsonElementType.Number, JsonElementType.False, JsonElementType.True, JsonElementType.EndArray, JsonElementType.EndObject, JsonElementType.None })]
        internal void ReaderContextProcessingSeqTest(string jsonStr, JsonElementType[] elTypeSeq)
        {
            var context = new JsonReader.ReaderContext(jsonStr);

            Stack<JsonScopeElement> scopeElemStack = new Stack<JsonScopeElement>();
            JsonScopeElement enclosingScope = JsonScopeElement.None;

            for (int i = 0; i < elTypeSeq.Length; i++)
            {
                var elem = context.ProcessNextElement(new JsonElementInfo(elTypeSeq[i], new JsonLexemeInfo(IntoJsonLexemeType(elTypeSeq[i]), i, (i + 1))));

                Assert.Equal(elTypeSeq[i], context.LastElement.Type);
                Assert.Equal(elem.Type, context.LastElement.Type);
                Assert.Equal(elem.Lexeme.Type, context.LastElement.Lexeme.Type);
                Assert.Equal(elem.Lexeme.Start, context.LastElement.Lexeme.Start);
                Assert.Equal(elem.Lexeme.End, context.LastElement.Lexeme.End);

                if (elem.Type == JsonElementType.PropertyName)
                    Assert.NotNull(context.PropertyNameElement);

                var prevScope = scopeElemStack.Count > 0 ? scopeElemStack.Peek() : JsonScopeElement.None;

                if (elem.Type == JsonElementType.StartObject)
                    scopeElemStack.Push(JsonScopeElement.Object);
                else if (elem.Type == JsonElementType.StartArray)
                    scopeElemStack.Push(JsonScopeElement.Array);
                else if (elem.Type == JsonElementType.EndObject)
                    Assert.Equal(JsonScopeElement.Object, scopeElemStack.Pop());
                else if (elem.Type == JsonElementType.EndArray)
                    Assert.Equal(JsonScopeElement.Array, scopeElemStack.Pop());

                if (elem.Type == JsonElementType.StartArray || elem.Type == JsonElementType.StartObject)
                    enclosingScope = prevScope;
                else
                    enclosingScope = scopeElemStack.Count > 0 ? scopeElemStack.Peek() : JsonScopeElement.None;

                if (scopeElemStack.Count == 0)
                {
                    Assert.Equal(JsonScopeElement.None, context.CurrentScope);
                    Assert.Equal(JsonScopeElement.None, context.EnclosingScope);
                    Assert.True(context.IsScopeStackEmpty);
                }
                else
                {
                    Assert.Equal(scopeElemStack.Peek(), context.CurrentScope);
                    Assert.Equal(enclosingScope, context.EnclosingScope);
                    Assert.False(context.IsScopeStackEmpty);
                }
            }
        }

        [Fact]
        public void ReaderContextProcessingSingleTest()
        {
            var context = new JsonReader.ReaderContext("{ \"abc\": 123, \"def\": [ false ] }");

            Assert.Equal("", context.GetScopeStackNotClosedSequence());
            Assert.Equal(0, context.ScopeStack.Count);
            Assert.Null(context.PropertyName);
            Assert.Equal(JsonScopeElement.None, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.StartObject, new JsonLexemeInfo(JsonLexemeType.StartObject, 0, 1)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Null(context.PropertyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.PropertyName, new JsonLexemeInfo(JsonLexemeType.String, 2, 7)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.True(context.IsPropertyNameEquals("abc"));
            Assert.Equal("abc", context.PropertyName);
            Assert.True(context.IsPropertyNameEquals("abc"));
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.Number, new JsonLexemeInfo(JsonLexemeType.Number, 9, 12)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.True(context.IsPropertyNameEquals("abc"));
            Assert.Equal("abc", context.PropertyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.PropertyName, new JsonLexemeInfo(JsonLexemeType.String, 14, 19)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.True(context.IsPropertyNameEquals("def"));
            Assert.Equal("def", context.PropertyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.StartArray, new JsonLexemeInfo(JsonLexemeType.StartArray, 21, 22)));
            Assert.Equal("{, [", context.GetScopeStackNotClosedSequence());
            Assert.Equal(2, context.ScopeStack.Count);
            Assert.True(context.IsPropertyNameEquals("def"));
            Assert.Equal("def", context.PropertyName);
            Assert.Equal(JsonScopeElement.Array, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.False, new JsonLexemeInfo(JsonLexemeType.False, 23, 28)));
            Assert.Equal("{, [", context.GetScopeStackNotClosedSequence());
            Assert.Equal(2, context.ScopeStack.Count);
            Assert.Null(context.PropertyName);
            Assert.Equal(JsonScopeElement.Array, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.EndArray, new JsonLexemeInfo(JsonLexemeType.EndArray, 29, 30)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Null(context.PropertyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.EndObject, new JsonLexemeInfo(JsonLexemeType.EndObject, 31, 32)));
            Assert.Equal("", context.GetScopeStackNotClosedSequence());
            Assert.Equal(0, context.ScopeStack.Count);
            Assert.Null(context.PropertyName);
            Assert.Equal(JsonScopeElement.None, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.None, new JsonLexemeInfo(JsonLexemeType.None, 32, 32)));
            Assert.Equal("", context.GetScopeStackNotClosedSequence());
            Assert.Equal(0, context.ScopeStack.Count);
            Assert.Null(context.PropertyName);
            Assert.Equal(JsonScopeElement.None, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);
        }


        [Theory]
        [InlineData("111, 222", new JsonElementType[] { JsonElementType.Number, JsonElementType.Number, JsonElementType.None })]
        [InlineData("{]", new JsonElementType[] { JsonElementType.StartObject, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("{abc: 123, [] }", new JsonElementType[] { JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.Number, JsonElementType.StartArray, JsonElementType.EndArray, JsonElementType.EndObject, JsonElementType.None })]
        [InlineData("}", new JsonElementType[] { JsonElementType.EndObject, JsonElementType.None })]
        [InlineData("{", new JsonElementType[] { JsonElementType.StartObject, JsonElementType.None })]
        internal void ReaderContextProcessingSeqFailTest(string jsonStr, JsonElementType[] elTypeSeq)
        {
            var context = new JsonReader.ReaderContext(jsonStr);


            Assert.Throws<JsonParsingException>(() =>
            {
                for (int i = 0; i < elTypeSeq.Length; i++)
                    context.ProcessNextElement(new JsonElementInfo(elTypeSeq[i], new JsonLexemeInfo(IntoJsonLexemeType(elTypeSeq[i]), i, (i + 1))));
            });
        }



        [Theory]
        [InlineData("false", new JsonElementType[] { JsonElementType.False, JsonElementType.None })]
        [InlineData("[]", new JsonElementType[] { JsonElementType.StartArray, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("[\n123,\n456]", new JsonElementType[] { JsonElementType.StartArray, JsonElementType.Number, JsonElementType.Number, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("[null, {},]", new JsonElementType[] { JsonElementType.StartArray, JsonElementType.Null, JsonElementType.StartObject, JsonElementType.EndObject, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("[{\"abc\": \"text\",}, {abc: true}, {}, []]",
            new JsonElementType[] { JsonElementType.StartArray, JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.String, JsonElementType.EndObject, JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.True, JsonElementType.EndObject, JsonElementType.StartObject, JsonElementType.EndObject, JsonElementType.StartArray, JsonElementType.EndArray, JsonElementType.EndArray, JsonElementType.None })]
        [InlineData("{abc: 123.1E12, def: [456, 789, false, true]}",
            new JsonElementType[] { JsonElementType.StartObject, JsonElementType.PropertyName, JsonElementType.Number, JsonElementType.PropertyName, JsonElementType.StartArray, JsonElementType.Number, JsonElementType.Number, JsonElementType.False, JsonElementType.True, JsonElementType.EndArray, JsonElementType.EndObject, JsonElementType.None })]
        internal void ReaderReadTest(string jsonStr, JsonElementType[] elTypeSeq)
        {
            var reader = new JsonReader(jsonStr);

            Stack<JsonScopeElement> scopeElemStack = new Stack<JsonScopeElement>();
            JsonScopeElement enclosingScope = JsonScopeElement.None;

            int index = -1;

            while (reader.Read())
            {
                index++;
                Assert.True(index < elTypeSeq.Length);

                Assert.Equal(elTypeSeq[index], reader.ElementType);

                if (elTypeSeq[index] == JsonElementType.PropertyName)
                {
                    Assert.NotNull(reader.PropertyName);
                    Assert.True(reader.IsPropertyNameEquals(reader.PropertyName));
                }

                var prevScope = scopeElemStack.Count > 0 ? scopeElemStack.Peek() : JsonScopeElement.None;

                if (elTypeSeq[index] == JsonElementType.StartObject)
                    scopeElemStack.Push(JsonScopeElement.Object);
                else if (elTypeSeq[index] == JsonElementType.StartArray)
                    scopeElemStack.Push(JsonScopeElement.Array);
                else if (elTypeSeq[index] == JsonElementType.EndObject)
                    Assert.Equal(JsonScopeElement.Object, scopeElemStack.Pop());
                else if (elTypeSeq[index] == JsonElementType.EndArray)
                    Assert.Equal(JsonScopeElement.Array, scopeElemStack.Pop());

                if (elTypeSeq[index] == JsonElementType.StartArray || elTypeSeq[index] == JsonElementType.StartObject)
                    enclosingScope = prevScope;
                else
                    enclosingScope = scopeElemStack.Count > 0 ? scopeElemStack.Peek() : JsonScopeElement.None;

                if (scopeElemStack.Count == 0)
                {
                    Assert.Equal(JsonScopeElement.None, reader.CurrentScope);
                    Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
                }
                else
                {
                    Assert.Equal(scopeElemStack.Peek(), reader.CurrentScope);
                    Assert.Equal(enclosingScope, reader.EnclosingScope);
                }
            }

            Assert.True(reader.IsEnd);
            Assert.False(reader.IsBroken);
        }


        [Theory]
        [InlineData("false, true")]
        [InlineData("abcd")]
        [InlineData("[12, 13}")]
        [InlineData("{ abc: 1, false }")]
        [InlineData("!")]
        [InlineData("]")]
        [InlineData("{ abc: \"\\a\" }")]
        [InlineData("{ abc: 12a3 }")]
        internal void ReaderReadFailTest(string jsonStr)
        {
            var reader = new JsonReader(jsonStr);

            Assert.Throws<JsonParsingException>(() =>
            {
                int index = -1;

                while (reader.Read())
                {
                    index++;
                    Assert.True(index < 10000);
                }
            });

            Assert.True(reader.IsBroken);
        }


        [Fact]
        internal void ReaderIsBrokenTest()
        {
            var reader = new JsonReader("{ 123, abc: 123 }");

            Assert.Throws<JsonParsingException>(() =>
            {
                int index = -1;

                while (reader.Read())
                {
                    index++;
                    Assert.True(index < 10000);
                }
            });

            Assert.True(reader.IsBroken);
            Assert.False(reader.IsEnd);

            Assert.Throws<JsonParsingException>(() =>
            {
                reader.Read();
            });
        }


        [Theory]
        [InlineData(@"Helpers\Json\TestJsonFiles\file1.json")]
        [InlineData(@"Helpers\Json\TestJsonFiles\file2.json")]
        [InlineData(@"Helpers\Json\TestJsonFiles\file3.json")]
        [InlineData(@"Helpers\Json\TestJsonFiles\file4.json")]
        internal void ReaderReadJsonFileTest(string jsonFileName)
        {
            string text = System.IO.File.ReadAllText(jsonFileName);
            var reader = new JsonReader(text);

            int index = -1;

            while (reader.Read())
            {
                index++;
                Assert.True(index < 10000);
            }

            Assert.True(reader.IsEnd);
        }


        [Fact]
        public void ReaderPropertyNameTests()
        {
            var reader = new JsonReader("{ \"abc\": 123, cde: 123, \"\\u006B\\u006C\\u006D\": 123 }");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartObject, reader.ElementType);
            Assert.Equal(JsonScopeElement.Object, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
            Assert.Null(reader.PropertyName);
            Assert.False(reader.IsPropertyNameEquals("abc"));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.Equal(JsonScopeElement.Object, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, reader.EnclosingScope);
            Assert.True(reader.IsPropertyNameEquals("abc"));
            Assert.NotNull(reader.PropertyName);
            Assert.Equal("abc", reader.PropertyName);

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.NotNull(reader.PropertyName);
            Assert.Equal("abc", reader.PropertyName);
            Assert.True(reader.IsPropertyNameEquals("abc"));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.True(reader.IsPropertyNameEquals("cde"));
            Assert.NotNull(reader.PropertyName);
            Assert.Equal("cde", reader.PropertyName);

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.NotNull(reader.PropertyName);
            Assert.Equal("cde", reader.PropertyName);
            Assert.True(reader.IsPropertyNameEquals("cde"));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.True(reader.IsPropertyNameEquals("klm"));
            Assert.NotNull(reader.PropertyName);
            Assert.Equal("klm", reader.PropertyName);

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.NotNull(reader.PropertyName);
            Assert.Equal("klm", reader.PropertyName);
            Assert.True(reader.IsPropertyNameEquals("klm"));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndObject, reader.ElementType);
            Assert.Equal(JsonScopeElement.None, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
            Assert.Null(reader.PropertyName);
            Assert.False(reader.IsPropertyNameEquals("abc"));

            Assert.False(reader.Read());
            Assert.True(reader.IsEnd);
            Assert.False(reader.IsBroken);
        }


        [Fact]
        public void ReaderIsValueNullTest()
        {
            var reader = new JsonReader("[123, null, \"abc\"]");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Equal(JsonScopeElement.Array, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Equal(JsonScopeElement.Array, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, reader.EnclosingScope);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Equal(JsonScopeElement.Array, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, reader.EnclosingScope);
            Assert.True(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal(JsonScopeElement.Array, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, reader.EnclosingScope);
            Assert.False(reader.IsValueNull());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Equal(JsonScopeElement.None, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
            Assert.False(reader.IsValueNull());

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
            Assert.Equal(JsonScopeElement.None, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
            Assert.False(reader.IsValueNull());
        }


        [Fact]
        public void ReaderGetValueTest()
        {
            var reader = new JsonReader("[123, null, \"abc\", true, false]");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => reader.GetValue());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Equal((double)123.0, reader.GetValue());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Null(reader.GetValue());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal("abc", reader.GetValue());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.True, reader.ElementType);
            Assert.Equal(true, reader.GetValue());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.False, reader.ElementType);
            Assert.Equal(false, reader.GetValue());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => reader.GetValue());

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
        }


        [Fact]
        public void ReaderGetValueStringTest()
        {
            var reader = new JsonReader("[123, null, \"abc\", \"12 \\t 34\", \"ab\\u0063\", { x: false, \"y\": true }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Equal("123", reader.GetValueString());      

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Null(reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal("abc", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal("12 \t 34", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal("abc", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartObject, reader.ElementType);
            Assert.Equal(JsonScopeElement.Object, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, reader.EnclosingScope);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.Equal(JsonScopeElement.Object, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, reader.EnclosingScope);
            Assert.True(reader.IsPropertyNameEquals("x"));
            Assert.False(reader.IsPropertyNameEquals(""));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.False(reader.IsEnd);
            Assert.Equal(JsonElementType.False, reader.ElementType);
            Assert.True(reader.IsPropertyNameEquals("x"));
            Assert.False(reader.IsPropertyNameEquals(""));
            Assert.Equal("false", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.True(reader.IsPropertyNameEquals("y"));
            Assert.False(reader.IsPropertyNameEquals("x"));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.True, reader.ElementType);
            Assert.True(reader.IsPropertyNameEquals("y"));
            Assert.False(reader.IsPropertyNameEquals("x"));
            Assert.Equal("true", reader.GetValueString());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndObject, reader.ElementType);
            Assert.Equal(JsonScopeElement.Array, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, reader.EnclosingScope);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Equal(JsonScopeElement.None, reader.CurrentScope);
            Assert.Equal(JsonScopeElement.None, reader.EnclosingScope);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal("-", reader.GetValueString()));
            Assert.True(reader.IsEnd);
        }


        [Fact]
        public void ReaderGetValueInt32Test()
        {
            var reader = new JsonReader("[123, null, \"abc\", \"456\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Equal(123, reader.GetValueInt32());
            Assert.Equal(123, reader.GetValueInt32Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Null(reader.GetValueInt32Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal(456, reader.GetValueInt32());
            Assert.Equal(456, reader.GetValueInt32Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.False, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt32Nullable()));
        }


        [Fact]
        public void ReaderGetValueInt64Test()
        {
            var reader = new JsonReader("[1230000000000, null, \"abc\", \"4560000000000\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Equal(1230000000000, reader.GetValueInt64());
            Assert.Equal(1230000000000, reader.GetValueInt64Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Null(reader.GetValueInt64Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal(4560000000000, reader.GetValueInt64());
            Assert.Equal(4560000000000, reader.GetValueInt64Nullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.False, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueInt64Nullable()));
        }

        [Fact]
        public void ReaderGetValueDoubleTest()
        {
            var reader = new JsonReader("{ \"x\": [1230, null, \"abc\", \"456.1\", true, \"78:9\"] }");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.Equal("x", reader.PropertyName);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Equal(1230.0, reader.GetValueDouble());
            Assert.Equal(1230.0, reader.GetValueDoubleNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Null(reader.GetValueDoubleNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Equal(456.1, reader.GetValueDouble());
            Assert.Equal(456.1, reader.GetValueDoubleNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.True, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<FormatException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDouble()));
            Assert.Throws<InvalidOperationException>(() => Assert.Equal(-1, reader.GetValueDoubleNullable()));
        }

        [Fact]
        public void ReaderGetValueBoolTest()
        {
            var reader = new JsonReader("[1230, null, \"abc\", \"True\", { x: false }]");

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Number, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.Null, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBool()));
            Assert.Null(reader.GetValueBoolNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<FormatException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.String, reader.ElementType);
            Assert.True(reader.GetValueBool());
            Assert.True(reader.GetValueBoolNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.StartObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.PropertyName, reader.ElementType);
            Assert.Equal("x", reader.PropertyName);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.False, reader.ElementType);
            Assert.False(reader.GetValueBool());
            Assert.False(reader.GetValueBoolNullable());

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndObject, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.True(reader.Read());
            Assert.Equal(JsonElementType.EndArray, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));

            Assert.False(reader.Read());
            Assert.Equal(JsonElementType.None, reader.ElementType);
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBool()));
            Assert.Throws<InvalidOperationException>(() => Assert.False(reader.GetValueBoolNullable()));
        }


        [Theory]
        [InlineData("\"str\"", 1, JsonElementType.String, JsonElementType.None, null)]
        [InlineData("[1, 2, 3]", 1, JsonElementType.StartArray, JsonElementType.None, null)]
        [InlineData("[1, true, false]", 2, JsonElementType.Number, JsonElementType.True, null)]
        [InlineData("[{x: 1}, {x: 2}, {x:3}]", 1, JsonElementType.StartArray, JsonElementType.None, null)]
        [InlineData("{x: [1,2,{a:false}], y: [3, 4, {a:true}]}", 1, JsonElementType.StartObject, JsonElementType.None, null)]
        [InlineData("{ x: [1, 2, 3], y: false }", 3, JsonElementType.StartArray, JsonElementType.PropertyName, "y")]
        [InlineData("{ x: [1, 2], y: false }", 2, JsonElementType.PropertyName, JsonElementType.PropertyName, "y")]
        [InlineData("[{ x: [1, 2], y: false }, {\"abc\": \"bcd\"}]", 2, JsonElementType.StartObject, JsonElementType.StartObject, null)]
        [InlineData("{ x: [1, 2], y: false, a: { m: 1 } }", 2, JsonElementType.PropertyName, JsonElementType.PropertyName, "y")]
        [InlineData("{ x: [1, 2], y: false, a: { m: 1 } }", 7, JsonElementType.PropertyName, JsonElementType.PropertyName, "a")]
        [InlineData("{ x: [1, 2], y: false, a: { m: 1 } }", 9, JsonElementType.PropertyName, JsonElementType.EndObject, null)]
        internal void ReaderSkipTest(string json, int readBeforeSkip, JsonElementType elementToSkip, JsonElementType elementAfterSkip, string propertyNameAfterSkip)
        {
            var reader = new JsonReader(json);

            for (int i = 0; i < readBeforeSkip; i++)
                Assert.True(reader.Read());

            Assert.Equal(elementToSkip, reader.ElementType);

            reader.Skip();

            Assert.Equal(elementAfterSkip, reader.ElementType);
            Assert.Equal(propertyNameAfterSkip, reader.PropertyName);
        }
    }
}
