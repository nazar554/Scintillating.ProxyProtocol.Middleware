**Scintillating.ProxyProtocol.Middleware** is a ASP.NET Core middleware for accepting connections with [PROXY protocol](https://www.haproxy.org/download/2.6/doc/proxy-protocol.txt) header.

## Quickstart

* Note that it's impossible to accept both PROXY and non-PROXY connections on same port (specification forbids it).
* If your proxy also does TLS offload then it's possible to force the connection to be detected as TLS.
* When TLS-offloading `PP2_TYPE_ALPN` is required to detect ALPN for HTTP2 connections, however not every sender supports it. You can use `DetectApplicationProtocolByH2Preface` as a workaround to detect protocol by presence of H2 client preamble, or use a single protocol (`Http1`/`Http2`) instead.
* `IProxyProtocolFeature` will be set for all connections utilizing PROXY protocol.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    // overrides ASPNETCORE_URLS and enables PROXY protocol
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.UseProxyProtocol(proxyProtocolOptions =>
            context.Configuration.GetSection("ProxyProtocol").Bind(proxyProtocolOptions)
        );
    });
    // enables PROXY protocol for all endpoints
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.UseProxyProtocol();
    });

    var kestrelSection = context.Configuration.GetSection("Kestrel");
    // enables only for specific named endpoints inside config
    options.Configure(kestrelSection, reloadOnChange: true)
        // unencrypted endpoint
        .Endpoint("NamedEndpoint", endpointOptions => endpointOptions.ListenOptions.UseProxyProtocol()
        // endpoint with TLS-offload, http://<host>:<port>
        .Endpoint("OffloadTLS", options =>
         {
             // Protocols can also be set inside appsettings.json
             options.ListenOptions.Protocols = HttpProtocols.Http1AndHttp2;
             options.ListenOptions.UseProxyProtocol(new ProxyProtocolOptions
             {
                 // can also set DetectApplicationProtocolByH2Preface if ALPN is missing
                 TlsOptions = new ProxyProtocolTlsOptions { Enabled = true }
             });
         })
        // endpoint without TLS-offload, https://<host>:<port>
        .Endpoint("HttpsEndpoint", endpointOptions => endpointOptions.ListenOptions
            .UseProxyProtocol()
            .UseHttps()
        )
    );
});

var app = builder.Build();
app.MapGet("/", (HttpContext context) =>
{
    var feature = context.Features.Get<IProxyProtocolFeature>();
    if (feature != null)
    {
        context.Response.Headers["X-Connection-Orignal-Remote-EndPoint"] = feature.OriginalRemoteEndPoint?.ToString();
        context.Response.Headers["X-Connection-Orignal-Local-EndPoint"] = feature.OriginalLocalEndPoint?.ToString();
    }

    context.Response.Headers["X-Request-Protocol"] = context.Request.Protocol;
    context.Response.Headers["X-Connection-Remote-IP"] = context.Connection.RemoteIpAddress?.ToString();
    context.Response.Headers["X-Connection-Remote-Port"] = context.Connection.RemotePort.ToString();
    context.Response.Headers["X-Connection-Local-IP"] = context.Connection.LocalIpAddress?.ToString();
    context.Response.Headers["X-Connection-Local-Port"] = context.Connection.LocalPort.ToString();

    context.Response.Headers["X-Request-IsHttps"] = context.Request.IsHttps.ToString();
    return new { ProxyProtocol = feature?.ToString() };
});

app.MapWhen(c => c.Features.Get<IProxyProtocolFeature>() is null, b =>
{
    // map endpoints/controllers for connections that don't have PROXY protocol enabled.
});

app.Run();
```
