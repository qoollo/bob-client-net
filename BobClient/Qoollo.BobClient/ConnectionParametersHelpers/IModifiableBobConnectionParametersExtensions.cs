using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    internal static class IModifiableBobConnectionParametersExtensions
    {
        private static TimeSpan ParseTimeInterval(string forKey, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (int.TryParse(value, out int intervalInMs))
            {
                if (intervalInMs < 0)
                    throw new FormatException($"'{forKey}' time interval cannot be negative: {value}");
                return TimeSpan.FromMilliseconds(intervalInMs);
            }
            else if (TimeSpan.TryParse(value, out TimeSpan intervalTimeSpan))
            {
                if (intervalTimeSpan < TimeSpan.Zero)
                    throw new FormatException($"'{forKey}' time interval cannot be negative: {value}");
                return intervalTimeSpan;
            }
            else
            {
                throw new FormatException($"Unable to parse time interval for key '{forKey}': {value}");
            }
        }


        public static void SetValue(this IModifiableBobConnectionParameters parameters, string key, string value, bool allowCustomParameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be an empty string", nameof(key));

            switch (key.ToLower())
            {
                case "host":
                    if (value == null)
                        throw new ArgumentNullException(nameof(value), $"Value cannot be null for '{key}' parameter");
                    if (string.IsNullOrWhiteSpace(value))
                        throw new FormatException($"Value cannot be an empty string for '{key}' parameter");
                    parameters.Host = value;
                    break;
                case "port":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.Port = null;
                    else if (!int.TryParse(value, out int portVal))
                        throw new FormatException($"Unable to parse 'port' value: {value}");
                    else if (portVal < 0 || portVal > ushort.MaxValue)
                        throw new FormatException($"'Port' is not in a valid range: {value}");
                    else
                        parameters.Port = portVal;
                    break;
                case "address":
                case "server":
                    if (value == null)
                        throw new ArgumentNullException(nameof(value), $"Value cannot be null for '{key}' parameter");
                    if (string.IsNullOrWhiteSpace(value))
                        throw new FormatException($"Value cannot be an empty string for '{key}' parameter");
                    BobNodeAddress.TryParseCore(value, true, out string addrHostVal, out int? addrPortVal);
                    parameters.Host = addrHostVal;
                    if (addrPortVal != null)
                        parameters.Port = addrPortVal;
                    break;
                case "user":
                case "user id":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.User = null;
                    else
                        parameters.User = value;
                    break;
                case "password":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.Password = null;
                    else
                        parameters.Password = value;
                    break;
                case "maxreceivemessagelength":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.MaxReceiveMessageLength = null;
                    else if (!int.TryParse(value, out int maxReceiveMessageLengthVal))
                        throw new FormatException($"Unable to parse '{key}' value: {value}");
                    else if (maxReceiveMessageLengthVal < 0)
                        throw new FormatException($"'{key}' cannot be negative: {value}");
                    else
                        parameters.MaxReceiveMessageLength = maxReceiveMessageLengthVal;
                    break;
                case "maxsendmessagelength":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.MaxSendMessageLength = null;
                    else if (!int.TryParse(value, out int maxSendMessageLengthVal))
                        throw new FormatException($"Unable to parse '{key}' value: {value}");
                    else if (maxSendMessageLengthVal < 0)
                        throw new FormatException($"'{key}' cannot be negative: {value}");
                    else
                        parameters.MaxSendMessageLength = maxSendMessageLengthVal;
                    break;
                case "operationtimeout":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.OperationTimeout = null;
                    else
                        parameters.OperationTimeout = ParseTimeInterval(key, value);
                    break;
                case "connectiontimeout":
                case "connect timeout":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.ConnectionTimeout = null;
                    else
                        parameters.ConnectionTimeout = ParseTimeInterval(key, value);
                    break;
                case "timeout":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        parameters.ConnectionTimeout = null;
                        parameters.OperationTimeout = null;
                    }
                    else
                    {
                        parameters.ConnectionTimeout = ParseTimeInterval(key, value);
                        parameters.OperationTimeout = parameters.ConnectionTimeout;
                    }
                    break;
                default:
                    if (!allowCustomParameters)
                        throw new ArgumentException($"Unknown key: {key}", nameof(key));
                    parameters.CustomParameters[key] = value;
                    break;
            }
        }
    }
}
