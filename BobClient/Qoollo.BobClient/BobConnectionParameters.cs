using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob node connection parameters
    /// </summary>
    public class BobConnectionParameters : IModifiableBobConnectionParameters
    {
        /// <summary>
        /// Default Bob client port
        /// </summary>
        public const int DefaultPort = BobNodeAddress.DefaultPort;
        /// <summary>
        /// Default max receive message length
        /// </summary>
        public const int DefaultMaxReceiveMessageLength = int.MaxValue;
        /// <summary>
        /// Default max send message length
        /// </summary>
        public const int DefaultMaxSendMessageLength = int.MaxValue;
        /// <summary>
        /// Default operation timeout
        /// </summary>
        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(2);
        /// <summary>
        /// Default connection timeout
        /// </summary>
        public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromMinutes(2);


        private readonly Dictionary<string, string> _customParameters;
        private BobNodeAddress _nodeAddress;

        /// <summary>
        /// <see cref="BobConnectionParameters"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Bob node address</param>
        /// <param name="user">Bob user name</param>
        /// <param name="password">Password for specified user</param>
        public BobConnectionParameters(BobNodeAddress nodeAddress, string user, string password)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));

            Host = nodeAddress.Host;
            Port = nodeAddress.Port;
            _nodeAddress = nodeAddress;
            User = user;
            Password = password;

            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        /// <summary>
        /// <see cref="BobConnectionParameters"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Bob node address</param>
        public BobConnectionParameters(BobNodeAddress nodeAddress)
            : this(nodeAddress, null, null)
        {
        }
        /// <summary>
        /// <see cref="BobConnectionParameters"/> constructor from connection string
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        public BobConnectionParameters(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _nodeAddress = null;
            Host = string.Empty;

            BobConnectionStringParser.ParseConnectionStringInto(connectionString, this);
        }

        /// <summary>
        /// Host
        /// </summary>
        public string Host { get; private set; }
        /// <summary>
        /// Port. If not specified, the default port is assumed
        /// </summary>
        public int? Port { get; private set; }
        /// <summary>
        /// Bob node address (constructed from <see cref="Host"/> and <see cref="Port"/>)
        /// </summary>
        public BobNodeAddress NodeAddress { get { return _nodeAddress ?? InitNodeAddress(); } }

        /// <summary>
        /// User name for authorization. If not specified, an insecure connection is used 
        /// </summary>
        public string User { get; private set; }
        /// <summary>
        /// Password for the specified user
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Max receive message length. If not specified, 2 Gb is used
        /// </summary>
        public int? MaxReceiveMessageLength { get; private set; }
        /// <summary>
        /// Max send message length. If not specified, 2 Gb is used
        /// </summary>
        public int? MaxSendMessageLength { get; private set; }

        /// <summary>
        /// Timeout for all Bob operations. If not specified, 2 minutes is used
        /// </summary>
        public TimeSpan? OperationTimeout { get; private set; }
        /// <summary>
        /// Timeout for connection. If not specified, 2 minutes is used
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; private set; }

        /// <summary>
        /// Key-value storage for all unknown parameters (can be used for extensions)
        /// </summary>
        public IReadOnlyDictionary<string, string> CustomParameters { get { return _customParameters; } }

        /// <summary>
        /// Extension parameter to configure Key Serialization Pool size
        /// </summary>
        internal int? KeySerializationPoolSize
        {
            get
            {
                if (CustomParameters.TryGetValue("KeySerializationPoolSize", out string strValue) && int.TryParse(strValue, out int intValue))
                    return intValue;
                return null;
            }
        }
        /// <summary>
        /// Extension parameter to configure operation retry count in BobClusterClient
        /// </summary>
        internal int? OperationRetryCount
        {
            get
            {
                if (CustomParameters.TryGetValue("OperationRetryCount", out string strValue) && int.TryParse(strValue, out int intValue))
                    return intValue;
                return null;
            }
        }
        /// <summary>
        /// Extension parameter to configure node selection policy in BobClusterClient
        /// </summary>
        internal NodeSelectionPolicies.KnownBobNodeSelectionPolicies? NodeSelectionPolicy
        {
            get
            {
                if (CustomParameters.TryGetValue("NodeSelectionPolicy", out string strValue) && Enum.TryParse<NodeSelectionPolicies.KnownBobNodeSelectionPolicies>(strValue, true, out var enumValue))
                    return enumValue;
                return null;
            }
        }

        /// <summary>
        /// Initializes <see cref="NodeAddress"/> from <see cref="Host"/> and <see cref="Port"/>
        /// </summary>
        /// <returns>Created BobNodeAddress</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private BobNodeAddress InitNodeAddress()
        {
            BobNodeAddress result = _nodeAddress;
            if (result == null)
                _nodeAddress = result = new BobNodeAddress(Host, Port ?? BobNodeAddress.DefaultPort);

            return result;
        }

        /// <summary>
        /// Gets the value by the specified key 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="allowCustomParameters">If true, the CustomParameters dictionary is also used to retrieve the value</param>
        /// <returns>Extracted value in string representation</returns>
        public string GetValue(string key, bool allowCustomParameters = true)
        {
            return IModifiableBobConnectionParametersExtensions.GetValue(this, key, allowCustomParameters);
        }



        string IModifiableBobConnectionParameters.Host { get { return Host; } set { Host = value; } }
        int? IModifiableBobConnectionParameters.Port { get { return Port; } set { Port = value; } }
        string IModifiableBobConnectionParameters.User { get { return User; } set { User = value; } }
        string IModifiableBobConnectionParameters.Password { get { return Password; } set { Password = value; } }
        int? IModifiableBobConnectionParameters.MaxReceiveMessageLength { get { return MaxReceiveMessageLength; } set { MaxReceiveMessageLength = value; } }
        int? IModifiableBobConnectionParameters.MaxSendMessageLength { get { return MaxSendMessageLength; } set { MaxSendMessageLength = value; } }
        TimeSpan? IModifiableBobConnectionParameters.OperationTimeout { get { return OperationTimeout; } set { OperationTimeout = value; } }
        TimeSpan? IModifiableBobConnectionParameters.ConnectionTimeout { get { return ConnectionTimeout; } set { ConnectionTimeout = value; } }

        Dictionary<string, string> IModifiableBobConnectionParameters.CustomParameters { get { return _customParameters; } }
    }
}
