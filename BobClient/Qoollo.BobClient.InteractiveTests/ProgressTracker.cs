using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Qoollo.BobClient.InteractiveTests
{
    public class ProgressStats
    {
        public ProgressStats(IReadOnlyList<double> rpsList, int totalCount, int errorCount, long elapsedMilliseconds)
        {
            if (rpsList == null)
                throw new ArgumentNullException(nameof(rpsList));
            if (totalCount < 0)
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            if (errorCount < 0)
                throw new ArgumentOutOfRangeException(nameof(errorCount));
            if (elapsedMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));

            TotalCount = totalCount;
            ErrorCount = errorCount;
            ElapsedMilliseconds = elapsedMilliseconds;
            ProcessedCount = rpsList.Count;

            if (rpsList.Count > 0)
            {
                List<double> rpsListCopy = rpsList.ToList();
                if (rpsListCopy.Count > 1 && rpsListCopy[0] == 0.0)
                    rpsListCopy.RemoveAt(0);

                RpsAvg = rpsListCopy.Average();
                RpsDev = Math.Sqrt(rpsListCopy.Average(o => (o - RpsAvg) * (o - RpsAvg)));
                RpsMax = rpsListCopy.Max();
                RpsMin = rpsListCopy.Min();

                rpsListCopy.Sort();
                if (rpsListCopy.Count % 2 == 1)
                    RpsMedian = rpsListCopy[rpsListCopy.Count / 2];
                else
                    RpsMedian = (rpsListCopy[(rpsListCopy.Count / 2) - 1] + rpsListCopy[rpsListCopy.Count / 2]) / 2.0;

                Rps10P = rpsListCopy[(int)(rpsListCopy.Count * 0.1)];
                Rps90P = rpsListCopy[(int)(rpsListCopy.Count * 0.9)];
            }
        }

        public int TotalCount { get; }
        public int ErrorCount { get; }
        public long ElapsedMilliseconds { get; }

        public int ProcessedCount { get; }
        public double RpsAvg { get; }
        public double RpsDev { get; }
        public double RpsMin { get; }
        public double RpsMax { get; }
        public double RpsMedian { get; }
        public double Rps10P { get; }
        public double Rps90P { get; }
    }

    public class ProgressTracker : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly int _timerInterval;
        private volatile bool _isDisposed;

        private volatile int _currentCount;
        private volatile int _currentErrorCount;
        private volatile int _currentCountFromPreviousTick;
        private readonly List<double> _rpsList;
        private readonly int _totalCount;

        private readonly string _operationDescription;

        private readonly Stopwatch _stopwatch;
        private readonly Stopwatch _deltaStopwatch;

        private readonly bool _autoPrintMsg;
        private readonly Func<string> _customMessageBuilder;

        private readonly object _syncObj;

        public ProgressTracker(int intervalMs, string operationDescription, int totalCount, bool autoPrintMsg = true, Func<string> customMessageBuilder = null)
        {
            if (intervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMs));
            if (totalCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            if (operationDescription == null)
                throw new ArgumentNullException(nameof(operationDescription));

            _timer = new System.Timers.Timer(intervalMs);
            _timerInterval = intervalMs;
            _timer.AutoReset = true;
            _timer.Enabled = false;
            _timer.Elapsed += TimerElapsed;

            _totalCount = totalCount;
            _currentCount = 0;
            _currentErrorCount = 0;
            _currentCountFromPreviousTick = 0;
            _rpsList = new List<double>();

            _operationDescription = operationDescription;

            _stopwatch = new Stopwatch();
            _deltaStopwatch = new Stopwatch();

            _autoPrintMsg = autoPrintMsg;
            _customMessageBuilder = customMessageBuilder;

            _syncObj = new object();

            _isDisposed = false;
        }

        public bool IsStarted { get { return _timer.Enabled; } }

        public int TotalCount { get { return _totalCount; } }
        public int CurrentCount { get { return _currentCount; } }
        public int CurrentErrorCount { get { return _currentErrorCount; } }

        public long ElapsedMilliseconds { get { return _stopwatch.ElapsedMilliseconds; } }

        public double AvgRps { get { return (double)((long)CurrentCount * 1000) / _stopwatch.ElapsedMilliseconds; } }


        public ProgressStats GetProgressStats()
        {
            List<double> rpsListCopy = null;
            int totalCount = 0;
            int errorCount = 0;
            long elapsedMilliseconds = 0;

            lock (_syncObj)
            {
                rpsListCopy = _rpsList.ToList();
                totalCount = _totalCount;
                errorCount = _currentErrorCount;
                elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
            }



            return new ProgressStats(rpsListCopy, totalCount, errorCount, elapsedMilliseconds);
        }


        public ProgressTracker Start()
        {
            if (!_isDisposed)
            {
                lock (_syncObj)
                {
                    if (!_timer.Enabled)
                    {
                        _timer.Enabled = true;
                        _stopwatch.Start();
                        _deltaStopwatch.Start();
                    }
                }
            }

            return this;
        }

        public void RegisterEvent(bool isError)
        {
            Interlocked.Increment(ref _currentCount);
            if (isError)
                Interlocked.Increment(ref _currentErrorCount);
        }
        public void RegisterSuccess()
        {
            RegisterEvent(false);
        }
        public void RegisterError()
        {
            RegisterEvent(true);
        }

        public void RegisterEvents(int count, bool isError)
        {
            Interlocked.Add(ref _currentCount, count);
            if (isError)
                Interlocked.Add(ref _currentErrorCount, count);
        }


        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Print();
        }

        public void Print()
        {
            lock (_syncObj)
            {
                int currentErrorCount = _currentErrorCount;
                int currentCount = _currentCount;
                int currentCountFromPreviousTick = _currentCountFromPreviousTick;

                double instantaneousRps = (double)((long)(currentCount - currentCountFromPreviousTick) * 1000) / _deltaStopwatch.ElapsedMilliseconds;

                _rpsList.Add(instantaneousRps);

                _deltaStopwatch.Restart();
                _currentCountFromPreviousTick = currentCount;

                if (_autoPrintMsg)
                {
                    if (_customMessageBuilder != null)
                        Console.WriteLine($"{_operationDescription}: {currentCount,8}/{TotalCount},   {_customMessageBuilder()},   Errors: {currentErrorCount,5},   RPS: {instantaneousRps.ToString("F0", CultureInfo.InvariantCulture),4}");
                    else
                        Console.WriteLine($"{_operationDescription}: {currentCount,8}/{TotalCount},   Errors: {currentErrorCount,5},   RPS: {instantaneousRps.ToString("F0", CultureInfo.InvariantCulture),4}");
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _stopwatch.Stop();
            _deltaStopwatch.Stop();
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
