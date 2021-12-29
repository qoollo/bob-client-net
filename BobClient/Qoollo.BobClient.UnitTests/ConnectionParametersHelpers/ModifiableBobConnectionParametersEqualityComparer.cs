using Qoollo.BobClient.ConnectionParametersHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.UnitTests.ConnectionParametersHelpers
{
    internal class ModifiableBobConnectionParametersEqualityComparer : IEqualityComparer<IModifiableBobConnectionParameters>
    {
        public static ModifiableBobConnectionParametersEqualityComparer Instance { get; } = new ModifiableBobConnectionParametersEqualityComparer();

        public bool Equals(IModifiableBobConnectionParameters x, IModifiableBobConnectionParameters y)
        {
            if (object.ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            return x.Host == y.Host &&
                   x.Port == y.Port &&
                   x.User == y.User &&
                   x.Password == y.Password &&
                   x.MaxReceiveMessageSize == y.MaxReceiveMessageSize &&
                   x.MaxSendMessageSize == y.MaxSendMessageSize &&
                   x.OperationTimeout == y.OperationTimeout &&
                   x.ConnectionTimeout == y.ConnectionTimeout &&
                   x.CustomParameters.All((kv) => y.CustomParameters.ContainsKey(kv.Key) && y.CustomParameters[kv.Key] == kv.Value) &&
                   y.CustomParameters.All((kv) => x.CustomParameters.ContainsKey(kv.Key) && x.CustomParameters[kv.Key] == kv.Value);
        }

        public int GetHashCode(IModifiableBobConnectionParameters obj)
        {
            return 0;
        }
    }
}
