using System.Threading;
using System.Threading.Tasks;

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
        void Put(ulong key, byte[] data, CancellationToken token);

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task PutAsync(ulong key, byte[] data, CancellationToken token);

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        byte[] Get(ulong key, bool fullGet, CancellationToken token);

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result with data</returns>
        Task<byte[]> GetAsync(ulong key,  bool fullGet, CancellationToken token);

        /// <summary>
        /// Check data in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        bool[] Exist(ulong[] keys, bool fullGet, CancellationToken token);

        /// <summary>
        /// Check data in Bob asynchronously
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="fullGet">Try read data from sup nodes</param>
        /// <returns>Operation result</returns>
        Task<bool[]> ExistAsync(ulong[] keys, bool fullGet, CancellationToken token);
    }
}
