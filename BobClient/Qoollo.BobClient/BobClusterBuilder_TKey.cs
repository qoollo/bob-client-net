using Qoollo.BobClient.KeySerializers;
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
    /// <typeparam name="TKey">Type of the Key for Cluster</typeparam>
    public class BobClusterBuilder<TKey>
    {
        private readonly List<NodeAddress> _nodeAddresses;
        private TimeSpan _operationTimeout;
        private BobNodeSelectionPolicyFactory _nodeSelectionPolicyFactory;
        private BobKeySerializer<TKey> _keySerializer;
        private int? _keySerializationPoolSize;

        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        public BobClusterBuilder()
        {
            _nodeAddresses = new List<NodeAddress>();
            _operationTimeout = BobNodeClient.DefaultOperationTimeout;
            _nodeSelectionPolicyFactory = null;
            _keySerializer = null;
            _keySerializationPoolSize = null;
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="addresses">Addresses for cluster nodes</param>
        public BobClusterBuilder(IEnumerable<NodeAddress> addresses)
            : this()
        {
            this.WithAdditionalNodes(addresses);
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="nodeAddresses">Addresses for cluster nodes</param>
        public BobClusterBuilder(IEnumerable<string> nodeAddresses)
            : this()
        {
            this.WithAdditionalNodes(nodeAddresses);
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="addresses">Addresses for cluster nodes</param>
        public BobClusterBuilder(params NodeAddress[] addresses)
            : this()
        {
            if (addresses != null && addresses.Length > 0)
                this.WithAdditionalNodes(addresses);
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="nodeAddresses">Addresses for cluster nodes</param>
        public BobClusterBuilder(params string[] nodeAddresses)
            : this()
        {
            if (nodeAddresses != null && nodeAddresses.Length > 0)
                this.WithAdditionalNodes(nodeAddresses);
        }

        /// <summary>
        /// Adds a node to the cluster
        /// </summary>
        /// <param name="address">Address of a node</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNode(NodeAddress address)
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
        public BobClusterBuilder<TKey> WithAdditionalNode(string nodeAddress)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));

            _nodeAddresses.Add(new NodeAddress(nodeAddress));
            return this;
        }

        /// <summary>
        /// Adds a node list to the cluster
        /// </summary>
        /// <param name="addresses">Addresses of nodes</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNodes(IEnumerable<NodeAddress> addresses)
        {
            if (addresses == null)
                throw new ArgumentNullException(nameof(addresses));

            foreach (var address in addresses)
            {
                if (address == null)
                    throw new ArgumentNullException(nameof(addresses), "Node address inside list cannot be null");

                _nodeAddresses.Add(address);
            }
            return this;
        }

        /// <summary>
        /// Adds a node list to the cluster
        /// </summary>
        /// <param name="nodeAddresses">Addresses of nodes</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNodes(IEnumerable<string> nodeAddresses)
        {
            if (nodeAddresses == null)
                throw new ArgumentNullException(nameof(nodeAddresses));

            foreach (var nodeAddress in nodeAddresses)
            {
                if (nodeAddress == null)
                    throw new ArgumentNullException(nameof(nodeAddresses), "Node address inside list cannot be null");

                _nodeAddresses.Add(new NodeAddress(nodeAddress));
            }
            return this;
        }

        /// <summary>
        /// Adds timeout for api calls
        /// </summary>
        /// <param name="timeout">Timeout value (can be <see cref="Timeout.InfiniteTimeSpan"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithOperationTimeout(TimeSpan timeout)
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
        public BobClusterBuilder<TKey> WithOperationTimeout(int timeoutMs)
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
        public BobClusterBuilder<TKey> WithNodeSelectionPolicy(BobNodeSelectionPolicyFactory policyFactory)
        {
            if (policyFactory == null)
                throw new ArgumentNullException(nameof(policyFactory));

            _nodeSelectionPolicyFactory = policyFactory;
            return this;
        }

        /// <summary>
        /// Specifies key serializer to convert <typeparamref name="TKey"/> into byte array
        /// </summary>
        /// <param name="keySerializer">Key serializer for <typeparamref name="TKey"/></param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithKeySerializer(BobKeySerializer<TKey> keySerializer)
        {
            if (keySerializer == null)
                throw new ArgumentNullException(nameof(keySerializer));

            _keySerializer = keySerializer;
            return this;
        }

        /// <summary>
        /// Specifies key serialization pool size (null - shared pool, 0 or less - pool is disabled, 1 or greater - custom pool with specified size)
        /// </summary>
        /// <param name="poolSize">Size of the Key serialization pool (null - shared pool, 0 or less - pool is disabled, 1 or greater - custom pool with specified size)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithKeySerializationPoolSize(int? poolSize)
        {
            _keySerializationPoolSize = poolSize;
            return this;
        }
        /// <summary>
        /// Switches back to shared key serialization pool (equivalent to <see cref="WithKeySerializationPoolSize(int?)"/> called with 'null' argument)
        /// </summary>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithSharedKeySerializationPool()
        {
            return WithKeySerializationPoolSize(null);
        }

        /// <summary>
        /// Builds <see cref="BobClusterClient{TKey}"/>
        /// </summary>
        /// <returns>Created cluster</returns>
        public BobClusterClient<TKey> Build()
        {
            if (_nodeAddresses.Count == 0)
                throw new InvalidOperationException("At least one node should be added to cluster");

            return new BobClusterClient<TKey>(_nodeAddresses.Select(o => new BobNodeClient(o, _operationTimeout)).ToList(), _nodeSelectionPolicyFactory, _keySerializer, _keySerializationPoolSize);
        }
    }
}
