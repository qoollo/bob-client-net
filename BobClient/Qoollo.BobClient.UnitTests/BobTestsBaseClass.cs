using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Qoollo.BobClient.UnitTests
{
    public class BobTestsBaseClass : IDisposable
    {
        private const bool TraceLog = false;
        private const bool HangDetection = false;
        private const int HangIntervalMs = 10 * 60 * 1000;
        private const int MinRequiredThreadPoolThreads = -1;

        private readonly CancellationTokenSource _endingToken;

        public BobTestsBaseClass(ITestOutputHelper output)
        {
            Output = output;

            var type = output.GetType();
            var testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            if (testMember != null)
            {
                var test = (ITest)testMember.GetValue(output);
                RunningTestName = test.DisplayName;
            }

            TestStartTime = DateTime.Now;

            _endingToken = new CancellationTokenSource();

            if (TraceLog)
            {
#pragma warning disable CS0162 // Unreachable code detected
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        File.AppendAllText("Test_trace.log", $"-> {RunningTestName} ({TestStartTime})" + Environment.NewLine);
                        break;
                    }
                    catch { }
                }
#pragma warning restore CS0162 // Unreachable code detected
            }

            if (HangDetection)
            {
#pragma warning disable CS0162 // Unreachable code detected
                new Thread(HangDetectionThreadMethod).Start();
#pragma warning restore CS0162 // Unreachable code detected
            }


            if (MinRequiredThreadPoolThreads > 0)
            {
#pragma warning disable CS0162 // Unreachable code detected
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
                if (maxWorkerThreads < MinRequiredThreadPoolThreads)
                    ThreadPool.SetMaxThreads(MinRequiredThreadPoolThreads, maxCompletionPortThreads);
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        protected string RunningTestName { get; }
        protected ITestOutputHelper Output { get; }
        protected DateTime TestStartTime { get; }


        private void HangDetectionThreadMethod(object state)
        {
            if (!_endingToken.Token.WaitHandle.WaitOne(HangIntervalMs))
            {
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        File.AppendAllText("Test_hang.log", $"- {RunningTestName} (StartedAt: {TestStartTime}, Duration: {(int)(DateTime.Now - TestStartTime).TotalMilliseconds}ms. ProcessorCount: {Environment.ProcessorCount})" + Environment.NewLine);
                        break;
                    }
                    catch { }
                }
            }
        }

        public void Dispose()
        {
            _endingToken.Cancel();

            if (TraceLog)
            {
#pragma warning disable CS0162 // Unreachable code detected
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        File.AppendAllText("Test_trace.log", $"<- {RunningTestName} ({(int)(DateTime.Now - TestStartTime).TotalMilliseconds}ms)" + Environment.NewLine);
                        break;
                    }
                    catch { }
                }
#pragma warning restore CS0162 // Unreachable code detected
            }
        }
    }
}
