using System;
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
        /// <returns>Index of the selected node</returns>
        public override int SelectNextNodeIndex()
        {
            return 0;
        }
    }
}
