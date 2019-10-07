using System;
using System.Linq;
using System.Threading;

namespace BobClient.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new BobBuilder(new[] { new Node("192.168.1.21:20000") }.ToList())
                    .WithTimeout(TimeSpan.FromSeconds(1))
                    .Build();

            ulong id = 1;
            while  (true) {
                var result = client.Put(id++, new byte[0], new System.Threading.CancellationToken());
                Console.WriteLine(result);

                Thread.Sleep(2000);
            }
        }
    }
}
