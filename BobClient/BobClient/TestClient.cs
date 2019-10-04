using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using BobStorage;
using Grpc.Core;

namespace BobClient
{
    public class TestClient
    {
        public static void Ping()
        {
            try
            {
                var channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
                var client = new BobStorage.BobApi.BobApiClient(channel);
                var result = client.Ping(new Null());

                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }

    public class Node
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }
}
