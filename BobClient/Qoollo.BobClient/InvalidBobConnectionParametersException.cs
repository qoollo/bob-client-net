using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Indicates that <see cref="BobConnectionParameters"/> has invalid value
    /// </summary>
    public class InvalidBobConnectionParametersException: Exception
    {
        /// <summary>
        /// <see cref="InvalidBobConnectionParametersException"/> constructor
        /// </summary>
        public InvalidBobConnectionParametersException() : base("Bob connection parameters are invalid") { }
        /// <summary>
        /// <see cref="InvalidBobConnectionParametersException"/> constructor
        /// </summary>
        /// <param name="message">Message</param>
        public InvalidBobConnectionParametersException(string message) : base(message) { }
        /// <summary>
        /// <see cref="InvalidBobConnectionParametersException"/> constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        public InvalidBobConnectionParametersException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// Initializes a new instance of <see cref="InvalidBobConnectionParametersException"/> class with serialized data
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected InvalidBobConnectionParametersException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
