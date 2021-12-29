using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    /// <summary>
    /// Controls validation behaviour for BobConnectionParameters
    /// </summary>
    internal enum ValidationExceptionBehaviour
    {
        /// <summary>
        /// Do not throw exception
        /// </summary>
        NoException,
        /// <summary>
        /// Throws <see cref="FormatException"/> if parameters are invalid
        /// </summary>
        FormatException,
        /// <summary>
        /// Throws <see cref="InvalidBobConnectionParametersException"/> if parameters are invalid
        /// </summary>
        InvalidConnectionParametersException
    }
}
