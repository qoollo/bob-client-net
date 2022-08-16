using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.Helpers.Json
{
    internal enum JsonElementType
    {
        None = 0,
        StartObject,
        EndObject,
        StartArray,
        EndArray,
        PropertyName,
        Null,
        True,
        False,
        Number,
        String
    }

    internal static class JsonElementTypeExtensions
    {
        public static bool IsSimpleValueType(this JsonElementType elementType)
        {
            return elementType == JsonElementType.Null ||
                   elementType == JsonElementType.True ||
                   elementType == JsonElementType.False ||
                   elementType == JsonElementType.Number ||
                   elementType == JsonElementType.String;
        }

        public static bool IsValueType(this JsonElementType elementType)
        {
            return elementType == JsonElementType.StartObject ||
                   elementType == JsonElementType.StartArray ||
                   IsSimpleValueType(elementType);
        }

        public static JsonElementType IntoJsonElementType(this JsonLexemeType lexeme)
        {
            switch (lexeme)
            {
                case JsonLexemeType.None:
                    return JsonElementType.None;
                case JsonLexemeType.StartObject:
                    return JsonElementType.StartObject;
                case JsonLexemeType.EndObject:
                    return JsonElementType.EndObject;
                case JsonLexemeType.StartArray:
                    return JsonElementType.StartArray;
                case JsonLexemeType.EndArray:
                    return JsonElementType.EndArray;
                case JsonLexemeType.Null:
                    return JsonElementType.Null;
                case JsonLexemeType.True:
                    return JsonElementType.True;
                case JsonLexemeType.False:
                    return JsonElementType.False;
                case JsonLexemeType.Number:
                    return JsonElementType.Number;
                case JsonLexemeType.String:
                case JsonLexemeType.StringWithoutEscSeq:
                    return JsonElementType.String;
                case JsonLexemeType.KeyValueSeparator:
                case JsonLexemeType.ItemSeparator:
                case JsonLexemeType.Identifier:
                    throw new ArgumentException($"Conversion from JsonLexemeType.{lexeme} into JsonElementType is not supported");
                default:
                    throw new InvalidOperationException($"Unknown JsonLexemeType: {lexeme}");
            }
        }
    }

    internal readonly struct JsonElementInfo
    {
        public static JsonElementInfo None { get { return new JsonElementInfo(JsonElementType.None, JsonLexemeInfo.None); } }

        public JsonElementInfo(JsonElementType elementType, JsonLexemeInfo lexeme)
        {
            Type = elementType;
            Lexeme = lexeme;
        }
        public JsonElementInfo(JsonLexemeInfo lexeme)
        {
            Type = lexeme.Type.IntoJsonElementType();
            Lexeme = lexeme;
        }

        public readonly JsonElementType Type;
        public readonly JsonLexemeInfo Lexeme;

        public override string ToString()
        {
            return Lexeme.ToString();
        }
    }


    internal enum JsonScopeElement
    {
        None,
        Object,
        Array
    }


    [System.Diagnostics.DebuggerDisplay("{ElementType} (Scope = {EnclosingScope}, PropertyName = {PropertyName}, Value = {RawValueDebugView})")]
    internal class JsonReader
    {
        internal sealed class ReaderContext
        {
            private readonly string _source;
            private readonly List<JsonScopeElement> _scopeStack;
            private string _propertyNameLazy;

            public ReaderContext(string source)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));

                _scopeStack = new List<JsonScopeElement>();
                LastElement = JsonElementInfo.None;
                EnclosingScope = JsonScopeElement.None;
                PropertyNameElement = null;
                _propertyNameLazy = null;
            }

            public JsonElementInfo LastElement { get; private set; }
            public JsonScopeElement EnclosingScope { get; private set; }
            public JsonElementInfo? PropertyNameElement { get; private set; }
            public string PropertyName
            {
                get
                {
                    if (!PropertyNameElement.HasValue)
                        return null;
                    if (_propertyNameLazy != null)
                        return _propertyNameLazy;

                    return _propertyNameLazy = GetPropertyNameString();
                }
            }

            public IReadOnlyList<JsonScopeElement> ScopeStack { get { return _scopeStack; } }
            public bool IsScopeStackEmpty { get { return _scopeStack.Count == 0; } }
            public JsonScopeElement CurrentScope { get { return _scopeStack.Count > 0 ? _scopeStack[_scopeStack.Count - 1] : JsonScopeElement.None; } }


            private string GetPropertyNameString()
            {
                if (!PropertyNameElement.HasValue)
                    return null;

                var lexeme = PropertyNameElement.Value.Lexeme;
                if (lexeme.Type == JsonLexemeType.String)
                    return JsonLexemeReader.ParseString(_source, lexeme.Start, lexeme.End);
                else if (lexeme.Type == JsonLexemeType.StringWithoutEscSeq)
                    return JsonLexemeReader.ParseStringWithoutEscSeq(_source, lexeme.Start, lexeme.End);
                else if (lexeme.Type == JsonLexemeType.Identifier)
                    return JsonLexemeReader.ParseIdentifier(_source, lexeme.Start, lexeme.End, validate: false);
                else
                    throw new InvalidOperationException($"PropertyName can only be a string or identifier. It cannot be {lexeme.Type}");
            }

            public JsonElementInfo ProcessNextElement(JsonElementInfo newElement)
            {
                JsonScopeElement initialScope = CurrentScope;

                if (newElement.Type.IsValueType())
                {
                    if (!IsScopeStackEmpty && CurrentScope == JsonScopeElement.Object && LastElement.Type != JsonElementType.PropertyName)
                        throw new JsonParsingException($"Value inside object can be placed only after property name (position: {newElement.Lexeme.Start})");
                    if (IsScopeStackEmpty && LastElement.Type != JsonElementType.None)
                        throw new JsonParsingException($"Only one element allowed in the root scope of JSON (position: {newElement.Lexeme.Start})");
                }


                switch (newElement.Type)
                {
                    case JsonElementType.None:
                        if (!IsScopeStackEmpty)
                            throw new JsonParsingException($"Json ended too early, not all objects or arrays are closed. Not closed sequence: {GetScopeStackNotClosedSequence()}");
                        break;
                    case JsonElementType.StartObject:
                        _scopeStack.Add(JsonScopeElement.Object);
                        break;
                    case JsonElementType.EndObject:
                        if (IsScopeStackEmpty || CurrentScope != JsonScopeElement.Object)
                            throw new JsonParsingException($"Json object ended, but there was no assotiated json object start (position: {newElement.Lexeme.Start})");
                        _scopeStack.RemoveAt(_scopeStack.Count - 1);
                        break;
                    case JsonElementType.StartArray:
                        _scopeStack.Add(JsonScopeElement.Array);
                        break;
                    case JsonElementType.EndArray:
                        if (IsScopeStackEmpty || CurrentScope != JsonScopeElement.Array)
                            throw new JsonParsingException($"Json array ended, but there was no assotiated json array start (position: {newElement.Lexeme.Start})");
                        _scopeStack.RemoveAt(_scopeStack.Count - 1);
                        break;
                    case JsonElementType.PropertyName:
                        if (IsScopeStackEmpty || CurrentScope != JsonScopeElement.Object)
                            throw new JsonParsingException($"Json property name can be inside Json object (position: {newElement.Lexeme.Start})");
                        break;
                    case JsonElementType.Null:
                    case JsonElementType.True:
                    case JsonElementType.False:
                    case JsonElementType.Number:
                    case JsonElementType.String:
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown JsonElementType: {newElement.Type}");
                }

                if (newElement.Type == JsonElementType.PropertyName)
                {
                    PropertyNameElement = newElement;
                    _propertyNameLazy = null;
                }
                else if (newElement.Type == JsonElementType.None || LastElement.Type != JsonElementType.PropertyName)
                {
                    PropertyNameElement = null;
                    _propertyNameLazy = null;
                }

                if (newElement.Type == JsonElementType.StartArray || newElement.Type == JsonElementType.StartObject)
                    EnclosingScope = initialScope;
                else
                    EnclosingScope = CurrentScope;

                LastElement = newElement;
                return newElement;
            }


            public string GetScopeStackNotClosedSequence()
            {
                if (_scopeStack.Count == 0)
                    return "";

                StringBuilder result = new StringBuilder(capacity: _scopeStack.Count * 3);
                foreach (var item in _scopeStack)
                {
                    if (item == JsonScopeElement.Object)
                        result.Append("{, ");
                    else if (item == JsonScopeElement.Array)
                        result.Append("[, ");
                }

                result.Remove(result.Length - 2, 2);
                return result.ToString();
            }
        }


        // =================


        private readonly JsonLexemeReader _lexemeReader;
        private readonly ReaderContext _context;

        private bool _isBroken;

        public JsonReader(string source)
        {
            _lexemeReader = new JsonLexemeReader(source);
            _context = new ReaderContext(source);

            _isBroken = false;
        }

        public bool IsBroken { get { return _isBroken; } }
        public bool IsEnd { get { return _lexemeReader.IsEnd; } }

        internal int Position { get { return _lexemeReader.Position; } }

        private JsonElementInfo Element { get { return _context.LastElement; } }
        public JsonElementType ElementType { get { return _context.LastElement.Type; } }

        public JsonScopeElement EnclosingScope { get { return _context.EnclosingScope; } }
        public JsonScopeElement CurrentScope { get { return _context.CurrentScope; } }
        public string PropertyName { get { return _context.PropertyName; } }

        private string RawValueDebugView
        {
            get
            {
                switch (Element.Type)
                {
                    case JsonElementType.Null:
                    case JsonElementType.True:
                    case JsonElementType.False:
                    case JsonElementType.Number:
                        return _lexemeReader.GetRawString(Element.Lexeme, lengthLimit: 32);
                    case JsonElementType.String:
                        return _lexemeReader.GetRawString(Element.Lexeme, lengthLimit: 32).Trim('\"');
                    default:
                        return "-";
                }
            }
        }

        private static JsonElementInfo ReadObjectItem(JsonLexemeReader lexemeReader)
        {
            var currentLexeme = lexemeReader.CurrentLexeme;

            if (currentLexeme.Type == JsonLexemeType.EndObject)
            {
                return new JsonElementInfo(currentLexeme);
            }
            else if (currentLexeme.Type.IsStringLexeme() || currentLexeme.Type == JsonLexemeType.Identifier)
            {
                if (!lexemeReader.Read())
                    throw new JsonParsingException($"Json ended too early. ':' expected after property name '{lexemeReader.ExtractLexemeSurroundingText(currentLexeme)}'");
                if (lexemeReader.CurrentLexeme.Type != JsonLexemeType.KeyValueSeparator)
                    throw new JsonParsingException($"':' expected after property name at {lexemeReader.CurrentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");

                return new JsonElementInfo(JsonElementType.PropertyName, currentLexeme);
            }
            else
            {
                throw new JsonParsingException($"Object end or property name expected inside Json object at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");
            }
        }

        private static JsonElementInfo ReadArrayItem(JsonLexemeReader lexemeReader)
        {
            var currentLexeme = lexemeReader.CurrentLexeme;

            if (currentLexeme.Type == JsonLexemeType.EndArray)
            {
                return new JsonElementInfo(currentLexeme);
            }
            else if (currentLexeme.Type.IsValueType())
            {
                return new JsonElementInfo(currentLexeme);
            }
            else
            {
                throw new JsonParsingException($"Array end or value expected inside Json array at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");
            }
        }

        private static JsonElementInfo ReadNextElement(JsonLexemeReader lexemeReader, ReaderContext context)
        {
            bool isStart = lexemeReader.IsStart;

            if (!lexemeReader.Read())
            {
                if (!context.IsScopeStackEmpty)
                    throw new JsonParsingException($"Json ended too early, not all objects or arrays are closed. Not closed sequence: {context.GetScopeStackNotClosedSequence()}");

                return new JsonElementInfo(JsonElementType.None, lexemeReader.CurrentLexeme);
            }

            var currentLexeme = lexemeReader.CurrentLexeme;

            switch (context.LastElement.Type)
            {
                case JsonElementType.None:
                    if (!isStart)
                        throw new InvalidOperationException("JsonElementType.None can be only at the beggining");
                    if (!currentLexeme.Type.IsValueType())
                        throw new JsonParsingException($"Object, array or simple value expected at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");

                    return new JsonElementInfo(currentLexeme);

                case JsonElementType.StartObject:
                    return ReadObjectItem(lexemeReader);
                
                case JsonElementType.StartArray:
                    return ReadArrayItem(lexemeReader);

                case JsonElementType.PropertyName:
                    if (!currentLexeme.Type.IsValueType())
                        throw new JsonParsingException($"Value expected after property name at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");

                    return new JsonElementInfo(currentLexeme);

                case JsonElementType.EndObject:
                case JsonElementType.EndArray:
                case JsonElementType.Null:
                case JsonElementType.True:
                case JsonElementType.False:
                case JsonElementType.Number:
                case JsonElementType.String:
                    switch (context.CurrentScope)
                    {
                        case JsonScopeElement.None:
                            throw new JsonParsingException($"Json end expected after value at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");
                        
                        case JsonScopeElement.Object:
                            if (currentLexeme.Type == JsonLexemeType.EndObject)
                            {
                                return new JsonElementInfo(currentLexeme);
                            }
                            else if (currentLexeme.Type == JsonLexemeType.ItemSeparator)
                            {
                                if (!lexemeReader.Read())
                                    throw new JsonParsingException($"Json ended too early. New property expected after ',' inside object: '{lexemeReader.ExtractLexemeSurroundingText(currentLexeme)}'");

                                return ReadObjectItem(lexemeReader);
                            }
                            else
                            {
                                throw new JsonParsingException($"Object end or ',' expected inside Json object at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");
                            }

                        case JsonScopeElement.Array:
                            if (currentLexeme.Type == JsonLexemeType.EndArray)
                            {
                                return new JsonElementInfo(currentLexeme);
                            }
                            else if (currentLexeme.Type == JsonLexemeType.ItemSeparator)
                            {
                                if (!lexemeReader.Read())
                                    throw new JsonParsingException($"Json ended too early. New value expected after ',' inside array: '{lexemeReader.ExtractLexemeSurroundingText(currentLexeme)}'");

                                return ReadArrayItem(lexemeReader);
                            }
                            else
                            {
                                throw new JsonParsingException($"Array end or ',' expected inside Json array at {currentLexeme.Start}, but found: '{lexemeReader.ExtractLexemeSurroundingText()}'");
                            }

                        default:
                            throw new InvalidOperationException($"Unknown JsonSurroundingElementType: {context.CurrentScope}");
                    }

                default:
                    throw new InvalidOperationException($"Unknown JsonElementType: {context.LastElement.Type}");
            }
        }


        public bool Read()
        {
            if (IsBroken)
                throw new JsonParsingException("Json structure is broken");

            if (IsEnd)
                return false;

            try
            {
                var nextElement = ReadNextElement(_lexemeReader, _context);
                _context.ProcessNextElement(nextElement);

                return nextElement.Type != JsonElementType.None;
            }
            catch (JsonParsingException)
            {
                _isBroken = true;
                throw;
            }
        }


        public void Skip()
        {
            if (IsBroken)
                throw new JsonParsingException("Json structure is broken");

            if (IsEnd)
                return;

            if (ElementType == JsonElementType.PropertyName)
                this.Read();

            if (ElementType == JsonElementType.StartObject || ElementType == JsonElementType.StartArray)
            {
                int depth = _context.ScopeStack.Count;
                while (this.Read() && _context.ScopeStack.Count >= depth) { }
            }

            this.Read();
        }


        public bool IsValueNull()
        {
            if (IsBroken)
                throw new JsonParsingException("Json structure is broken");

            return _lexemeReader.IsValueNull(Element.Lexeme);
        }

        public object GetValue()
        {
            if (IsBroken)
                throw new JsonParsingException("JSON structure is broken");
            if (IsEnd)
                throw new InvalidOperationException("JSON reader run to an end");

            switch (Element.Type)
            {
                case JsonElementType.None:
                case JsonElementType.StartObject:
                case JsonElementType.EndObject:
                case JsonElementType.StartArray:
                case JsonElementType.EndArray:
                case JsonElementType.PropertyName:
                    throw new InvalidOperationException($"JSON element {Element.Type} is not a value");
                case JsonElementType.Null:
                    return null;
                case JsonElementType.True:
                    return true;
                case JsonElementType.False:
                    return false;
                case JsonElementType.Number:
                    return _lexemeReader.GetValueDouble(Element.Lexeme);
                case JsonElementType.String:
                    return _lexemeReader.GetValueString(Element.Lexeme);
                default:
                    throw new InvalidOperationException($"Unknown JSON element type: {Element.Type}");
            }
        }


        public string GetValueString()
        {
            if (IsBroken)
                throw new JsonParsingException("JSON structure is broken");
            if (IsEnd)
                throw new InvalidOperationException("JSON reader run to an end");

            switch (Element.Type)
            {
                case JsonElementType.None:
                case JsonElementType.StartObject:
                case JsonElementType.EndObject:
                case JsonElementType.StartArray:
                case JsonElementType.EndArray:
                case JsonElementType.PropertyName:
                    throw new InvalidOperationException($"JSON element {Element.Type} is not a value and cannot be read as string");
                case JsonElementType.Null:
                    return null;
                case JsonElementType.True:
                    return "true";
                case JsonElementType.False:
                    return "false";
                case JsonElementType.Number:
                    return _lexemeReader.GetRawString(Element.Lexeme);
                case JsonElementType.String:
                    return _lexemeReader.GetValueString(Element.Lexeme);
                default:
                    throw new InvalidOperationException($"Unknown JSON element type: {Element.Type}");
            }
        }

        public int GetValueInt32()
        {
            if (IsBroken)
                throw new JsonParsingException("JSON structure is broken");
            if (IsEnd)
                throw new InvalidOperationException("JSON reader run to an end");

            switch (Element.Type)
            {
                case JsonElementType.None:
                case JsonElementType.StartObject:
                case JsonElementType.EndObject:
                case JsonElementType.StartArray:
                case JsonElementType.EndArray:
                case JsonElementType.PropertyName:
                    throw new InvalidOperationException($"JSON element {Element.Type} is not a value and cannot be read as number");
                case JsonElementType.Null:
                case JsonElementType.True:
                case JsonElementType.False:
                    throw new FormatException($"JSON {Element.Type} cannot be parsed as number");
                case JsonElementType.Number:
                    return _lexemeReader.GetValueInt32(Element.Lexeme);
                case JsonElementType.String:
#if NET5_0_OR_GREATER
                    var strSpan = _lexemeReader.GetValueStringAsSpan(Element.Lexeme);
                    return JsonLexemeReader.ParseInt32(strSpan);
#else
                    var str = _lexemeReader.GetValueString(Element.Lexeme);
                    return JsonLexemeReader.ParseInt32(str);
#endif
                default:
                    throw new InvalidOperationException($"Unknown JSON element type: {Element.Type}");
            }
        }
        public int? GetValueInt32Nullable()
        {
            if (IsValueNull())
                return null;

            return GetValueInt32();
        }

        public long GetValueInt64()
        {
            if (IsBroken)
                throw new JsonParsingException("JSON structure is broken");
            if (IsEnd)
                throw new InvalidOperationException("JSON reader run to an end");

            switch (Element.Type)
            {
                case JsonElementType.None:
                case JsonElementType.StartObject:
                case JsonElementType.EndObject:
                case JsonElementType.StartArray:
                case JsonElementType.EndArray:
                case JsonElementType.PropertyName:
                    throw new InvalidOperationException($"JSON element {Element.Type} is not a value and cannot be read as number");
                case JsonElementType.Null:
                case JsonElementType.True:
                case JsonElementType.False:
                    throw new FormatException($"JSON {Element.Type} cannot be parsed as number");
                case JsonElementType.Number:
                    return _lexemeReader.GetValueInt64(Element.Lexeme);
                case JsonElementType.String:
#if NET5_0_OR_GREATER
                    var strSpan = _lexemeReader.GetValueStringAsSpan(Element.Lexeme);
                    return JsonLexemeReader.ParseInt64(strSpan);
#else
                    var str = _lexemeReader.GetValueString(Element.Lexeme);
                    return JsonLexemeReader.ParseInt64(str);
#endif
                default:
                    throw new InvalidOperationException($"Unknown JSON element type: {Element.Type}");
            }
        }
        public long? GetValueInt64Nullable()
        {
            if (IsValueNull())
                return null;

            return GetValueInt64();
        }


        public double GetValueDouble()
        {
            if (IsBroken)
                throw new JsonParsingException("JSON structure is broken");
            if (IsEnd)
                throw new InvalidOperationException("JSON reader run to an end");

            switch (Element.Type)
            {
                case JsonElementType.None:
                case JsonElementType.StartObject:
                case JsonElementType.EndObject:
                case JsonElementType.StartArray:
                case JsonElementType.EndArray:
                case JsonElementType.PropertyName:
                    throw new InvalidOperationException($"JSON element {Element.Type} is not a value and cannot be read as number");
                case JsonElementType.Null:
                case JsonElementType.True:
                case JsonElementType.False:
                    throw new FormatException($"JSON {Element.Type} cannot be parsed as number");
                case JsonElementType.Number:
                    return _lexemeReader.GetValueDouble(Element.Lexeme);
                case JsonElementType.String:
#if NET5_0_OR_GREATER
                    var strSpan = _lexemeReader.GetValueStringAsSpan(Element.Lexeme);
                    return JsonLexemeReader.ParseDouble(strSpan);
#else
                    var str = _lexemeReader.GetValueString(Element.Lexeme);
                    return JsonLexemeReader.ParseDouble(str);
#endif
                default:
                    throw new InvalidOperationException($"Unknown JSON element type: {Element.Type}");
            }
        }
        public double? GetValueDoubleNullable()
        {
            if (IsValueNull())
                return null;

            return GetValueDouble();
        }


        public bool GetValueBool()
        {
            if (IsBroken)
                throw new JsonParsingException("JSON structure is broken");
            if (IsEnd)
                throw new InvalidOperationException("JSON reader run to an end");

            switch (Element.Type)
            {
                case JsonElementType.None:
                case JsonElementType.StartObject:
                case JsonElementType.EndObject:
                case JsonElementType.StartArray:
                case JsonElementType.EndArray:
                case JsonElementType.PropertyName:
                    throw new InvalidOperationException($"JSON element {Element.Type} is not a value and cannot be read as bool");
                case JsonElementType.Null:
                case JsonElementType.Number:
                    throw new FormatException($"JSON {Element.Type} cannot be parsed as bool");
                case JsonElementType.True:
                    return true;
                case JsonElementType.False:
                    return false;
                case JsonElementType.String:
#if NET5_0_OR_GREATER
                    var strSpan = _lexemeReader.GetValueStringAsSpan(Element.Lexeme);
                    return bool.Parse(strSpan);
#else
                    var str = _lexemeReader.GetValueString(Element.Lexeme);
                    return bool.Parse(str);
#endif
                default:
                    throw new InvalidOperationException($"Unknown JSON element type: {Element.Type}");
            }
        }
        public bool? GetValueBoolNullable()
        {
            if (IsValueNull())
                return null;

            return GetValueBool();
        }
    }
}
