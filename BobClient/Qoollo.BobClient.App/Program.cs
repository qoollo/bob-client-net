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
            public int? DataLength { get; set; } = null;
            public ulong StartId { get; set; } = 0;
            public ulong? EndId { get; set; } = null;
            public uint Count { get; set; } = 1000;
            public bool RandomMode { get; set; } = false;
            public bool Verbose { get; set; } = false;
            public int Timeout { get; set; } = 60;
            public uint ThreadCount { get; set; } = 1;
            public uint ExistsPackageSize { get; set; } = 100;
            public List<string> Nodes { get; set; } = new List<string>();
        }

        static void PutTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, bool randomWrite, bool verbose, byte[] data)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            using (var progress = new ProgressTracker(1000, "Put", (int)count).Start())
            {
                //for (int i = 0; i < count; i++)
                Parallel.For(0, (int)count, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (int i) =>
                {
                    ulong currentId = startId + (ulong)i;
                    if (randomWrite)
                        currentId = startId + (ulong)random.Next(i, maxValue: (int)(endId - startId));

                    try
                    {
                        client.Put(currentId, data, default(CancellationToken));
                        progress.RegisterSuccess();
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

        static void GetTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, bool randomRead, bool verbose, int? expectedLength = null)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            ParallelRandom random = new ParallelRandom((int)threadCount);

            using (var progress = new ProgressTracker(1000, "Get", (int)count).Start())
            {
                int keyNotFoundErrors = 0;
                int lengthMismatchErrors = 0;
                int otherErrors = 0;

                //for (uint i = 0; i < count; i++)
                Parallel.For(0, (int)count, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (int i) =>
                {
                    ulong currentId = startId + (ulong)i;
                    if (randomRead)
                        currentId = startId + (ulong)random.Next(i, maxValue: (int)(endId - startId));

                    try
                    {
                        var result = client.Get(currentId, token: default(CancellationToken));
                        if (expectedLength != null && expectedLength.Value != result.Length)
                        {
                            lengthMismatchErrors++;
                            progress.RegisterError();
                            if (verbose)
                                Console.WriteLine($"Error ({currentId}): Length mismatch. Expected length: {expectedLength.Value}, actual: {result.Length}");
                        }
                        else
                        {
                            progress.RegisterSuccess();
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

        static void ExistsTest(IBobApi<ulong> client, ulong startId, ulong endId, uint count, uint threadCount, uint packageSize, bool verbose)
        {
            if (endId < startId || count > endId - startId)
                endId = startId + count;

            int expectedRequestsCount = (int)((count - 1) / packageSize) + 1;
            int totalExistedCount = 0;

            using (var progress = new ProgressTracker(1000, "Exists", (int)count, () => $"Result: {Volatile.Read(ref totalExistedCount),8}/{count}").Start())
            {
                //for (int pckgNum = 0; pckgNum < expectedRequestsCount; pckgNum++)
                Parallel.For(0, expectedRequestsCount, new ParallelOptions() { MaxDegreeOfParallelism = (int)threadCount },
                (int pckgNum) =>
                {
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
            Console.WriteLine("Bob client tests");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --mode   | -m  : Work mode combined by comma. Possible values: 'Get,Put,Exists'. Default: 'Get,Exists'");
            Console.WriteLine("  --length | -l  : Set size of the single record. Default: 1024");
            Console.WriteLine("  --start  | -s  : Start Id. Default: 0");
            Console.WriteLine("  --end    | -e  : End Id (optional)");
            Console.WriteLine("  --count  | -c  : Count of ids to process. Default: 1000");
            Console.WriteLine("  --threads      : Number of threads. Default: 1");
            Console.WriteLine("  --timeout      : Timeout in seconds. Default: 60");
            Console.WriteLine("  --random       : Random read/write mode. Default: false");
            Console.WriteLine("  --verbose      : Enable verbose output for errors. Default: false");
            Console.WriteLine("  --packageSize  : Exists package size. Default: 100");
            Console.WriteLine("  --nodes        : Comma separated node addresses. Example: '127.0.0.1:20000, 127.0.0.2:20000'");
            Console.WriteLine("  --help         : Print help");
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
                    case "--nodes":
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

            return result;
        }

        static void Main(string[] args)
        {
            ExecutionConfig config = new ExecutionConfig()
            {
                RunMode = RunMode.Get | RunMode.Put | RunMode.Exists,
                DataLength = 1024,
                StartId = 62000,
                EndId = null,
                Count = 20000,
                ExistsPackageSize = 100,
                RandomMode = false,
                Verbose = true,
                Timeout = 60,
                ThreadCount = 4,
                Nodes = new List<string>() { "10.5.5.127:20000", "10.5.5.128:20000" }
            };


            if (args.Length > 0)
                config = ParseConfigFromArgs(args);
 
            if (config.Nodes.Count == 0)
            {
                Console.WriteLine("Node addresses not specified");
                return;
            }

            byte[] sampleData = null;

            if ((config.RunMode & RunMode.Put) != 0)
            {
                sampleData = new byte[config.DataLength ?? 1024];
                for (int i = 0; i < sampleData.Length; i++)
                    sampleData[i] = (byte)(i & byte.MaxValue);
            }


            using (var client = new BobClusterBuilder<ulong>(config.Nodes)
                .WithOperationTimeout(TimeSpan.FromSeconds(config.Timeout))
                .WithSequentialNodeSelectionPolicy()
                .Build())
            {
                client.Open(TimeSpan.FromSeconds(config.Timeout), BobClusterOpenCloseMode.SkipErrors);

                if ((config.RunMode & RunMode.Put) != 0)
                    PutTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.RandomMode, config.Verbose, sampleData);
                if ((config.RunMode & RunMode.Get) != 0)
                    GetTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.RandomMode, config.Verbose, config.DataLength);
                if ((config.RunMode & RunMode.Exists) != 0)
                    ExistsTest(client, config.StartId, config.EndId ?? (config.StartId + config.Count), config.Count, config.ThreadCount, config.ExistsPackageSize, config.Verbose);

                client.Close();
            }
        }
    }
}
