using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Qoollo.BobClient
{
    public class BobOperationException : Exception
    {
        public BobOperationException() { }
        public BobOperationException(string message) : base(message) { }
        public BobOperationException(string message, Exception innerException) : base(message, innerException) { }
    }


    public class BobKeyNotFoundException: BobOperationException
    {
        public BobKeyNotFoundException() { }
        public BobKeyNotFoundException(string message) : base(message) { }
        public BobKeyNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
