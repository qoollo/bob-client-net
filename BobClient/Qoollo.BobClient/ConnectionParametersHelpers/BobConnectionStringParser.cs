using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    internal static class BobConnectionStringParser
    {
        private static readonly HashSet<char> _keyStopCharacters = new HashSet<char>() { '=', ';' };
        private static readonly HashSet<char> _valueStopCharacters = new HashSet<char>() { ';' };

        internal struct KeyValuePair : IEquatable<KeyValuePair>
        {
            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }
            public string Key { get; }
            public string Value { get; }

            public bool Equals(KeyValuePair other)
            {
                return Key == other.Key && Value == other.Value;
            }
            public override bool Equals(object obj)
            {
                if (obj is KeyValuePair other)
                    return Equals(other);
                return false;
            }
            public override int GetHashCode()
            {
                return (Key?.GetHashCode() ?? 0) ^ (Value?.GetHashCode() ?? 0);
            }

            public override string ToString()
            {
                return $"[{Key}: {Value}]";
            }
        }

        private static string SubstringAfter(string str, int position, int maxCharacters = 8)
        {
            if (maxCharacters < str.Length - position)
                return str.Substring(position, maxCharacters) + "...";
            else
                return str.Substring(position, str.Length - position);
        }

        private static void SkipSpaces(string connectionString, ref int position)
        {
            while (position < connectionString.Length && char.IsWhiteSpace(connectionString[position]))
                position++;
        }
        private static string ReadToken(string connectionString, ref int position, HashSet<char> stopCharacters)
        {
            SkipSpaces(connectionString, ref position);
            if (position >= connectionString.Length)
                return "";

            char? quote = null;
            if (connectionString[position] == '"' || connectionString[position] == '\'')
            {
                quote = connectionString[position];
                position++;
            }

            int startPosition = position;
            if (quote != null)
            {
                while (position < connectionString.Length && connectionString[position] != quote.GetValueOrDefault())
                    position++;
            }
            else
            {
                while (position < connectionString.Length && !stopCharacters.Contains(connectionString[position]))
                    position++;
            }

            if (position == connectionString.Length)   // String end
            {
                if (quote != null)
                    throw new FormatException($"Bob connection string contains a token that starts with an opening quote, but does not end with a closing quote. Position: {startPosition - 1}, Token: '{connectionString.Substring(startPosition - 1)}'");

                return connectionString.Substring(startPosition).TrimEnd();
            }
            else if (quote != null) // Stopped on closing quote
            {
                int endPosition = position;
                position++;
                SkipSpaces(connectionString, ref position);

                if (position < connectionString.Length && !stopCharacters.Contains(connectionString[position]))
                    throw new FormatException($"Bob connection string token has characters after the closing quotation mark, which is prohibited. Position: {position}, Token: '{connectionString.Substring(startPosition - 1, position - startPosition + 2)}'");

                return connectionString.Substring(startPosition, endPosition - startPosition);
            }
            else // Stopped on stopCharacter
            {
                return connectionString.Substring(startPosition, position - startPosition).TrimEnd();
            }
        }
        private static KeyValuePair ParseKeyValue(string connectionString, ref int position)
        {
            SkipSpaces(connectionString, ref position);

            int keyPosition = position;
            string key = ReadToken(connectionString, ref position, stopCharacters: _keyStopCharacters);
            if (string.IsNullOrWhiteSpace(key))
                throw new FormatException($"Empty or invalid key in Bob connection string. Position: {keyPosition}, Token: '{SubstringAfter(connectionString, keyPosition)}'");

            if (position >= connectionString.Length)
                throw new FormatException($"Key in Bob connection string has no associated value. Position: {keyPosition}, Key: {key}");

            if (connectionString[position] != '=')
                throw new FormatException($"Key in Bob connection string has ended with unexpected character '{connectionString[position]}', expected '='. Position: {keyPosition}, Key: '{key}'");


            position++;
            SkipSpaces(connectionString, ref position);

            int valuePosition = position;
            string value = ReadToken(connectionString, ref position, stopCharacters: _valueStopCharacters);

            if (position < connectionString.Length && connectionString[position] != ';')
                throw new FormatException($"Value in Bob connection string has ended with unexpected character '{connectionString[position]}', expected ';'. Position: {valuePosition}, Key: '{key}', Value: '{value}'");

            if (position < connectionString.Length && connectionString[position] == ';')
                position++;

            return new KeyValuePair(key, value);
        }

        internal static List<KeyValuePair> ParseConnectionStringIntoKeyValues(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            List<KeyValuePair> result = new List<KeyValuePair>();
            int position = 0;

            SkipSpaces(connectionString, ref position);
            while (position < connectionString.Length)
            {
                if (connectionString[position] == ';')
                    position++; // Skip empty values in sequence ';;;;'
                else
                    result.Add(ParseKeyValue(connectionString, ref position));

                SkipSpaces(connectionString, ref position);
            }

            return result;
        }
    }
}
