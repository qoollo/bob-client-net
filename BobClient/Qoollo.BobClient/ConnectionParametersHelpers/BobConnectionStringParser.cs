﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    /// <summary>
    /// Connection string parser
    /// </summary>
    internal static class BobConnectionStringParser
    {
        private static readonly HashSet<char> _keyStopCharacters = new HashSet<char>() { '=', ';' };
        private static readonly HashSet<char> _valueStopCharacters = new HashSet<char>() { ';' };

        private static readonly char[] _connectionStringMarkers = new char[] { '=', ';' };

        /// <summary>
        /// Key value pair
        /// </summary>
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

        /// <summary>
        /// Extract substring after specified position, which is limited to specified number of characters
        /// </summary>
        /// <param name="str">Source string</param>
        /// <param name="position">Start position</param>
        /// <param name="maxCharacters">Max number of characters in result substring</param>
        /// <returns>Substring</returns>
        private static string SubstringAfter(string str, int position, int maxCharacters = 8)
        {
            if (maxCharacters < str.Length - position)
                return str.Substring(position, maxCharacters) + "...";
            else
                return str.Substring(position, str.Length - position);
        }

        /// <summary>
        /// Skips whitespaces by moving <paramref name="position"/> forward
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="position">Position</param>
        private static void SkipSpaces(string connectionString, ref int position)
        {
            while (position < connectionString.Length && char.IsWhiteSpace(connectionString[position]))
                position++;
        }
        /// <summary>
        /// Read single token from connection string
        /// </summary>
        /// <param name="connectionString">Source connection string</param>
        /// <param name="position">Current position inside connection string</param>
        /// <param name="stopCharacters">Stop characters for token</param>
        /// <returns>Token</returns>
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
            bool hasDoubleQuote = false;
            if (quote != null)
            {
                while (true)
                {
                    while (position < connectionString.Length && connectionString[position] != quote.GetValueOrDefault())
                        position++;

                    // Process quotes pair
                    if (position + 1 < connectionString.Length && connectionString[position + 1] == quote.GetValueOrDefault())
                    {
                        hasDoubleQuote = true;
                        position += 2;
                    }
                    else
                    {
                        break;
                    }
                }
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

                string valueSubstring = connectionString.Substring(startPosition, endPosition - startPosition);

                if (hasDoubleQuote)
                    valueSubstring = valueSubstring.Replace(new string(quote.Value, 2), new string(quote.Value, 1));

                return valueSubstring;
            }
            else // Stopped on stopCharacter
            {
                return connectionString.Substring(startPosition, position - startPosition).TrimEnd();
            }
        }
        /// <summary>
        /// Parses single key value pair from connection string
        /// </summary>
        /// <param name="connectionString">Source connection string</param>
        /// <param name="position">Current position inside connection string</param>
        /// <returns>Parsed key value pair</returns>
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

        /// <summary>
        /// Parses connection string into list of <see cref="KeyValuePair"/>
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>Parsed values</returns>
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

        /// <summary>
        /// Parses connection string or node address into <paramref name="parameters"/>
        /// </summary>
        /// <param name="connectionString">Connection string or node address</param>
        /// <param name="parameters">Target parameters</param>
        /// <exception cref="ArgumentNullException">Null arguments</exception>
        /// <exception cref="FormatException">Incorrect format</exception>
        public static void ParseConnectionStringInto(string connectionString, IModifiableBobConnectionParameters parameters)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new FormatException("Connection string cannot be empty. At least 'address' should be specified");

            if (connectionString.IndexOfAny(_connectionStringMarkers) < 0)
            {
                BobNodeAddress.TryParseCore(connectionString, true, out string host, out int? port);
                parameters.Host = host;
                if (port != null)
                    parameters.Port = port;
            }
            else
            {
                var keyValues = ParseConnectionStringIntoKeyValues(connectionString);

                foreach (var keyValue in keyValues)
                    parameters.SetValue(keyValue.Key, keyValue.Value, allowCustomParameters: true);
            }

            parameters.Validate(ValidationExceptionBehaviour.FormatException);
        }
    }
}
