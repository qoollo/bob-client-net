using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Selection policy that returns nodes one-by-one in round and skips not working nodes
    /// </summary>
    public sealed class SequentialWorkingNodeSelectionPolicy : BobNodeSelectionPolicy
    {
        /// <summary>
        /// Default value for Max number of sequential errors to mark node as not-working
        /// </summary>
        public const int DefaultMaxSequentialErrorCount = 2;
        /// <summary>
        /// Default value for Time after that the not-working node can be used again
        /// </summary>
        public const int DefaultRecoveryTimeoutMs = 60 * 1000;

        /// <summary>
        /// Factory
        /// </summary>
        private class ThisPolicyFactory : BobNodeSelectionPolicyFactory
        {
            private readonly int _maxSequentialErrorCount;
            private readonly int _recoveryTimeoutMs;

            public ThisPolicyFactory(int maxSequentialErrorCount, int recoveryTimeoutMs)
            {
                if (maxSequentialErrorCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(maxSequentialErrorCount));
                if (recoveryTimeoutMs < 0)
                    throw new ArgumentOutOfRangeException(nameof(recoveryTimeoutMs));

                _maxSequentialErrorCount = maxSequentialErrorCount;
                _recoveryTimeoutMs = recoveryTimeoutMs;
            }

            public override BobNodeSelectionPolicy Create(IReadOnlyList<IBobNodeClientStatus> nodes)
            {
                return new SequentialWorkingNodeSelectionPolicy(nodes, _maxSequentialErrorCount, _recoveryTimeoutMs);
            }
        }


        /// <summary>
        /// Creates factory for <see cref="SequentialWorkingNodeSelectionPolicy"/>
        /// </summary>
        /// <returns>Created factory</returns>
        public static BobNodeSelectionPolicyFactory CreateFactory() => new ThisPolicyFactory(DefaultMaxSequentialErrorCount, DefaultRecoveryTimeoutMs);
        /// <summary>
        /// Creates factory for <see cref="SequentialWorkingNodeSelectionPolicy"/>
        /// </summary>
        /// <param name="maxSequentialErrorCount">Max number of sequential errors to mark node as not-working</param>
        /// <param name="recoveryTimeoutMs">Time after that the not-working node can be used again (need to detect node recovery)</param>
        /// <returns>Created factory</returns>
        public static BobNodeSelectionPolicyFactory CreateFactory(int maxSequentialErrorCount, int recoveryTimeoutMs) => new ThisPolicyFactory(maxSequentialErrorCount, recoveryTimeoutMs);

        // =======

        private readonly int _maxSequentialErrorCount;
        private readonly int _recoveryTimeoutMs;
        private volatile int _index;

        /// <summary>
        /// <see cref="SequentialWorkingNodeSelectionPolicy"/> constructor
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        /// <param name="maxSequentialErrorCount">Max number of sequential errors to mark node as not-working</param>
        /// <param name="recoveryTimeoutMs">Time after that the not-working node can be used again (need to detect node recovery)</param>
        public SequentialWorkingNodeSelectionPolicy(IReadOnlyList<IBobNodeClientStatus> nodes, int maxSequentialErrorCount, int recoveryTimeoutMs)
            : base(nodes)
        {
            if (maxSequentialErrorCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSequentialErrorCount));
            if (recoveryTimeoutMs < 0)
                throw new ArgumentOutOfRangeException(nameof(recoveryTimeoutMs));

            _maxSequentialErrorCount = maxSequentialErrorCount;
            _recoveryTimeoutMs = recoveryTimeoutMs;
        }
        /// <summary>
        /// <see cref="SequentialWorkingNodeSelectionPolicy"/> constructor
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        public SequentialWorkingNodeSelectionPolicy(IReadOnlyList<IBobNodeClientStatus> nodes)
            : this(nodes, DefaultMaxSequentialErrorCount, DefaultRecoveryTimeoutMs)
        {

        }

        /// <summary>
        /// Detects whether the node can be used to perform operations
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>True if node can be used</returns>
        private bool CanBeUsed(IBobNodeClientStatus node)
        {
            if (node.State == BobNodeClientState.Shutdown)
                return false;

            if (node.SequentialErrorCount < _maxSequentialErrorCount)
                return true;

            if (_recoveryTimeoutMs == 0)
                return true;

            if (node.TimeSinceLastOperationMs > _recoveryTimeoutMs)
                return true;

            return false;
        }

        /// <summary>
        /// Selects one of the node from cluster to perform operation
        /// </summary>
        /// <returns>Index of the selected node</returns>
        public override int SelectNextNodeIndex()
        {
            var nodes = this.Nodes;
            if (nodes.Count == 1)
                return 0;

            int indexRawValue = Interlocked.Increment(ref _index);
            int index = (indexRawValue & int.MaxValue) % nodes.Count;
            if (CanBeUsed(nodes[index]))
                return index;

            for (int repCnt = 1; repCnt < nodes.Count; repCnt++)
            {
                index = (index + 1) % nodes.Count;
                if (CanBeUsed(nodes[index]))
                {
                    Interlocked.CompareExchange(ref _index, index, indexRawValue);
                    return index;
                }
            }

            int fallback_index = Interlocked.Increment(ref _index) & int.MaxValue;
            return fallback_index % nodes.Count;
        }
    }
}
