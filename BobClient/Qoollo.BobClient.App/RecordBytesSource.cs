using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.App
{
    public abstract class RecordBytesSource : IDisposable
    {
        public abstract byte[] GetData(ulong key);
        public abstract void StoreData(ulong key, byte[] data);
        public abstract bool VerifyData(ulong key, byte[] data);

        protected virtual void Dispose(bool isUserCall)
        {

        }
        public void Dispose()
        {
            Dispose(true);
        }
    }


    public class NopRecordBytesSource : RecordBytesSource
    {
        public override byte[] GetData(ulong key) { throw new InvalidOperationException("Record is not available"); }
        public override void StoreData(ulong key, byte[] data) { }
        public override bool VerifyData(ulong key, byte[] data) { return data != null; }
    }

    public class SizeOnlyRecordBytesSource : RecordBytesSource
    {
        public SizeOnlyRecordBytesSource(int? dataLength) { DataLength = dataLength; }
        public int? DataLength { get; }
        public override byte[] GetData(ulong key) { throw new InvalidOperationException("Record is not available"); }
        public override void StoreData(ulong key, byte[] data) { }
        public override bool VerifyData(ulong key, byte[] data) { return data != null && (!DataLength.HasValue || (data.Length == DataLength.Value)); }
    }

    public class PredefinedArrayRecordBytesSource : RecordBytesSource
    {
        private static byte[] StringToByteArray(string hex)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Incorrect hex string");

            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return result;
        }

        public static PredefinedArrayRecordBytesSource CreateDefaultWithSize(int size)
        {
            var dataArray = new byte[size];

            for (int i = 0; i < size; i++)
                dataArray[i] = (byte)(i & byte.MaxValue);

            return new PredefinedArrayRecordBytesSource(dataArray);
        }
        public static PredefinedArrayRecordBytesSource CreateFromByteArrayPattern(byte[] pattern, int size)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var dataArray = new byte[size];
            if (pattern.Length > 0)
            {
                int patPos = 0;
                for (int i = 0; i < size; i++, patPos = (patPos + 1) % pattern.Length)
                    dataArray[i] = pattern[patPos];
            }

            return new PredefinedArrayRecordBytesSource(dataArray);
        }
        public static PredefinedArrayRecordBytesSource CreateFromHexPattern(string hexPattern, int size)
        {
            return CreateFromByteArrayPattern(StringToByteArray(hexPattern), size);
        }
        public static PredefinedArrayRecordBytesSource CreateFromHexPattern(string hexPattern)
        {
            return new PredefinedArrayRecordBytesSource(StringToByteArray(hexPattern));
        }

        public PredefinedArrayRecordBytesSource(byte[] dataArray)
        {
            DataArray = dataArray ?? throw new ArgumentNullException(nameof(dataArray));
        }

        public byte[] DataArray { get; }

        public override byte[] GetData(ulong key) { return DataArray; }
        public override void StoreData(ulong key, byte[] data) { }
        public override bool VerifyData(ulong key, byte[] data) 
        {
            if (data == null)
                return false;
            if (data.Length != DataArray.Length)
                return false;

            for (int i = 0; i < data.Length; i++)
                if (data[i] != DataArray[i])
                    return false;

            return true;
        }
    }
}
