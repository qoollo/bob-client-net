﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Selection policy that returns nodes one-by-one in round
    /// </summary>
    public sealed class SequentialNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        /// <summary>
        /// <see cref="SequentialNodeSelectionPolicy"/> factory
        /// </summary>
        public static BobNodeSelectionPolicyFactory Factory { get; } = BobNodeSelectionPolicyFactory.FromDelegate(nodes => new SequentialNodeSelectionPolicy(nodes));

        // =======

        private volatile int _index;

        /// <summary>
        /// <see cref="SequentialNodeSelectionPolicy"/> constructor
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        public SequentialNodeSelectionPolicy(IReadOnlyList<IBobNodeClientStatus> nodes)
            : base(nodes)
        {
        }


        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <param name="operation">Operation for which the node selection is performing</param>
        /// <param name="key">Key for which the node selection is performing (can be empty)</param>
        /// <returns>Index of the selected node</returns>
        public override int SelectNodeIndex(BobOperationKind operation, BobKey key)
        {
            int index = System.Threading.Interlocked.Increment(ref _index) & int.MaxValue;
            return index % Nodes.Count;
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
            return (prevNodeIndex + 1) % Nodes.Count;
        }
    }
}
