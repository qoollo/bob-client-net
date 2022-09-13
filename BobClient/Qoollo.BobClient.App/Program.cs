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
        enum ErrorStatus
        {
            NoErrors = 0,
            WithErrors = 1
        }


        static string RoundToStr(double val)
        {
            return Math.Round(val, 2).ToString(CultureInfo.InvariantCulture);
        }

        static ErrorStatus PutTest(IBobApi<ulong> client, IKeySource keySource, RecordBytesSource recordBytesSource, uint threadCount, VerbosityLevel verbosity, int progressIntervalMs)
        {
            if (threadCount > keySource.Count)
                threadCount = (uint)keySource.Count;

            bool isInitialRun = true;
            Barrier bar = new Barrier((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Put", keySource.Count, autoPrintMsg: verbosity != VerbosityLevel.Min))
            {
                Parallel.ForEach(keySource, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (ulong currentId) =>
                {
                    if (isInitialRun)
                    {
                        bar.SignalAndWait();
                        progress.Start();
                        isInitialRun = false;
                    }

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

                return stat.ErrorCount > 0 ? ErrorStatus.WithErrors : ErrorStatus.NoErrors;
            }
        }

        static ErrorStatus GetTest(IBobApi<ulong> client, IKeySource keySource, RecordBytesSource recordBytesSource, uint threadCount, bool validationMode, VerbosityLevel verbosity, int progressIntervalMs)
        {
            if (threadCount > keySource.Count)
                threadCount = (uint)keySource.Count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Get", keySource.Count, autoPrintMsg: verbosity != VerbosityLevel.Min))
            {
                int keyNotFoundErrors = 0;
                int lengthMismatchErrors = 0;
                int otherErrors = 0;

                bool isInitialRun = true;
                Barrier bar = new Barrier((int)threadCount);

                Parallel.ForEach(keySource, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (ulong currentId) =>
                {
                    if (isInitialRun)
                    {
                        bar.SignalAndWait();
                        progress.Start();
                        isInitialRun = false;
                    }

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
                    Console.WriteLine($"Errors: KeyNotFound: {Volatile.Read(ref keyNotFoundErrors)}, LengthMismatch: {Volatile.Read(ref lengthMismatchErrors)}, OtherErrors: {Volatile.Read(ref otherErrors)}");
                Console.WriteLine();

                return stat.ErrorCount > 0 ? ErrorStatus.WithErrors : ErrorStatus.NoErrors;
            }
        }

        static ErrorStatus ExistsTest(IBobApi<ulong> client, IKeySource keySource, uint packageSize, uint threadCount, VerbosityLevel verbosity, int progressIntervalMs)
        {
            var packageSource = new KeyPackageAggregator(keySource, (int)packageSize);

            if (threadCount > packageSource.PackageCount)
                threadCount = (uint)packageSource.PackageCount;

            bool isInitialRun = true;
            Barrier bar = new Barrier((int)threadCount);

            int totalExistedCount = 0;

            using (var progress = new ProgressTracker(progressIntervalMs, "Exists", packageSource.KeyCount, autoPrintMsg: verbosity != VerbosityLevel.Min, customMessageBuilder: () => $"Result: {Volatile.Read(ref totalExistedCount),8}/{packageSource.KeyCount}"))
            {
                Parallel.ForEach(packageSource, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (ulong[] ids) =>
                {
                    if (isInitialRun)
                    {
                        bar.SignalAndWait();
                        progress.Start();
                        isInitialRun = false;
                    }

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
                Console.WriteLine($"Exists finished in {stat.ElapsedMilliseconds}ms. RpsAvg: {RoundToStr(stat.RpsAvg)}, RpsDev: {RoundToStr(stat.RpsDev)}, RpsMedian: {RoundToStr(stat.RpsMedian)}, Rps10P: {RoundToStr(stat.Rps10P)}, Rps90P: {RoundToStr(stat.Rps90P)}, RpsMin: {RoundToStr(stat.RpsMin)}, RpsMax: {RoundToStr(stat.RpsMax)}, Packages Per Second: {RoundToStr((double)(1000 * packageSource.PackageCount) / stat.ElapsedMilliseconds)}");
                Console.WriteLine($"Exists result: {totalExistedCount}/{packageSource.KeyCount}");
                if (stat.ErrorCount > 0)
                    Console.WriteLine($"Errors: {stat.ErrorCount}");
                Console.WriteLine();

                return stat.ErrorCount > 0 ? ErrorStatus.WithErrors : ErrorStatus.NoErrors;
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
                    DataLength = ByteSizeLib.ByteSize.FromBytes(1024),
                    DataPatternHex = null,
                    ValidateGet = false,
                    GetFileTargetPattern = null,
                    PutFileSourcePattern = null,
                    Keys = new KeyList(KeyRange.CreateWithCount(start: 10000, count: 20000)),
                    ExistsPackageSize = 100,
                    KeySize = sizeof(ulong),
                    RandomCount = null,
                    Verbosisty = VerbosityLevel.Normal,
                    Timeout = 60,
                    ThreadCount = 4,
                    ProgressPeriodMs = 1000,
                    Nodes = new List<string>() { "10.5.5.127:20000", "10.5.5.128:20000" }
                };
            }
            else
            {
                config = CommandLineParametersParser.ParseConfigFromArgs(args);
            }

            if (config == null)
            {
                return -1;
            }
 

            RecordBytesSource putRecordBytesSource = new NopRecordBytesSource();
            RecordBytesSource getRecordBytesSource = new NopRecordBytesSource();

            try
            {
                if ((config.RunMode & RunMode.Put) != 0)
                {
                    if (!string.IsNullOrEmpty(config.DataPatternHex) && config.DataLength != null)
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex, (int)config.DataLength.Value.Bytes);
                    else if (!string.IsNullOrEmpty(config.DataPatternHex))
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex);
                    else if (!string.IsNullOrEmpty(config.PutFileSourcePattern))
                        putRecordBytesSource = new FileRecordBytesSource(config.PutFileSourcePattern, disableStore: true);
                    else if (config.DataLength != null)
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize((int)config.DataLength.Value.Bytes);
                    else
                        putRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize(1024);
                }

                if ((config.RunMode & RunMode.Get) != 0 && (config.ValidateGet || !string.IsNullOrEmpty(config.GetFileTargetPattern)))
                {
                    if (!string.IsNullOrEmpty(config.DataPatternHex) && config.DataLength != null)
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex, (int)config.DataLength.Value.Bytes);
                    else if (!string.IsNullOrEmpty(config.DataPatternHex))
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateFromHexPattern(config.DataPatternHex);
                    else if (!string.IsNullOrEmpty(config.GetFileTargetPattern) && config.ValidateGet)
                        getRecordBytesSource = new FileRecordBytesSource(config.GetFileTargetPattern, disableStore: true);
                    else if (!string.IsNullOrEmpty(config.GetFileTargetPattern))
                        getRecordBytesSource = new FileRecordBytesSource(config.GetFileTargetPattern, disableStore: false);
                    else if (config.DataLength != null)
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize((int)config.DataLength.Value.Bytes);
                    else
                        getRecordBytesSource = PredefinedArrayRecordBytesSource.CreateDefaultWithSize(1024);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }

            IKeySource keySource = config.Keys;
            if (config.RandomCount != null)
            {
                int randomCount = (int)config.RandomCount.Value;
                if (randomCount == 0)
                    randomCount = config.Keys.Count;

                keySource = new RandomizedKeySource(config.Keys, randomCount);
            }


            ThreadPool.GetMinThreads(out int workerThreadsMin, out int completionPortThreadsMin);
            ThreadPool.GetMaxThreads(out int workerThreadsMax, out _);
            ThreadPool.SetMinThreads(Math.Min(Math.Max(workerThreadsMin, (int)config.ThreadCount + 4), workerThreadsMax), completionPortThreadsMin);

            ErrorStatus errorStatus = ErrorStatus.NoErrors;

            using (var client = new BobClusterBuilder<ulong>(config.Nodes)
                .WithOperationTimeout(TimeSpan.FromSeconds(config.Timeout))
                .WithConnectionTimeout(TimeSpan.FromSeconds(config.Timeout))
                .WithKeySerializer(new CustomSizeUInt64BobKeySerializer((int)config.KeySize))
                .WithSequentialNodeSelectionPolicy()
                .Build())
            {
                try
                {
                    client.Open(BobClusterOpenCloseMode.ThrowOnFirstError);
                }
                catch (BobOperationException ex)
                {
                    Console.WriteLine($"Error opening connection: {ex.Message}");
                    return -2;
                }

                if ((config.RunMode & RunMode.Put) != 0)
                    errorStatus |= PutTest(client, keySource, putRecordBytesSource, config.ThreadCount, config.Verbosisty, config.ProgressPeriodMs);
                if ((config.RunMode & RunMode.Get) != 0)
                    errorStatus |= GetTest(client, keySource, getRecordBytesSource, config.ThreadCount, config.ValidateGet, config.Verbosisty, config.ProgressPeriodMs);
                if ((config.RunMode & RunMode.Exists) != 0)
                    errorStatus |= ExistsTest(client, keySource, config.ThreadCount, config.ExistsPackageSize, config.Verbosisty, config.ProgressPeriodMs);

                client.Close();
            }

            return errorStatus == ErrorStatus.NoErrors ? 0 : -2;
        }
    }
}
