using Qoollo.BobClient;
using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.App
{
    class Program
    {
        static string RoundToStr(double val)
        {
            return Math.Round(val, 2).ToString(CultureInfo.InvariantCulture);
        }

        static void PutTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, bool randomWrite, VerbosityLevel verbosity, int progressIntervalMs, RecordBytesSource recordBytesSource)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            if (threadCount > count)
                threadCount = count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            bool isInitialRun = true;
            Barrier bar = new Barrier((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Put", (int)count, autoPrintMsg: verbosity != VerbosityLevel.Min))
            {
                Parallel.For(0, (int)count, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (int i) =>
                {
                    if (isInitialRun)
                    {
                        bar.SignalAndWait();
                        progress.Start();
                        isInitialRun = false;
                    }

                    ulong currentId = startId + (ulong)i;
                    if (randomWrite)
                        currentId = startId + (ulong)random.Next(i, maxValue: (int)(endId - startId));

                    try
                    {
                        if (recordBytesSource.TryGetData(currentId, out byte[] curData))
                        {
                            client.Put(currentId, curData, default(CancellationToken));
                            progress.RegisterSuccess();
                        }
                        else
                        {
                            progress.RegisterError();
                            if (verbosity == VerbosityLevel.Max)
                            {
                                Console.WriteLine($"Error ({currentId}): Data source for key is not found");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.RegisterError();
                        if (verbosity == VerbosityLevel.Max)
                        {
                            Console.WriteLine($"Error ({currentId}): {ex.Message}");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                });

                progress.Dispose();
                if (verbosity != VerbosityLevel.Min)
                    progress.Print();

                var stat = progress.GetProgressStats();
                Console.WriteLine($"Put finished in {stat.ElapsedMilliseconds}ms. RpsAvg: {RoundToStr(stat.RpsAvg)}, RpsDev: {RoundToStr(stat.RpsDev)}, RpsMedian: {RoundToStr(stat.RpsMedian)}, Rps10P: {RoundToStr(stat.Rps10P)}, Rps90P: {RoundToStr(stat.Rps90P)}, RpsMin: {RoundToStr(stat.RpsMin)}, RpsMax: {RoundToStr(stat.RpsMax)}");
                if (stat.ErrorCount > 0)
                    Console.WriteLine($"Errors: {stat.ErrorCount}");
                Console.WriteLine();
            }
        }

        static void GetTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, bool randomRead, bool validationMode, VerbosityLevel verbosity, int progressIntervalMs, RecordBytesSource recordBytesSource)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            if (threadCount > count)
                threadCount = count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Get", (int)count, autoPrintMsg: verbosity != VerbosityLevel.Min))
            {
                int keyNotFoundErrors = 0;
                int lengthMismatchErrors = 0;
                int otherErrors = 0;

                bool isInitialRun = true;
                Barrier bar = new Barrier((int)threadCount);

                Parallel.For(0, (int)count, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (int i) =>
                {
                    if (isInitialRun)
                    {
                        bar.SignalAndWait();
                        progress.Start();
                        isInitialRun = false;
                    }

                    ulong currentId = startId + (ulong)i;
                    if (randomRead)
                        currentId = startId + (ulong)random.Next(i, maxValue: (int)(endId - startId));

                    try
                    {
                        var result = client.Get(currentId, token: default(CancellationToken));
                        if (validationMode && !recordBytesSource.VerifyData(currentId, result))
                        {
                            Interlocked.Increment(ref lengthMismatchErrors);
                            progress.RegisterError();
                            if (verbosity == VerbosityLevel.Max)
                                Console.WriteLine($"Error ({currentId}): Data mismatch");
                        }
                        else
                        {
                            progress.RegisterSuccess();
                            if (!validationMode)
                                recordBytesSource.StoreData(currentId, result);
                        }
                    }
                    catch (BobKeyNotFoundException)
                    {
                        Interlocked.Increment(ref keyNotFoundErrors);
                        progress.RegisterError();
                        if (verbosity == VerbosityLevel.Max)
                            Console.WriteLine($"Error ({currentId}): Key not found");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref otherErrors);
                        progress.RegisterError();
                        if (verbosity == VerbosityLevel.Max)
                        {
                            Console.WriteLine($"Error ({currentId}): {ex.Message}");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                });
                progress.Dispose();
                if (verbosity != VerbosityLevel.Min)
                    progress.Print();

                var stat = progress.GetProgressStats();
                Console.WriteLine($"Get finished in {stat.ElapsedMilliseconds}ms. RpsAvg: {RoundToStr(stat.RpsAvg)}, RpsDev: {RoundToStr(stat.RpsDev)}, RpsMedian: {RoundToStr(stat.RpsMedian)}, Rps10P: {RoundToStr(stat.Rps10P)}, Rps90P: {RoundToStr(stat.Rps90P)}, RpsMin: {RoundToStr(stat.RpsMin)}, RpsMax: {RoundToStr(stat.RpsMax)}");
                if (stat.ErrorCount > 0)
                    Console.WriteLine($"KeyNotFound: {Volatile.Read(ref keyNotFoundErrors)}, LengthMismatch: {Volatile.Read(ref lengthMismatchErrors)}, OtherErrors: {Volatile.Read(ref otherErrors)}");
                Console.WriteLine();
            }
        }

        static void ExistsTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, uint packageSize, VerbosityLevel verbosity, int progressIntervalMs)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            int expectedRequestsCount = (int)((count - 1) / packageSize) + 1;
            int totalExistedCount = 0;

            if (threadCount > expectedRequestsCount)
                threadCount = (uint)expectedRequestsCount;

            bool isInitialRun = true;
            Barrier bar = new Barrier((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Exists", (int)count, autoPrintMsg: verbosity != VerbosityLevel.Min, customMessageBuilder: () => $"Result: {Volatile.Read(ref totalExistedCount),8}/{count}"))
            {
                Parallel.For(0, expectedRequestsCount, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (int pckgNum) =>
                {
                    if (isInitialRun)
                    {
                        bar.SignalAndWait();
                        progress.Start();
                        isInitialRun = false;
                    }

                    int i = (int)(pckgNum * packageSize);
                    ulong[] ids = new ulong[Math.Min(packageSize, count - i)];
                    for (int j = 0; j < ids.Length; j++)
                        ids[j] = startId + (ulong)i + (ulong)j;

                    try
                    {
                        var result = client.Exists(ids, token: default(CancellationToken));
                        int existedCount = result.Count(o => o == true);
                        Interlocked.Add(ref totalExistedCount, existedCount);
                        progress.RegisterEvents(ids.Length, isError: false);
                    }
                    catch (Exception ex)
                    {
                        progress.RegisterEvents(ids.Length, isError: true);
                        if (verbosity == VerbosityLevel.Max)
                        {
                            Console.WriteLine($"Error ({ids[0]} - {ids[ids.Length - 1]}): {ex.Message}");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                });

                progress.Dispose();
                if (verbosity != VerbosityLevel.Min)
                    progress.Print();

                var stat = progress.GetProgressStats();
                Console.WriteLine($"Exists finished in {stat.ElapsedMilliseconds}ms. RpsAvg: {RoundToStr(stat.RpsAvg)}, RpsDev: {RoundToStr(stat.RpsDev)}, RpsMedian: {RoundToStr(stat.RpsMedian)}, Rps10P: {RoundToStr(stat.Rps10P)}, Rps90P: {RoundToStr(stat.Rps90P)}, RpsMin: {RoundToStr(stat.RpsMin)}, RpsMax: {RoundToStr(stat.RpsMax)}, Packages Per Second: {RoundToStr((double)(1000 * expectedRequestsCount) / stat.ElapsedMilliseconds)}");
                Console.WriteLine($"Exists result: {totalExistedCount}/{count}");
                if (stat.ErrorCount > 0)
                    Console.WriteLine($"Errors: {stat.ErrorCount}");
                Console.WriteLine();
            }
        }

        static int Main(string[] args)
        {
            ExecutionConfig config = new ExecutionConfig();

            if (args.Length == 1 && args[0] == ":test")
            {
                config = new ExecutionConfig()
                {
                    RunMode = RunMode.Get | RunMode.Put | RunMode.Exists,
                    DataLength = 1024,
                    DataPatternHex = null,
                    ValidateGet = false,
                    GetFileTargetPattern = null,
                    PutFileSourcePattern = null,
                    StartId = 62000,
                    EndId = null,
                    Count = 20000,
                    ExistsPackageSize = 100,
                    KeySize = sizeof(ulong),
                    RandomMode = true,
                    Verbosisty = VerbosityLevel.Normal,
                    Timeout = 60,
                    ThreadCount = 4,
                    ProgressIntervalMs = 1000,
                    Nodes = new List<string>() { "10.5.5.127:20000", "10.5.5.128:20000" }
                };
            }
            else
            {
                config = CommandLineParametersParser.ParseConfigFromArgsCmdParser(args);
            }
 
            if (config.Nodes.Count() == 0)
            {
                Console.WriteLine("Node addresses not specified");
                return -1;
            }

            RecordBytesSource putRecordBytesSource = new NopRecordBytesSource();
            RecordBytesSource getRecordBytesSource = new NopRecordBytesSource();

            try
            {
                if ((config.RunMode & RunMode.Put) != 0)
                {
                    if (!string.IsNullOrEmpty(config.DataPatternHex) && config.DataLength != null)
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex, config.DataLength.Value);
                    else if (!string.IsNullOrEmpty(config.DataPatternHex))
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex);
                    else if (!string.IsNullOrEmpty(config.PutFileSourcePattern))
                        putRecordBytesSource = new FileRecordBytesSource(config.PutFileSourcePattern, disableStore: true);
                    else if (config.DataLength != null)
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize(config.DataLength.Value);
                    else
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize(1024);
                }

                if ((config.RunMode & RunMode.Get) != 0 && (config.ValidateGet || !string.IsNullOrEmpty(config.GetFileTargetPattern)))
                {
                    if (!string.IsNullOrEmpty(config.DataPatternHex) && config.DataLength != null)
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex, config.DataLength.Value);
                    else if (!string.IsNullOrEmpty(config.DataPatternHex))
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex);
                    else if (!string.IsNullOrEmpty(config.GetFileTargetPattern) && config.ValidateGet)
                        getRecordBytesSource = new FileRecordBytesSource(config.GetFileTargetPattern, disableStore: true);
                    else if (!string.IsNullOrEmpty(config.GetFileTargetPattern))
                        getRecordBytesSource = new FileRecordBytesSource(config.GetFileTargetPattern, disableStore: false);
                    else if (config.DataLength != null)
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize(config.DataLength.Value);
                    else
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize(1024);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }

            ThreadPool.GetMinThreads(out int workerThreadsMin, out int completionPortThreadsMin);
            ThreadPool.GetMaxThreads(out int workerThreadsMax, out _);
            ThreadPool.SetMinThreads(Math.Min(Math.Max(workerThreadsMin, (int)config.ThreadCount + 4), workerThreadsMax), completionPortThreadsMin);

            using (var client = new BobClusterBuilder<ulong>(config.Nodes)
                .WithOperationTimeout(TimeSpan.FromSeconds(config.Timeout))
                .WithConnectionTimeout(TimeSpan.FromSeconds(config.Timeout))
                .WithKeySerializer(new CustomSizeUInt64BobKeySerializer((int)config.KeySize))
                .WithSequentialNodeSelectionPolicy()
                .Build())
            {
                client.Open(BobClusterOpenCloseMode.ThrowOnFirstError);

                if ((config.RunMode & RunMode.Put) != 0)
                    PutTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.RandomMode, config.Verbosisty, config.ProgressIntervalMs, putRecordBytesSource);
                if ((config.RunMode & RunMode.Get) != 0)
                    GetTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.RandomMode, config.ValidateGet, config.Verbosisty, config.ProgressIntervalMs, getRecordBytesSource);
                if ((config.RunMode & RunMode.Exists) != 0)
                    ExistsTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.ExistsPackageSize, config.Verbosisty, config.ProgressIntervalMs);

                client.Close();
            }

            return 0;
        }
    }
}
