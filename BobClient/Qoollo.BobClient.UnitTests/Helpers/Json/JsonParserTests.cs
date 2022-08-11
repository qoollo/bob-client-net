using Qoollo.BobClient.Helpers.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Qoollo.BobClient.UnitTests.Helpers.Json
{
    public class JsonParserTests
    {
        [Fact]
        public void ParseSimpleValueTest()
        {
            var parser = new JsonParser("\"abc\"");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.Equal("abc", parser.ParseString());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("123");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.Equal(123, parser.ParseInt32());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("123");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.Equal(123, parser.ParseInt32Nullable());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("123");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.False(parser.IsNull);
            Assert.Equal(123, parser.ParseInt64());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("null");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.True(parser.IsNull);
            Assert.Null(parser.ParseInt64Nullable());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("123.0");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.Equal(123.0, parser.ParseDouble());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("123.0");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.Equal(123.0, parser.ParseDoubleNullable());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("false");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.False(parser.ParseBool());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("false");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.False(parser.ParseBoolNullable());
            Assert.True(parser.IsComplete);

            parser = new JsonParser("true");
            Assert.True(parser.IsSimpleValue);
            Assert.False(parser.IsArray);
            Assert.False(parser.IsObject);
            Assert.True(parser.ParseSimpleValue<bool>());
            Assert.True(parser.IsComplete);
        }

        [Fact]
        public void ParseArrayTest()
        {
            var parser = new JsonParser("[1, 2, 3]");
            Assert.False(parser.IsSimpleValue);
            Assert.True(parser.IsArray);
            Assert.False(parser.IsObject);
            var arr = parser.ParseArray<int>(p => p.ParseInt32()).ToArray();
            Assert.Equal(new int[] { 1, 2, 3 }, arr);
            Assert.True(parser.IsComplete);

            parser = new JsonParser("[true, false, null]");
            Assert.False(parser.IsSimpleValue);
            Assert.True(parser.IsArray);
            Assert.False(parser.IsObject);
            var arrBool = parser.ParseArray<bool?>(p => p.ParseBoolNullable()).ToArray();
            Assert.Equal(new bool?[] { true, false, null }, arrBool);
            Assert.True(parser.IsComplete);

            parser = new JsonParser("[\"abc\", \"cde\"]");
            Assert.False(parser.IsSimpleValue);
            Assert.True(parser.IsArray);
            Assert.False(parser.IsObject);
            var arrStr = parser.ParseArray<string>(p => p.ParseString()).ToArray();
            Assert.Equal(new string[] { "abc", "cde" }, arrStr);
            Assert.True(parser.IsComplete);
        }

        [Theory]
        [InlineData("[]", new int[0])]
        [InlineData("[1]", new int[] { 1 })]
        [InlineData("[1, -1]", new int[] { 1, -1 })]
        public void ParseArrayInt32Test(string json, int[] expected)
        {
            var parser = new JsonParser(json);
            Assert.False(parser.IsSimpleValue);
            Assert.True(parser.IsArray);
            Assert.False(parser.IsObject);
            var arr = parser.ParseArray<int>(p => p.ParseInt32()).ToArray();
            Assert.Equal(expected, arr);
            Assert.True(parser.IsComplete);
        }


        public class SimpleObject
        {
            public int ValInt { get; set; }
            public bool ValBool { get; set; }
            public string ValString { get; private set; }
        }

        [Fact]
        public void JsonObjectInfoTest()
        {
            var jsonObjInfo = new JsonObjectInfo<SimpleObject>();
            Assert.Equal(0, jsonObjInfo.Properties.Count);

            jsonObjInfo.AddProperty("val_int", true, (o, p) => o.ValInt = p.ParseInt32());
            Assert.Equal(1, jsonObjInfo.Properties.Count);
            Assert.True(jsonObjInfo.TryGetProperty("val_int", out var prop1));
            Assert.True(prop1.IsRequired);
            Assert.Equal("val_int", prop1.Name);

            jsonObjInfo.AddProperty("val_bool", true, (o, p) => { o.ValBool = p.ParseBool(); return o; });
            Assert.Equal(2, jsonObjInfo.Properties.Count);
            Assert.True(jsonObjInfo.TryGetProperty("val_bool", out var prop2));
            Assert.True(prop2.IsRequired);
            Assert.Equal("val_bool", prop2.Name);

            jsonObjInfo.AddProperty("val_string", false, o => o.ValString, p => p.ParseString());
            Assert.Equal(3, jsonObjInfo.Properties.Count);
            Assert.True(jsonObjInfo.TryGetProperty("val_string", out var prop3));
            Assert.False(prop3.IsRequired);
            Assert.Equal("val_string", prop3.Name);


            SimpleObject testObj = new SimpleObject();

            jsonObjInfo.Properties["val_int"].ParseProperty(testObj, new JsonParser("123"));
            Assert.Equal(123, testObj.ValInt);

            jsonObjInfo.Properties["val_bool"].ParseProperty(testObj, new JsonParser("true"));
            Assert.True(testObj.ValBool);

            jsonObjInfo.Properties["val_string"].ParseProperty(testObj, new JsonParser("\"abc\""));
            Assert.Equal("abc", testObj.ValString);
        }
    }
}
