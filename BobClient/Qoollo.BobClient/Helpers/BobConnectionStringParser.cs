using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.Helpers
{
    internal static class BobConnectionStringParser
    {
        private struct KeyValuePair
        {
            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }
            public string Key { get; }
            public string Value { get; }

            public override string ToString()
            {
                return $"[{Key}: {Value}]";
            }
        }



        private static void SkipSpaces(string connectionString, ref int position)
        {
            while (position < connectionString.Length && char.IsWhiteSpace(connectionString[position]))
                position++;
        }
        private static string ParseName(string connectionString, ref int position, char stopCharacter)
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
            while (position < connectionString.Length && connectionString[position] != (quote ?? stopCharacter))
                position++;


            throw new NotImplementedException();
        }
        private static KeyValuePair ParseKeyValue(string connectionString, ref int position)
        {
            SkipSpaces(connectionString, ref position);

            // Parse
            throw new NotImplementedException();
        }

        private static List<KeyValuePair> ParseConnectionStringIntoKeyValues(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            List<KeyValuePair> result = new List<KeyValuePair>();
            int position = 0;

            SkipSpaces(connectionString, ref position);
            while (position < connectionString.Length)
            {
                result.Add(ParseKeyValue(connectionString, ref position));
                SkipSpaces(connectionString, ref position);
            }

            return result;
        }
    }
}
