using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob key structure
    /// </summary>
    public readonly struct BobKey : IEquatable<BobKey>
    {
        private static readonly char[] _hexTable = new char[16] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        /// <summary>
        /// Creates <see cref="BobKey"/> from <see cref="ulong"/>
        /// </summary>
        /// <param name="value">Integer value</param>
        /// <returns>Created BobKey</returns>
        public static BobKey FromUInt64(ulong value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 4; i++)
                {
                    byte tmp = bytes[i];
                    bytes[i] = bytes[7 - i];
                    bytes[7 - i] = tmp;
                }
            }
            return new BobKey(bytes);
        }

        // ================

        private readonly byte[] _keyBytes;

        /// <summary>
        /// <see cref="BobKey"/> constructor
        /// </summary>
        /// <param name="keyBytes">Key bytes</param>
        /// <exception cref="ArgumentNullException">keyBytes is null</exception>
        /// <exception cref="ArgumentException">keyBytes has zero length</exception>
        public BobKey(byte[] keyBytes)
        {
            if (keyBytes == null)
                throw new ArgumentNullException(nameof(keyBytes));
            if (keyBytes.Length == 0)
                throw new ArgumentException("BobKey cannot have zero length", nameof(keyBytes));

            _keyBytes = keyBytes ?? throw new ArgumentNullException(nameof(keyBytes));
        }

        /// <summary>
        /// Key length
        /// </summary>
        public int Length { get { return _keyBytes?.Length ?? 0; } }
        /// <summary>
        /// Gets one key byte
        /// </summary>
        /// <param name="index">Byte index</param>
        /// <returns>byte value</returns>
        /// <exception cref="NullReferenceException">Attempt to get byte from not initialized key</exception>
        /// <exception cref="IndexOutOfRangeException">Attempt to get key byte with an index that is outside the key length</exception>
        public byte this[int index] { get { return _keyBytes[index]; } }
        /// <summary>
        /// Whether the key is initialized (created with <see cref="BobKey(byte[])"/>)
        /// </summary>
        internal bool IsInitialized { get { return _keyBytes != null; } }

        /// <summary>
        /// Extract inner key bytes
        /// </summary>
        /// <returns>Extracted bytes</returns>
        internal byte[] GetKeyBytes()
        {
            return _keyBytes;
        }

        /// <summary>
        /// Calculates the remainder of a division
        /// </summary>
        /// <param name="divisor">Divisor</param>
        /// <returns>Remainder</returns>
        internal int Remainder(int divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException();

            if (_keyBytes == null)
                return 0;

            long rem = 0;
            long byteMaxRem = 1;
            for (int i = 0; i < _keyBytes.Length; i++)
            {
                rem = (rem + _keyBytes[i] * byteMaxRem) % divisor;
                byteMaxRem = (byteMaxRem * 256) % divisor;
            }

            return (int)rem;
        }

        /// <summary>
        /// Indicates whether the current BobKey is equal to another BobKey
        /// </summary>
        /// <param name="other">Other BobKey</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false</returns>
        public bool Equals(BobKey other)
        {
            if (object.ReferenceEquals(_keyBytes, other._keyBytes))
                return true;
            if (_keyBytes == null || other._keyBytes == null)
                return false;

            if (_keyBytes.Length != other._keyBytes.Length)
                return false;

            for (int i = 0; i < _keyBytes.Length; i++)
                if (_keyBytes[i] != other._keyBytes[i])
                    return false;

            return true;
        }
        /// <summary>
        /// Indicates whether the current BobKey is equal to another BobKey
        /// </summary>
        /// <param name="obj">Object to compare with</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (obj is BobKey other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="left">Left value</param>
        /// <param name="right">Right value</param>
        /// <returns>true when equals; otherwise, false</returns>
        public static bool operator ==(BobKey left, BobKey right)
        {
            return left.Equals(right);
        }
        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="left">Left value</param>
        /// <param name="right">Right value</param>
        /// <returns>true when not equals; otherwise, false</returns>
        public static bool operator !=(BobKey left, BobKey right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Calculates hash code for byte array
        /// </summary>
        /// <param name="byteArray">byte array</param>
        /// <returns>Calculated hash code</returns>
        private static int GetHashCodeSlow(byte[] byteArray)
        {
            int result = 0;
            int index = 0;
            int size = (byteArray.Length / 4) * 4;

            while (index < size)
            {
                result ^= BitConverter.ToInt32(byteArray, index);
                index += 4;
            }

            if (byteArray.Length > index)
            {
                result ^= (int)byteArray[index];
                if (byteArray.Length > index + 1)
                    result ^= ((int)byteArray[index + 1] << 8);
                if (byteArray.Length > index + 2)
                    result ^= ((int)byteArray[index + 2] << 16);
                if (byteArray.Length > index + 3)
                    result ^= ((int)byteArray[index + 3] << 24);
            }

            return result;
        }
        /// <summary>
        /// Calculates the hash code for this instance
        /// </summary>
        /// <returns>Calculated hash code</returns>
        public override int GetHashCode()
        {
            if (_keyBytes == null)
                return 0;

            if (_keyBytes.Length == 8)
                return BitConverter.ToInt64(_keyBytes, 0).GetHashCode();
            if (_keyBytes.Length == 4)
                return BitConverter.ToInt32(_keyBytes, 0).GetHashCode();

            return GetHashCodeSlow(_keyBytes);
        }


        /// <summary>
        /// Returns string representation of BobKey
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            if (_keyBytes == null)
                return "";
            if (_keyBytes.Length == 0)
                return "0x0";

            char[] result = new char[2 + 2 * _keyBytes.Length];
            result[0] = '0';
            result[1] = 'x';

            for (int i = 0; i < _keyBytes.Length; i++)
            {
                result[2 * i + 2] = _hexTable[_keyBytes[i] >> 4];
                result[2 * i + 3] = _hexTable[_keyBytes[i] & 15];
            }

            return new string(result);
        }
    }
}
