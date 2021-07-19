using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Qoollo.BobClient.App
{
    public class ProgressTracker : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private volatile bool _isDisposed;

        private volatile int _currentCount;
        private volatile int _currentErrorCount;
        private readonly int _totalCount;

        private readonly string _operationDescription;

        private readonly Stopwatch _stopwatch;

        private readonly Func<string> _customMessageBuilder;

        public ProgressTracker(int intervalMs, string operationDescription, int totalCount, Func<string> customMessageBuilder = null)
        {
            if (intervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMs));
            if (totalCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            if (operationDescription == null)
                throw new ArgumentNullException(nameof(operationDescription));

            _timer = new System.Timers.Timer(intervalMs);
            _timer.AutoReset = true;
            _timer.Enabled = false;
            _timer.Elapsed += TimerElapsed;

            _totalCount = totalCount;
            _currentCount = 0;
            _currentErrorCount = 0;

            _operationDescription = operationDescription;

            _stopwatch = new Stopwatch();

            _customMessageBuilder = customMessageBuilder;

            _isDisposed = false;
        }

        public int TotalCount { get { return _totalCount; } }
        public int CurrentCount { get { return _currentCount; } }
        public int CurrentErrorCount { get { return _currentErrorCount; } }

        public long ElapsedMilliseconds { get { return _stopwatch.ElapsedMilliseconds; } }

        public ProgressTracker Start()
        {
            if (!_isDisposed)
            {
                _timer.Enabled = true;
                _stopwatch.Start();
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
            int currentErrorCount = _currentErrorCount;
            long currentCount = _currentCount;

            if (_customMessageBuilder != null)
                Console.WriteLine($"{_operationDescription}: {currentCount,8}/{TotalCount},   {_customMessageBuilder()},   Errors: {currentErrorCount,5},   RPS: {(currentCount * 1000) / _stopwatch.ElapsedMilliseconds,4}");
            else
                Console.WriteLine($"{_operationDescription}: {currentCount,8}/{TotalCount},   Errors: {currentErrorCount,5},   RPS: {(currentCount * 1000) / _stopwatch.ElapsedMilliseconds,4}");
        }

        public void Dispose()
        {
            _isDisposed = true;
            _stopwatch.Stop();
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
