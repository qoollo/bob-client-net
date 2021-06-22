using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Provides status information for node client
    /// </summary>
    public interface IBobNodeClientStatus
    {
        /// <summary>
        /// Address of the Node
        /// </summary>
        NodeAddress Address { get; }

        /// <summary>
        /// State of the client
        /// </summary>
        BobNodeClientState State { get; }

        /// <summary>
        /// Number of sequential errors
        /// </summary>
        int SequentialErrorCount { get; }

        /// <summary>
        /// Time elapsed since the last operation started (in milliseconds)
        /// </summary>
        int TimeSinceLastOperationMs { get; }
    }
}
