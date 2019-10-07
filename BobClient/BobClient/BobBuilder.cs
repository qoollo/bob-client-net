using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace BobClient
{
    public class BobBuilder
    {
        private readonly List<BobStorage.BobApi.BobApiClient> _clients;
        private TimeSpan _timeout;

        public BobBuilder(List<Node> clients)
        {
            _clients = clients.Select(x => {
                x.Validate();
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

        internal void Validate() 
        {
            if (!Uri.TryCreate($"http://{Address}", UriKind.Absolute, out var url) ||
               !IPAddress.TryParse(url.Host, out _))
            {
                throw new ArgumentException($"cannot parse {Address} like ip address");
            }
        }
    }
}
