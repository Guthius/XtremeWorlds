using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using Serilog;

namespace Client.Net;

public sealed class NetworkClient
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private Channel<byte[]>? _sendChannel;
    private volatile bool _isConnected;
    private bool _started;

    public bool Connected => _isConnected;

    public async Task StartAsync(string hostname, int port, INetworkEventHandler eventHandler, CancellationToken cancellationToken)
    {
        // Ensure only a single runner loop
        if (Interlocked.Exchange(ref _started, true))
        {
            return;
        }
        
        try
        {
            Log.Information("Connecting to server {HostName} on port {Port}...", hostname, port);

            while (!cancellationToken.IsCancellationRequested)
            {
                _isConnected = false;

                TcpClient tcpClient = null;
                try
                {
                    _sendChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false
                    });
                    
                    tcpClient = new TcpClient();

                    var connect = tcpClient.ConnectAsync(hostname, port, cancellationToken).AsTask();
                    var timeout = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                    // TcpClient.ConnectAsync ignores CancellationTokens; race a timeout instead
                    await Task.WhenAny(connect, timeout);

                    if (!tcpClient.Connected)
                    {
                        Log.Warning("Failed to connect to server");
                        
                        await Task.Delay(ReconnectDelay, cancellationToken);

                        continue;
                    }

                    Log.Information("Connected to server successfully");

                    _isConnected = true;
                    
                    await RunAsync(tcpClient, _sendChannel, eventHandler, cancellationToken);
                }
                catch (Exception ex) when (ex is not ObjectDisposedException and not OperationCanceledException)
                {
                    await eventHandler.OnConnectionLostAsync(cancellationToken);
                    
                    Log.Error(ex, "Unexpected exception in network loop");
                }
                finally
                {
                    tcpClient?.Close();
                    
                    _isConnected = false;
                    
                    await Task.Delay(RetryDelay, cancellationToken);
                    
                    Log.Information("Reconnecting...");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isConnected = false;

            Interlocked.Exchange(ref _started, false);
        }
    }

    private static async Task RunAsync(TcpClient tcpClient, Channel<byte[]> sendChannel, INetworkEventHandler eventHandler, CancellationToken cancellationToken)
    {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            var completedTask = await Task.WhenAny(
                RunReceive(tcpClient, eventHandler,
                    linkedTokenSource.Token),
                RunSend(tcpClient, sendChannel,
                    linkedTokenSource.Token));

            await completedTask;
        }
        catch
        {
            await linkedTokenSource.CancelAsync();

            throw;
        }
    }

    private static async Task RunReceive(TcpClient tcpClient, INetworkEventHandler eventHandler, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            var networkStream = tcpClient.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesReceived = await networkStream.ReadAsync(buffer, cancellationToken);
                if (bytesReceived == 0)
                {
                    Log.Warning("Connection with the server has been lost");
                    break;
                }

                await eventHandler.OnBytesReceivedAsync(buffer.AsMemory(0, bytesReceived), cancellationToken);
            }
        }
        finally
        {
            tcpClient.Close();

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task RunSend(TcpClient tcpClient, Channel<byte[]> sendChannel, CancellationToken cancellationToken)
    {
        try
        {
            var networkStream = tcpClient.GetStream();

            await foreach (var bytes in sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await networkStream.WriteAsync(bytes, cancellationToken);
            }
        }
        catch (ObjectDisposedException) // Happens when RunReceive closes the TcpClient and disposes the stream
        {
        }
        catch (SocketException ex)
        {
            Log.Error(ex, "Error sending data to server");
        }
        finally
        {
            sendChannel.Writer.TryComplete();
        }
    }

    public void Send(byte[] bytes)
    {
        _sendChannel?.Writer.TryWrite(bytes);
    }
}