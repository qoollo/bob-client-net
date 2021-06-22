using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Bob api
    /// </summary>
    /// <typeparam name="TKey">Type of the Key</typeparam>
    public interface IBobApi<TKey>
    {
        /// <summary>
        /// Writes data to Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        void Put(TKey key, byte[] data, CancellationToken token);

        /// <summary>
        /// Writes data to Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">binary data</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task PutAsync(TKey key, byte[] data, CancellationToken token);

        /// <summary>
        /// Reads data from Bob
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        byte[] Get(TKey key, CancellationToken token);

        /// <summary>
        /// Reads data from Bob asynchronously
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result with data</returns>
        Task<byte[]> GetAsync(TKey key, CancellationToken token);

        /// <summary>
        /// Checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        bool[] Exists(TKey[] keys, CancellationToken token);

        /// <summary>
        /// Asynchronously checks data presented in Bob
        /// </summary>
        /// <param name="keys">Keys array</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<bool[]> ExistsAsync(TKey[] keys, CancellationToken token);
    }
}
