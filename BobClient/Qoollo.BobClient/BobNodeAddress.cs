using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    /// <summary>
    /// Address of Bob node
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("[Address: {Address}]")]
    public class BobNodeAddress : IEquatable<BobNodeAddress>
    {
        /// <summary>
        /// Default Bob client port
        /// </summary>
        public const int DefaultPort = 20000;

        /// <summary>
        /// <see cref="BobNodeAddress"/> constructor
        /// </summary>
        /// <param name="host">Node host</param>
        /// <param name="port">Node port</param>
        public BobNodeAddress(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("host cannot be empty", nameof(host));
            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            Host = host;
            Port = port;

            Address = host + ":" + port.ToString();
        }

        /// <summary>
        /// Node Host
        /// </summary>
        public string Host { get; }
        /// <summary>
        /// Node Port
        /// </summary>
        public int Port { get; }
        /// <summary>
        /// Address of node in format 'host:port'
        /// </summary>
        public string Address { get; }


        internal static bool TryParseCore(string address, bool throwFormatException, out string host, out int? port1)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            host = null;
            port1 = null;

            if (address.Length == 0)
                return false;

            int pos = -1;

            // Parse port
            while (++pos < address.Length)
            {
                if (address[pos] >= 'a' && address[pos] <= 'z')
                    continue;
                if (address[pos] >= 'A' && address[pos] <= 'Z')
                    continue;
                if (address[pos] >= '0' && address[pos] <= '9')
                    continue;
                if (address[pos] == '-' && pos > 0)
                    continue;
                if (address[pos] == '.')
                    continue;
                if (address[pos] == ':')
                    break;

                if (throwFormatException)
                    throw new FormatException($"Host name in node address contains invalid symbol '{address[pos]}': {address}");

                return false;
            }

            if (pos == address.Length)
            {
                host = address;
                port1 = null;

                return true;
            }

            // Parse port
            if (pos + 1 < address.Length && address[pos] == ':')
            {
                int portAccum = 0;
                while (++pos < address.Length)
                {
                    if (portAccum > ushort.MaxValue)
                        return false;

                    if (address[pos] >= '0' && address[pos] <= '9')
                    {
                        portAccum = portAccum * 10 + (address[pos] - '0');
                        continue;
                    }

                    return false;
                }

                if (portAccum > ushort.MaxValue)
                    return false;
            }


            return false;
        }

        /// <summary>
        /// Converts address to <paramref name="host"/> and <paramref name="port"/>
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="host">Parsed host</param>
        /// <param name="port">Parsed port</param>
        internal static void ParseCore(string address, out string host, out int? port)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("address cannot be empty", nameof(address));

            host = address;
            port = null;

            int portSeparatorPosition = address.LastIndexOf(':');
            if (portSeparatorPosition == address.Length - 1)
            {
                throw new FormatException($"Empty port after ':' in bob address: {address}");
            }
            else if (portSeparatorPosition == 0)
            {
                throw new FormatException($"Empty host before ':' in bob address: {address}");
            }
            else if (portSeparatorPosition > 0)
            {
                if (!int.TryParse(address.Substring(portSeparatorPosition + 1), out int portVal))
                    throw new FormatException($"Unable to parse port in bob node address: {address}");
                if (portVal <= 0 || portVal > ushort.MaxValue)
                    throw new FormatException($"Port is not in a valid range: {address}");

                host = address.Substring(0, portSeparatorPosition);
                port = portVal;
            }
        }

        /// <summary>
        /// Converts the string representation of an address to <see cref="BobNodeAddress"/>
        /// </summary>
        /// <param name="address">String represenation of an address</param>
        /// <returns>Converted address</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="address"/> is empty</exception>
        /// <exception cref="FormatException">Incorrect format</exception>
        public static BobNodeAddress Parse(string address)
        {
            ParseCore(address, out string host, out int? port);
            return new BobNodeAddress(host, port ?? DefaultPort);
        }

        /// <summary>
        /// Indicates whether the current <see cref="BobNodeAddress"/> is equal to another <see cref="BobNodeAddress"/>
        /// </summary>
        /// <param name="other">Other <see cref="BobNodeAddress"/></param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false</returns>
        public bool Equals(BobNodeAddress other)
        {
            if (object.ReferenceEquals(this, other))
                return true;
            if (object.ReferenceEquals(other, null))
                return false;

            return Port == other.Port && string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Indicates whether the current <see cref="BobNodeAddress"/> is equal to another object
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BobNodeAddress);
        }
        /// <summary>
        /// Calculates hash code of the current <see cref="BobNodeAddress"/>
        /// </summary>
        /// <returns>Calculated hash code</returns>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Host) ^ Port.GetHashCode();
        }

        /// <summary>
        /// Returns string representation of <see cref="BobNodeAddress"/>
        /// </summary>
        /// <returns>String representation of <see cref="BobNodeAddress"/></returns>
        public override string ToString()
        {
            return Address;
        }
    }
}
