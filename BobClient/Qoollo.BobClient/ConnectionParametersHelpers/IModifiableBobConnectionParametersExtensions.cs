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
        private static readonly char[] _connectionStringMarkers = new char[] { '=', ';' };


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
                return TimeSpan.FromMilliseconds(intervalInMs);
            }
            else if (TimeSpan.TryParse(value, out TimeSpan intervalTimeSpan))
            {
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
                    parameters.Host = value;
                    break;
                case "port":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.Port = null;
                    else if (!int.TryParse(value, out int portVal))
                        throw new FormatException($"Unable to parse 'port' value: {value}");
                    else
                        parameters.Port = portVal;
                    break;
                case "address":
                case "server":
                    if (value == null)
                    {
                        parameters.Host = null;
                        parameters.Port = null;
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        parameters.Host = value;
                    }
                    else
                    {
                        BobNodeAddress.TryParseCore(value, true, out string addrHostVal, out int? addrPortVal);
                        parameters.Host = addrHostVal;
                        if (addrPortVal != null)
                            parameters.Port = addrPortVal;
                    }
                    break;
                case "user":
                case "user id":
                    parameters.User = value;
                    break;
                case "password":
                    parameters.Password = value;
                    break;
                case "maxreceivemessagesize":
                case "maxreceivemessagelength":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.MaxReceiveMessageSize = null;
                    else if (!int.TryParse(value, out int maxReceiveMessageSizeVal))
                        throw new FormatException($"Unable to parse '{key}' value: {value}");
                    else
                        parameters.MaxReceiveMessageSize = maxReceiveMessageSizeVal;
                    break;
                case "maxsendmessagesize":
                case "maxsendmessagelength":
                    if (string.IsNullOrWhiteSpace(value))
                        parameters.MaxSendMessageSize = null;
                    else if (!int.TryParse(value, out int maxSendMessageSizeVal))
                        throw new FormatException($"Unable to parse '{key}' value: {value}");
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
                    if (string.IsNullOrWhiteSpace(parameters.Host))
                        return null;
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



        /// <summary>
        /// Escapes connection string token
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Escaped value</returns>
        private static string EscapeConnectionStringToken(string value)
        {
            if (value == null)
                return value;

            if (value.Length == 0)
                return "''";

            if (value[0] == '\'')
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            else if (value[0] == '"')
            {
                return "'" + value.Replace("'", "''") + "'";
            }
            else if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1]))
            {
                return "'" + value.Replace("'", "''") + "'";
            }
            else if (value.IndexOfAny(_connectionStringMarkers) >= 0)
            {
                return "'" + value.Replace("'", "''") + "'";
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Append connection string key-value pair
        /// </summary>
        /// <param name="stringBuilder">String builder</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="ending">Optional ending</param>
        /// <returns>String builder with appended key-value pair</returns>
        private static StringBuilder AppendConnectionStringValue(this StringBuilder stringBuilder, string key, string value, string ending)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new FormatException("Key cannot be empty string");
            if (value == null)
                return stringBuilder;

            stringBuilder.Append(key).Append(" = ").Append(value);

            if (ending != null)
                stringBuilder.Append(ending);

            return stringBuilder;
        }

        /// <summary>
        /// Converts parameters to its string representation
        /// </summary>
        /// <param name="parameters">Parameters instance</param>
        /// <param name="includePassword">When True password is included into string representation, otherwise it is not</param>
        /// <returns>String representation</returns>
        public static string ToString(this IModifiableBobConnectionParameters parameters, bool includePassword)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            const string pairEnding = "; ";

            StringBuilder result = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(parameters.Host))
            {
                if (parameters.Port.HasValue)
                    result.Append("Address = ").Append(parameters.Host).Append(":").Append(parameters.Port.Value).Append(pairEnding);
                else
                    result.Append("Address = ").Append(parameters.Host).Append(pairEnding);
            }
            else
            {
                if (parameters.Host != null)
                {
                    result.AppendConnectionStringValue("Host", EscapeConnectionStringToken(parameters.Host), pairEnding);
                }
                if (parameters.Port.HasValue)
                {
                    result.AppendConnectionStringValue("Port", parameters.Port.Value.ToString(), pairEnding);
                }
            }

            result.AppendConnectionStringValue("User", EscapeConnectionStringToken(parameters.User), pairEnding);
            if (includePassword)
                result.AppendConnectionStringValue("Password", EscapeConnectionStringToken(parameters.Password), pairEnding);
            result.AppendConnectionStringValue("MaxReceiveMessageSize", parameters.MaxReceiveMessageSize?.ToString(), pairEnding);
            result.AppendConnectionStringValue("MaxSendMessageSize", parameters.MaxSendMessageSize?.ToString(), pairEnding);
            result.AppendConnectionStringValue("OperationTimeout", parameters.OperationTimeout?.ToString(), pairEnding);
            result.AppendConnectionStringValue("ConnectionTimeout", parameters.ConnectionTimeout?.ToString(), pairEnding);

            foreach (var customParam in parameters.CustomParameters)
                result.AppendConnectionStringValue(EscapeConnectionStringToken(customParam.Key), EscapeConnectionStringToken(customParam.Value), pairEnding);

            if (result.Length == 0)
                return "";

            return result.ToString(0, result.Length - pairEnding.Length);
        }


        /// <summary>
        /// Returns false or throws exception based on <paramref name="exceptionBehaviour"/> value
        /// </summary>
        /// <param name="exceptionBehaviour">Controls exception behaviour</param>
        /// <param name="message">Exception message</param>
        /// <param name="value">Optional value to append to message</param>
        /// <returns>False</returns>
        private static bool ReturnOrThrowValidationError<T>(ValidationExceptionBehaviour exceptionBehaviour, string message, T value)
        {
            if (exceptionBehaviour != ValidationExceptionBehaviour.NoException)
            {
                if (value != null)
                    message = message + ": " + value.ToString();

                if (exceptionBehaviour == ValidationExceptionBehaviour.FormatException)
                    throw new FormatException(message);

                throw new InvalidBobConnectionParametersException(message);
            }
            return false;
        }
        /// <summary>
        /// Validates connection parameters
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <param name="exceptionBehaviour">Controls exception behaviour</param>
        /// <returns>True if valid, otherwise False or exception</returns>
        public static bool Validate(this IModifiableBobConnectionParameters parameters, ValidationExceptionBehaviour exceptionBehaviour)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (string.IsNullOrWhiteSpace(parameters.Host))
                return ReturnOrThrowValidationError(exceptionBehaviour, "Value cannot be an empty string for 'Host' parameter", (object)null);

            if (parameters.Port.HasValue && (parameters.Port.Value < 0 || parameters.Port.Value > ushort.MaxValue))
                return ReturnOrThrowValidationError(exceptionBehaviour, "'Port' is not in a valid range", parameters.Port.Value);

            if (parameters.MaxReceiveMessageSize.HasValue && parameters.MaxReceiveMessageSize.Value < 0)
                return ReturnOrThrowValidationError(exceptionBehaviour, "'MaxReceiveMessageSize' cannot be negative", parameters.MaxReceiveMessageSize.Value);

            if (parameters.MaxSendMessageSize.HasValue && parameters.MaxSendMessageSize.Value < 0)
                return ReturnOrThrowValidationError(exceptionBehaviour, "'MaxSendMessageSize' cannot be negative", parameters.MaxSendMessageSize.Value);

            if (parameters.OperationTimeout.HasValue && parameters.OperationTimeout.Value < TimeSpan.Zero)
                return ReturnOrThrowValidationError(exceptionBehaviour, "'OperationTimeout' cannot be negative", parameters.OperationTimeout.Value);

            if (parameters.ConnectionTimeout.HasValue && parameters.ConnectionTimeout.Value < TimeSpan.Zero)
                return ReturnOrThrowValidationError(exceptionBehaviour, "'ConnectionTimeout' cannot be negative", parameters.ConnectionTimeout.Value);

            return true;
        }
        /// <summary>
        /// Validates connection parameters
        /// </summary>
        /// <param name="parameters">Parameters</param>
        /// <returns>True if valid, otherwise False</returns>
        public static bool IsValid(this IModifiableBobConnectionParameters parameters)
        {
            return Validate(parameters, ValidationExceptionBehaviour.NoException);
        }
    }
}
