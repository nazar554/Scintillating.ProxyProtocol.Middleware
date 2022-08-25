using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Scintillating.ProxyProtocol.Middleware;

/// <summary>
/// Settings that configure the PROXY protocol connection middleware.
/// </summary>
public class ProxyProtocolOptions
{
    /// <summary>
    /// Name of logger used for the middleware.
    /// </summary>
    public string? LoggerName { get; init; }

    /// <summary>
    /// Timeout for reading the PROXY protocol header.
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "The type passed to RangeAttribute ctor is not nullable.")]
    [Range(typeof(TimeSpan), "00:00:00", "10675199.02:48:05.4775807", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan? ConnectTimeout { get; init; }

    /// <summary>
    /// Options for TLS-offloaded connections.
    /// </summary>
    [EnumDataType(typeof(ProxyProtocolTlsOffloadOptions))]
    public ProxyProtocolTlsOffloadOptions TlsOffloadOptions { get; init; }
}