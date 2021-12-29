using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Enum with main bob operations
    /// </summary>
    public enum BobOperationKind
    {
        /// <summary>
        /// An operation that reads data from Bob
        /// </summary>
        Get = 0,
        /// <summary>
        /// An operation that stores data to Bob
        /// </summary>
        Put,
        /// <summary>
        /// An operation that checks that data is present in Bob
        /// </summary>
        Exist
    }
}
