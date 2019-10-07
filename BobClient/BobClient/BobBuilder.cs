using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BobClient
{
    public class BobBuilder
    {
        private readonly List<BobStorage.BobApi.BobApiClient> _clients;
        private TimeSpan _timeout;

        public BobBuilder(List<Node> clients)
        {
            _clients = clients.Select(x => {
                //TODO check address
                var channel = new Channel(x.Address, ChannelCredentials.Insecure);
                return new BobStorage.BobApi.BobApiClient(channel);
            }).ToList();
        }

        public BobBuilder WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public IBobApi Build()
        {
            return new BobApi(_clients, _timeout);
        }
    }

    public class Node
    {
        public string Address { get; }

        public Node(string address)
        {
            Address = address;
        }
    }
}
