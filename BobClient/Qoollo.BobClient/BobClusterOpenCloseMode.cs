using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Open or close mode for <see cref="BobClusterClient"/>. Controls error handling
    /// </summary>
    public enum BobClusterOpenCloseMode
    {
        /// <summary>
        /// Throw an error on the first failed operation on <see cref="BobNodeClient"/> (this is the default mode)
        /// </summary>
        ThrowOnFirstError = 0,
        /// <summary>
        /// Skip failed operations on <see cref="BobNodeClient"/>
        /// </summary>
        SkipErrors = 1
    }
}
