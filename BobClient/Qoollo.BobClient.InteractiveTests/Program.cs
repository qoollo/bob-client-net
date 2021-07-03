using Qoollo.BobClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.InteractiveTests
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
            public int DataLength { get; set; } = 1024;
            public ulong StartId { get; set; } = 0;
            public int Count { get; set; } = 1000;
            public List<string> Nodes { get; set; } = new List<string>();
        }

        static void PutTest(IBobApi client, ulong startId, int count, byte[] data)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    client.Put(startId + (ulong)i, data, default(CancellationToken));
                    if (i % 100 == 0)
                        Console.WriteLine($"Put {startId + (ulong)i}: Ok");
                }
                catch
                {
                    Console.WriteLine($"Put {startId + (ulong)i}: Error");
                }
            }

            Console.WriteLine($"Put finished in {sw.ElapsedMilliseconds}ms. Rps: {(double)(1000 * count) / sw.ElapsedMilliseconds}");
        }

        static void GetTest(IBobApi client, ulong startId, int count, byte[] expectedData = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var result = client.Get(startId + (ulong)i, fullGet: false, token: default(CancellationToken));
                    if (expectedData != null && result.Length != expectedData.Length)
                        Console.WriteLine("Result length mismatch");
                    if (i % 100 == 0)
                        Console.WriteLine($"Get {startId + (ulong)i}: Ok");
                }
                catch (BobKeyNotFoundException)
                {
                    Console.WriteLine($"Get {startId + (ulong)i}: Key not found");
                }
                catch
                {
                    Console.WriteLine($"Get {startId + (ulong)i}: Error");
                }
            }

            Console.WriteLine($"Get finished in {sw.ElapsedMilliseconds}ms. Rps: {(double)(1000 * count) / sw.ElapsedMilliseconds}");
        }

        static void ExistsTest(IBobApi client, ulong startId, int count)
        {
            const int packageSize = 100;

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i += packageSize)
            {
                ulong[] ids = new ulong[Math.Min(packageSize, count - i)];
                for (int j = 0; j < ids.Length; j++)
                    ids[j] = startId + (ulong)i + (ulong)j;

                try
                {
                    var result = client.Exists(ids, fullGet: false, token: default(CancellationToken));
                    int existedCount = result.Count(o => o == true);
                    Console.WriteLine($"Exists {startId + (ulong)i} - {startId + (ulong)i + (ulong)ids.Length}: {existedCount}/{ids.Length}");
                }
                catch
                {
                    Console.WriteLine($"Exists {startId + (ulong)i} - {startId + (ulong)i + (ulong)ids.Length}: Error");
                }
            }

            Console.WriteLine($"Exists finished in {sw.ElapsedMilliseconds}ms. Rps: {(double)(1000 * count) / sw.ElapsedMilliseconds}");
        }


        static void PrintHelp()
        {
            Console.WriteLine("Bob client tests");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --mode   | -m  : Work mode combined by comma. Possible values: 'Get,Put,Exists'");
            Console.WriteLine("  --length | -l  : Set size of the single record");
            Console.WriteLine("  --start  | -s  : Start Id");
            Console.WriteLine("  --count  | -c  : Count of ids to process");
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
                        result.RunMode = (RunMode)Enum.Parse(typeof(RunMode), args[i + 1]);
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
                    case "--count":
                    case "-c":
                        result.Count = int.Parse(args[i + 1]);
                        i++;
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

        static BobClusterBuilder CreateClusterBuilder(IEnumerable<string> nodes)
        {
            BobClusterBuilder result = new BobClusterBuilder();
            foreach (var node in nodes)
                result.WithAdditionalNode(node);

            return result;
        }

        static void Main(string[] args)
        {
            ExecutionConfig config = new ExecutionConfig()
            {
                RunMode = RunMode.Get | RunMode.Put | RunMode.Exists,
                DataLength = 1024,
                StartId = 30000,
                Count = 1000,
                Nodes = new List<string>() { "10.5.5.127:20000", "10.5.5.128:20000" }
            };


            if (args.Length > 0)
                config = ParseConfigFromArgs(args);

            if (config.Nodes.Count == 0)
            {
                Console.WriteLine("Node addresses not specified");
                return;
            }

            byte[] sampleData = Enumerable.Range(0, config.DataLength).Select(o => (byte)(o % byte.MaxValue)).ToArray();

            using (var client = CreateClusterBuilder(config.Nodes)
                .WithOperationTimeout(TimeSpan.FromSeconds(10))
                .WithNodeSelectionPolicy(new SequentialNodeSelectionPolicy())
                .Build())
            {
                client.Open(TimeSpan.FromSeconds(10));

                if ((config.RunMode & RunMode.Put) != 0)
                    PutTest(client, config.StartId, config.Count, sampleData);
                if ((config.RunMode & RunMode.Get) != 0)
                    GetTest(client, config.StartId, config.Count, expectedData: sampleData);
                if ((config.RunMode & RunMode.Exists) != 0)
                    ExistsTest(client, config.StartId, config.Count);

                client.Close();
            }
        }
    }
}
