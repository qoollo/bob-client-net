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

            parser = new JsonParser("null");
            var arrNull = parser.ParseArrayNullable<int>(p => p.ParseInt32())?.ToArray();
            Assert.Null(arrNull);
            Assert.True(parser.IsComplete);

            parser = new JsonParser("null");
            var arrNull2 = parser.ParseArray<int>(p => p.ParseInt32(), nullable: true)?.ToArray();
            Assert.Null(arrNull2);
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


        public class SimpleObject : IEquatable<SimpleObject>
        {
            public SimpleObject() { }
            public SimpleObject(int valInt, bool valBool, string valString)
            {
                ValInt = valInt;
                ValBool = valBool;
                ValString = valString;
            }

            public int ValInt { get; set; }
            public bool ValBool { get; set; }
            public string ValString { get; private set; }


            private static JsonParsingObjectInfo<SimpleObject> _objInfo =
                new JsonParsingObjectInfo<SimpleObject>(() => new SimpleObject())
                .AddProperty("val_int", true, o => o.ValInt, p => p.ParseInt32())
                .AddProperty("val_bool", true, o => o.ValBool, p => p.ParseBool())
                .AddProperty("val_string", false, o => o.ValString, p => p.ParseStringNullable());

            internal static SimpleObject Parse(IJsonValueParser parser, bool nullable = false)
            {
                return parser.ParseObject(_objInfo, nullable);
            }

            public bool Equals(SimpleObject other)
            {
                if (other is null)
                    return false;

                return ValInt == other.ValInt &&
                    ValBool == other.ValBool &&
                    ValString == other.ValString;
            }

            public override bool Equals(object obj)
            {
                if (obj is SimpleObject other)
                    return Equals(other);
                return false;
            }

            public override int GetHashCode()
            {
                return ValInt.GetHashCode();
            }
        }

        [Fact]
        public void JsonObjectInfoTest()
        {
            var jsonObjInfo = new JsonParsingObjectInfo<SimpleObject>(() => new SimpleObject());
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


        [Theory]
        [InlineData("{\"val_int\": 1, \"val_bool\": false, \"val_string\": \"str\"}", 1, false, "str")]
        [InlineData("{\"val_string\": null, \"val_int\": -1, \"val_bool\": true}", -1, true, null)]
        [InlineData("{\"val_string\": \"\\t_str\", \"non_exist\": 1.1, \"val_int\": 100, \"val_bool\": true}", 100, true, "\t_str")]
        [InlineData("{\"val_int\": 1, \"val_bool\": false}", 1, false, null)]
        public void SimpleObjectParsingTest(string json, int expectedValInt, bool expectedValBool, string expectedValString)
        {
            var parser = new JsonParser(json);
            var obj = SimpleObject.Parse(parser);

            Assert.Equal(expectedValInt, obj.ValInt);
            Assert.Equal(expectedValBool, obj.ValBool);
            Assert.Equal(expectedValString, obj.ValString);
        }

        [Fact]
        public void SimpleObjectParsingNullTest()
        {
            var parser = new JsonParser("null");
            var obj = SimpleObject.Parse(parser, nullable: true);
            Assert.Null(obj);
            Assert.True(parser.IsComplete);

            parser = new JsonParser("null");
            Assert.Throws<FormatException>(() => SimpleObject.Parse(parser, nullable: false));
        }

        [Fact]
        public void SimpleObjectParsingFailTest()
        {
            var parser = new JsonParser("{\"val_bool\": false, \"val_string\": \"str\"}");
            Assert.Throws<JsonParsingException>(() => SimpleObject.Parse(parser));

            parser = new JsonParser("{\"val_int\": 1,\"val_bool\": 1, \"val_string\": \"str\"}");
            Assert.Throws<FormatException>(() => SimpleObject.Parse(parser));

            parser = new JsonParser("{\"val_int\": \"false\",\"val_bool\": false, \"val_string\": \"str\"}");
            Assert.Throws<FormatException>(() => SimpleObject.Parse(parser));
        }


        public class LargeObject: IEquatable<LargeObject>
        {
            public double? ValDouble { get; internal set; }
            public SimpleObject MainSimpleObject { get; internal set; }
            public List<SimpleObject> SimpleObjects { get; internal set; }
            public int[] IntArray { get; internal set; }
            public string[] StringOptionalArray { get; internal set; }
            public LargeObject ChildLargeObject { get; internal set; }


            private static JsonParsingObjectInfo<LargeObject> _objInfo =
                new JsonParsingObjectInfo<LargeObject>(() => new LargeObject())
                .AddProperty("val_double", false, o => o.ValDouble, p => p.ParseDoubleNullable())
                .AddProperty("main_simple_object", true, o => o.MainSimpleObject, p => SimpleObject.Parse(p))
                .AddProperty("simple_objects", false, o => o.SimpleObjects, p => p.ParseArray(pItem => SimpleObject.Parse(pItem, nullable: true), nullable: true)?.ToList())
                .AddProperty("int_array", true, o => o.IntArray, p => p.ParseArray(pItem => pItem.ParseInt32()).ToArray())
                .AddProperty("string_optional_array", false, o => o.StringOptionalArray, p => p.ParseArray(pItem => pItem.ParseStringNullable(), nullable: true)?.ToArray())
                .AddProperty("child_large_object", false, o => o.ChildLargeObject, p => LargeObject.Parse(p, nullable: true));

            internal static LargeObject Parse(IJsonValueParser parser, bool nullable = false)
            {
                return parser.ParseObject(_objInfo, nullable);
            }

            public bool Equals(LargeObject other)
            {
                if (other is null)
                    return false;

                return ValDouble == other.ValDouble &&
                       ((MainSimpleObject == null && other.MainSimpleObject == null) || (MainSimpleObject?.Equals(other.MainSimpleObject) ?? false)) &&
                       ((SimpleObjects == null && other.SimpleObjects == null) || (SimpleObjects != null && other.SimpleObjects != null && SimpleObjects.SequenceEqual(other.SimpleObjects))) &&
                       ((IntArray == null && other.IntArray == null) || (IntArray != null && other.IntArray != null && IntArray.SequenceEqual(other.IntArray))) &&
                       ((StringOptionalArray == null && other.StringOptionalArray == null) || (StringOptionalArray != null && other.StringOptionalArray != null && StringOptionalArray.SequenceEqual(other.StringOptionalArray))) &&
                       ((ChildLargeObject == null && other.ChildLargeObject == null) || (ChildLargeObject?.Equals(other.ChildLargeObject) ?? false));
            }
            public override bool Equals(object obj)
            {
                if (obj is LargeObject other)
                    return Equals(other);
                return false;
            }
            public override int GetHashCode()
            {
                return ValDouble?.GetHashCode() ?? 0;
            }
        }


        public static IEnumerable<object[]> LargeObjectParsingDataSource
        {
            get
            {
                return new[]
                {
                    new object[] { "{\"val_double\": 1.5, " +
                                    "\"main_simple_object\": { \"val_int\": 1, \"val_bool\": false, \"val_string\": \"str\"}," +
                                    "simple_objects: [{ \"val_int\": 1, \"val_bool\": false, \"val_string\": \"str\"}]," +
                                    "\"int_array\": [1, 2, 3]," +
                                    "\"child_large_object\": null }",
                                    new LargeObject()
                                    {
                                        ValDouble = 1.5,
                                        MainSimpleObject = new SimpleObject(1, false, "str"),
                                        SimpleObjects = new List<SimpleObject>() { new SimpleObject(1, false, "str") },
                                        IntArray = new int[] {1, 2, 3},
                                        StringOptionalArray = null,
                                        ChildLargeObject = null
                                    }
                    },
                    new object[] { "{\"val_double\": null, " +
                                    "\"main_simple_object\": { \"val_int\": 12, \"val_bool\": false, \"val_string\": \"str\"}," +
                                    "simple_objects: null," +
                                    "\"something\": 100," +
                                    "\"int_array\": [\"1\", \"2\", \"3\"]," +
                                    "\"string_optional_array\": [\"str\", null, \"str2\"]," +
                                    "\"child_large_object\": { \"main_simple_object\": { \"val_int\": 1, \"val_bool\": \"false\", \"val_string\": \"str\"}, \"int_array\": [] } }",
                                    new LargeObject()
                                    {
                                        ValDouble = null,
                                        MainSimpleObject = new SimpleObject(12, false, "str"),
                                        SimpleObjects = null,
                                        IntArray = new int[] {1, 2, 3},
                                        StringOptionalArray = new string[] { "str", null, "str2" },
                                        ChildLargeObject = new LargeObject() { MainSimpleObject = new SimpleObject(1, false, "str"), IntArray = new int[] { } }
                                    }
                    },
                };
            }
        }


        [Theory]
        [MemberData(nameof(LargeObjectParsingDataSource))]
        public void LargeObjectParsingTest(string json, LargeObject largeObjExpected)
        {
            var parser = new JsonParser(json);
            var obj = LargeObject.Parse(parser);

            Assert.Equal(largeObjExpected, obj);
        }
    }
}
