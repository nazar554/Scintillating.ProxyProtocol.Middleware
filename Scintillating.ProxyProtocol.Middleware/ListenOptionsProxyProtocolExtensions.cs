using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Scintillating.ProxyProtocol.Middleware;

/// <summary>
/// Extension methods on <see cref="ListenOptions"/> for PROXY protocol connection middleware.
/// </summary>
public static class ListenOptionsProxyProtocolExtensions
{
    /// <summary>
    /// Enable &amp; require PROXY protocol processing on this endpoint.
    /// </summary>
    /// <param name="listenOptions">Listen options for the endpoint.</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static ListenOptions UseProxyProtocol(this ListenOptions listenOptions)
    {
        ArgumentNullException.ThrowIfNull(listenOptions);
        var options = listenOptions.KestrelServerOptions.ApplicationServices
            .GetService<IOptions<ProxyProtocolOptions>>();

        var proxyProtocolOptions = options?.Value ?? new ProxyProtocolOptions();
        return listenOptions.UseProxyProtocol(proxyProtocolOptions);
    }

    /// <summary>
    /// Enable &amp; require PROXY protocol processing on this endpoint with configuration delegate.
    /// </summary>
    /// <param name="listenOptions">Listen options for the endpoint.</param>
    /// <param name="configureOptions">Action to configure the options for PROXY protocol connection middleware.</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static ListenOptions UseProxyProtocol(this ListenOptions listenOptions, Action<ProxyProtocolOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(listenOptions);
        ArgumentNullException.ThrowIfNull(configureOptions);
        var proxyProtocolOptions = new ProxyProtocolOptions();
        configureOptions(proxyProtocolOptions);
        return listenOptions.UseProxyProtocol(proxyProtocolOptions);
    }

    /// <summary>
    /// Enable &amp; require PROXY protocol processing on this endpoint with pre-configured options.
    /// </summary>
    /// <param name="listenOptions">Listen options for the endpoint.</param>
    /// <param name="proxyProtocolOptions">Options for PROXY protocol connection middleware.</param>
    /// <returns>The same options instance for method chaining.</returns>
    public static ListenOptions UseProxyProtocol(this ListenOptions listenOptions, ProxyProtocolOptions proxyProtocolOptions)
    {
        ArgumentNullException.ThrowIfNull(listenOptions);
        ArgumentNullException.ThrowIfNull(proxyProtocolOptions);

        var loggerFactory = listenOptions.KestrelServerOptions.ApplicationServices
            .GetRequiredService<ILoggerFactory>();

        var loggerName = proxyProtocolOptions.LoggerName;
        var logger = loggerName == null ? loggerFactory.CreateLogger<ProxyProtocolConnectionMiddleware>()
            : loggerFactory.CreateLogger(loggerName);

        listenOptions.Use(next => new ProxyProtocolConnectionMiddleware(next, logger, proxyProtocolOptions).OnConnectionAsync);

        return listenOptions;
    }
}