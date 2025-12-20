using Core;
using Core.Configurations;
using Core.Net;
using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace Client.Net;

public static class Network
{
    private static bool DebugPackets => SettingsManager.Instance.NetworkDebug;

    private static readonly ConcurrentDictionary<int, long> SentCounts = new();
    private static long _sentBytes;
    private static int _lastSentReportTick;

    private sealed class NetworkEventHandler : INetworkEventHandler
    {
        private const int BufferSize = 0xFFFF;
        private readonly GamePacketParser _parser = new();
        private byte[] _buffer = new byte[BufferSize];
        private int _bufferOffset;
        
        public Task OnBytesReceivedAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            // Ensure capacity for incoming bytes (allow dynamic growth beyond initial BufferSize)
            var required = _bufferOffset + bytes.Length;
            if (required > _buffer.Length)
            {
                var newCapacity = Math.Max(required, _buffer.Length * 2);
                Array.Resize(ref _buffer, newCapacity);
            }

            // Append new bytes
            bytes.Span.CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += bytes.Length;
            if (_bufferOffset == 0)
            {
                return Task.CompletedTask;
            }

            // Parse as many packets as possible
            var count = _parser.Parse(_buffer.AsMemory(0, _bufferOffset));
            if (count == 0)
            {
                return Task.CompletedTask;
            }

            // Move any leftover bytes to the beginning of the buffer for the next read
            var bytesLeft = _bufferOffset - count;
            if (bytesLeft > 0)
            {
                _buffer.AsSpan(count, bytesLeft).CopyTo(_buffer.AsSpan(0));
            }

            _bufferOffset = bytesLeft;
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Network disconnected");
                // Ensure game returns to menu/login state
                GameState.InMenu = true;
                Game.UI.WindowManager.HideWindows();
                Game.UI.WindowManager.ShowWindow("winLogin");
                GameLogic.Dialogue("Disconnect", "You lost connection to game server.", "Try to log back in again.", Core.Globals.DialogueType.Disconnect, Core.Globals.DialogueStyle.Okay);
                // Hide any open windows and show login window
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnDisconnected UI handling failed: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }

    private static readonly NetworkClient Client = new();
    private static readonly NetworkEventHandler EventHandler = new();
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    public static bool IsConnected => Client.Connected;
    
    public static async System.Threading.Tasks.Task Start()
    {
        await Client.StartAsync(
            SettingsManager.Instance.Ip,
            SettingsManager.Instance.Port,
            EventHandler,
            CancellationTokenSource.Token);
    }

    public static void Stop()
    {
        CancellationTokenSource.Cancel();
    }

    public static void Send(byte[] data)
    {
        if (DebugPackets)
        {
            if (data.Length >= 8)
            {
                // PacketWriter.GetBytes() prefix: [len:int32][packetId:int32]...
                int id = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4, 4));
                SentCounts.AddOrUpdate(id, 1, static (_, v) => v + 1);
            }

            Interlocked.Add(ref _sentBytes, data.Length);

            // Report at most once per second.
            int now = General.GetTickCount();
            if (now > _lastSentReportTick + 1000 && Interlocked.Exchange(ref _lastSentReportTick, now) < now)
            {
                try
                {
                    long bytes = Interlocked.Exchange(ref _sentBytes, 0);
                    var snapshot = SentCounts.ToArray();
                    SentCounts.Clear();

                    Array.Sort(snapshot, static (a, b) => b.Value.CompareTo(a.Value));
                    int take = Math.Min(6, snapshot.Length);
                    if (take > 0)
                    {
                        var parts = new List<string>(take);
                        for (int i = 0; i < take; i++)
                        {
                            int id = snapshot[i].Key;
                            string name = Enum.GetName(typeof(Packets.ClientPackets), id) ?? id.ToString();
                            parts.Add($"{name}({id}):{snapshot[i].Value}");
                        }

                        Console.WriteLine($"[SEND] bytes={bytes} header={{ {string.Join(", ", parts)} }}");
                    }
                    else
                    {
                        Console.WriteLine($"[SEND] bytes={bytes} header={{ }}");
                    }
                }
                catch
                {
                    // ignore debug stats failures
                }
            }
        }

        Client.Send(data);
    }
    
    public static void Send(PacketWriter data)
    {
        Send(data.GetBytes());
    }
}