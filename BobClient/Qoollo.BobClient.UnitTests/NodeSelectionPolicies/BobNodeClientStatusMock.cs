using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.BobClient.UnitTests.NodeSelectionPolicies
{
    internal class BobNodeClientStatusMock : IBobNodeClientStatus
    {
        public BobNodeClientState State { get; set; } = BobNodeClientState.Ready;

        public int SequentialErrorCount { get; set; } = 0;

        public int TimeSinceLastOperationMs { get; set; } = 0;

        public BobNodeAddress NodeAddress { get; } = new BobNodeAddress("127.0.0.1:20000");

        public BobConnectionParameters ConnectionParameters { get; } = new BobConnectionParameters("Address = 127.0.0.1:20000");
    }
}
