using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Single node describtion
    /// </summary>
    public class NodeAddress
    {
        /// <summary>
        /// Node constructor
        /// </summary>
        /// <param name="address">Node address. Format like host:port </param>
        public NodeAddress(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            Address = address;
        }

        /// <summary>
        /// Address of node in format 'host:port'
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// Returns string representation of NodeAddress
        /// </summary>
        /// <returns>String representation of NodeAddress</returns>
        public override string ToString()
        {
            return $"[Address: {Address}]";
        }
    }
}
