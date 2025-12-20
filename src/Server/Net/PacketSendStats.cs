using System.Buffers.Binary;
using System.Collections.Concurrent;
using Core.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Server.Net;

internal static class PacketSendStats
{
    private const uint CompressionFlag = 1u << 31;

    private static readonly ConcurrentDictionary<int, long> SentCounts = new();
    private static long _sentBytes;
    private static long _windowStartTick;
    private static volatile bool _enabled;
    private static int _topN = 6;

    public static bool Enabled => _enabled;

    public static void Configure(IConfiguration configuration)
    {
        _enabled = configuration.GetValue("Networking:LogSentPacketsPerSecond", false);
        _topN = Math.Clamp(configuration.GetValue("Networking:LogSentPacketsTopN", 6), 0, 50);
    }

    public static void RecordSent(ReadOnlySpan<byte> packetBytes, ILogger logger)
    {
        if (!_enabled)
        {
            return;
        }

        // Start the time window on the first packet.
        if (Volatile.Read(ref _windowStartTick) == 0)
        {
            Interlocked.CompareExchange(ref _windowStartTick, Environment.TickCount64, 0);
        }

        if (packetBytes.Length >= 8)
        {
            var packetId = ReadPacketId(packetBytes);
            if (packetId >= 0)
            {
                SentCounts.AddOrUpdate(packetId, 1, static (_, v) => v + 1);
            }
        }

        Interlocked.Add(ref _sentBytes, packetBytes.Length);

        // Flush once per second across all channels.
        var now = Environment.TickCount64;
        var start = Volatile.Read(ref _windowStartTick);
        if (start != 0 && now - start >= 1000)
        {
            // Only one thread should advance the window.
            if (Interlocked.CompareExchange(ref _windowStartTick, now, start) == start)
            {
                Flush(logger);
            }
        }
    }

    public static void FlushIfPending(ILogger logger)
    {
        if (!_enabled)
        {
            return;
        }

        // Fast-path: nothing to report.
        if (Interlocked.Read(ref _sentBytes) == 0 && SentCounts.IsEmpty)
        {
            return;
        }

        // Force a flush/reset even if < 1 second has elapsed.
        Flush(logger);
        // Reset the window so the next burst starts a fresh 1-second interval.
        Interlocked.Exchange(ref _windowStartTick, 0);
    }

    private static int ReadPacketId(ReadOnlySpan<byte> packetBytes)
    {
        // Packet format from PacketWriter.GetBytes():
        // [0..4) = int32 payloadSize
        // [4.. ) = payload
        // payload starts with int32 packetId (or uint32 packetId|CompressionFlag for compressed)
        if (packetBytes.Length < 8)
        {
            return -1;
        }

        var raw = BinaryPrimitives.ReadUInt32LittleEndian(packetBytes.Slice(4, 4));
        raw &= ~CompressionFlag;
        return unchecked((int)raw);
    }

    private static void Flush(ILogger logger)
    {
        var bytes = Interlocked.Exchange(ref _sentBytes, 0);
        var snapshot = SentCounts.ToArray();
        SentCounts.Clear();

        Array.Sort(snapshot, static (a, b) => b.Value.CompareTo(a.Value));
        var take = Math.Min(_topN, snapshot.Length);

        if (take <= 0)
        {
            logger.LogInformation("[SEND] bytes={Bytes} header={{ }}", bytes);
            return;
        }

        var parts = new List<string>(take);
        for (var i = 0; i < take; i++)
        {
            var id = snapshot[i].Key;
            var name = Enum.GetName(typeof(Packets.ServerPackets), id) ?? id.ToString();
            parts.Add($"{name}({id}):{snapshot[i].Value}");
        }

        logger.LogInformation("[SEND] bytes={Bytes} header={{ {Header} }}", bytes, string.Join(", ", parts));
    }
}
