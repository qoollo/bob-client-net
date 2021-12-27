using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.InteractiveTests
{
    public class ParallelRandom
    {
        private readonly Random[] _randoms;
        public ParallelRandom(int threadCount)
        {
            _randoms = new Random[threadCount];
            for (int i = 0; i < threadCount; i++)
                _randoms[i] = new Random(Environment.TickCount + i * 2);
        }

        public int Next(int thread, int maxValue)
        {
            var myRand = _randoms[thread % _randoms.Length];
            lock (myRand)
            {
                return myRand.Next(maxValue);
            }
        }
    }
}
