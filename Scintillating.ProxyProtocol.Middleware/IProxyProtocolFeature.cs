using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Scintillating.ProxyProtocol.Parser;
using System.Net;
using System.Net.Security;

namespace Scintillating.ProxyProtocol.Middleware;

/// <summary>
/// Feature that will be set for PROXY protocol connections.
/// </summary>
public interface IProxyProtocolFeature : IHttpConnectionFeature, ITlsConnectionFeature, ITlsApplicationProtocolFeature
{
    /// <summary>
    /// Processed PROXY protocol header.
    /// </summary>
    ProxyProtocolHeader ProtocolHeader { get; }

    /// <summary>
    /// Original local endpoint from <see cref="ConnectionContext"/>.
    /// </summary>
    EndPoint? OriginalLocalEndPoint { get; }

    /// <summary>
    /// Original remote endpoint from <see cref="ConnectionContext"/>.
    /// </summary>
    EndPoint? OriginalRemoteEndPoint { get; }

    /// <summary>
    /// Value of TLS Application protocol.
    /// </summary>
    new SslApplicationProtocol ApplicationProtocol { get; }
}
