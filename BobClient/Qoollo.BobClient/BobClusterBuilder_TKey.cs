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
        private readonly List<BobConnectionParameters> _nodeConnectionParameters;
        private TimeSpan? _operationTimeout;
        private TimeSpan? _connectionTimeout;
        private BobNodeSelectionPolicyFactory _nodeSelectionPolicyFactory;
        private int? _operationRetryCount;
        private BobKeySerializer<TKey> _keySerializer;
        private int? _keySerializationPoolSize;
        private string _user;
        private string _password;

        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        public BobClusterBuilder()
        {
            _nodeConnectionParameters = new List<BobConnectionParameters>();
            _operationTimeout = null;
            _connectionTimeout = null;
            _nodeSelectionPolicyFactory = null;
            _operationRetryCount = null;
            _keySerializer = null;
            _keySerializationPoolSize = null;
            _user = null;
            _password = null;
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="nodeConnectionParameters">Connection parameters for cluster nodes</param>
        public BobClusterBuilder(IEnumerable<BobConnectionParameters> nodeConnectionParameters)
            : this()
        {
            this.WithAdditionalNodes(nodeConnectionParameters);
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="nodeConnectionStrings">Connection strings collection for cluster nodes</param>
        public BobClusterBuilder(IEnumerable<string> nodeConnectionStrings)
            : this()
        {
            this.WithAdditionalNodes(nodeConnectionStrings);
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="nodeConnectionParameters">Connection parameters for cluster nodes</param>
        public BobClusterBuilder(params BobConnectionParameters[] nodeConnectionParameters)
            : this()
        {
            if (nodeConnectionParameters != null && nodeConnectionParameters.Length > 0)
                this.WithAdditionalNodes(nodeConnectionParameters);
        }
        /// <summary>
        /// <see cref="BobClusterBuilder{TKey}"/> constructor
        /// </summary>
        /// <param name="nodeConnectionStrings">Connection strings collection for cluster nodes</param>
        public BobClusterBuilder(params string[] nodeConnectionStrings)
            : this()
        {
            if (nodeConnectionStrings != null && nodeConnectionStrings.Length > 0)
                this.WithAdditionalNodes(nodeConnectionStrings);
        }

        /// <summary>
        /// Adds a node to the cluster
        /// </summary>
        /// <param name="nodeConnectionParameters">Node connection parameters</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNode(BobConnectionParameters nodeConnectionParameters)
        {
            if (nodeConnectionParameters == null)
                throw new ArgumentNullException(nameof(nodeConnectionParameters));

            _nodeConnectionParameters.Add(nodeConnectionParameters);
            return this;
        }

        /// <summary>
        /// Adds a node to the cluster
        /// </summary>
        /// <param name="nodeConnectionString">Node connection string</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNode(string nodeConnectionString)
        {
            if (nodeConnectionString == null)
                throw new ArgumentNullException(nameof(nodeConnectionString));

            _nodeConnectionParameters.Add(new BobConnectionParameters(nodeConnectionString));
            return this;
        }

        /// <summary>
        /// Adds a node list to the cluster
        /// </summary>
        /// <param name="nodeConnectionParameters">Connection parameters for cluster nodes</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNodes(IEnumerable<BobConnectionParameters> nodeConnectionParameters)
        {
            if (nodeConnectionParameters == null)
                throw new ArgumentNullException(nameof(nodeConnectionParameters));

            foreach (var nodeConnectionParameter in nodeConnectionParameters)
            {
                if (nodeConnectionParameter == null)
                    throw new ArgumentNullException(nameof(nodeConnectionParameters), "Node connection parameters inside list cannot be null");

                _nodeConnectionParameters.Add(nodeConnectionParameter);
            }
            return this;
        }

        /// <summary>
        /// Adds a node list to the cluster
        /// </summary>
        /// <param name="nodeConnectionStrings">Connection strings collection for cluster nodes</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAdditionalNodes(IEnumerable<string> nodeConnectionStrings)
        {
            if (nodeConnectionStrings == null)
                throw new ArgumentNullException(nameof(nodeConnectionStrings));

            foreach (var nodeConnectionString in nodeConnectionStrings)
            {
                if (nodeConnectionString == null)
                    throw new ArgumentNullException(nameof(nodeConnectionStrings), "Node connection string inside list cannot be null");

                _nodeConnectionParameters.Add(new BobConnectionParameters(nodeConnectionString));
            }
            return this;
        }

        /// <summary>
        /// Adds timeout for api calls
        /// </summary>
        /// <param name="timeout">Timeout value (can be <see cref="Timeout.InfiniteTimeSpan"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithOperationTimeout(TimeSpan? timeout)
        {
            if (timeout.HasValue && timeout.Value < TimeSpan.Zero && timeout.Value != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            _operationTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Adds timeout for api calls
        /// </summary>
        /// <param name="timeoutMs">Timeout value in milliseconds (can be <see cref="Timeout.Infinite"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithOperationTimeout(int? timeoutMs)
        {
            if (timeoutMs.HasValue && timeoutMs.Value < 0 && timeoutMs.Value != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));

            if (timeoutMs.HasValue)
                _operationTimeout = timeoutMs == Timeout.Infinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeoutMs.Value);
            else
                _operationTimeout = null;

            return this;
        }

        /// <summary>
        /// Adds timeout for connection
        /// </summary>
        /// <param name="timeout">Timeout value (can be <see cref="Timeout.InfiniteTimeSpan"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithConnectionTimeout(TimeSpan? timeout)
        {
            if (timeout.HasValue && timeout.Value < TimeSpan.Zero && timeout.Value != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            _connectionTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Adds timeout for connection
        /// </summary>
        /// <param name="timeoutMs">Timeout value in milliseconds (can be <see cref="Timeout.Infinite"/>)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithConnectionTimeout(int? timeoutMs)
        {
            if (timeoutMs.HasValue && timeoutMs.Value < 0 && timeoutMs.Value != Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs));

            if (timeoutMs.HasValue)
                _connectionTimeout = timeoutMs == Timeout.Infinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeoutMs.Value);
            else
                _connectionTimeout = null;

            return this;
        }


        /// <summary>
        /// Sets user name and password for all nodes
        /// </summary>
        /// <param name="user">User name for authentication. If not specified, an insecure connection is used</param>
        /// <param name="password">Password for the specified user</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithAuthenticationData(string user, string password)
        {
            _user = user;
            _password = password;

            return this;
        }


        /// <summary>
        /// Specifies a node selection policy for opertions on cluster
        /// </summary>
        /// <param name="policyFactory">Factory to create node selection policy</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithNodeSelectionPolicy(BobNodeSelectionPolicyFactory policyFactory)
        {
            _nodeSelectionPolicyFactory = policyFactory;
            return this;
        }

        /// <summary>
        /// Specifies the number of retries of operations on cluster (null - default value (no retries), 0 - no retries, >= 1 - number of retries after failure, -1 - number of retries is equal to number of nodes)
        /// </summary>
        /// <param name="operationsRetryCount">The number of times the operation retries in case of failure (null - default value (no retries), 0 - no retries, >= 1 - number of retries after failure, -1 - number of retries is equal to number of nodes)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithOperationRetryCount(int? operationsRetryCount)
        {
            _operationRetryCount = operationsRetryCount;
            return this;
        }
        /// <summary>
        /// Specifies the special retries mode
        /// </summary>
        /// <param name="operationsRetryMode">Special retries mode</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithOperationRetry(BobCommonOperationRetryMode operationsRetryMode)
        {
            switch (operationsRetryMode)
            {
                case BobCommonOperationRetryMode.Default:
                    return WithOperationRetryCount(null);
                case BobCommonOperationRetryMode.ByNumberOfNodes:
                    return WithOperationRetryCount(-1);
                case BobCommonOperationRetryMode.NoRetry:
                    return WithOperationRetryCount(0);
                default:
                    throw new ArgumentException($"Unknown {nameof(BobCommonOperationRetryMode)} value: {operationsRetryMode}");
            }
        }


        /// <summary>
        /// Specifies key serializer to convert <typeparamref name="TKey"/> into byte array
        /// </summary>
        /// <param name="keySerializer">Key serializer for <typeparamref name="TKey"/></param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithKeySerializer(BobKeySerializer<TKey> keySerializer)
        {
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
        /// Specifies the special key serialization pool usage mode
        /// </summary>
        /// <param name="poolUsageMode">Usage mode</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public BobClusterBuilder<TKey> WithKeySerializationPool(BobCommonKeySerailizationPoolUsageMode poolUsageMode)
        {
            switch (poolUsageMode)
            {
                case BobCommonKeySerailizationPoolUsageMode.Default:
                    return WithKeySerializationPoolSize(null);
                case BobCommonKeySerailizationPoolUsageMode.NoPool:
                    return WithKeySerializationPoolSize(0);
                default:
                    throw new ArgumentException($"Unknown {nameof(BobCommonKeySerailizationPoolUsageMode)} value: {poolUsageMode}");
            }
        }

        /// <summary>
        /// Builds <see cref="BobClusterClient{TKey}"/>
        /// </summary>
        /// <returns>Created cluster</returns>
        public BobClusterClient<TKey> Build()
        {
            if (_nodeConnectionParameters.Count == 0)
                throw new InvalidOperationException("At least one node should be added to cluster");

            List<BobConnectionParameters> localConnectionParameters = _nodeConnectionParameters;

            if (_operationTimeout != null || _connectionTimeout != null || _user != null)
            {
                for (int i = 0; i < localConnectionParameters.Count; i++)
                {
                    var connectionParamsBuilder = new BobConnectionParametersBuilder(localConnectionParameters[i]);

                    if (_operationTimeout != null)
                        connectionParamsBuilder.OperationTimeout = _operationTimeout;
                    if (_connectionTimeout != null)
                        connectionParamsBuilder.ConnectionTimeout = _connectionTimeout;

                    if (_user != null)
                    {
                        connectionParamsBuilder.User = _user;
                        connectionParamsBuilder.Password = _password;
                    }

                    localConnectionParameters[i] = connectionParamsBuilder.Build();
                }
            }

            return new BobClusterClient<TKey>(localConnectionParameters, _nodeSelectionPolicyFactory, _operationRetryCount, _keySerializer, _keySerializationPoolSize);
        }
    }


    /// <summary>
    /// Defines several common retry modes
    /// </summary>
    public enum BobCommonOperationRetryMode
    {
        /// <summary>
        /// Resets number of retries to default value 
        /// (equivalent to calling <see cref="BobClusterBuilder{TKey}.WithOperationRetryCount(int?)"/> with <c>null</c> value)
        /// </summary>
        Default,
        /// <summary>
        /// Sets the number of retries equal to the number of nodes in cluster minus one, so the operation will be performed when at least one node is working 
        /// (equivalent to calling <see cref="BobClusterBuilder{TKey}.WithOperationRetryCount(int?)"/> with <c>-1</c> value)
        /// </summary>
        ByNumberOfNodes,
        /// <summary>
        /// Disables operation retries on cluster
        /// (equivalent to calling <see cref="BobClusterBuilder{TKey}.WithOperationRetryCount(int?)"/> with <c>0</c> value)
        /// </summary>
        NoRetry
    }


    /// <summary>
    /// Defines several common key serialization pool usage modes
    /// </summary>
    public enum BobCommonKeySerailizationPoolUsageMode
    {
        /// <summary>
        /// Resets to default shared pool
        /// (equivalent to calling <see cref="BobClusterBuilder{TKey}.WithKeySerializationPoolSize(int?)"/> with <c>null</c> value)
        /// </summary>
        Default,
        /// <summary>
        /// Disables key serialization pool
        /// (equivalent to calling <see cref="BobClusterBuilder{TKey}.WithKeySerializationPoolSize(int?)"/> with <c>0</c> value)
        /// </summary>
        NoPool
    }
}
