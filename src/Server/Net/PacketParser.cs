using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Server.Net;

public abstract class PacketParser<TPacketId, TSession> where TPacketId : Enum
{
    private const uint CompressionFlag = 1u << 31;
    private const int MaxPacketSize = 64 * 1024 * 1024; // Must be <= GameSession MaxBufferSize

    private readonly Dictionary<int, Func<TSession, ReadOnlyMemory<byte>, ValueTask>> _handlers = [];

    protected void Bind(TPacketId packetId, Func<TSession, ReadOnlyMemory<byte>, ValueTask> handler)
    {
        _handlers[Convert.ToInt32(packetId)] = handler;
    }

    protected void Bind(TPacketId packetId, Action<TSession, ReadOnlyMemory<byte>> handler)
    {
        _handlers[Convert.ToInt32(packetId)] = (session, bytes) =>
        {
            handler(session, bytes);
            return ValueTask.CompletedTask;
        };
    }

    /// <summary>
    /// Allow derived parsers to validate a session before dispatching packet handlers.
    /// Returning false will silently drop the packet.
    /// </summary>
    protected virtual bool ValidateSession(TSession session) => true;

    public async ValueTask<int> Parse(TSession session, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        var totalNumberOfBytes = bytes.Length;

        while (bytes.Length >= 4)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packetSize = BitConverter.ToInt32(bytes.Span);

            // Defensive: invalid sizes indicate stream corruption or a bad client.
            if (packetSize < 0 || packetSize > MaxPacketSize)
            {
                throw new InvalidDataException($"Invalid packet size {packetSize} (max={MaxPacketSize})");
            }

            if (packetSize > bytes.Length - 4)
            {
                break;
            }
            
            bytes = bytes[4..];
            if (packetSize == 0)
            {
                continue;
            }

            await Handle(session, bytes[..packetSize]).ConfigureAwait(false);

            bytes = bytes[packetSize..];
        }

        var bytesLeft = bytes.Length;
        var bytesProcessed = totalNumberOfBytes - bytesLeft;

        return bytesProcessed;
    }

    private async ValueTask Handle(TSession session, ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        if (!ValidateSession(session))
        {
            return;
        }

        var packetId = BitConverter.ToInt32(bytes.Span);
        var packetData = bytes[4..];

        var compressed = IsCompressed(packetId);
        if (compressed)
        {
            packetId = (int) (packetId & ~CompressionFlag);
        }

        if (!Enum.IsDefined(typeof(TPacketId), packetId))
        {
            return;
        }

        if (!_handlers.TryGetValue(packetId, out var handler))
        {
            return;
        }

        if (compressed)
        {
            await HandleCompressed(session, packetData, packetId, handler).ConfigureAwait(false);
            return;
        }

        try
        {
            await handler(session, packetData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Server.General.Logger.LogError(ex, "Packet handler error (id={PacketId})", packetId);
        }
    }

    private async ValueTask HandleCompressed(TSession session, ReadOnlyMemory<byte> bytes, int packetId, Func<TSession, ReadOnlyMemory<byte>, ValueTask> handler)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        var decompressedSize = BitConverter.ToInt32(bytes.Span);
        if (decompressedSize == 0)
        {
            return;
        }

        var buffer = new byte[decompressedSize];
        if (!Decompress(bytes[4..], buffer))
        {
            return;
        }

        try
        {
            await handler(session, buffer.AsMemory()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Server.General.Logger.LogError(ex, "Packet handler error (id={PacketId})", packetId);
        }
    }

    private static bool IsCompressed(int packetId)
    {
        return (packetId & CompressionFlag) == CompressionFlag;
    }

    public static bool Decompress(ReadOnlyMemory<byte> src, byte[] dest)
    {
        if (!MemoryMarshal.TryGetArray(src, out var segment) || segment.Array is null)
        {
            return false;
        }

        using var memoryStream = new MemoryStream(segment.Array, segment.Offset, segment.Count);
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);

        int bytesRead, totalBytesRead = 0;
        while ((bytesRead = gzipStream.Read(dest, totalBytesRead, dest.Length - totalBytesRead)) > 0)
        {
            totalBytesRead += bytesRead;
        }

        return totalBytesRead == dest.Length;
    }
}