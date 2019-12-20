using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    public abstract class BobNodeSelectionPolicy
    {
        public abstract BobNodeClient Select(IReadOnlyList<BobNodeClient> clients);
    }


    public sealed class FirstNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        public static FirstNodeSelectionPolicy Instance { get; } = new FirstNodeSelectionPolicy();

        public override BobNodeClient Select(IReadOnlyList<BobNodeClient> clients)
        {
            return clients[0];
        }
    }

    public sealed class SequentialNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        private volatile int _index;

        public override BobNodeClient Select(IReadOnlyList<BobNodeClient> clients)
        {
            int index = System.Threading.Interlocked.Increment(ref _index) & int.MaxValue;
            return clients[index % clients.Count];
        }
    }

    public sealed class FirstWorkingNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        private volatile int _lastActiveNode;

        public override BobNodeClient Select(IReadOnlyList<BobNodeClient> clients)
        {
            int lastActiveNode = _lastActiveNode;
            if (lastActiveNode < clients.Count && clients[lastActiveNode].State != BobNodeClientState.Shutdown)
                return clients[lastActiveNode];

            int testingNode = lastActiveNode;
            for (int i = 0; i < clients.Count; i++)
            {
                testingNode = (testingNode + 1) % clients.Count;
                if (clients[testingNode].State != BobNodeClientState.Shutdown)
                {
                    System.Threading.Interlocked.CompareExchange(ref _lastActiveNode, testingNode, lastActiveNode);
                    return clients[testingNode];
                }
            }

            return clients[lastActiveNode % clients.Count];
        }
    }
}
