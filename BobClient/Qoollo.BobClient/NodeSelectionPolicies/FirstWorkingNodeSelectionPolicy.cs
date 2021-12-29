using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Selection policy that selects first working node cluster
    /// </summary>
    public sealed class FirstWorkingNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        /// <summary>
        /// <see cref="FirstWorkingNodeSelectionPolicy"/> factory
        /// </summary>
        public static BobNodeSelectionPolicyFactory Factory { get; } = BobNodeSelectionPolicyFactory.FromDelegate(nodes => new FirstWorkingNodeSelectionPolicy(nodes));

        // =======

        private volatile int _lastActiveNode;

        /// <summary>
        /// <see cref="FirstWorkingNodeSelectionPolicy"/> constructor
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        public FirstWorkingNodeSelectionPolicy(IReadOnlyList<IBobNodeClientStatus> nodes)
            : base(nodes)
        {
        }

        /// <summary>
        /// Detects whether the node can be used to perform operations
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>True if node can be used</returns>
        private static bool CanBeUsed(IBobNodeClientStatus node)
        {
            return node.State != BobNodeClientState.Shutdown && 
                node.State != BobNodeClientState.TransientFailure && 
                node.SequentialErrorCount == 0;
        }

        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <param name="operation">Operation for which the node selection is performing</param>
        /// <param name="key">Key for which the node selection is performing (can be empty)</param>
        /// <returns>Index of the selected node</returns>
        public override int SelectNodeIndex(BobOperationKind operation, BobKey key)
        {
            int lastActiveNode = _lastActiveNode;
            if (lastActiveNode < Nodes.Count && CanBeUsed(Nodes[lastActiveNode]))
                return lastActiveNode;

            int testingNode = lastActiveNode;
            for (int i = 1; i < Nodes.Count; i++)
            {
                testingNode = (testingNode + 1) % Nodes.Count;
                if (CanBeUsed(Nodes[testingNode]))
                {
                    System.Threading.Interlocked.CompareExchange(ref _lastActiveNode, testingNode, lastActiveNode);
                    return testingNode;
                }
            }

            // Select next node to prevent stucking on a single when all nodes are unavailable
            testingNode = (lastActiveNode + 1) % Nodes.Count;
            System.Threading.Interlocked.CompareExchange(ref _lastActiveNode, testingNode, lastActiveNode);
            return testingNode;
        }

        /// <summary>
        /// Selects one of the node from cluster to retry previously failed operation (may return negative value to stop trying)
        /// </summary>
        /// <param name="prevNodeIndex">Previously selected node index</param>
        /// <param name="operation">Operation for which the node selection is performing</param>
        /// <param name="key">Key for which the node selection is performing (can be empty)</param>
        /// <returns>Index of the selected node or -1 to stop trying</returns>
        public override int SelectNodeIndexOnRetry(int prevNodeIndex, BobOperationKind operation, BobKey key)
        {
            int testingNode = prevNodeIndex;
            for (int i = 1; i < Nodes.Count; i++)
            {
                testingNode = (testingNode + 1) % Nodes.Count;
                if (CanBeUsed(Nodes[testingNode]))
                    return testingNode;
            }

            return -1;
        }
    }
}
