using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.BobClient
{
    public class BobConnectionParameters : IModifiableBobConnectionParameters
    {
        private readonly Dictionary<string, string> _customParameters;
        private BobNodeAddress _nodeAddress;

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
        public BobConnectionParameters(BobNodeAddress nodeAddress)
            : this(nodeAddress, null, null)
        {
        }
        public BobConnectionParameters(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString cannot be empty", nameof(connectionString));

            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _nodeAddress = null;
            Host = string.Empty;

            BobConnectionStringParser.ParseConnectionStringInto(connectionString, this);
        }

        public string Host { get; private set; }
        public int? Port { get; private set; }
        public BobNodeAddress NodeAddress { get { return _nodeAddress ?? InitNodeAddress(); } }

        public string User { get; private set; }
        public string Password { get; private set; }

        public int? MaxReceiveMessageLength { get; private set; }
        public int? MaxSendMessageLength { get; private set; }

        public TimeSpan? OperationTimeout { get; private set; }
        public TimeSpan? ConnectionTimeout { get; private set; }

        public IReadOnlyDictionary<string, string> CustomParameters { get { return _customParameters; } }


        internal int? KeySerializationPoolSize { get; }
        internal int? OperationRetryCount { get; }
        internal NodeSelectionPolicies.KnownBobNodeSelectionPolicies? NodeSelectionPolicy { get; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private BobNodeAddress InitNodeAddress()
        {
            BobNodeAddress result = _nodeAddress;
            if (result == null)
                _nodeAddress = result = new BobNodeAddress(Host, Port ?? BobNodeAddress.DefaultPort);

            return result;
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
