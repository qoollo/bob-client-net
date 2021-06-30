using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient.KeyArrayPools
{
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

    internal sealed class ByteArrayPool : IDisposable
    {
        private const int GapSize = 16;

        private struct HeadTailIndicesStruct
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public HeadTailIndicesStruct(ushort head, ushort tail)
            {
                Head = head;
                Tail = tail;
                OriginalPackedHeadTailIndices = unchecked((int)(((uint)Head << 16) | (uint)Tail));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public HeadTailIndicesStruct(int packedHeadTailIndices)
            {
                unchecked
                {
                    OriginalPackedHeadTailIndices = packedHeadTailIndices;
                    Head = (ushort)(((uint)packedHeadTailIndices >> 16) & ushort.MaxValue);
                    Tail = (ushort)((uint)packedHeadTailIndices & ushort.MaxValue);
                }
            }

            public readonly int OriginalPackedHeadTailIndices;
            public ushort Head;
            public ushort Tail;

            public bool HasElements
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Head != Tail; }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int MoveHeadForward(int pooledArrayLength)
            {
                int result = Head;
                Head = (ushort)((Head + 1) % pooledArrayLength);
                return result;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Pack()
            {
                unchecked
                {
                    return (int)(((uint)Head << 16) | (uint)Tail);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CompareExchangeCurState(ref int target)
            {
                return Interlocked.CompareExchange(ref target, Pack(), OriginalPackedHeadTailIndices) == OriginalPackedHeadTailIndices;
            }
        }


        // =================

        private readonly int _pooledArrayLength;
        private readonly int _maxElementCount;

        private readonly byte[][] _arrayContainer;
        private volatile int _headTailIndices;

        private readonly ThreadLocal<byte[]> _perThreadContainer;

        public ByteArrayPool(int pooledArrayLength, int maxElementCount)
        {
            if (pooledArrayLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(pooledArrayLength));
            if (maxElementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxElementCount));

            _pooledArrayLength = pooledArrayLength;
            _maxElementCount = maxElementCount;

            _arrayContainer = new byte[maxElementCount + GapSize][];
            _headTailIndices = new HeadTailIndicesStruct(0, 0).Pack();

            _perThreadContainer = new ThreadLocal<byte[]>(trackAllValues: false);
        }

        public int PooledArrayLength { get { return _pooledArrayLength; } }
        public int MaxElementCount { get { return _maxElementCount; } }


        internal byte[] TryRentGlobal()
        {
            SpinWait sw = new SpinWait();
            int iteration = 0;
            var unpackedIndices = new HeadTailIndicesStruct(_headTailIndices);

            while (unpackedIndices.HasElements)
            {
                int headIndex = unpackedIndices.MoveHeadForward(_pooledArrayLength);
                if (unpackedIndices.CompareExchangeCurState(ref _headTailIndices))
                    return Interlocked.Exchange(ref _arrayContainer[headIndex], null);

                if (++iteration > 8)
                    sw.SpinOnce();

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
                result = new byte[_pooledArrayLength];

            return result;
        }

        public void Release(byte[] array)
        {
            if (array == null || array.Length != _pooledArrayLength)
                return;
        }

        public void Dispose()
        {
            _perThreadContainer.Dispose();
        }
    }

#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
}
