using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Scintillating.ProxyProtocol.Middleware;

/// <summary>
/// Options for TLS-offloaded connections.
/// </summary>
public class ProxyProtocolTlsOffloadOptions
{
    /// <summary>
    /// Set to true in order to enable <see cref="ITlsConnectionFeature"/> for all PROXY protocol connections, to mark them as secure.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Set to true in order to enable <see cref="ITlsApplicationProtocolFeature"/> by examining the request for H2 client preamble.
    /// </summary>
    /// <remarks>
    /// <para>This can be used if proxy doesn't support PP2_TYPE_ALPN</para>
    /// <para>Requires setting <see cref="Enabled"/> to true.</para>
    /// </remarks>
    public bool DetectApplicationProtocolByH2Preface { get; init; }
}
