﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Selection policy that always use first node to perform operations
    /// </summary>
    public sealed class FirstNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        /// <summary>
        /// <see cref="FirstNodeSelectionPolicy"/> factory
        /// </summary>
        public static BobNodeSelectionPolicyFactory Factory { get; } = BobNodeSelectionPolicyFactory.FromDelegate(nodes => new FirstNodeSelectionPolicy(nodes));

        // =======

        /// <summary>
        /// <see cref="FirstNodeSelectionPolicy"/> constructor
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        public FirstNodeSelectionPolicy(IReadOnlyList<IBobNodeClientStatus> nodes)
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
            return 0;
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
            return 0;
        }
    }
}
