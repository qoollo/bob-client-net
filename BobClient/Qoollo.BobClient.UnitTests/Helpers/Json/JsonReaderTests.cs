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
            Assert.Null(context.ProperyName);

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
                    Assert.NotNull(context.ProperyName);

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
            Assert.Null(context.ProperyName);
            Assert.Equal(JsonScopeElement.None, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.StartObject, new JsonLexemeInfo(JsonLexemeType.StartObject, 0, 1)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Null(context.ProperyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.PropertyName, new JsonLexemeInfo(JsonLexemeType.String, 2, 7)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Equal("\"abc\"", context.ProperyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.Number, new JsonLexemeInfo(JsonLexemeType.Number, 9, 12)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Equal("\"abc\"", context.ProperyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.PropertyName, new JsonLexemeInfo(JsonLexemeType.Number, 14, 19)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Equal("\"def\"", context.ProperyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.StartArray, new JsonLexemeInfo(JsonLexemeType.StartArray, 21, 22)));
            Assert.Equal("{, [", context.GetScopeStackNotClosedSequence());
            Assert.Equal(2, context.ScopeStack.Count);
            Assert.Equal("\"def\"", context.ProperyName);
            Assert.Equal(JsonScopeElement.Array, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.False, new JsonLexemeInfo(JsonLexemeType.False, 23, 28)));
            Assert.Equal("{, [", context.GetScopeStackNotClosedSequence());
            Assert.Equal(2, context.ScopeStack.Count);
            Assert.Null(context.ProperyName);
            Assert.Equal(JsonScopeElement.Array, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Array, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.EndArray, new JsonLexemeInfo(JsonLexemeType.EndArray, 29, 30)));
            Assert.Equal("{", context.GetScopeStackNotClosedSequence());
            Assert.Equal(1, context.ScopeStack.Count);
            Assert.Null(context.ProperyName);
            Assert.Equal(JsonScopeElement.Object, context.CurrentScope);
            Assert.Equal(JsonScopeElement.Object, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.EndObject, new JsonLexemeInfo(JsonLexemeType.EndArray, 31, 32)));
            Assert.Equal("", context.GetScopeStackNotClosedSequence());
            Assert.Equal(0, context.ScopeStack.Count);
            Assert.Null(context.ProperyName);
            Assert.Equal(JsonScopeElement.None, context.CurrentScope);
            Assert.Equal(JsonScopeElement.None, context.EnclosingScope);

            context.ProcessNextElement(new JsonElementInfo(JsonElementType.None, new JsonLexemeInfo(JsonLexemeType.None, 32, 32)));
            Assert.Equal("", context.GetScopeStackNotClosedSequence());
            Assert.Equal(0, context.ScopeStack.Count);
            Assert.Null(context.ProperyName);
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
                    Assert.NotNull(reader.ProperyName);

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
        }
    }
}
