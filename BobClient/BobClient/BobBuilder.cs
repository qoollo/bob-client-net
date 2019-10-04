using System;
using System.Collections.Generic;

namespace BobClient
{
    public class BobBuilder
    {
        private readonly List<BobStorage.BobApi.BobApiClient> _clients;
        private TimeSpan _timeout;

        public BobBuilder(List<BobStorage.BobApi.BobApiClient> clients)
        {
            _clients = clients;
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
}
