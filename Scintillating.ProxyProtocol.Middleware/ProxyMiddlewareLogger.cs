namespace Scintillating.ProxyProtocol.Middleware;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Scintillating.ProxyProtocol.Parser;

internal static partial class ProxyMiddlewareLogger
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Connection id \"{connectionId}\" pre-read started with timeout {connectTimeout}."
    )]
    public static partial void StartingConnectionWithTimeout(ILogger logger, string connectionId, TimeSpan connectTimeout);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Connection id \"{connectionId}\" pre-read started without timeout."
    )]
    public static partial void StartingConnectionWithoutTimeout(ILogger logger, string connectionId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Connection id \"{connectionId}\" H2 and HTTP/1.1 ALPN detection enabled."
    )]
    public static partial void AlpnDetectionEnabled(ILogger logger, string connectionId);

    [LoggerMessage(
       EventId = 3,
       Level = LogLevel.Trace,
       Message = "Connection id \"{connectionId}\" setting PROXY protocol feature."
    )]
    public static partial void SettingProxyProtocolFeature(ILogger logger, string connectionId);

    [LoggerMessage(
       EventId = 4,
       Level = LogLevel.Trace,
       Message = "Connection id \"{connectionId}\" setting local & remote endpoints."
    )]
    public static partial void SettingLocalRemoteEndpoints(ILogger logger, string connectionId);

    [LoggerMessage(
       EventId = 5,
       Level = LogLevel.Trace,
       Message = "Connection id \"{connectionId}\" setting HTTP connection feature."
    )]
    public static partial void SettingHttpConnectionFeature(ILogger logger, string connectionId);

    [LoggerMessage(
       EventId = 6,
       Level = LogLevel.Trace,
       Message = "Connection id \"{connectionId}\" setting TLS connection feature."
    )]
    public static partial void SettingTlsConnectionFeature(ILogger logger, string connectionId);

    [LoggerMessage(
       EventId = 7,
       Level = LogLevel.Trace,
       Message = "Connection id \"{connectionId}\" setting TLS ALPN connection feature."
    )]
    public static partial void SettingTlsAlpnFeature(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 8,
      Level = LogLevel.Debug,
      Message = "Connection id \"{connectionId}\" proxy protocol parsing failed."
    )]
    public static partial void ParsingFailed(ILogger logger, string connectionId, ProxyProtocolException exception);

    [LoggerMessage(
      EventId = 9,
      Level = LogLevel.Debug,
      Message = "Connection id \"{connectionId}\" aborted."
    )]
    public static partial void ConnectionAborted(ILogger logger, string connectionId, ConnectionAbortedException exception);

    [LoggerMessage(
      EventId = 10,
      Level = LogLevel.Debug,
      Message = "Connection id \"{connectionId}\" timed out or disconnected."
    )]
    public static partial void ConnectionTimeout(ILogger logger, string connectionId, OperationCanceledException exception);

    [LoggerMessage(
      EventId = 11,
      Level = LogLevel.Debug,
      Message = "Connection id \"{connectionId}\" calling next middleware."
    )]
    public static partial void CallingNextMiddleware(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 12,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" read operation cancellled."
    )]
    public static partial void ReadCancelled(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 13,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" parsing protocol header."
    )]
    public static partial void ParsingProtocolHeader(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 14,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" requesting more data for protocol header."
    )]
    public static partial void RequestingMoreDataProtocolHeader(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 15,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" trying to detect HTTP2 preamble."
    )]
    public static partial void DetectingHttp2Preamble(ILogger logger, string connectionId);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Trace,
        Message = "Connection id \"{connectionId}\" successfully detected HTTP2 preamble."
    )]
    public static partial void DetectedHttp2(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 17,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" didn't detect HTTP2 preamble, falling back to HTTP/1.1."
    )]
    public static partial void DetectionFallbackHttp11(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 18,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" data finished, falling back to HTTP/1.1."
    )]
    public static partial void DetectionDataFinishedFallbackHttp11(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 19,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" requesting more data for HTTP2 preamble detection."
    )]
    public static partial void RequestingMoreDataAlpn(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 20,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" advancing pipeReader."
    )]
    public static partial void AdvancingPipeReader(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 21,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" protocol header parsed {proxyProtocolHeader}."
    )]
    public static partial void ProxyHeaderParsed(ILogger logger, string connectionId, ProxyProtocolHeader proxyProtocolHeader);

    [LoggerMessage(
      EventId = 22,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" detected TLS TLV PP2_TYPE_ALPN at {index}."
    )]
    public static partial void DetectedAlpnTLV(ILogger logger, string connectionId, int index);

    [LoggerMessage(
      EventId = 23,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" has no TLV PP2_TYPE_ALPN."
    )]
    public static partial void NoDetectedAlpnTLV(ILogger logger, string connectionId);

    [LoggerMessage(
      EventId = 24,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" detected TLV PP2_TYPE_SSL at {index}."
    )]
    public static partial void DetectedSslTLV(ILogger logger, string connectionId, int index);

    [LoggerMessage(
      EventId = 25,
      Level = LogLevel.Trace,
      Message = "Connection id \"{connectionId}\" has no TLV PP2_TYPE_SSL."
    )]
    public static partial void NoDetectedSslTLV(ILogger logger, string connectionId);
}