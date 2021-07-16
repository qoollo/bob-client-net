using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient
{
    [System.Diagnostics.DebuggerDisplay("[Address: {Address}]")]
    public class BobNodeAddress : IEquatable<BobNodeAddress>
    {
        public const int DefaultPort = 20000;

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

        public string Host { get; }
        public int Port { get; }

        public string Address { get; }


        public static BobNodeAddress Parse(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("address cannot be empty", nameof(address));

            string host = address;
            int port = DefaultPort;

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
                if (!int.TryParse(address.Substring(portSeparatorPosition + 1), out port))
                    throw new FormatException($"Unable to parse port in bob node address: {address}");
                if (port <= 0 || port > ushort.MaxValue)
                    throw new FormatException($"Port is not in a valid range: {address}");

                host = address.Substring(0, portSeparatorPosition);
            }

            return new BobNodeAddress(host, port);
        }


        public bool Equals(BobNodeAddress other)
        {
            if (object.ReferenceEquals(this, other))
                return true;
            if (object.ReferenceEquals(other, null))
                return false;

            return Port == other.Port && string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as BobNodeAddress);
        }
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Host) ^ Port.GetHashCode();
        }

        public override string ToString()
        {
            return Address;
        }
    }
}
