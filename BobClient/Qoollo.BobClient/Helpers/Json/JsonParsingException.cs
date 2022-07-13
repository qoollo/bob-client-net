using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Qoollo.BobClient.Helpers.Json
{
    internal class JsonParsingException : Exception
    {
        public JsonParsingException() { }
        public JsonParsingException(string message) : base(message) { }
        public JsonParsingException(string message, Exception innerException) : base(message, innerException) { }
        protected JsonParsingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
