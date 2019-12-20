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
    /// Bob api. It chooses random node for access
    /// </summary>
    public interface IBobApi
    {
        /// <summary>
        /// Write data to Bob
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">binary data</param>
        /// <param name="token">token</param>
        /// <returns>operation result</returns>
        BobResult Put(ulong key, byte[] data, CancellationToken token);

        /// <summary>
        /// Write data to Bob asynchronously
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">binary data</param>
        /// <param name="token">token</param>
        /// <returns>operation result</returns>
        Task<BobResult> PutAsync(ulong key, byte[] data, CancellationToken token);

        /// <summary>
        /// Read data from Bob
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="token">token</param>
        /// /// <param name="fullGet">try read data from sup nodes</param>
        /// <returns>operation result</returns>
        BobGetResult Get(ulong key, bool fullGet, CancellationToken token);

        /// <summary>
        /// Read data from Bob asynchronously
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="token">token</param>
        /// <param name="fullGet">try read data from sup nodes</param>
        /// <returns>operation result with data</returns>
        Task<BobGetResult> GetAsync(ulong key, bool fullGet, CancellationToken token);
    }
}
