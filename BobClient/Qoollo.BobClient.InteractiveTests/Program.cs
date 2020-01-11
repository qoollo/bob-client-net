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

        static void PutTest(BobClusterClient client, ulong startId, int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                client.Put(startId, _sampleData);
                Console.WriteLine($"Put: OK");
            }

            Console.WriteLine($"Put finished in {sw.ElapsedMilliseconds}ms. Rps: {(double)(1000 * count) / sw.ElapsedMilliseconds}");
        }

        static void GetTest(BobClusterClient client, ulong startId, int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                var result = client.Get(startId);
                Console.WriteLine($"Get: {result.Length}");
            }

            Console.WriteLine($"Get finished in {sw.ElapsedMilliseconds}ms. Rps: {(double)(1000 * count) / sw.ElapsedMilliseconds}");
        }
        

        static void Main(string[] args)
        {
            using (var client = new BobClusterBuilder()
                .WithAdditionalNode("10.5.5.131:20000")
                .WithAdditionalNode("10.5.5.132:20000")
                .WithOperationTimeout(TimeSpan.FromSeconds(1))
                .Build())
            {
                client.Open();

                PutTest(client, 8000, 1000);
                GetTest(client, 8000, 1000);

                client.Close();
            }
        }
    }
}
