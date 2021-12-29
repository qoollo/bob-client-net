using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    /// <summary>
    /// Modifiable Bob connection parameters. Used internally for connection string parsing and some other common operations
    /// </summary>
    internal interface IModifiableBobConnectionParameters
    {
        /// <summary>
        /// Host
        /// </summary>
        string Host { get; set; }
        /// <summary>
        /// Port
        /// </summary>
        int? Port { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        string User { get; set; }
        /// <summary>
        /// Password for specified user
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Max receive message size
        /// </summary>
        int? MaxReceiveMessageSize { get; set; }
        /// <summary>
        /// Max send message size
        /// </summary>
        int? MaxSendMessageSize { get; set; }

        /// <summary>
        /// Operation timeout
        /// </summary>
        TimeSpan? OperationTimeout { get; set; }
        /// <summary>
        /// Connection timeout
        /// </summary>
        TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// Key-value storage for all unknown parameters (can be used for extensions)
        /// </summary>
        Dictionary<string, string> CustomParameters { get; }
    }
}
