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
    private static readonly ConcurrentDictionary<int, long> SentBytesByPacket = new();
    private static long _sentPackets;
    private static long _sentBytes;
    private static long _windowStartTick;
    private static volatile bool _enabled;
    private static int _top = 6;
    private static int _perConnectionThreshold = 1000;

    private static volatile bool _logEachSentPacket;

    public static bool Enabled => _enabled;
    public static int PerConnectionThreshold => _perConnectionThreshold;
    public static int Top => _top;
    public static bool LogEachSentPacket => _logEachSentPacket;

    public static void Configure(IConfiguration configuration)
    {
        _enabled = configuration.GetValue("Networking:LogSentPacketsPerSecond", true);
        _top = Math.Clamp(configuration.GetValue("Networking:LogSentPacketsTop", 6), 0, 50);
        _perConnectionThreshold = Math.Clamp(configuration.GetValue("Networking:LogSentPacketsConnectionThreshold", 1000), 1, 1_000_000);
        _logEachSentPacket = configuration.GetValue("Networking:LogEachSentPacket", true);
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

        Interlocked.Increment(ref _sentPackets);
        Interlocked.Add(ref _sentBytes, packetBytes.Length);

        var packetId = TryReadPacketId(packetBytes);
        if (packetId >= 0)
        {
            var packetLength = packetBytes.Length;
            SentCounts.AddOrUpdate(packetId, 1, static (_, v) => v + 1);
            SentBytesByPacket.AddOrUpdate(packetId, packetLength, (_, v) => v + packetLength);
        }

        // Flush once per second across all channels.
        var now = Environment.TickCount64;
        var start = Volatile.Read(ref _windowStartTick);
        if (start != 0 && now - start >= 1000)
        {
            // Only one thread should advance the window.
            if (Interlocked.CompareExchange(ref _windowStartTick, now, start) == start)
            {
                Flush(logger, windowMs: now - start);
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

        var start = Volatile.Read(ref _windowStartTick);
        var now = Environment.TickCount64;

        // Force a flush/reset even if < 1 second has elapsed.
        Flush(logger, windowMs: start == 0 ? 0 : Math.Max(0, now - start));
        // Reset the window so the next burst starts a fresh 1-second interval.
        Interlocked.Exchange(ref _windowStartTick, 0);
    }

    internal static int TryReadPacketId(ReadOnlySpan<byte> packetBytes)
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

    private static void Flush(ILogger logger, long windowMs)
    {
        var packets = Interlocked.Exchange(ref _sentPackets, 0);
        var bytes = Interlocked.Exchange(ref _sentBytes, 0);
        var snapshot = SentCounts.ToArray();
        SentCounts.Clear();
        var bytesByPacket = SentBytesByPacket.ToArray();
        SentBytesByPacket.Clear();

        Array.Sort(snapshot, static (a, b) => b.Value.CompareTo(a.Value));
        var take = Math.Min(_top, snapshot.Length);

        if (take <= 0)
        {
            logger.LogInformation("[SEND] packets={Packets} bytes={Bytes} windowMs={WindowMs} header={{ }}", packets, bytes, windowMs);
            return;
        }

        var bytesMap = bytesByPacket.ToDictionary(k => k.Key, v => v.Value);

        var parts = new List<string>(take);
        for (var i = 0; i < take; i++)
        {
            var id = snapshot[i].Key;
            var name = Enum.GetName(typeof(Packets.ServerPackets), id) ?? id.ToString();
            bytesMap.TryGetValue(id, out var bytesForId);
            parts.Add($"{name}({id}):{snapshot[i].Value} ({bytesForId}B)");
        }

        var avg = packets > 0 ? (bytes / (double)packets) : 0;

        logger.LogInformation(
            "[SEND] packets={Packets} bytes={Bytes} avg={AvgBytes:F1} windowMs={WindowMs} top={{ {Header} }}",
            packets,
            bytes,
            avg,
            windowMs,
            string.Join(", ", parts));
    }
}
