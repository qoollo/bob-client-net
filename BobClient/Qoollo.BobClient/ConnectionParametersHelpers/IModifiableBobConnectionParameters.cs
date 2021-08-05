using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    internal interface IModifiableBobConnectionParameters
    {
        string Host { get; set; }
        int? Port { get; set; }

        string User { get; set; }
        string Password { get; set; }

        int? MaxReceiveMessageLength { get; set; }
        int? MaxSendMessageLength { get; set; }

        TimeSpan? OperationTimeout { get; set; }
        TimeSpan? ConnectionTimeout { get; set; }

        Dictionary<string, string> CustomParameters { get; }
    }
}
