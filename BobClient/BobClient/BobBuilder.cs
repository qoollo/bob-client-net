using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace BobClient
{
    /// <summary>
    /// Bob api builder
    /// </summary>
    public class BobBuilder
    {
        private readonly List<BobStorage.BobApi.BobApiClient> _clients;
        private TimeSpan _timeout;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clients">list of Bob's nodes. Api will have access only to them</param>
        public BobBuilder(List<Node> clients)
        {
            _clients = clients.Select(x => {
                var channel = new Channel(x.Address, ChannelCredentials.Insecure);
                return new BobStorage.BobApi.BobApiClient(channel);
            }).ToList();
        }

        /// <summary>
        /// Add timeout for api calls
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public BobBuilder WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Build api
        /// </summary>
        /// <returns></returns>
        public IBobApi Build()
        {
            return new BobApi(_clients, _timeout);
        }
    }

    /// <summary>
    /// Single node describtion
    /// </summary>
    public class Node
    {
        internal string Address { get; }

        /// <summary>
        /// Node constructor
        /// </summary>
        /// <param name="address">Node address. Format like host:port </param>
        public Node(string address)
        {
            Address = address;
            Validate();
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
