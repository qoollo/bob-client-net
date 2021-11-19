using Qoollo.BobClient;
using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.App
{
    class Program
    {
        [Flags]
        enum RunMode
        {
            None = 0,
            Get = 1,
            Put = 2,
            Exists = 4
        }

        class ExecutionConfig
        {
            public RunMode RunMode { get; set; } = RunMode.Get | RunMode.Exists;
            public ulong StartId { get; set; } = 0;
            public ulong? EndId { get; set; } = null;
            public uint Count { get; set; } = 1;
            public bool RandomMode { get; set; } = false;
            public bool Verbose { get; set; } = false;
            public int Timeout { get; set; } = 60;
            public uint ThreadCount { get; set; } = 1;
            public uint ExistsPackageSize { get; set; } = 100;
            public uint KeySize { get; set; } = sizeof(ulong);
            public int? DataLength { get; set; } = null;
            public string DataPatternHex { get; set; } = null;
            public bool ValidateGet { get; set; } = false;
            public string PutFileSourcePattern { get; set; } = null;
            public string GetFileTargetPattern { get; set; } = null;
            public int ProgressIntervalMs { get; set; } = 1000;
            public List<string> Nodes { get; set; } = new List<string>();
        }

        static void PutTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, bool randomWrite, bool verbose, int progressIntervalMs, RecordBytesSource recordBytesSource)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            bool isInitialRun = true;
            Barrier bar = new Barrier((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Put", (int)count))
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
                            if (verbose)
                            {
                                Console.WriteLine($"Error ({currentId}): Data source for key is not found");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.RegisterError();
                        if (verbose)
                        {
                            Console.WriteLine($"Error ({currentId}): {ex.Message}");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                });

                progress.Dispose();
                progress.Print();

                Console.WriteLine($"Put finished in {progress.ElapsedMilliseconds}ms. Rps: {Math.Round((double)(1000 * count) / progress.ElapsedMilliseconds, 2)}");
                if (progress.CurrentErrorCount > 0)
                    Console.WriteLine($"Errors: {progress.CurrentErrorCount}");
                Console.WriteLine();
            }
        }

        static void GetTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, bool randomRead, bool validationMode, bool verbose, int progressIntervalMs, RecordBytesSource recordBytesSource)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Get", (int)count))
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
                            lengthMismatchErrors++;
                            progress.RegisterError();
                            if (verbose)
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
                        keyNotFoundErrors++;
                        progress.RegisterError();
                        if (verbose)
                            Console.WriteLine($"Error ({currentId}): Key not found");
                    }
                    catch (Exception ex)
                    {
                        otherErrors++;
                        progress.RegisterError();
                        if (verbose)
                        {
                            Console.WriteLine($"Error ({currentId}): {ex.Message}");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                });
                progress.Dispose();
                progress.Print();

                Console.WriteLine($"Get finished in {progress.ElapsedMilliseconds}ms. Rps: {Math.Round((double)(1000 * count) / progress.ElapsedMilliseconds, 2)}");
                if (progress.CurrentErrorCount > 0)
                    Console.WriteLine($"KeyNotFound: {keyNotFoundErrors}, LengthMismatch: {lengthMismatchErrors}, OtherErrors: {otherErrors}");
                Console.WriteLine();
            }
        }

        static void ExistsTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, uint packageSize, bool verbose, int progressIntervalMs)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            int expectedRequestsCount = (int)((count - 1) / packageSize) + 1;
            int totalExistedCount = 0;

            bool isInitialRun = true;
            Barrier bar = new Barrier((int)threadCount);

            using (var progress = new ProgressTracker(progressIntervalMs, "Exists", (int)count, () => $"Result: {Volatile.Read(ref totalExistedCount),8}/{count}"))
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
                        if (verbose)
                        {
                            Console.WriteLine($"Error ({ids[0]} - {ids[ids.Length - 1]}): {ex.Message}");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                });

                progress.Dispose();
                progress.Print();

                Console.WriteLine($"Exists finished in {progress.ElapsedMilliseconds}ms. Rps for records: {Math.Round((double)(1000 * count) / progress.ElapsedMilliseconds, 2)}. Rps for packages: {Math.Round((double)(1000 * expectedRequestsCount) / progress.ElapsedMilliseconds, 2)}");
                Console.WriteLine($"Exists result: {totalExistedCount}/{count}");
                if (progress.CurrentErrorCount > 0)
                    Console.WriteLine($"Errors: {progress.CurrentErrorCount}");
                Console.WriteLine();
            }
        }


        static void PrintHelp()
        {
            Console.WriteLine($"Bob client tests (version: v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version})");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --mode   | -m     : Work mode combined by comma. Possible values: 'Get,Put,Exists'. Default: 'Get,Exists'");
            Console.WriteLine("  --length | -l     : Set size of the single record. Default: 1024");
            Console.WriteLine("  --start  | -s     : Start Id. Default: 0");
            Console.WriteLine("  --end    | -e     : End Id (optional)");
            Console.WriteLine("  --count  | -c     : Count of ids to process. Default: 1");
            Console.WriteLine("  --nodes  | -n     : Comma separated node addresses. Example: '127.0.0.1:20000, 127.0.0.2:20000'");
            Console.WriteLine("  --keySize         : Target key size in bytes. Default: 8");
            Console.WriteLine("  --threads         : Number of threads. Default: 1");
            Console.WriteLine("  --timeout         : Operation and connection timeout in seconds. Default: 60");
            Console.WriteLine("  --random          : Random read/write mode. Default: false");
            Console.WriteLine("  --verbose         : Enable verbose output for errors. Default: false");
            Console.WriteLine("  --packageSize     : Exists package size. Default: 100");
            Console.WriteLine("  --validateGet     : Validates data received by Get. Default: false");
            Console.WriteLine("  --hexDataPattern  : Data pattern as hex string (optional)");
            Console.WriteLine("  --putFileSource   : Path to the file with source data. Supports '{key}' as pattern (optional)");
            Console.WriteLine("  --getFileTarget   : Path to the file to store data from Get or to validate. Supports '{key}' as pattern (optional)");
            Console.WriteLine("  --progressPeriod  : Progress printing period in milliseconds. Default: 1000");
            Console.WriteLine("  --help            : Print help");
            Console.WriteLine();
        }

        static ExecutionConfig ParseConfigFromArgs(string[] args)
        {
            var result = new ExecutionConfig();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--mode":
                    case "-m":
                        result.RunMode = (RunMode)Enum.Parse(typeof(RunMode), args[i + 1], ignoreCase: true);
                        i++;
                        break;
                    case "--length":
                    case "-l":
                        result.DataLength = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--start":
                    case "-s":
                        result.StartId = ulong.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--end":
                    case "-e":
                        result.EndId = ulong.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--count":
                    case "-c":
                        result.Count = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--timeout":
                        result.Timeout = int.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--threads":
                        result.ThreadCount = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--packagesize":
                        result.ExistsPackageSize = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--keysize":
                        result.KeySize = uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--random":
                        if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool randomMode))
                        {
                            result.RandomMode = randomMode;
                            i++;
                        }
                        else
                        {
                            result.RandomMode = true;
                        }
                        break;
                    case "--verbose":
                        if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool verboseMode))
                        {
                            result.Verbose = verboseMode;
                            i++;
                        }
                        else
                        {
                            result.Verbose = true;
                        }
                        break;
                    case "--hexdatapattern":
                        result.DataPatternHex = args[i + 1];
                        i++;
                        break;
                    case "--validateget":
                        if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool validateGet))
                        {
                            result.ValidateGet = validateGet;
                            i++;
                        }
                        else
                        {
                            result.ValidateGet = true;
                        }
                        break;
                    case "--putfilesource":
                        result.PutFileSourcePattern = args[i + 1];
                        i++;
                        break;
                    case "--getfiletarget":
                        result.GetFileTargetPattern = args[i + 1];
                        i++;
                        break;
                    case "--progressperiod":
                        result.ProgressIntervalMs = (int)uint.Parse(args[i + 1]);
                        i++;
                        break;
                    case "--nodes":
                    case "-n":
                        result.Nodes = args[i + 1].Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList();
                        i++;
                        break;
                    case "--help":
                        PrintHelp();
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}. Use '--help' to get help");
                        break;
                }
            }

            if (result.DataPatternHex != null && (result.GetFileTargetPattern != null || result.PutFileSourcePattern != null))
                Console.WriteLine("HexDataPattern cannot be combined with GetFileTarget or PutFileSource");

            return result;
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
                    Verbose = false,
                    Timeout = 60,
                    ThreadCount = 4,
                    ProgressIntervalMs = 1000,
                    Nodes = new List<string>() { "10.5.5.127:20000", "10.5.5.128:20000" }
                };
            }
            else if (args.Length > 0)
            {
                config = ParseConfigFromArgs(args);
            }
 
            if (config.Nodes.Count == 0)
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


            using (var client = new BobClusterBuilder<ulong>(config.Nodes)
                .WithOperationTimeout(TimeSpan.FromSeconds(config.Timeout))
                .WithKeySerializer(new CustomSizeUInt64BobKeySerializer((int)config.KeySize))
                .WithSequentialNodeSelectionPolicy()
                .Build())
            {
                client.Open(TimeSpan.FromSeconds(config.Timeout), BobClusterOpenCloseMode.SkipErrors);

                if ((config.RunMode & RunMode.Put) != 0)
                    PutTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.RandomMode, config.Verbose, config.ProgressIntervalMs, putRecordBytesSource);
                if ((config.RunMode & RunMode.Get) != 0)
                    GetTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.RandomMode, config.ValidateGet, config.Verbose, config.ProgressIntervalMs, getRecordBytesSource);
                if ((config.RunMode & RunMode.Exists) != 0)
                    ExistsTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.ExistsPackageSize, config.Verbose, config.ProgressIntervalMs);

                client.Close();
            }

            return 0;
        }
    }
}
