using BobStorage;
namespace BobClient
{
    public enum BobCode
    {
        Error = -1,
        Ok = 0,
        KeyNotFound = 1,
    }

    public class BobResult
    {
        public string Message { get; }
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

        public bool IsError() => Code == BobCode.Error;

        public override string ToString()
        {
            return $"code: {Code}, message: {Message}";
        }
    }
}
