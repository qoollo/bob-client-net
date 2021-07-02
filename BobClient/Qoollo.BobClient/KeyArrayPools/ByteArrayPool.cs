using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient.KeyArrayPools
{
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

    /// <summary>
    /// Byte array pool for Bob keys serialization
    /// </summary>
    internal sealed class ByteArrayPool : IDisposable
    {
        /// <summary>
        /// Gap between head and tail (in both directions). It is used to prevent contention between threads
        /// </summary>
        internal const int GapSize = 16;

        /// <summary>
        /// Struct to work with packed head-tail indices.
        /// (internal for test purposes)
        /// </summary>
        internal struct HeadTailIndicesStruct
        {
            [StructLayout(LayoutKind.Explicit)]
            private struct UInt16Pack
            {
                [FieldOffset(0)]
                public UInt16 A;
                [FieldOffset(2)]
                public UInt16 B;
            }

            private readonly int _originalPackedHeadTailIndices;
            private int _pack;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public HeadTailIndicesStruct(ushort head, ushort tail)
            {
                _originalPackedHeadTailIndices = unchecked((int)(((uint)head << 16) | (uint)tail));
                _pack = _originalPackedHeadTailIndices;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public HeadTailIndicesStruct(int packedHeadTailIndices)
            {
                _originalPackedHeadTailIndices = packedHeadTailIndices;
                _pack = packedHeadTailIndices;
            }

            public int OriginalPackedHeadTailIndices 
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _originalPackedHeadTailIndices; } 
            }

            /// <summary>
            /// Queue head index
            /// </summary>
            public ushort Head
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Unsafe.As<int, UInt16Pack>(ref _pack).B; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set { Unsafe.As<int, UInt16Pack>(ref _pack).B = value; }
            }
            /// <summary>
            /// Queue tail index
            /// </summary>
            public ushort Tail
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Unsafe.As<int, UInt16Pack>(ref _pack).A; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set { Unsafe.As<int, UInt16Pack>(ref _pack).A = value; }
            }

            public int ElementCount(int poolSize)
            {
                return ((poolSize + (int)Tail - (int)Head) % poolSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasAvailableElements(int poolSize)
            {
                return ((poolSize + (int)Tail - (int)Head) % poolSize) > GapSize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int MoveHeadForward(int poolSize)
            {
                int result = Head;
                Head = (ushort)((Head + 1) % poolSize);
                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasFreeSpace(int poolSize)
            {
                return ((poolSize + (int)Head - (int)Tail - 1) % poolSize) > GapSize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int MoveTailForward(int poolSize)
            {
                int result = Tail;
                Tail = (ushort)((Tail + 1) % poolSize);
                return result;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Pack()
            {
                return _pack;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CompareExchangeCurState(ref int target)
            {
                return Interlocked.CompareExchange(ref target, Pack(), OriginalPackedHeadTailIndices) == OriginalPackedHeadTailIndices;
            }
        }


        // =================

        private readonly byte[][] _arrayContainer;
        private volatile int _headTailIndices;

        private readonly ThreadLocal<byte[]> _perThreadContainer;

        /// <summary>
        /// <see cref="ByteArrayPool"/> constructor
        /// </summary>
        /// <param name="byteArrayLength">Length of byte arrays stored in pool</param>
        /// <param name="maxElementCount">Max count of available for Rent elements</param>
        public ByteArrayPool(int byteArrayLength, int maxElementCount)
        {
            if (byteArrayLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteArrayLength));
            if (maxElementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxElementCount));

            if (maxElementCount > ushort.MaxValue - 2 * GapSize - 1)
                maxElementCount = ushort.MaxValue - 2 * GapSize - 1;

            ByteArrayLength = byteArrayLength;
            MaxElementCount = maxElementCount;

            // '2 * GapSize' is used o reserve Gap in both directions
            _arrayContainer = new byte[maxElementCount + 2 * GapSize + 1][];
            _headTailIndices = new HeadTailIndicesStruct(0, 0).Pack();

            _perThreadContainer = new ThreadLocal<byte[]>(trackAllValues: false);
        }

        /// <summary>
        /// Length of byte arrays stored in pool
        /// </summary>
        public int ByteArrayLength { get; }
        /// <summary>
        /// Max count of available for Rent elements
        /// </summary>
        public int MaxElementCount { get; }
        /// <summary>
        /// Max capacity (<see cref="MaxElementCount"/> + GapSize)
        /// </summary>
        public int FullCapacity { get { return MaxElementCount + GapSize; } }


        /// <summary>
        /// Attempts to rent from global array container
        /// </summary>
        /// <returns>Byte array if success, null otherwise</returns>
        internal byte[] TryRentGlobal()
        {
            int iteration = 0;
            var unpackedIndices = new HeadTailIndicesStruct(_headTailIndices);

            while (unpackedIndices.HasAvailableElements(_arrayContainer.Length))
            {
                int headIndex = unpackedIndices.MoveHeadForward(_arrayContainer.Length);
                if (unpackedIndices.CompareExchangeCurState(ref _headTailIndices))
                    return Interlocked.Exchange(ref _arrayContainer[headIndex], null);

                if (++iteration > 16)
                    Thread.Yield();
                else if (iteration > 4)
                    Thread.SpinWait(iteration << 3);

                unpackedIndices = new HeadTailIndicesStruct(_headTailIndices);
            }

            return null;
        }
        /// <summary>
        /// Attempts to rent from local thread storage
        /// </summary>
        /// <returns>Byte array if success, null otherwise</returns>
        internal byte[] TryRentThreadLocal()
        {
            return _perThreadContainer.Value;
        }
        /// <summary>
        /// Rent byte array from pool (or allocate new if pool is empty)
        /// </summary>
        /// <param name="skipLocal">True if you want to ignore Local per Thread storage</param>
        /// <returns>Byte array</returns>
        public byte[] Rent(bool skipLocal)
        {
            byte[] result = null;
            if (!skipLocal)
                result = TryRentThreadLocal();
            if (result == null)
                result = TryRentGlobal();
            if (result == null)
                result = new byte[ByteArrayLength];

            return result;
        }

        /// <summary>
        /// Attempt to release array to global storage
        /// </summary>
        /// <param name="array">Array</param>
        /// <returns>True if array was returned back to the pool storage, otherwise false</returns>
        internal bool TryReleaseGlobal(byte[] array)
        {
            int iteration = 0;
            var unpackedIndices = new HeadTailIndicesStruct(_headTailIndices);

            while (unpackedIndices.HasFreeSpace(_arrayContainer.Length))
            {
                int tailIndex = unpackedIndices.MoveTailForward(_arrayContainer.Length);
                if (unpackedIndices.CompareExchangeCurState(ref _headTailIndices))
                {
                    Interlocked.Exchange(ref _arrayContainer[tailIndex], array);
                    return true;
                }

                if (++iteration > 16)
                    Thread.Yield();
                else if (iteration > 4)
                    Thread.SpinWait(iteration << 3);

                unpackedIndices = new HeadTailIndicesStruct(_headTailIndices);
            }

            return false;
        }
        /// <summary>
        /// Attempt to release array to local per Thread storage
        /// </summary>
        /// <param name="array">Array</param>
        /// <returns>True if array was returned back to local per Thread storage, otherwise false</returns>
        internal bool TryReleaseThreadLocal(byte[] array)
        {
            if (_perThreadContainer.Value != null)
                return false;

            _perThreadContainer.Value = array;
            return true;
        }
        /// <summary>
        /// Release array to pool
        /// </summary>
        /// <param name="array">Array to release</param>
        /// <param name="skipLocal">True if you want to ignore Local per Thread storage</param>
        public void Release(byte[] array, bool skipLocal)
        {
            if (array == null || array.Length != ByteArrayLength)
                return;

            if (!skipLocal && TryReleaseThreadLocal(array))
                return;

            TryReleaseGlobal(array);
        }

        /// <summary>
        /// Release all resources
        /// </summary>
        public void Dispose()
        {
            _perThreadContainer.Dispose();
        }
    }

#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
}
