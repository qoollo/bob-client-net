using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.Helpers.Json
{
    internal enum JsonLexemeType
    {
        None = 0,
        StartObject,
        EndObject,
        StartArray,
        EndArray,
        KeyValueSeparator,
        ItemSeparator,
        Identifier,
        Null,
        True,
        False,
        Number,
        String
    }

    internal static class JsonLexemeTypeExtensions
    {
        public static bool IsSimpleValueType(this JsonLexemeType lexemeType)
        {
            return lexemeType == JsonLexemeType.Null ||
                   lexemeType == JsonLexemeType.True ||
                   lexemeType == JsonLexemeType.False ||
                   lexemeType == JsonLexemeType.Number ||
                   lexemeType == JsonLexemeType.String;
        }

        public static bool IsValueType(this JsonLexemeType lexemeType)
        {
            return lexemeType == JsonLexemeType.StartObject ||
                   lexemeType == JsonLexemeType.StartArray ||
                   IsSimpleValueType(lexemeType);
        }
    }


    internal readonly struct JsonLexemeInfo
    {
        public static JsonLexemeInfo None { get { return new JsonLexemeInfo(JsonLexemeType.None, 0, 0); } }
        public static JsonLexemeInfo EndNone(int position) { return new JsonLexemeInfo(JsonLexemeType.None, position, position); }

        public JsonLexemeInfo(JsonLexemeType type, int start, int end)
        {
            Type = type;
            Start = start;
            End = end;
        }

        public readonly JsonLexemeType Type;
        public readonly int Start;
        public readonly int End;
        public int Length { get { return End - Start; } }


        public string RawLexemeString(string str)
        {
            if (End <= Start)
                return "";
            return str.Substring(Start, Length);
        }
        public bool IsEqualToRefString(string str, string referenceStr)
        {
            if (Length != referenceStr.Length)
                return false;

            int index = Start;
            int referenceIndex = 0;
            while (referenceIndex < referenceStr.Length)
            {
                if (index >= str.Length)
                    return false;

                if (str[index] != referenceStr[referenceIndex])
                    return false;

                index++;
                referenceIndex++;
            }

            return true;
        }

        public override string ToString()
        {
            return $"[{Start}-{End}]:{Type}";
        }
    }


    [System.Diagnostics.DebuggerDisplay("{CurrentLexeme}")]
    internal class JsonLexemeReader
    {
        private static bool IsHexDigit(char ch)
        {
            return (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
        }
        private static int ParseHexDigit(char ch)
        {
            if (ch >= '0' && ch <= '9')
                return ch - '0';
            else if (ch >= 'A' && ch <= 'F')
                return 10 + (ch - 'A');
            else if (ch >= 'a' && ch <= 'f')
                return 10 + (ch - 'a');
            else
                throw new ArgumentException($"Char is not a HexDigit: {ch}");
        }
        private static bool IsIdentifierStartSymbol(char ch)
        {
            return char.IsLetter(ch) || ch == '_';
        }
        private static bool IsIdentifierSymbol(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        private static string ExtractSurroundingText(string str, int index)
        {
            const int MaxLength = 16;

            if (!SkipSpaces(str, ref index))
                index = Math.Max(0, index - MaxLength);

            int length = Math.Min(str.Length - index, MaxLength);
            if (index + length < str.Length)
                return str.Substring(index, length) + "...";

            return str.Substring(index, length);
        }

        // ===============

        private readonly string _source;
        private JsonLexemeInfo _currentLexeme;

        public JsonLexemeReader(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _currentLexeme = JsonLexemeInfo.None;
        }


        public string Source { get { return _source; } }

        public JsonLexemeInfo CurrentLexeme { get { return _currentLexeme; } }
        public int Index { get { return _currentLexeme.Start; } }
        public bool IsStart { get { return Index == 0; } }
        public bool IsEnd { get { return Index >= _source.Length; } }



        private static bool SkipSpaces(string str, ref int index)
        {
            while (index < str.Length && char.IsWhiteSpace(str[index]))
                index++;

            return index < str.Length;
        }


        internal static JsonLexemeInfo ReadString(string str, int index)
        {
            if (index >= str.Length)
                throw new JsonParsingException($"Unexpected end of JSON. Expected string instead: '{ExtractSurroundingText(str, index)}'");
            if (str[index] != '"')
                throw new JsonParsingException($"Expected string at position {index}, but found: '{ExtractSurroundingText(str, index)}'");

            int startIndex = index;

            index++;
            while (index < str.Length)
            {
                if (str[index] == '\\')
                {
                    if (index + 1 >= str.Length)
                        throw new JsonParsingException($"Unexpected end of Json. Cannot finish string parsing started at {startIndex}: '{ExtractSurroundingText(str, startIndex)}'");

                    char escapedChar = str[index + 1];
                    if (escapedChar != '\\' && escapedChar != '/' && escapedChar != '"' && escapedChar != 'b' && escapedChar != 'f' && escapedChar != 'n' && escapedChar != 'r' && escapedChar != 't' && escapedChar != 'u')
                        throw new JsonParsingException($"Unrecognized escape sequence at {index}: '{ExtractSurroundingText(str, index)}'");

                    if (escapedChar == 'u')
                    {
                        if (index + 5 >= str.Length || !IsHexDigit(str[index + 2]) || !IsHexDigit(str[index + 3]) || !IsHexDigit(str[index + 4]) || !IsHexDigit(str[index + 5]))
                            throw new JsonParsingException($"Unrecognized escape sequence at {index}: '{ExtractSurroundingText(str, index)}'");

                        index += 5;
                    }
                    else
                    {
                        index++;
                    }
                }
                else if (str[index] == '"')
                {
                    return new JsonLexemeInfo(JsonLexemeType.String, startIndex, index + 1);
                }

                index++;
            }

            throw new JsonParsingException($"Unexpected end of Json. Cannot finish string parsing started at {startIndex}: '{ExtractSurroundingText(str, startIndex)}'");
        }

        internal static string ParseString(string str, int startIndex, int expectedEndIndex)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (startIndex < 0 || startIndex >= str.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (expectedEndIndex <= startIndex || expectedEndIndex > str.Length)
                throw new ArgumentOutOfRangeException(nameof(expectedEndIndex));

            if (str[startIndex] != '"')
                throw new FormatException($"String is not started with quotation mark. It's started with: '{ExtractSurroundingText(str, startIndex)}'");

            int index = startIndex + 1;

            StringBuilder parsedString = null;
            int continuousSequenceStart = index;

            while (index < str.Length && index < expectedEndIndex)
            {
                if (str[index] == '\\')
                {
                    if (index + 1 >= str.Length)
                        throw new FormatException($"Unexpected end of string. Cannot finish string parsing: '{ExtractSurroundingText(str, index)}'");

                    char escapedChar = str[index + 1];
                    if (escapedChar != '\\' && escapedChar != '/' && escapedChar != '"' && escapedChar != 'b' && escapedChar != 'f' && escapedChar != 'n' && escapedChar != 'r' && escapedChar != 't' && escapedChar != 'u')
                        throw new FormatException($"Unrecognized escape sequence at {index}: '{ExtractSurroundingText(str, index)}'");

                    if (parsedString == null)
                        parsedString = new StringBuilder(str.Length);

                    parsedString.Append(str, continuousSequenceStart, index - continuousSequenceStart);

                    if (escapedChar == 'u')
                    {
                        if (index + 5 >= str.Length || !IsHexDigit(str[index + 2]) || !IsHexDigit(str[index + 3]) || !IsHexDigit(str[index + 4]) || !IsHexDigit(str[index + 5]))
                            throw new FormatException($"Unrecognized escape sequence at {index}: '{ExtractSurroundingText(str, index)}'");

                        int charValue = (ParseHexDigit(str[index + 2]) << 12) | (ParseHexDigit(str[index + 3]) << 8) | (ParseHexDigit(str[index + 4]) << 4) | ParseHexDigit(str[index + 5]);
                        parsedString.Append((char)charValue);

                        index += 5;
                    }
                    else
                    {
                        switch (escapedChar)
                        {
                            case 'b':
                                parsedString.Append('\b');
                                break;
                            case 'f':
                                parsedString.Append('\f');
                                break;
                            case 'n':
                                parsedString.Append('\n');
                                break;
                            case 'r':
                                parsedString.Append('\r');
                                break;
                            case 't':
                                parsedString.Append('\t');
                                break;
                            default:
                                parsedString.Append(escapedChar);
                                break;
                        }

                        index++;
                    }

                    continuousSequenceStart = index + 1;
                }
                else if (str[index] == '"')
                {
                    if (index + 1 != expectedEndIndex)
                        throw new FormatException($"String ended earlier than expected at position {index}: '{ExtractSurroundingText(str, index)}'");

                    if (parsedString == null)
                        return str.Substring(startIndex + 1, index - startIndex - 1);

                    if (index - continuousSequenceStart - 1 > 0)
                        parsedString.Append(str, continuousSequenceStart, index - continuousSequenceStart);

                    return parsedString.ToString();
                }

                index++;
            }

            throw new FormatException($"String does not have ending quotation mark before position {index}: '{ExtractSurroundingText(str, index)}'");
        }


        internal static JsonLexemeInfo ReadIdentifier(string str, int index)
        {
            if (index >= str.Length)
                throw new JsonParsingException($"Unexpected end of JSON. Expected identifier instead: '{ExtractSurroundingText(str, index)}'");

            if (!IsIdentifierStartSymbol(str[index]))
                throw new JsonParsingException($"Expected identifier at position {index}, but found: '{ExtractSurroundingText(str, index)}'");

            int startIndex = index;
            while (index < str.Length && IsIdentifierSymbol(str[index]))
                index++;

            return new JsonLexemeInfo(JsonLexemeType.Identifier, startIndex, index);
        }

        internal static JsonLexemeInfo ReadNumber(string str, int index)
        {
            if (index >= str.Length)
                throw new JsonParsingException($"Unexpected end of JSON. Expected number instead: '{ExtractSurroundingText(str, index)}'");
            if (str[index] != '-' && str[index] != '+' && !char.IsDigit(str[index]))
                throw new JsonParsingException($"Expected number at position {index}, but found: '{ExtractSurroundingText(str, index)}'");

            int startIndex = index;

            // +/-
            if (str[index] == '+' || str[index] == '-')
                index++;

            if (index >= str.Length || !char.IsDigit(str[index]))
                throw new JsonParsingException($"Expected number at position {startIndex}, but found: '{ExtractSurroundingText(str, startIndex)}'");

            // digits
            while (index < str.Length && char.IsDigit(str[index]))
                index++;

            // decimal point
            if (index < str.Length && str[index] == '.')
            {
                index++;
                // digits after decimal point
                while (index < str.Length && char.IsDigit(str[index]))
                    index++;
            }

            // exponent
            if (index < str.Length && (str[index] == 'E' || str[index] == 'e'))
            {
                index++;
                if (index >= str.Length)
                    throw new JsonParsingException($"Incorrectly formatted number at position {startIndex}: '{ExtractSurroundingText(str, startIndex)}'");

                // +/-
                if (str[index] == '+' || str[index] == '-')
                    index++;

                if (index >= str.Length || !char.IsDigit(str[index]))
                    throw new JsonParsingException($"Incorrectly formatted number at position {startIndex}: '{ExtractSurroundingText(str, startIndex)}'");

                // exponent digits
                while (index < str.Length && char.IsDigit(str[index]))
                    index++;
            }

            return new JsonLexemeInfo(JsonLexemeType.Number, startIndex, index);
        }


        internal static JsonLexemeInfo ReadLexeme(string str, int index)
        {
            if (!SkipSpaces(str, ref index))
                return JsonLexemeInfo.EndNone(index);

            switch (str[index])
            {
                case '{':
                    return new JsonLexemeInfo(JsonLexemeType.StartObject, index, index + 1);
                case '}':
                    return new JsonLexemeInfo(JsonLexemeType.EndObject, index, index + 1);
                case '[':
                    return new JsonLexemeInfo(JsonLexemeType.StartArray, index, index + 1);
                case ']':
                    return new JsonLexemeInfo(JsonLexemeType.EndArray, index, index + 1);
                case ':':
                    return new JsonLexemeInfo(JsonLexemeType.KeyValueSeparator, index, index + 1);
                case ',':
                    return new JsonLexemeInfo(JsonLexemeType.ItemSeparator, index, index + 1);
                case '"':
                    return ReadString(str, index);
                case '+':
                case '-':
                case char digitCh when digitCh >= '0' && digitCh <= '9':
                    return ReadNumber(str, index);
                case char identifierCh when IsIdentifierStartSymbol(identifierCh):
                    var identifier = ReadIdentifier(str, index);
                    if (identifier.IsEqualToRefString(str, "true"))
                        return new JsonLexemeInfo(JsonLexemeType.True, identifier.Start, identifier.End);
                    if (identifier.IsEqualToRefString(str, "false"))
                        return new JsonLexemeInfo(JsonLexemeType.False, identifier.Start, identifier.End);
                    if (identifier.IsEqualToRefString(str, "null"))
                        return new JsonLexemeInfo(JsonLexemeType.Null, identifier.Start, identifier.End);
                    return identifier;
                default:
                    throw new JsonParsingException($"Unexpected JSON structure at {index}: '{ExtractSurroundingText(str, index)}'");
            }
        }


        public bool Read()
        {
            if (_currentLexeme.Start >= _source.Length)
                return false;

            _currentLexeme = ReadLexeme(_source, _currentLexeme.End);
            return _currentLexeme.Start < _source.Length && _currentLexeme.Type != JsonLexemeType.None;
        }

        public string ExtractLexemeSurroundingText(JsonLexemeInfo lexeme)
        {
            return ExtractSurroundingText(_source, lexeme.Start);
        }
        public string ExtractLexemeSurroundingText()
        {
            return ExtractLexemeSurroundingText(CurrentLexeme);
        }
    }
}
