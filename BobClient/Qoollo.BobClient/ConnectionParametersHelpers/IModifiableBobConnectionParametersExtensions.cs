using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.BobClient.ConnectionParametersHelpers
{
    /// <summary>
    /// Common methods for <see cref="IModifiableBobConnectionParameters"/>
    /// </summary>
    internal static class IModifiableBobConnectionParametersExtensions
    {
        /// <summary>
        /// Parse time interval from string (as time and as number in milliseconds)
        /// </summary>
        /// <param name="forKey">Key name to generate Exception description</param>
        /// <param name="value">Value to parse</param>
        /// <returns>Parsed time interval</returns>
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

        /// <summary>
        /// Sets value for specified key
        /// </summary>
        /// <param name="parameters">Parameters that will be changed in this operation</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="allowCustomParameters">If false, an exception will be thrown if the parameter does not match any known. Otherwise, the value will be written into CustomParameters</param>
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
                case "maxreceivemessagesize":
                case "maxreceivemessagelength":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.MaxReceiveMessageSize = null;
                    else if (!int.TryParse(value, out int maxReceiveMessageSizeVal))
                        throw new FormatException($"Unable to parse '{key}' value: {value}");
                    else if (maxReceiveMessageSizeVal < 0)
                        throw new FormatException($"'{key}' cannot be negative: {value}");
                    else
                        parameters.MaxReceiveMessageSize = maxReceiveMessageSizeVal;
                    break;
                case "maxsendmessagesize":
                case "maxsendmessagelength":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.MaxSendMessageSize = null;
                    else if (!int.TryParse(value, out int maxSendMessageSizeVal))
                        throw new FormatException($"Unable to parse '{key}' value: {value}");
                    else if (maxSendMessageSizeVal < 0)
                        throw new FormatException($"'{key}' cannot be negative: {value}");
                    else
                        parameters.MaxSendMessageSize = maxSendMessageSizeVal;
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

        /// <summary>
        /// Gets value from parameters by specified key
        /// </summary>
        /// <param name="parameters">Parameters instance</param>
        /// <param name="key">Key</param>
        /// <param name="allowCustomParameters">If true, the CustomParameters dictionary is also used to retrieve the value</param>
        /// <returns>Extracted value</returns>
        public static string GetValue(this IModifiableBobConnectionParameters parameters, string key, bool allowCustomParameters)
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
                    return parameters.Host;
                case "port":
                    return parameters.Port?.ToString();
                case "address":
                case "server":
                    if (parameters.Host == null)
                        return "";
                    if (parameters.Port.HasValue)
                        return (parameters.Host ?? "") + ":" + parameters.Port.Value.ToString();
                    return parameters.Host ?? "";
                case "user":
                case "user id":
                    return parameters.User;
                case "password":
                    return parameters.Password;
                case "maxreceivemessagesize":
                case "maxreceivemessagelength":
                    return parameters.MaxReceiveMessageSize?.ToString();
                case "maxsendmessagesize":
                case "maxsendmessagelength":
                    return parameters.MaxSendMessageSize?.ToString();
                case "operationtimeout":
                    return parameters.OperationTimeout?.ToString();
                case "connectiontimeout":
                case "connect timeout":
                    return parameters.ConnectionTimeout?.ToString();
                default:
                    if (!allowCustomParameters)
                        throw new ArgumentException($"Unknown key: {key}", nameof(key));
                    if (!parameters.CustomParameters.ContainsKey(key))
                        throw new ArgumentException($"Key is unknown and not presented in CustomParameters dictionary: {key}", nameof(key));
                    return parameters.CustomParameters[key];
            }
        }
    }
}
