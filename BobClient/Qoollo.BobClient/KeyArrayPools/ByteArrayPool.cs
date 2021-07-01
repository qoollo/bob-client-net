using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient.KeyArrayPools
{
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

    internal sealed class ByteArrayPool : IDisposable
    {
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

            public ushort Head
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Unsafe.As<int, UInt16Pack>(ref _pack).B; }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set { Unsafe.As<int, UInt16Pack>(ref _pack).B = value; }
            }
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

            public bool HasElements
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Head != Tail; }
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

        public ByteArrayPool(int byteArrayLength, int maxElementCount)
        {
            if (byteArrayLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteArrayLength));
            if (maxElementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxElementCount));

            if (maxElementCount > ushort.MaxValue - GapSize)
                maxElementCount = ushort.MaxValue - GapSize;

            ByteArrayLength = byteArrayLength;
            MaxElementCount = maxElementCount;

            _arrayContainer = new byte[maxElementCount + GapSize][];
            _headTailIndices = new HeadTailIndicesStruct(0, 0).Pack();

            _perThreadContainer = new ThreadLocal<byte[]>(trackAllValues: false);
        }

        public int ByteArrayLength { get; }
        public int MaxElementCount { get; }


        internal byte[] TryRentGlobal()
        {
            int iteration = 0;
            var unpackedIndices = new HeadTailIndicesStruct(_headTailIndices);

            while (unpackedIndices.HasElements)
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


        internal byte[] TryRentThreadLocal()
        {
            return _perThreadContainer.Value;
        }

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

        internal bool TryReleaseThreadLocal(byte[] array)
        {
            if (_perThreadContainer.Value != null)
                return false;

            _perThreadContainer.Value = array;
            return true;
        }

        public void Release(byte[] array, bool skipLocal)
        {
            if (array == null || array.Length != ByteArrayLength)
                return;

            if (!skipLocal && TryReleaseThreadLocal(array))
                return;

            TryReleaseGlobal(array);
        }

        public void Dispose()
        {
            _perThreadContainer.Dispose();
        }
    }

#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
}
