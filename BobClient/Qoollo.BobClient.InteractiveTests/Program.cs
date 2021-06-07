using Qoollo.BobClient;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient.InteractiveTests
{
    class Program
    {
        private static readonly byte[] _sampleData = new byte[] { 0, 1, 2, 3 };

        static void PutTest(IBobApi client, ulong startId, int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    client.Put(startId + (ulong)i, _sampleData, default(CancellationToken));
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
                var result = client.Get(startId + (ulong)i, fullGet: false, token: default(CancellationToken));
                if (result.Length != _sampleData.Length)
                    Console.WriteLine("Result length mismatch");
                if (i % 100 == 0)
                    Console.WriteLine($"Get {startId + (ulong)i}: Ok");
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

                var result = client.Exists(ids, fullGet: false, token: default(CancellationToken));
                if (result.Any(o => o == false))
                    Console.WriteLine("Some id is not exist");

                Console.WriteLine($"Exists {startId + (ulong)i}");
            }

            Console.WriteLine($"Exists finished in {sw.ElapsedMilliseconds}ms. Rps: {(double)(1000 * count) / sw.ElapsedMilliseconds}");
        }



        static void Main(string[] args)
        {
            //using (var client = new BobClusterBuilder()
            //    .WithAdditionalNode("http://10.5.5.127:20000")
            //    .WithAdditionalNode("http://10.5.5.128:20000")
            //    .WithOperationTimeout(TimeSpan.FromSeconds(1))
            //    .Build())
            using (var client = new BobNodeClient("10.5.5.127:20000", TimeSpan.FromSeconds(10)))
            {
                client.Open();

                PutTest(client, 8000, 1000);
                GetTest(client, 8000, 1000);
                ExistsTest(client, 8000, 1000);

                client.Close();
            }
        }
    }
}
