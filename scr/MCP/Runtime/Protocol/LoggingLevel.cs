using Newtonsoft.Json;
using System;

/// <summary>
/// Indicates the severity of a log message.
/// </summary>
/// <remarks>
/// These values map to syslog message severities, as specified in <see href="https://datatracker.ietf.org/doc/html/rfc5424#section-6.2.1">RFC-5424</see>.
/// </remarks>
[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[Obsolete]
public enum LoggingLevel
{
    /// <summary>Detailed debug information, typically only valuable to developers.</summary>
    [JsonProperty("debug")]
    Debug,

    /// <summary>Normal operational messages that require no action.</summary>
    [JsonProperty("info")]
    Info,

    /// <summary>Normal but significant events that might deserve attention.</summary>
    [JsonProperty("notice")]
    Notice,

    /// <summary>Warning conditions that don't represent an error but indicate potential issues.</summary>
    [JsonProperty("warning")]
    Warning,

    /// <summary>Error conditions that should be addressed but don't require immediate action.</summary>
    [JsonProperty("error")]
    Error,

    /// <summary>Critical conditions that require immediate attention.</summary>
    [JsonProperty("critical")]
    Critical,

    /// <summary>Action must be taken immediately to address the condition.</summary>
    [JsonProperty("alert")]
    Alert,

    /// <summary>System is unusable and requires immediate attention.</summary>
    [JsonProperty("emergency")]
    Emergency
}

