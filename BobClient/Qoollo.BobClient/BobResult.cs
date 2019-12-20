using BobStorage;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob operations result codes
    /// </summary>
    public enum BobCode
    {
        /// <summary>
        /// Operation ends with error
        /// </summary>
        Error = -1,
        /// <summary>
        /// Operation ends normally
        /// </summary>
        Ok = 0,
        /// <summary>
        /// Target key not found
        /// </summary>
        KeyNotFound = 1,
    }

    /// <summary>
    /// Bob operations result
    /// </summary>
    public class BobResult
    {
        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// Operation result code
        /// </summary>
        public BobCode Code { get; }

        internal BobResult(string message, BobCode code)
        {
            Message = message;
            Code = code;
        }

        internal static BobResult FromOp(OpStatus status)
        {
            return status.Error is null ? BobResult.Ok() : new BobResult(status.Error.Desc, BobCode.Error);
        }
        internal static BobResult Error(string message)
        {
            return new BobResult(message, BobCode.Error);
        }
        internal static BobResult KeyNotFound()
        {
            return new BobResult(string.Empty, BobCode.KeyNotFound);
        }
        internal static BobResult Ok()
        {
            return new BobResult(string.Empty, BobCode.Ok);
        }

        /// <summary>
        /// Check operation result status
        /// </summary>
        /// <returns></returns>
        public bool IsError() => Code == BobCode.Error;

        /// <summary>
        /// print result to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"code: {Code}, message: {Message}";
        }
    }

    /// <summary>
    /// Bob get operation result
    /// </summary>
    public class BobGetResult
    {
        /// <summary>
        /// Operation result message
        /// </summary>
        public BobResult Result { get; }

        /// <summary>
        /// Operation result data
        /// </summary>
        public byte[] Data { get; }

        internal BobGetResult(BobResult result, byte[] data)
        {
            Result = result;
            Data = data;
        }

        internal BobGetResult(BobResult result) : this(result, null)
        {
        }
    }
}
