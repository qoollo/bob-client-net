using Qoollo.BobClient;
using Qoollo.BobClient.NodeSelectionPolicies;
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

        static void PutTest(IBobApi<ulong> client, ulong startId, int count, byte[] data)
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

        static void GetTest(IBobApi<ulong> client, ulong startId, int count, byte[] expectedData = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var result = client.Get(startId + (ulong)i, token: default(CancellationToken));
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

        static void ExistsTest(IBobApi<ulong> client, ulong startId, int count)
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
                    var result = client.Exists(ids, token: default(CancellationToken));
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


        static void Main(string[] args)
        {
            RunMode runMode = RunMode.Get | RunMode.Put | RunMode.Exists;
            int dataLength = 1024;
            ulong startId = 30000;
            int count = 1000;
            List<string> nodes = new List<string>() { "10.5.5.127:20000", "10.5.5.128:20000" };



            byte[] sampleData = Enumerable.Range(0, dataLength).Select(o => (byte)(o % byte.MaxValue)).ToArray();

            using (var client = new BobClusterBuilder<ulong>(nodes)
                .WithOperationTimeout(TimeSpan.FromSeconds(1))
                .WithNodeSelectionPolicy(SequentialNodeSelectionPolicy.Factory)
                .Build())
            {
                client.Open(TimeSpan.FromSeconds(5));

                if ((runMode & RunMode.Put) != 0)
                    PutTest(client, startId, count, sampleData);
                if ((runMode & RunMode.Get) != 0)
                    GetTest(client, startId, count, expectedData: sampleData);
                if ((runMode & RunMode.Exists) != 0)
                    ExistsTest(client, startId, count);

                client.Close();
            }
        }
    }
}
