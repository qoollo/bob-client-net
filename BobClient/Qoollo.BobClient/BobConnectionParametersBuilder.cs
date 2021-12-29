using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob node connection parameters builder
    /// </summary>
    public class BobConnectionParametersBuilder : IModifiableBobConnectionParameters
    {
        /// <summary>
        /// Default Bob client port
        /// </summary>
        public const int DefaultPort = BobNodeAddress.DefaultPort;
        /// <summary>
        /// Default max receive message length
        /// </summary>
        public const int DefaultMaxReceiveMessageLength = BobConnectionParameters.DefaultMaxReceiveMessageSize;
        /// <summary>
        /// Default max send message length
        /// </summary>
        public const int DefaultMaxSendMessageLength = BobConnectionParameters.DefaultMaxSendMessageSize;
        /// <summary>
        /// Default operation timeout
        /// </summary>
        public static readonly TimeSpan DefaultOperationTimeout = BobConnectionParameters.DefaultOperationTimeout;
        /// <summary>
        /// Default connection timeout
        /// </summary>
        public static readonly TimeSpan DefaultConnectionTimeout = BobConnectionParameters.DefaultConnectionTimeout;


        private readonly Dictionary<string, string> _customParameters;

        /// <summary>
        /// <see cref="BobConnectionParametersBuilder"/> constructor
        /// </summary>
        public BobConnectionParametersBuilder()
        {
            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Host = string.Empty;
        }
        /// <summary>
        /// <see cref="BobConnectionParametersBuilder"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Bob node address</param>
        /// <param name="user">Bob user name</param>
        /// <param name="password">Password for specified user</param>
        public BobConnectionParametersBuilder(BobNodeAddress nodeAddress, string user, string password)
        {
            if (nodeAddress == null)
                throw new ArgumentNullException(nameof(nodeAddress));

            Host = nodeAddress.Host;
            Port = nodeAddress.Port;
            User = user;
            Password = password;

            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        /// <summary>
        /// <see cref="BobConnectionParametersBuilder"/> constructor
        /// </summary>
        /// <param name="nodeAddress">Bob node address</param>
        public BobConnectionParametersBuilder(BobNodeAddress nodeAddress)
            : this(nodeAddress, null, null)
        {
        }
        /// <summary>
        /// <see cref="BobConnectionParametersBuilder"/> constructor from connection string
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        public BobConnectionParametersBuilder(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Host = string.Empty;

            BobConnectionStringParser.ParseConnectionStringInto(connectionString, this);
        }
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="sourceParameters">Source parameters</param>
        internal BobConnectionParametersBuilder(IModifiableBobConnectionParameters sourceParameters)
        {
            if (sourceParameters == null)
                throw new ArgumentNullException(nameof(sourceParameters));

            _customParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Host = string.Empty;

            IModifiableBobConnectionParametersExtensions.CopyFrom(targetParameters: this, sourceParameters: sourceParameters);
        }
        /// <summary>
        /// <see cref="BobConnectionParametersBuilder"/> copy constructor from <see cref="BobConnectionParameters"/>
        /// </summary>
        /// <param name="sourceParameters">Source parameters</param>
        public BobConnectionParametersBuilder(BobConnectionParameters sourceParameters)
            : this((IModifiableBobConnectionParameters)sourceParameters)
        {
        }
        /// <summary>
        /// <see cref="BobConnectionParametersBuilder"/> copy constructor from <see cref="BobConnectionParametersBuilder"/>
        /// </summary>
        /// <param name="sourceParameters">Source parameters</param>
        public BobConnectionParametersBuilder(BobConnectionParametersBuilder sourceParameters)
            : this((IModifiableBobConnectionParameters)sourceParameters)
        {
        }

        /// <summary>
        /// Host
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Port. If not specified, the default port is assumed
        /// </summary>
        public int? Port { get; set; }
        /// <summary>
        /// Bob node address (constructed from <see cref="Host"/> and <see cref="Port"/>)
        /// </summary>
        public BobNodeAddress NodeAddress { get { return new BobNodeAddress(Host, Port ?? BobNodeAddress.DefaultPort); } }

        /// <summary>
        /// User name for authentication. If not specified, an insecure connection is used 
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Password for the specified user
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Max receive message size. If not specified, 2 Gb is used
        /// </summary>
        public int? MaxReceiveMessageSize { get; set; }
        /// <summary>
        /// Max send message size. If not specified, 2 Gb is used
        /// </summary>
        public int? MaxSendMessageSize { get; set; }

        /// <summary>
        /// Timeout for all Bob operations. If not specified, 2 minutes is used
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }
        /// <summary>
        /// Timeout for connection. If not specified, 2 minutes is used
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// Key-value storage for all unknown parameters (can be used for extensions)
        /// </summary>
        public Dictionary<string, string> CustomParameters { get { return _customParameters; } }

        /// <summary>
        /// Sets custom parameter value for the specified key
        /// </summary>
        /// <param name="key">Custom parameters key</param>
        /// <param name="value">Associated value</param>
        /// <returns>Current instance of the builder</returns>
        public BobConnectionParametersBuilder WithCustomParameter(string key, string value)
        {
            CustomParameters[key] = value;
            return this;
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
        /// <summary>
        /// Sets value for specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="allowCustomParameters">If false, an exception will be thrown if the parameter does not match any known. Otherwise, the value will be written into CustomParameters</param>
        public void SetValue(string key, string value, bool allowCustomParameters = true)
        {
            IModifiableBobConnectionParametersExtensions.SetValue(this, key, value, allowCustomParameters);
        }

        /// <summary>
        /// Whether all parameters have valid values
        /// </summary>
        public bool IsValid
        {
            get { return IModifiableBobConnectionParametersExtensions.IsValid(this); }
        }

        /// <summary>
        /// Creates <see cref="BobConnectionParameters"/> from the current parameters builder
        /// </summary>
        /// <returns>Constructed <see cref="BobConnectionParameters"/></returns>
        public BobConnectionParameters Build()
        {
            return new BobConnectionParameters(this);
        }

        /// <summary>
        /// Converts connection parameters to its string representation
        /// </summary>
        /// <param name="includePassword">When True password is included into string representation, otherwise it is not</param>
        /// <returns>String representation</returns>
        public string ToString(bool includePassword)
        {
            return IModifiableBobConnectionParametersExtensions.ToString(this, includePassword);
        }

        /// <summary>
        /// Converts connection parameters to its string representation (password not included)
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return this.ToString(includePassword: false);
        }
    }
}
