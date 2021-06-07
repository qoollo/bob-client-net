using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Single node describtion
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("[Address: {Address}]")]
    public class NodeAddress
    {
        /// <summary>
        /// Node address constructor
        /// </summary>
        /// <param name="address">Node address. Format like host:port </param>
        public NodeAddress(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            Address = address;
        }
        /// <summary>
        /// Node address constructor
        /// </summary>
        /// <param name="host">Node host</param>
        /// <param name="port">Node port</param>
        public NodeAddress(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            Address = host + ":" + port.ToString();
        }

        /// <summary>
        /// Address of node in format 'host:port'
        /// </summary>
        public string Address { get; }
        /// <summary>
        /// Convert address to valid URI
        /// </summary>
        internal Uri GetAddressAsUri()
        {
            if (Address.IndexOf("://", StringComparison.OrdinalIgnoreCase) < 0)
                return new Uri("http://" + Address);

            return new Uri(Address);
        }

        /// <summary>
        /// Returns string representation of NodeAddress
        /// </summary>
        /// <returns>String representation of NodeAddress</returns>
        public override string ToString()
        {
            return Address;
        }
    }
}
