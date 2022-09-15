using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.App
{
    public interface IKeySource : IReadOnlyCollection<ulong>
    {
    }
    public interface IIndexedKeySource : IKeySource, IReadOnlyList<ulong>
    { 
    }



    public class SingleKey : IIndexedKeySource
    { 
        public SingleKey(ulong key)
        {
            Key = key;
        }

        public ulong Key { get; }
        public int Count { get { return 1; } }

        public ulong this[int index]
        {
            get
            {
                if (index != 0)
                    throw new IndexOutOfRangeException();
                return Key;
            }
        }

        public IEnumerator<ulong> GetEnumerator()
        {
            yield return Key;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool TryParse(string str, out SingleKey result)
        {
            if (ulong.TryParse(str, out ulong keyVal))
            {
                result = new SingleKey(keyVal);
                return true;
            }

            result = null;
            return false;
        }
        public static SingleKey Parse(string str)
        {
            return new SingleKey(ulong.Parse(str));
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }


    public class KeyRange : IIndexedKeySource
    { 
        public static KeyRange CreateWithCount(ulong start, uint count)
        {
            return new KeyRange(start, start + count - 1, 1);
        }
        public static KeyRange CreateWithCount(ulong start, uint count, int step)
        {
            return new KeyRange(start, start + count - 1, step);
        }


        private readonly ulong _startKey;

        public KeyRange(ulong min, ulong max, int step)
        {
            if (min > max)
                throw new ArgumentException("Min should be less than max");
            if (step == 0)
                throw new ArgumentException("Step cannot be zero", nameof(step));

            Min = min;
            Max = max;
            Step = step;

            Count = (int)((max - min) / (ulong)Math.Abs(step)) + 1;
            _startKey = Step > 0 ? Min : (Min + ((ulong)Count - 1) * (ulong)Math.Abs(step));
        }
        public KeyRange(ulong key)
            : this(key, key, 1)
        {
        }
        public KeyRange(ulong min, ulong max)
            : this(min, max, 1)
        {
        }

        public ulong Min { get; }
        public ulong Max { get; }
        public int Step { get; }

        public int Count { get; }

        public ulong this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                return unchecked(_startKey + (ulong)((long)Step * (long)index)); 
            }
        }


        public IEnumerator<ulong> GetEnumerator()
        {
            ulong curKey = _startKey;
            do
            {
                yield return curKey;
                curKey = unchecked(curKey + (ulong)(long)Step);
            }
            while (curKey >= Min && curKey <= Max);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



        private static bool TryParseCore(string str, bool throwExc, out KeyRange result)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            str = str.Trim();

            result = null;
            if (str.Length == 0)
            {
                if (throwExc)
                    throw new FormatException("Can't parse key range from empty string");
                return false;
            }

            string stepPart = null;
            string minPart = null;
            string maxPart = null;
            string countPart = null;

            if (str[0] == '[')
            {
                if (str[str.Length - 1] != ']')
                {
                    if (throwExc)
                        throw new FormatException("KeyRange starts with square bracket, but not end with it");
                    return false;
                }

                str = str.Substring(1, str.Length - 2);
            }

            int colonIndex = str.IndexOf(':');
            if (colonIndex >= 0)
            {
                stepPart = str.Substring(colonIndex + 1);
                str = str.Substring(0, colonIndex);
            }

            int minusSignIndex = str.IndexOf('-');
            int plusSignIndex = minusSignIndex >= 0 ? -1 : str.IndexOf('+');
            if (minusSignIndex >= 0)
            {
                minPart = str.Substring(0, minusSignIndex);
                maxPart = str.Substring(minusSignIndex + 1);
            }
            else if (plusSignIndex >= 0)
            {
                minPart = str.Substring(0, plusSignIndex);
                countPart = str.Substring(plusSignIndex + 1);
            }
            else
            {
                minPart = str;
            }

            ulong min = 0;
            ulong max = 0;
            uint count = 0;
            int step = 1;

            if (!ulong.TryParse(minPart, NumberStyles.None, CultureInfo.InvariantCulture, out min))
            {
                if (throwExc)
                    throw new FormatException($"Unable to parse Min value of key range: {minPart}");
                return false;
            }

            if (maxPart != null)
            {
                if (!ulong.TryParse(maxPart, NumberStyles.None, CultureInfo.InvariantCulture, out max))
                {
                    if (throwExc)
                        throw new FormatException($"Unable to parse Max value of key range: {maxPart}");
                    return false;
                }
            }
            else
            {
                max = min;
            }

            if (countPart != null)
            {
                if (!uint.TryParse(countPart, NumberStyles.None, CultureInfo.InvariantCulture, out count))
                {
                    if (throwExc)
                        throw new FormatException($"Unable to parse Count value of key range: {countPart}");
                    return false;
                }
            }
            else
            {
                count = 1;
            }

            if (stepPart != null)
            {
                if (maxPart == null && countPart == null)
                {
                    if (throwExc)
                        throw new FormatException($"Step can be specified only for range and not for a single key");
                    return false;
                }

                if (!int.TryParse(stepPart, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out step))
                {
                    if (throwExc)
                        throw new FormatException($"Unable to parse Step value of key range: {stepPart}");
                    return false;
                }
            }

            if (step == 0)
            {
                if (throwExc)
                    throw new FormatException("Step of KeyRange cannot be zero");
                return false;
            }

            if (min > max)
            {
                if (throwExc)
                    throw new FormatException("Min should be less or equal to Max in KeyRange");
                return false;
            }

            if (countPart != null)
                result = CreateWithCount(min, count, step);
            else
                result = new KeyRange(min, max, step);

            return true;
        }
        public static bool TryParse(string str, out KeyRange result)
        {
            return TryParseCore(str, false, out result);
        }
        public static KeyRange Parse(string str)
        {
            if (!TryParseCore(str, true, out var result))
                throw new FormatException("Unable to parse key range");
            return result;
        }

        public override string ToString()
        {
            if (Min == Max)
                return Min.ToString();

            if (Step == 1)
                return $"{Min}-{Max}";

            return $"{Min}-{Max}:{Step}";
        }
    }


    public class KeyList : IIndexedKeySource
    {
        private readonly IIndexedKeySource[] _keySources;

        public KeyList(IEnumerable<IIndexedKeySource> keySources)
        {
            if (keySources == null)
                throw new ArgumentNullException(nameof(keySources));

            _keySources = keySources.ToArray();
            Count = _keySources.Length > 0 ? _keySources.Sum(o => o.Count) : 0;
        }
        public KeyList(params IIndexedKeySource[] keySources)
            : this((IEnumerable<IIndexedKeySource>)keySources ?? Array.Empty<IIndexedKeySource>())
        {
        }

        public IReadOnlyList<IIndexedKeySource> KeySources { get { return _keySources; } }

        public int Count { get; }

        public ulong this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                int srcInd = 0;
                while (srcInd < _keySources.Length && index >= _keySources[srcInd].Count)
                {
                    index -= _keySources[srcInd].Count;
                    srcInd++;
                }

                return _keySources[srcInd][index];
            }
        }


        public IEnumerator<ulong> GetEnumerator()
        {
            for (int i = 0; i < _keySources.Length; i++)
            {
                foreach (var key in _keySources[i])
                    yield return key;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public static KeyList Parse(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            str = str.Trim();
            if (str.Length == 0)
                throw new FormatException("Empty key list is not allowed");

            string[] parts = str.Split(',');

            List<IIndexedKeySource> sources = new List<IIndexedKeySource>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (SingleKey.TryParse(parts[i], out var singleKey))
                    sources.Add(singleKey);
                else
                    sources.Add(KeyRange.Parse(parts[i]));
            }

            return new KeyList(sources);
        }

        public override string ToString()
        {
            return string.Join(",", _keySources.Select(o => o.ToString()));
        }
    }



    public class RandomizedKeySource : IKeySource
    {
        private static readonly Random _rndInit = new Random();
        private static Random CreateRandom()
        {
            int seed = 0;
            lock (_rndInit)
                seed = _rndInit.Next();

            return new Random(seed);
        }

        public RandomizedKeySource(IIndexedKeySource keySource, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            KeySource = keySource ?? throw new ArgumentNullException(nameof(keySource));
            Count = count;
        }   

        public IIndexedKeySource KeySource { get; }
        public int Count { get; }

        public IEnumerator<ulong> GetEnumerator()
        {
            Random rnd = CreateRandom();

            for (int i = 0; i < Count; i++)
                yield return KeySource[rnd.Next(KeySource.Count)];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class RandomizedShuffleKeySource : IKeySource
    {
        private const int MaxShuffleBlockSize = 65536;

        private static readonly Random _rndInit = new Random();
        private static Random CreateRandom()
        {
            int seed = 0;
            lock (_rndInit)
                seed = _rndInit.Next();

            return new Random(seed);
        }

        private class ShuffleBlockProgress
        {
            private readonly int[] _block;
            private int _curPorgress;

            public ShuffleBlockProgress(int[] block)
            {
                _block = block;
                _curPorgress = 0;
            }

            public bool CanMove { get { return _curPorgress < _block.Length; } }
            public int Next() { return _block[_curPorgress++]; }
        }


        public RandomizedShuffleKeySource(IIndexedKeySource keySource)
        {
            KeySource = keySource ?? throw new ArgumentNullException(nameof(keySource));
        }

        public IIndexedKeySource KeySource { get; }
        public int Count { get { return KeySource.Count; } }



        private static int[] GenerateShuffleBlock(int size, Random rnd)
        {
            int[] result = new int[size];
            for (int i = 0; i < result.Length; i++)
                result[i] = i;

            for (int i = 0; i < result.Length - 1; i++)
            {
                int r = rnd.Next(i, result.Length);
                (result[i], result[r]) = (result[r], result[i]);
            }

            return result;
        }

        private static ShuffleBlockProgress[] CreateShuffleBlockProgresses(int keyCount, Random rnd)
        {
            if (keyCount <= MaxShuffleBlockSize)
                return new ShuffleBlockProgress[1] { new ShuffleBlockProgress(GenerateShuffleBlock(keyCount, rnd)) };

            int[] mainBlock = GenerateShuffleBlock(Math.Min(keyCount, MaxShuffleBlockSize), rnd);
            int[] lastBlock = keyCount % MaxShuffleBlockSize == 0 ? mainBlock : GenerateShuffleBlock(keyCount % MaxShuffleBlockSize, rnd);

            ShuffleBlockProgress[] result = new ShuffleBlockProgress[((keyCount - 1) / MaxShuffleBlockSize) + 1];
            for (int i = 0; i < result.Length - 1; i++)
                result[i] = new ShuffleBlockProgress(mainBlock);
            result[result.Length - 1] = new ShuffleBlockProgress(lastBlock);
            return result;
        }

        private static int ChooseBlock(ShuffleBlockProgress[] shuffleBlockProgress, int blockIndex)
        {
            if (!shuffleBlockProgress[blockIndex].CanMove)
            {
                for (int b = 0; b < shuffleBlockProgress.Length; b++)
                {
                    blockIndex = (blockIndex + 1) % shuffleBlockProgress.Length;
                    if (shuffleBlockProgress[blockIndex].CanMove)
                        break;
                }
            }

            return blockIndex;
        }

        public IEnumerator<ulong> GetEnumerator()
        {
            if (Count == 0)
                yield break;

            Random rnd = CreateRandom();

            var shuffleBlockProgress = CreateShuffleBlockProgresses(Count, rnd);
            for (int i = 0; i < Count; i++)
            {
                int curBlock = ChooseBlock(shuffleBlockProgress, rnd.Next(shuffleBlockProgress.Length));
                int inBlockKeyIndex = shuffleBlockProgress[curBlock].Next();
                int keyIndex = curBlock * MaxShuffleBlockSize + inBlockKeyIndex;

                yield return KeySource[keyIndex];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    public class KeyPackageAggregator : IEnumerable<ulong[]>
    { 
        public KeyPackageAggregator(IKeySource keySource, int packageSize)
        {
            if (packageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(packageSize));

            KeySource = keySource ?? throw new ArgumentNullException(nameof(keySource));
            PackageSize = packageSize;
            PackageCount = ((KeySource.Count - 1) / packageSize) + 1;
        }

        public IKeySource KeySource { get; }
        public int PackageSize { get; }
        public int KeyCount { get { return KeySource.Count; } }
        public int PackageCount { get; }

        public IEnumerator<ulong[]> GetEnumerator()
        {
            ulong[] curResult = null;
            int curResultPos = 0;

            foreach (var key in KeySource)
            {
                if (curResult == null)
                {
                    curResult = new ulong[PackageSize];
                    curResultPos = 0;
                }

                curResult[curResultPos++] = key;
                if (curResultPos == curResult.Length)
                {
                    yield return curResult;
                    curResult = null;
                }
            }

            if (curResultPos > 0 && curResult != null)
            {
                if (curResultPos < curResult.Length)
                    Array.Resize(ref curResult, curResultPos);

                yield return curResult;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
