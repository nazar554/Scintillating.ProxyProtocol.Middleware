using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging;
using Scintillating.ProxyProtocol.Parser;
using Scintillating.ProxyProtocol.Parser.Tlv;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Security;
namespace Scintillating.ProxyProtocol.Middleware;

internal class ProxyProtocolConnectionMiddleware
{
    private readonly ConnectionDelegate _next;
    private readonly ILogger _logger;
    private readonly ProxyProtocolOptions _options;

    public ProxyProtocolConnectionMiddleware(ConnectionDelegate next, ILogger logger, ProxyProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        CancellationToken cancellationToken = context.ConnectionClosed;

        CancellationTokenSource? cancellationTokenSource = null;
        string connectionId = context.ConnectionId;
        try
        {
            if (_options.ConnectTimeout is TimeSpan connectTimeout)
            {
                ProxyMiddlewareLogger.StartingConnectionWithTimeout(_logger, connectionId, connectTimeout);
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationTokenSource.CancelAfter(connectTimeout);
                cancellationToken = cancellationTokenSource.Token;
            }
            else
            {
                ProxyMiddlewareLogger.StartingConnectionWithoutTimeout(_logger, connectionId);
            }

            var parser = new ProxyProtocolParser();
            ProxyProtocolHeader proxyProtocolHeader = null!;
            SslApplicationProtocol applicationProtocol = default;

            var pipeReader = context.Transport.Input;
            ReadResult readResult;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                readResult = await pipeReader.ReadAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            while (!TryParse(connectionId, pipeReader, in readResult, ref parser, ref applicationProtocol, ref proxyProtocolHeader));

            IProxyProtocolFeature proxyProtocolFeature = new ProxyProtocolFeature(
                context,
                proxyProtocolHeader,
                applicationProtocol
            );
            ProxyMiddlewareLogger.SettingProxyProtocolFeature(_logger, connectionId);

            context.Features.Set(proxyProtocolFeature);

            if (proxyProtocolHeader.Command == ProxyCommand.Proxy)
            {
                ProxyMiddlewareLogger.SettingLocalRemoteEndpoints(_logger, connectionId);
                context.LocalEndPoint = proxyProtocolHeader.Destination;
                context.RemoteEndPoint = proxyProtocolHeader.Source;

                ProxyMiddlewareLogger.SettingHttpConnectionFeature(_logger, connectionId);
                context.Features.Set<IHttpConnectionFeature>(proxyProtocolFeature);

                bool hasApplicationProtocol = !applicationProtocol.Protocol.IsEmpty;
                
                var sslDetails = GetSslDetails(connectionId, proxyProtocolHeader);
                // TODO: Add method to lookup client certificate by CN (if configured to do so)
                // Requires to choose a certificate store, and specify required flags
                // As well as checking the verify flag

                if (_options.TlsOffloadOptions == ProxyProtocolTlsOffloadOptions.AlwaysEnabled || hasApplicationProtocol || sslDetails is not null)
                {
                    ProxyMiddlewareLogger.SettingTlsConnectionFeature(_logger, connectionId);
                    context.Features.Set<ITlsConnectionFeature>(proxyProtocolFeature);
                    if (hasApplicationProtocol)
                    {
                        ProxyMiddlewareLogger.SettingTlsAlpnFeature(_logger, connectionId);
                        context.Features.Set<ITlsApplicationProtocolFeature>(proxyProtocolFeature);
                    }
                }
            }
        }
        catch (ProxyProtocolException proxyProtocolException)
        {
            ProxyMiddlewareLogger.ParsingFailed(_logger, connectionId, proxyProtocolException);
            context.Abort(new ConnectionAbortedException("PROXY V1/V2: parsing protocol header failed.", proxyProtocolException));
            return;
        }
        catch (ConnectionAbortedException abortReason)
        {
            ProxyMiddlewareLogger.ConnectionAborted(_logger, connectionId, abortReason);
            context.Abort(abortReason);
            return;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken && cancellationToken.IsCancellationRequested)
        {
            ProxyMiddlewareLogger.ConnectionTimeout(_logger, connectionId, ex);
            context.Abort(new ConnectionAbortedException("PROXY V1/V2: Timeout when reading PROXY protocol header.", ex));
            return;
        }
        finally
        {
            cancellationTokenSource?.Dispose();
        }

        ProxyMiddlewareLogger.CallingNextMiddleware(_logger, connectionId);
        await _next(context).ConfigureAwait(false);
    }

    private ProxyProtocolTlvSsl? GetSslDetails(string connectionId, ProxyProtocolHeader proxyProtocolHeader)
    {
        var typeLengthValues = proxyProtocolHeader.TypeLengthValues;
        int count = typeLengthValues.Count;

        for (int index = 0; index < count; ++index)
        {
            if (typeLengthValues[index] is ProxyProtocolTlvSsl sslDetails)
            {
                ProxyMiddlewareLogger.DetectedSslTLV(_logger, connectionId, index);
                return sslDetails;
            }
        }

        ProxyMiddlewareLogger.NoDetectedSslTLV(_logger, connectionId);
        return null;
    }

    private void AdjustAlpnUsingTlv(string connectionId, ProxyProtocolHeader proxyProtocolHeader, out SslApplicationProtocol applicationProtocol)
    {
        var typeLengthValues = proxyProtocolHeader.TypeLengthValues;
        int count = typeLengthValues.Count;

        for (int index = 0; index < count; ++index)
        {
            if (typeLengthValues[index] is ProxyProtocolTlvAlpn alpn)
            {
                ProxyMiddlewareLogger.DetectedAlpnTLV(_logger, connectionId, index);
                applicationProtocol = alpn.Value;
                return;
            }
        }
        applicationProtocol = default;
        ProxyMiddlewareLogger.NoDetectedAlpnTLV(_logger, connectionId);
    }

    [DoesNotReturn]
    private static void ThrowConnectionClosed()
    {
        throw new ConnectionAbortedException("PROXY V1/V2: Connection closed while reading PROXY protocol header.");
    }

    private bool TryParse(string connectionId, PipeReader pipeReader, in ReadResult readResult, ref ProxyProtocolParser parser,
        ref SslApplicationProtocol applicationProtocol,
        ref ProxyProtocolHeader proxyProtocolHeader)
    {
        if (readResult.IsCanceled)
        {
            ProxyMiddlewareLogger.ReadCancelled(_logger, connectionId);
            return false;
        }

        bool success = proxyProtocolHeader != null;
        SequencePosition? consumed = null;
        SequencePosition? examined = null;
        if (!success)
        {
            ProxyMiddlewareLogger.ParsingProtocolHeader(_logger, connectionId);
            if (parser.TryParse(readResult.Buffer, out var advanceTo, out var value))
            {
                ProxyMiddlewareLogger.ProxyHeaderParsed(_logger, connectionId, value);
                if (_options.TlsOffloadOptions == ProxyProtocolTlsOffloadOptions.AlwaysEnabledWithAlpnDetection)
                {
                    ProxyMiddlewareLogger.AlpnDetectionEnabled(_logger, connectionId);
                }
                else
                {
                    AdjustAlpnUsingTlv(connectionId, value, out applicationProtocol);
                }
                proxyProtocolHeader = value;
                success = true;
            }
            else
            {
                if (readResult.IsCompleted)
                {
                    ThrowConnectionClosed();
                }
                ProxyMiddlewareLogger.RequestingMoreDataProtocolHeader(_logger, connectionId);
            }
            consumed = advanceTo.Consumed;
            examined = advanceTo.Examined;
        }

        if (success && _options.TlsOffloadOptions == ProxyProtocolTlsOffloadOptions.AlwaysEnabledWithAlpnDetection)
        {
            ProxyMiddlewareLogger.DetectingHttp2Preamble(_logger, connectionId);
            var sequenceReader = new SequenceReader<byte>(
                consumed.HasValue ? readResult.Buffer.Slice(consumed.Value) : readResult.Buffer
            );
            consumed = sequenceReader.Position;

            long remaining = sequenceReader.Remaining;
            if (remaining >= MiddlewareConstants.PrefaceHTTP2Length)
            {
                if (sequenceReader.IsNext(MiddlewareConstants.PrefaceHTTP2, advancePast: true))
                {
                    ProxyMiddlewareLogger.DetectedHttp2(_logger, connectionId);
                    applicationProtocol = SslApplicationProtocol.Http2;
                }
                else
                {
                    sequenceReader.Advance(MiddlewareConstants.PrefaceHTTP2Length);
                    ProxyMiddlewareLogger.DetectionFallbackHttp11(_logger, connectionId);
                    applicationProtocol = SslApplicationProtocol.Http11;
                }

                success = true;
            }
            else if (readResult.IsCompleted)
            {
                ProxyMiddlewareLogger.DetectionDataFinishedFallbackHttp11(_logger, connectionId);
                sequenceReader.AdvanceToEnd();
                applicationProtocol = SslApplicationProtocol.Http11;
                success = true;
            }
            else
            {
                ProxyMiddlewareLogger.RequestingMoreDataAlpn(_logger, connectionId);
                sequenceReader.AdvanceToEnd();
                success = false;
            }

            examined = sequenceReader.Position;
        }

        ProxyMiddlewareLogger.AdvancingPipeReader(_logger, connectionId);
        pipeReader.AdvanceTo(consumed!.Value, examined!.Value);

        return success;
    }
}