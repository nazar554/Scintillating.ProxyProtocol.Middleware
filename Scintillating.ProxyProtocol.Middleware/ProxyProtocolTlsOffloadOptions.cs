namespace Scintillating.ProxyProtocol.Middleware;

/// <summary>
/// Options for TLS-offloaded connections.
/// </summary>
public enum ProxyProtocolTlsOffloadOptions
{
    /// <summary>
    /// Detect the security status of connection from PROXY protocol header.
    /// </summary>
    Default,

    /// <summary>
    /// Always mark connections as secure.
    /// </summary>
    AlwaysEnabled,

    /// <summary>
    /// Always mark connections as secure and try to detect TLS ALPN.
    /// </summary>
    AlwaysEnabledWithAlpnDetection,
}