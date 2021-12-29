using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.NodeSelectionPolicies
{
    /// <summary>
    /// Extension methods for node selection policies
    /// </summary>
    public static class BobNodeSelectionPolicyExtensions
    {
        /// <summary>
        /// Specifies that <see cref="FirstNodeSelectionPolicy"/> should be used for node selection on Cluster
        /// </summary>
        /// <typeparam name="TKey">Type of the Key for Cluster</typeparam>
        /// <param name="clusterBuilder">Cluster builder</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public static BobClusterBuilder<TKey> WithFirstNodeSelectionPolicy<TKey>(this BobClusterBuilder<TKey> clusterBuilder)
        {
            return clusterBuilder.WithNodeSelectionPolicy(FirstNodeSelectionPolicy.Factory);
        }
        /// <summary>
        /// Specifies that <see cref="SequentialNodeSelectionPolicy"/> should be used for node selection on Cluster
        /// </summary>
        /// <typeparam name="TKey">Type of the Key for Cluster</typeparam>
        /// <param name="clusterBuilder">Cluster builder</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public static BobClusterBuilder<TKey> WithSequentialNodeSelectionPolicy<TKey>(this BobClusterBuilder<TKey> clusterBuilder)
        {
            return clusterBuilder.WithNodeSelectionPolicy(SequentialNodeSelectionPolicy.Factory);
        }
        /// <summary>
        /// Specifies that <see cref="FirstWorkingNodeSelectionPolicy"/> should be used for node selection on Cluster
        /// </summary>
        /// <typeparam name="TKey">Type of the Key for Cluster</typeparam>
        /// <param name="clusterBuilder">Cluster builder</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public static BobClusterBuilder<TKey> WithFirstWorkingNodeSelectionPolicy<TKey>(this BobClusterBuilder<TKey> clusterBuilder)
        {
            return clusterBuilder.WithNodeSelectionPolicy(FirstWorkingNodeSelectionPolicy.Factory);
        }
        /// <summary>
        /// Specifies that <see cref="SequentialWorkingNodeSelectionPolicy"/> should be used for node selection on Cluster
        /// </summary>
        /// <typeparam name="TKey">Type of the Key for Cluster</typeparam>
        /// <param name="clusterBuilder">Cluster builder</param>
        /// <param name="maxSequentialErrorCount">Max number of sequential errors to mark node as not-working</param>
        /// <param name="recoveryTimeoutMs">Time after that the not-working node can be used again (need to detect node recovery)</param>
        /// <returns>The reference to the current builder instatnce</returns>
        public static BobClusterBuilder<TKey> WithSequentialWorkingNodeSelectionPolicy<TKey>(this BobClusterBuilder<TKey> clusterBuilder, int maxSequentialErrorCount = SequentialWorkingNodeSelectionPolicy.DefaultMaxSequentialErrorCount, int recoveryTimeoutMs = SequentialWorkingNodeSelectionPolicy.DefaultRecoveryTimeoutMs)
        {
            return clusterBuilder.WithNodeSelectionPolicy(SequentialWorkingNodeSelectionPolicy.CreateFactory(maxSequentialErrorCount, recoveryTimeoutMs));
        }
    }
}
