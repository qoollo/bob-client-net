using BobStorage;
namespace BobClient
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

        public override string ToString()
        {
            return $"code: {Code}, message: {Message}";
        }
    }
}
