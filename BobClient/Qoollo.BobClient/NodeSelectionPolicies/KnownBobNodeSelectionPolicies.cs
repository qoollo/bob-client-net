using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Known node selection policies enum
    /// </summary>
    internal enum KnownBobNodeSelectionPolicies
    {
        /// <summary>
        /// <see cref="FirstNodeSelectionPolicy"/>
        /// </summary>
        First,
        /// <summary>
        /// <see cref="SequentialNodeSelectionPolicy"/>
        /// </summary>
        Sequential,
        /// <summary>
        /// <see cref="FirstWorkingNodeSelectionPolicy"/>
        /// </summary>
        FirstWorking,
        /// <summary>
        /// <see cref="SequentialWorkingNodeSelectionPolicy"/>
        /// </summary>
        SequentialWorking
    }
}
