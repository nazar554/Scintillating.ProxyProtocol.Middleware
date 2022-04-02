﻿using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging;
using Scintillating.ProxyProtocol.Parser;
using System.Buffers;
using System.IO.Pipelines;

namespace Scintillating.ProxyProtocol.Middleware;

internal partial class ProxyProtocolConnectionMiddleware
{
    private readonly TimeSpan? _connectTimeout;
    private readonly ConnectionDelegate _next;
    private readonly ILogger _logger;
    private readonly bool _tlsOffloadEnabled;
    private readonly bool _detectApplicationProtocolByH2Preface;

    public ProxyProtocolConnectionMiddleware(ConnectionDelegate next!!, ILogger logger!!, ProxyProtocolOptions options!!)
    {
        _next = next;
        _logger = logger;
        _connectTimeout = options.ConnectTimeout;

        var tlsOffloadOptions = options.TlsOffloadOptions;
        if (tlsOffloadOptions is not null)
        {
            bool enabled = _tlsOffloadEnabled = tlsOffloadOptions.Enabled;
            if (enabled)
            {
                _detectApplicationProtocolByH2Preface = tlsOffloadOptions.DetectApplicationProtocolByH2Preface;
            }
        }
    }

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        CancellationToken cancellationToken = context.ConnectionClosed;

        CancellationTokenSource? cancellationTokenSource = null;
        string connectionId = context.ConnectionId;
        try
        {
            if (_connectTimeout is TimeSpan connectTimeout)
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
            ReadOnlyMemory<byte>? applicationProtocol = null;
            if (!_detectApplicationProtocolByH2Preface)
            {
                applicationProtocol = ReadOnlyMemory<byte>.Empty;
            }
            else
            {
                ProxyMiddlewareLogger.AlpnDetectionEnabled(_logger, connectionId);
            }

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
                applicationProtocol ?? throw new InvalidOperationException("PROXY: Unexpected failure detecting H2 preface.")
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

                if (_tlsOffloadEnabled)
                {
                    ProxyMiddlewareLogger.SettingTlsConnectionFeature(_logger, connectionId);
                    context.Features.Set<ITlsConnectionFeature>(proxyProtocolFeature);
                    if (!proxyProtocolFeature.ApplicationProtocol.IsEmpty)
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

    private bool TryParse(string connectionId, PipeReader pipeReader, in ReadResult readResult, ref ProxyProtocolParser parser,
        ref ReadOnlyMemory<byte>? applicationProtocol,
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
                proxyProtocolHeader = value;
                success = true;
            }
            else
            {
                if (readResult.IsCompleted)
                {
                    throw new ConnectionAbortedException("PROXY V1/V2: Connection closed while reading PROXY protocol header.");
                }
                ProxyMiddlewareLogger.RequestingMoreDataProtocolHeader(_logger, connectionId);
            }
            consumed = advanceTo.Consumed;
            examined = advanceTo.Examined;
        }

        if (success && !applicationProtocol.HasValue)
        {
            ProxyMiddlewareLogger.DetectingHttp2Preamble(_logger, connectionId);
            var sequenceReader = new SequenceReader<byte>(
                consumed.HasValue ? readResult.Buffer.Slice(consumed.Value) : readResult.Buffer
            );
            consumed = sequenceReader.Position;

            long remaining = sequenceReader.Remaining;
            if (remaining >= MiddlewareConstants.PrefaceHTTP2Length)
            {
                ReadOnlyMemory<byte> value;
                if (sequenceReader.IsNext(MiddlewareConstants.PrefaceHTTP2, advancePast: true))
                {
                    ProxyMiddlewareLogger.DetectedHttp2(_logger, connectionId);
                    value = MiddlewareConstants.Http2Id;
                }
                else
                {
                    sequenceReader.Advance(MiddlewareConstants.PrefaceHTTP2Length);
                    ProxyMiddlewareLogger.DetectionFallbackHttp11(_logger, connectionId);
                    value = MiddlewareConstants.Http11Id;
                }

                applicationProtocol = value;
                success = true;
            }
            else if (readResult.IsCompleted)
            {
                ProxyMiddlewareLogger.DetectionDataFinishedFallbackHttp11(_logger, connectionId);
                sequenceReader.AdvanceToEnd();
                applicationProtocol = MiddlewareConstants.Http11Id;
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