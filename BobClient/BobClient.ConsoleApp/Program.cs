using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BobClient.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new BobBuilder(new[] { new Node("10.5.5.124:20000") }.ToList())
                    .WithTimeout(TimeSpan.FromSeconds(1))
                    .Build();

//            ulong id = 1;
            while  (true) {
                //                var result = client.Put(id, new byte[0], new System.Threading.CancellationToken());
                //
                //                //byte[] data;
                //                //var result = client.Get(id++, out data);
                //                Console.WriteLine(result);


                Task.WaitAll(client.GetAsync(80900015600211).ContinueWith(x => Console.WriteLine(x.Result)));
                Thread.Sleep(2000);
            }
        }
    }
}
