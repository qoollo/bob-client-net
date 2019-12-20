using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient
{
    public class BobClusterBuilder
    {
        private readonly List<NodeAddress> _nodeAddresses;
        private TimeSpan _operationTimeout;
        private BobNodeSelectionPolicy _nodeSelectionPolicy;

        public BobClusterBuilder()
        {
            _nodeAddresses = new List<NodeAddress>();
            _operationTimeout = Timeout.InfiniteTimeSpan;
            _nodeSelectionPolicy = null;
        }


        public BobClusterBuilder WithAdditionalNode(NodeAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            _nodeAddresses.Add(address);
            return this;
        }

        public BobClusterBuilder WithAdditionalNode(string nodeAddress)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));

            _nodeAddresses.Add(new NodeAddress(nodeAddress));
            return this;
        }

        /// <summary>
        /// Add timeout for api calls
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public BobClusterBuilder WithOperationTimeout(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            _operationTimeout = timeout;
            return this;
        }
        public BobClusterBuilder WithOperationTimeout(int timeoutMs)
        {
            if (timeoutMs < 0 && timeoutMs != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));

            _operationTimeout = timeoutMs == Timeout.Infinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeoutMs);
            return this;
        }


        public BobClusterBuilder WithNodeSelectionPolicy(BobNodeSelectionPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            _nodeSelectionPolicy = policy;
            return this;
        }

        /// <summary>
        /// Build api
        /// </summary>
        /// <returns></returns>
        public BobClusterClient Build()
        {
            if (_nodeAddresses.Count == 0)
                throw new InvalidOperationException("At least one node shoulde be added to cluster");

            var policy = _nodeSelectionPolicy;
            if (policy == null)
            {
                if (_nodeAddresses.Count == 1)
                    policy = FirstNodeSelectionPolicy.Instance;
                else
                    policy = new SequentialNodeSelectionPolicy();
            }

            return new BobClusterClient(_nodeAddresses.Select(o => new BobNodeClient(o, _operationTimeout)).ToList(), policy);
        }
    }
}
