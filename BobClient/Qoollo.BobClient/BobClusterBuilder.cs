using Qoollo.BobClient.NodeSelectionPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob cluster builder
    /// </summary>
    public class BobClusterBuilder
    {
        private readonly List<NodeAddress> _nodeAddresses;
        private TimeSpan _operationTimeout;
        private BobNodeSelectionPolicyFactory _nodeSelectionPolicyFactory;

        /// <summary>
        /// Builder constructor
        /// </summary>
        public BobClusterBuilder()
        {
            _nodeAddresses = new List<NodeAddress>();
            _operationTimeout = BobNodeClient.DefaultOperationTimeout;
            _nodeSelectionPolicyFactory = null;
        }

        /// <summary>
        /// Adds a node to the cluster
        /// </summary>
        /// <param name="address">Address of a node</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder WithAdditionalNode(NodeAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            _nodeAddresses.Add(address);
            return this;
        }

        /// <summary>
        /// Adds a node to the cluster
        /// </summary>
        /// <param name="nodeAddress">Address of a node</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder WithAdditionalNode(string nodeAddress)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));

            _nodeAddresses.Add(new NodeAddress(nodeAddress));
            return this;
        }

        /// <summary>
        /// Adds timeout for api calls
        /// </summary>
        /// <param name="timeout">Timeout value (can be <see cref="Timeout.InfiniteTimeSpan"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder WithOperationTimeout(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            _operationTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Adds timeout for api calls
        /// </summary>
        /// <param name="timeoutMs">Timeout value in milliseconds (can be <see cref="Timeout.Infinite"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder WithOperationTimeout(int timeoutMs)
        {
            if (timeoutMs < 0 && timeoutMs != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));

            _operationTimeout = timeoutMs == Timeout.Infinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeoutMs);
            return this;
        }

        /// <summary>
        /// Specifies a node selection policy for opertions on cluster
        /// </summary>
        /// <param name="policyFactory">Factory to create node selection policy</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder WithNodeSelectionPolicy(BobNodeSelectionPolicyFactory policyFactory)
        {
            if (policyFactory == null)
                throw new ArgumentNullException(nameof(policyFactory));

            _nodeSelectionPolicyFactory = policyFactory;
            return this;
        }

        /// <summary>
        /// Builds <see cref="BobClusterClient"/>
        /// </summary>
        /// <returns>Created cluster</returns>
        public BobClusterClient Build()
        {
            if (_nodeAddresses.Count == 0)
                throw new InvalidOperationException("At least one node shoulde be added to cluster");

            return new BobClusterClient(_nodeAddresses.Select(o => new BobNodeClient(o, _operationTimeout)).ToList(), _nodeSelectionPolicyFactory);
        }
    }
}
