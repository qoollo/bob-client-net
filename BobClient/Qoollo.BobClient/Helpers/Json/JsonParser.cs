using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Qoollo.BobClient.Helpers.Json
{
    internal delegate T JsonValueParserDelegate<T>(IJsonValueParser valueParser);
    internal delegate T JsonObjectPropertyParserDelegate<T>(T obj, IJsonValueParser valueParser);
    internal delegate void JsonObjectPropertyParserInPlaceDelegate<T>(T obj, IJsonValueParser valueParser);

    internal class JsonObjectInfo<T>
    { 
        public class JsonPropertyInfo
        {
            private readonly JsonObjectPropertyParserDelegate<T> _propertyParser;

            public JsonPropertyInfo(string name, bool isRequired, JsonObjectPropertyParserDelegate<T> propertyParser)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                IsRequired = isRequired;
                _propertyParser = propertyParser ?? throw new ArgumentNullException(nameof(propertyParser));
            }

            public string Name { get; }
            public bool IsRequired { get; }

            public T ParseProperty(T obj, IJsonValueParser valueParser)
            {
                return _propertyParser(obj, valueParser);
            }
        }

        // ================

        private readonly Dictionary<string, JsonPropertyInfo> _properties;

        public JsonObjectInfo()
        {
            _properties = new Dictionary<string, JsonPropertyInfo>();
        }

        public IReadOnlyDictionary<string, JsonPropertyInfo> Properties { get { return _properties; } }

        public bool TryGetProperty(string name, out JsonPropertyInfo property)
        {
            return _properties.TryGetValue(name, out property);
        }

        public JsonObjectInfo<T> AddProperty(string jsonPropertyName, bool isRequired, JsonObjectPropertyParserDelegate<T> propertyParser)
        {
            if (jsonPropertyName == null)
                throw new ArgumentNullException(nameof(jsonPropertyName));
            if (propertyParser == null)
                throw new ArgumentNullException(nameof(propertyParser));

            _properties.Add(jsonPropertyName, new JsonPropertyInfo(jsonPropertyName, isRequired, propertyParser));
            return this;
        }
        public JsonObjectInfo<T> AddProperty(string jsonPropertyName, bool isRequired, JsonObjectPropertyParserInPlaceDelegate<T> propertyParser)
        {
            if (jsonPropertyName == null)
                throw new ArgumentNullException(nameof(jsonPropertyName));
            if (propertyParser == null)
                throw new ArgumentNullException(nameof(propertyParser));

            _properties.Add(jsonPropertyName, new JsonPropertyInfo(jsonPropertyName, isRequired, (obj, valueParser) => { propertyParser(obj, valueParser); return obj; }));
            return this;
        }
        public JsonObjectInfo<T> AddProperty<TProp>(string jsonPropertyName, bool isRequired, Expression<Func<T, TProp>> objectPropertyExpression, JsonValueParserDelegate<TProp> valueParser)
        {
            if (jsonPropertyName == null)
                throw new ArgumentNullException(nameof(jsonPropertyName));
            if (objectPropertyExpression == null)
                throw new ArgumentNullException(nameof(objectPropertyExpression));
            if (valueParser == null)
                throw new ArgumentNullException(nameof(valueParser));


            var bodyExpr = objectPropertyExpression.Body;
            if (!(bodyExpr is MemberExpression memberExpr))
                throw new ArgumentException("Expression should contain a Property accessor on supplied argument", nameof(objectPropertyExpression));
            
            if (!(memberExpr.Expression is ParameterExpression))
                throw new ArgumentException("Expression should contain a Property accessor on supplied argument", nameof(objectPropertyExpression));

            if (!(memberExpr.Member is System.Reflection.PropertyInfo propertyInfo))
                throw new ArgumentException("Expression should contain a Property accessor on supplied argument", nameof(objectPropertyExpression));

            var propertySetter = propertyInfo.GetSetMethod(nonPublic: true);
            if (propertySetter == null)
                throw new ArgumentException($"Object of type '{typeof(T)}' does not contains setter for property '{propertyInfo.Name}'");

            var setterDelegate = (Action<T, TProp>)propertySetter.CreateDelegate(typeof(Action<T, TProp>));

            JsonObjectPropertyParserDelegate<T> propertyParser = (obj, objParser) => { setterDelegate(obj, valueParser(objParser)); return obj; };
            _properties.Add(jsonPropertyName, new JsonPropertyInfo(jsonPropertyName, isRequired, propertyParser));
            return this;
        }
    }


    internal interface IJsonValueParser
    {
        bool IsNull { get; }
        bool IsArray { get; }
        bool IsObject { get; }
        bool IsSimpleValue { get; }

        T ParseObject<T>(T obj, JsonObjectInfo<T> objInfo);
        IEnumerable<T> ParseArray<T>(JsonValueParserDelegate<T> itemParser);


        string ParseString();

        int ParseInt32();
        int? ParseInt32Nullable();

        long ParseInt64();
        long? ParseInt64Nullable();

        double ParseDouble();
        double? ParseDoubleNullable();

        bool ParseBool();
        bool? ParseBoolNullable();

        T ParseSimpleValue<T>();
    }

    internal class JsonParser : IJsonValueParser
    {
        private readonly JsonReader _reader;

        public JsonParser(string source)
        {
            _reader = new JsonReader(source);

            _reader.Read();
        }

        public bool IsNull { get { return _reader.IsValueNull(); } }
        public bool IsArray { get { return _reader.ElementType == JsonElementType.StartArray; } }
        public bool IsObject { get { return _reader.ElementType == JsonElementType.StartObject; } }
        public bool IsSimpleValue { get { return _reader.ElementType.IsSimpleValueType(); } }

        public bool IsComplete { get { return _reader.IsEnd; } }


        public T ParseObject<T>(T obj, JsonObjectInfo<T> objInfo)
        {
            if (objInfo == null)
                throw new ArgumentNullException(nameof(objInfo));
            if (!IsObject)
                throw new InvalidOperationException($"ParseObject can be executed only on JSON Object element. Current element: {_reader.ElementType}");

            HashSet<string> parsedProps = new HashSet<string>();

            while (_reader.Read() && _reader.ElementType != JsonElementType.EndObject)
            {
                if (_reader.ElementType != JsonElementType.PropertyName)
                    throw new InvalidOperationException($"PropertyName expected inside object, but found {_reader.ElementType}");

                if (objInfo.TryGetProperty(_reader.PropertyName, out var property))
                {
                    _reader.Read();

                    parsedProps.Add(_reader.PropertyName);
                    obj = property.ParseProperty(obj, this);
                }
                else
                {
                    _reader.Skip();
                }
            }

            _reader.Read();

            foreach (var property in objInfo.Properties)
            {
                if (property.Value.IsRequired && !parsedProps.Contains(property.Value.Name))
                    throw new JsonParsingException($"Required property '{property.Value.Name}' was not found in supplied JSON");
            }

            return obj;
        }

        public IEnumerable<T> ParseArray<T>(JsonValueParserDelegate<T> itemParser)
        {
            if (itemParser == null)
                throw new ArgumentNullException(nameof(itemParser));
            if (!IsArray)
                throw new InvalidOperationException($"ParseArray can be executed only on JSON Array element. Current element: {_reader.ElementType}");

            _reader.Read();

            while (_reader.ElementType != JsonElementType.EndArray)
            {
                int position = _reader.Position;

                yield return itemParser(this);

                if (_reader.Position == position)
                    throw new ArgumentException("Item parser should parse a value to move forward", nameof(itemParser));
            }

            _reader.Read();
        }


        public string ParseString()
        {
            var result = _reader.GetValueString();
            _reader.Read();
            return result;
        }

        public int ParseInt32()
        {
            var result = _reader.GetValueInt32();
            _reader.Read();
            return result;
        }
        public int? ParseInt32Nullable()
        {
            var result = _reader.GetValueInt32Nullable();
            _reader.Read();
            return result;
        }

        public long ParseInt64()
        {
            var result = _reader.GetValueInt64();
            _reader.Read();
            return result;
        }
        public long? ParseInt64Nullable()
        {
            var result = _reader.GetValueInt64Nullable();
            _reader.Read();
            return result;
        }

        public double ParseDouble()
        {
            var result = _reader.GetValueDouble();
            _reader.Read();
            return result;
        }
        public double? ParseDoubleNullable()
        {
            var result = _reader.GetValueDoubleNullable();
            _reader.Read();
            return result;
        }

        public bool ParseBool()
        {
            var result = _reader.GetValueBool();
            _reader.Read();
            return result;
        }
        public bool? ParseBoolNullable()
        {
            var result = _reader.GetValueBoolNullable();
            _reader.Read();
            return result;
        }

        public T ParseSimpleValue<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)ParseString();
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)ParseInt32();
            }
            else if (typeof(T) == typeof(int?))
            {
                return (T)(object)ParseInt32Nullable();
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)ParseInt64();
            }
            else if (typeof(T) == typeof(long?))
            {
                return (T)(object)ParseInt64Nullable();
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)ParseDouble();
            }
            else if (typeof(T) == typeof(double?))
            {
                return (T)(object)ParseDoubleNullable();
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)ParseBool();
            }
            else if (typeof(T) == typeof(bool?))
            {
                return (T)(object)ParseBoolNullable();
            }
            else
            {
                throw new InvalidOperationException($"Not supported property type: {typeof(T)}");
            }
        }
    }
}
