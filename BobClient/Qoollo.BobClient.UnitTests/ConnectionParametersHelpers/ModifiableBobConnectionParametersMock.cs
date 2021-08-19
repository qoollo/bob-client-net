using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.UnitTests.ConnectionParametersHelpers
{
    public class ModifiableBobConnectionParametersMock : IModifiableBobConnectionParameters, IEquatable<ModifiableBobConnectionParametersMock>
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int? MaxReceiveMessageLength { get; set; }
        public int? MaxSendMessageLength { get; set; }
        public TimeSpan? OperationTimeout { get; set; }
        public TimeSpan? ConnectionTimeout { get; set; }

        public Dictionary<string, string> CustomParameters { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ModifiableBobConnectionParametersMock WithCustomParam(string key, string value)
        {
            CustomParameters[key] = value;
            return this;
        }

        public bool Equals(ModifiableBobConnectionParametersMock other)
        {
            if (other is null)
                return false;

            return Host == other.Host &&
                   Port == other.Port &&
                   User == other.User &&
                   Password == other.Password &&
                   MaxReceiveMessageLength == other.MaxReceiveMessageLength &&
                   MaxSendMessageLength == other.MaxSendMessageLength &&
                   OperationTimeout == other.OperationTimeout &&
                   ConnectionTimeout == other.ConnectionTimeout &&
                   CustomParameters.All((kv) => other.CustomParameters.ContainsKey(kv.Key) && other.CustomParameters[kv.Key] == kv.Value) &&
                   other.CustomParameters.All((kv) => CustomParameters.ContainsKey(kv.Key) && CustomParameters[kv.Key] == kv.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is ModifiableBobConnectionParametersMock other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
