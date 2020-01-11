using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Base class for node selection policy in cluster
    /// </summary>
    public abstract class BobNodeSelectionPolicy
    {
        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <param name="clients">List of clients (will be the same for every operation on single cluster)</param>
        /// <returns>Selected node (cannot be null)</returns>
        public abstract BobNodeClient Select(IReadOnlyList<BobNodeClient> clients);
    }

    /// <summary>
    /// Selection policy that always use first node to perform operations
    /// </summary>
    public sealed class FirstNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static FirstNodeSelectionPolicy Instance { get; } = new FirstNodeSelectionPolicy();

        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <param name="clients">List of clients (will be the same for every operation on single cluster)</param>
        /// <returns>Selected node (cannot be null)</returns>
        public override BobNodeClient Select(IReadOnlyList<BobNodeClient> clients)
        {
            return clients[0];
        }
    }

    /// <summary>
    /// Selection policy that returns nodes one-by-one in round
    /// </summary>
    public sealed class SequentialNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        private volatile int _index;

        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <param name="clients">List of clients (will be the same for every operation on single cluster)</param>
        /// <returns>Selected node (cannot be null)</returns>
        public override BobNodeClient Select(IReadOnlyList<BobNodeClient> clients)
        {
            int index = System.Threading.Interlocked.Increment(ref _index) & int.MaxValue;
            return clients[index % clients.Count];
        }
    }

    /// <summary>
    /// Selection policy that selects first working node cluster
    /// </summary>
    public sealed class FirstWorkingNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        private volatile int _lastActiveNode;

        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <param name="clients">List of clients (will be the same for every operation on single cluster)</param>
        /// <returns>Selected node (cannot be null)</returns>
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
