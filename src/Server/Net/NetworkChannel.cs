using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Core.Net;
using Microsoft.Extensions.Logging;

namespace Server.Net;

internal sealed class NetworkChannel<TSession> : INetworkChannel where TSession : IDisposable
{
    private const int BufferSize = 8 * 1024;

    private readonly ILogger<NetworkChannel<TSession>> _logger;
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _networkStream;
    private readonly Channel<byte[]> _sendChannel = Channel.CreateUnbounded<byte[]>();
    private bool _started;

    private long _packetsEnqueued;
    private long _packetsSent;
    private long _bytesSent;

    private long _secondStartTick;
    private long _packetsSentThisSecond;
    private long _bytesSentThisSecond;

    private readonly Dictionary<int, (long Count, long Bytes)> _packetsByIdThisSecond = new();

    public string IpAddress { get; }

    public NetworkChannel(ILogger<NetworkChannel<TSession>> logger, TcpClient tcpClient)
    {
        _logger = logger;
        _tcpClient = tcpClient;
        _networkStream = tcpClient.GetStream();
        IpAddress = (tcpClient.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "(none)";
    }

    public async System.Threading.Tasks.Task StartAsync(INetworkChannelProxy channelProxy, TSession session, CancellationToken cancellationToken)
    {
        var started = Interlocked.Exchange(ref _started, true);
        if (started)
        {
            return;
        }

        if (Debugger.IsAttached)
        {
            await channelProxy.OnConnectedAsync(this, cancellationToken);

            await Task.WhenAll(
                RunSend(cancellationToken),
                RunReceive(channelProxy, cancellationToken));
        }
        else
        {
            try
            {
                await channelProxy.OnConnectedAsync(this, cancellationToken);

                await Task.WhenAll(
                    RunSend(cancellationToken),
                    RunReceive(channelProxy, cancellationToken));
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
                _logger.LogDebug(ex, "Network connection terminated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception handling network connection");
            }
            finally
            {
                await channelProxy.OnDisconnectedAsync(this, cancellationToken);

                _tcpClient.Close();

                session.Dispose();
            }
        }
    }

    private async System.Threading.Tasks.Task RunSend(CancellationToken cancellationToken)
    {
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_sendChannel.Reader.TryRead(out var bytes))
                {
                    await _networkStream.WriteAsync(bytes, cancellationToken);

                    Interlocked.Increment(ref _packetsSent);
                    Interlocked.Add(ref _bytesSent, bytes.Length);

                    // Per-connection per-second counter (enabled explicitly, or as a fallback when global stats are disabled).
                    if (PacketSendStats.PerConnectionEnabled || !PacketSendStats.Enabled)
                    {
                        var now = Environment.TickCount64;
                        if (_secondStartTick == 0)
                        {
                            _secondStartTick = now;
                        }

                        _packetsSentThisSecond++;
                        _bytesSentThisSecond += bytes.Length;

                        if (PacketSendStats.PerConnectionEnabled)
                        {
                            var packetId = PacketSendStats.TryReadPacketId(bytes);
                            if (packetId >= 0)
                            {
                                if (_packetsByIdThisSecond.TryGetValue(packetId, out var cur))
                                {
                                    _packetsByIdThisSecond[packetId] = (cur.Count + 1, cur.Bytes + bytes.Length);
                                }
                                else
                                {
                                    _packetsByIdThisSecond[packetId] = (1, bytes.Length);
                                }
                            }
                        }

                        if (now - _secondStartTick >= 1000 && _packetsSentThisSecond >= (PacketSendStats.PerConnectionEnabled ? PacketSendStats.PerConnectionThreshold : 1000))
                        {
                            if (PacketSendStats.PerConnectionEnabled && _packetsByIdThisSecond.Count > 0 && PacketSendStats.TopN > 0)
                            {
                                var top = _packetsByIdThisSecond
                                    .OrderByDescending(kvp => kvp.Value.Count)
                                    .Take(PacketSendStats.TopN)
                                    .Select(kvp =>
                                    {
                                        var name = Enum.GetName(typeof(Core.Net.Packets.ServerPackets), kvp.Key) ?? kvp.Key.ToString();
                                        return $"{name}({kvp.Key}):{kvp.Value.Count} ({kvp.Value.Bytes}B)";
                                    });

                                _logger.LogInformation(
                                    "Sent {PacketsSent} packets ({BytesSent} bytes) to {Ip} in last second; top={{ {Top} }}",
                                    _packetsSentThisSecond,
                                    _bytesSentThisSecond,
                                    IpAddress,
                                    string.Join(", ", top));
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "Sent {PacketsSent} packets ({BytesSent} bytes) to {Ip} in last second",
                                    _packetsSentThisSecond,
                                    _bytesSentThisSecond,
                                    IpAddress);
                            }

                            _packetsSentThisSecond = 0;
                            _bytesSentThisSecond = 0;
                            _secondStartTick = now;

                            _packetsByIdThisSecond.Clear();
                        }
                    }

                    PacketSendStats.RecordSent(bytes, _logger);
                }

                // We drained the current burst; force a flush/reset so stats don't carry across idle gaps.
                PacketSendStats.FlushIfPending(_logger);

                // If we're idle, flush any partial per-second window so it doesn't carry into the next burst.
                if (!PacketSendStats.Enabled && _packetsSentThisSecond >= 1000)
                {
                    _logger.LogInformation(
                        "Sent {PacketsSent} packets ({BytesSent} bytes) to {Ip} in last second",
                        _packetsSentThisSecond,
                        _bytesSentThisSecond,
                        IpAddress);

                    _packetsSentThisSecond = 0;
                    _bytesSentThisSecond = 0;
                    _secondStartTick = 0;

                    _packetsByIdThisSecond.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Send loop terminated for {Ip}", IpAddress);
        }
    }

    private async System.Threading.Tasks.Task RunReceive(INetworkChannelProxy channelProxy, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytes = new byte[BufferSize];

                var bytesRead = await _networkStream.ReadAsync(bytes, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await channelProxy.OnBytesReceivedAsync(this, bytes.AsSpan(0, bytesRead), cancellationToken);
            }
        }
        finally
        {
            _sendChannel.Writer.TryComplete();
        }
    }

    public void Send(byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        var enqueued = Interlocked.Increment(ref _packetsEnqueued);
        if (!_sendChannel.Writer.TryWrite(bytes))
        {
            _logger.LogWarning("Send dropped (channel closed) to {Ip} (attempted packet size={Size})", IpAddress, bytes.Length);
            return;
        }

        // Log the first enqueue so we can confirm the server attempted to send anything.
        if (enqueued == 1)
        {
            _logger.LogInformation("Enqueued first packet (size={Size}) to {Ip}", bytes.Length, IpAddress);
        }
    }

    public void Send<TPacket>(TPacket packet) where TPacket : IPacket
    {
        var packetWriter = new PacketWriter();

        packet.Serialize(packetWriter);

        Send(packetWriter.GetBytes());
    }

    public void Close()
    {
        _sendChannel.Writer.TryComplete();

        // Ensure the receive loop unblocks so the connection fully tears down.
        // This will ultimately trigger OnDisconnectedAsync and session cleanup.
        try
        {
            _tcpClient.Close();
        }
        catch
        {
            // Ignore close errors; connection may already be disposed.
        }
    }
}