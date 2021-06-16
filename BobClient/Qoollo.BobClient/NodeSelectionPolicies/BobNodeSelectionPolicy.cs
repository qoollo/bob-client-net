using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Base class for node selection policy in cluster
    /// </summary>
    public abstract class BobNodeSelectionPolicy
    {
        /// <summary>
        /// <see cref="BobNodeSelectionPolicy"/> constructor
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        protected BobNodeSelectionPolicy(IReadOnlyList<IBobNodeClientStatus> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (nodes.Count == 0)
                throw new ArgumentException("Nodes list cannot be empty", nameof(nodes));

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    throw new ArgumentNullException($"{nameof(nodes)}[{i}]");
            }

            Nodes = nodes;
        }

        /// <summary>
        /// List of nodes from which the selection should be performed
        /// </summary>
        protected IReadOnlyList<IBobNodeClientStatus> Nodes { get; }

        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <returns>Index of the selected node</returns>
        public abstract int SelectNextNodeIndex();
    }
}
