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

        /// <summary>
        /// Throws <see cref="FormatException"/> if <paramref name="throwFormatException"/> is true. Otherwise returns 'false'
        /// </summary>
        /// <param name="throwFormatException">'true' to throw <see cref="FormatException"/></param>
        /// <param name="address">Address string</param>
        /// <param name="message">Message for <see cref="FormatException"/></param>
        /// <returns>False</returns>
        private static bool ThrowFormatExceptionOrReturnFalse(bool throwFormatException, string address, string message)
        {
            if (throwFormatException)
                throw new FormatException(message + ": '" + address + "'");
            return false;
        }

        /// <summary>
        /// Attempts to convert address to <paramref name="host"/> and <paramref name="port"/>
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="throwFormatException">'true' to throw <see cref="FormatException"/> on parsing error</param>
        /// <param name="host">Parsed host</param>
        /// <param name="port">Parsed port</param>
        /// <returns>True if parsed, otherwise false</returns>
        internal static bool TryParseCore(string address, bool throwFormatException, out string host, out int? port)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            host = null;
            port = null;

            if (string.IsNullOrWhiteSpace(address))
                return ThrowFormatExceptionOrReturnFalse(throwFormatException, address, "Bob Node address cannot be empty");

            address = address.Trim();
            int portSeparatorPosition = address.LastIndexOf(':');

            if (portSeparatorPosition < 0)
            {
                host = address;
                return true;
            }
            else if (address.Length > 2 && address[0] == '[' && address[address.Length - 1] == ']')
            {
                // Assume IPv6 address
                host = address;
                return true;
            }
            else if (portSeparatorPosition == address.Length - 1)
            {
                return ThrowFormatExceptionOrReturnFalse(throwFormatException, address, "Empty port after ':' in bob address");
            }
            else if (portSeparatorPosition == 0)
            {
                return ThrowFormatExceptionOrReturnFalse(throwFormatException, address, "Empty host before ':' in bob address");
            }
            else
            {
                if (!int.TryParse(address.Substring(portSeparatorPosition + 1), out int portVal))
                    return ThrowFormatExceptionOrReturnFalse(throwFormatException, address, "Unable to parse port in bob node address");
                if (portVal <= 0 || portVal > ushort.MaxValue)
                    return ThrowFormatExceptionOrReturnFalse(throwFormatException, address, "Port is not in a valid range");

                host = address.Substring(0, portSeparatorPosition);
                port = portVal;
                return true;
            }
        }


        /// <summary>
        /// Attempts to convert the string representation of an address to <see cref="BobNodeAddress"/>
        /// </summary>
        /// <param name="address">String represenation of an address</param>
        /// <param name="parsedAddress">Converted address</param>
        /// <returns>True if was converted successfully, otherwise false</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is null</exception>
        public static bool TryParse(string address, out BobNodeAddress parsedAddress)
        {
            if (TryParseCore(address, false, out string host, out int? port))
            {
                parsedAddress = new BobNodeAddress(host, port ?? DefaultPort);
                return true;
            }

            parsedAddress = null;
            return false;
        }
        /// <summary>
        /// Converts the string representation of an address to <see cref="BobNodeAddress"/>
        /// </summary>
        /// <param name="address">String represenation of an address</param>
        /// <returns>Converted address</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is null</exception>
        /// <exception cref="FormatException">Incorrect format</exception>
        public static BobNodeAddress Parse(string address)
        {
            TryParseCore(address, true, out string host, out int? port);
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
