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
        /// <returns>Index of the selected node</returns>
        public override int SelectNextNodeIndex()
        {
            int lastActiveNode = _lastActiveNode;
            if (lastActiveNode < Nodes.Count && CanBeUsed(Nodes[lastActiveNode]))
                return lastActiveNode;

            int testingNode = lastActiveNode;
            for (int i = 0; i < Nodes.Count; i++)
            {
                testingNode = (testingNode + 1) % Nodes.Count;
                if (CanBeUsed(Nodes[testingNode]))
                {
                    System.Threading.Interlocked.CompareExchange(ref _lastActiveNode, testingNode, lastActiveNode);
                    return testingNode;
                }
            }

            return lastActiveNode % Nodes.Count;
        }
    }
}
