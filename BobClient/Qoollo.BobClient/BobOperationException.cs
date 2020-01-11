using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Common exception for operations in Bob
    /// </summary>
    public class BobOperationException : Exception
    {
        /// <summary>
        /// <see cref="BobOperationException"/> constructor
        /// </summary>
        public BobOperationException() { }
        /// <summary>
        /// <see cref="BobOperationException"/> constructor
        /// </summary>
        /// <param name="message">Message</param>
        public BobOperationException(string message) : base(message) { }
        /// <summary>
        /// <see cref="BobOperationException"/> constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        public BobOperationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception for the case when specified key was not found in Bob cluster
    /// </summary>
    public class BobKeyNotFoundException: BobOperationException
    {
        /// <summary>
        /// <see cref="BobKeyNotFoundException"/> constructor
        /// </summary>
        public BobKeyNotFoundException() { }
        /// <summary>
        /// <see cref="BobKeyNotFoundException"/> constructor
        /// </summary>
        /// <param name="message">Message</param>
        public BobKeyNotFoundException(string message) : base(message) { }
        /// <summary>
        /// <see cref="BobKeyNotFoundException"/> constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        public BobKeyNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
