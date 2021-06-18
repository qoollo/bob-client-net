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
        void Put(BobKey key, byte[] data, CancellationToken token);

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task PutAsync(BobKey key, byte[] data, CancellationToken token);

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        byte[] Get(BobKey key, CancellationToken token);

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result with data</returns>
        Task<byte[]> GetAsync(BobKey key, CancellationToken token);

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        bool[] Exists(BobKey[] keys, CancellationToken token);

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<bool[]> ExistsAsync(BobKey[] keys, CancellationToken token);
    }
}
