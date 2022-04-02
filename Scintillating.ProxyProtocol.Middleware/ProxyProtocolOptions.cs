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
    public TimeSpan? ConnectTimeout { get; init; }

    /// <summary>
    /// Options for TLS-offloaded connections.
    /// </summary>
    public ProxyProtocolTlsOffloadOptions? TlsOffloadOptions { get; init; }
}