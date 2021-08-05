using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.BobClient
{
    public class BobConnectionParameters
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

            _customParameters = new Dictionary<string, string>();
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
    }
}
