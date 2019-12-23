using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BobStorage;
using Grpc.Core;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob api
    /// </summary>
    public interface IBobApi
    {
        /// <summary>
        /// Writes data to Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        BobResult Put(ulong key, byte[] data, CancellationToken token);

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<BobResult> PutAsync(ulong key, byte[] data, CancellationToken token);

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        BobGetResult Get(ulong key, bool fullGet, CancellationToken token);

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result with data</returns>
        Task<BobGetResult> GetAsync(ulong key, bool fullGet, CancellationToken token);
    }
}
