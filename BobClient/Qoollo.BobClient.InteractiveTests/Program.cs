using Qoollo.BobClient;
using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.InteractiveTests
{
    class Program
    {
        private static readonly byte[] _sampleData = Enumerable.Range(0, 1024).Select(o => (byte)(o % byte.MaxValue)).ToArray();

        static void PutTest(IBobApi client, ulong startId, int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    client.Put(BobKey.FromUInt64(startId + (ulong)i), _sampleData, default(CancellationToken));
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

        static void GetTest(IBobApi client, ulong startId, int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var result = client.Get(BobKey.FromUInt64(startId + (ulong)i), token: default(CancellationToken));
                    if (result.Length != _sampleData.Length)
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
                BobKey[] ids = new BobKey[Math.Min(packageSize, count - i)];
                for (int j = 0; j < ids.Length; j++)
                    ids[j] = BobKey.FromUInt64(startId + (ulong)i + (ulong)j);

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
            using (var client = new BobClusterBuilder()
                .WithAdditionalNode("10.5.5.127:20000")
                .WithAdditionalNode("10.5.5.128:20000")
                .WithOperationTimeout(TimeSpan.FromSeconds(1))
                .WithNodeSelectionPolicy(SequentialNodeSelectionPolicy.Factory)
                .Build())
            //using (var client = new BobNodeClient("10.5.5.127:20000", TimeSpan.FromSeconds(10)))
            {
                client.Open(TimeSpan.FromSeconds(5));

                PutTest(client, 10000, 1000);
                GetTest(client, 10000, 1000);
                ExistsTest(client, 10000, 1000);

                client.Close();
            }
        }
    }
}
