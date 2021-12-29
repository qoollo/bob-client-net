using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Factory to create concrete BobNodeSelectionPolicy
    /// </summary>
    public abstract class BobNodeSelectionPolicyFactory
    {
        /// <summary>
        /// Creates factory from delegate
        /// </summary>
        /// <param name="creationFunc">Delegate to create node selection policy</param>
        /// <returns>Created factory</returns>
        public static BobNodeSelectionPolicyFactory FromDelegate(Func<IReadOnlyList<IBobNodeClientStatus>, BobNodeSelectionPolicy> creationFunc)
        {
            return new DelegateBobNodeSelectionPolicyFactory(creationFunc);
        }
        /// <summary>
        /// Creates factory by <see cref="KnownBobNodeSelectionPolicies"/>
        /// </summary>
        /// <param name="policy">Known policy</param>
        /// <returns>Created factory</returns>
        internal static BobNodeSelectionPolicyFactory FromKnownNodeSelectionPolicy(KnownBobNodeSelectionPolicies policy)
        {
            switch (policy)
            {
                case KnownBobNodeSelectionPolicies.First:
                    return FirstNodeSelectionPolicy.Factory;
                case KnownBobNodeSelectionPolicies.Sequential:
                    return SequentialNodeSelectionPolicy.Factory;
                case KnownBobNodeSelectionPolicies.FirstWorking:
                    return FirstWorkingNodeSelectionPolicy.Factory;
                case KnownBobNodeSelectionPolicies.SequentialWorking:
                    return SequentialWorkingNodeSelectionPolicy.CreateFactory();
                default:
                    throw new ArgumentException($"Unknown node selection policy: {policy}", nameof(policy));
            }
        }

        /// <summary>
        /// Creates node selection policy
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        /// <returns>Created BobNodeSelectionPolicy</returns>
        public abstract BobNodeSelectionPolicy Create(IReadOnlyList<IBobNodeClientStatus> nodes);
    }


    /// <summary>
    /// Factory to create concrete BobNodeSelectionPolicy that wraps delegate
    /// </summary>
    internal sealed class DelegateBobNodeSelectionPolicyFactory : BobNodeSelectionPolicyFactory
    {
        private readonly Func<IReadOnlyList<IBobNodeClientStatus>, BobNodeSelectionPolicy> _creationFunc;

        /// <summary>
        /// <see cref="DelegateBobNodeSelectionPolicyFactory"/> constructor
        /// </summary>
        /// <param name="creationFunc">Delegate to create node selection policy</param>
        public DelegateBobNodeSelectionPolicyFactory(Func<IReadOnlyList<IBobNodeClientStatus>, BobNodeSelectionPolicy> creationFunc)
        {
            if (creationFunc == null)
                throw new ArgumentNullException(nameof(creationFunc));

            _creationFunc = creationFunc;
        }

        /// <summary>
        /// Creates node selection policy
        /// </summary>
        /// <param name="nodes">List of nodes from which the selection should be performed</param>
        /// <returns>Created BobNodeSelectionPolicy</returns>
        public override BobNodeSelectionPolicy Create(IReadOnlyList<IBobNodeClientStatus> nodes)
        {
            return _creationFunc(nodes);
        }
    }
}
