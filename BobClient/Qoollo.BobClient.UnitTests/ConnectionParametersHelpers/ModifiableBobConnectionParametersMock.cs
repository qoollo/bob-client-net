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
        public int? MaxReceiveMessageSize { get; set; }
        public int? MaxSendMessageSize { get; set; }
        public TimeSpan? OperationTimeout { get; set; }
        public TimeSpan? ConnectionTimeout { get; set; }

        public Dictionary<string, string> CustomParameters { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ModifiableBobConnectionParametersMock WithCustomParam(string key, string value)
        {
            CustomParameters[key] = value;
            return this;
        }

        internal bool Equals(IModifiableBobConnectionParameters other)
        {
            return ModifiableBobConnectionParametersEqualityComparer.Instance.Equals(this, other);
        }
        public bool Equals(ModifiableBobConnectionParametersMock other)
        {
            return ModifiableBobConnectionParametersEqualityComparer.Instance.Equals(this, other);
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
