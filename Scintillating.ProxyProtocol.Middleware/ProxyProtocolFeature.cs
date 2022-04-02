using Microsoft.AspNetCore.Connections;
using Scintillating.ProxyProtocol.Parser;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Scintillating.ProxyProtocol.Middleware;

internal class ProxyProtocolFeature : IProxyProtocolFeature
{
    public ProxyProtocolHeader ProtocolHeader { get; }

    public ProxyProtocolFeature(ConnectionContext connectionContext!!, ProxyProtocolHeader protocolHeader!!, ReadOnlyMemory<byte> applicationProtocol)
    {
        ConnectionId = connectionContext.ConnectionId;
        ProtocolHeader = protocolHeader;

        OriginalLocalEndPoint = connectionContext.LocalEndPoint;
        OriginalRemoteEndPoint = connectionContext.RemoteEndPoint;

        if (protocolHeader.Source is IPEndPoint source)
        {
            RemoteIpAddress = source.Address;
            RemotePort = source.Port;
        }
        if (protocolHeader.Destination is IPEndPoint destination)
        {
            LocalIpAddress = destination.Address;
            LocalPort = destination.Port;
        }
        ApplicationProtocol = applicationProtocol;
    }

    public string ConnectionId { get; set; }

    public IPAddress? RemoteIpAddress { get; set; }

    public IPAddress? LocalIpAddress { get; set; }

    public int RemotePort { get; set; }

    public int LocalPort { get; set; }

    public EndPoint? OriginalLocalEndPoint { get; }

    public EndPoint? OriginalRemoteEndPoint { get; }

    public X509Certificate2? ClientCertificate { get; set; }

    public ReadOnlyMemory<byte> ApplicationProtocol { get; }

    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken) => Task.FromResult<X509Certificate2?>(null);
}