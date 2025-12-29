using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Core.Net;

public sealed class PacketWriter(int capacity = PacketWriter.InitialCapacity)
{
    private const int InitialCapacity = 8192;
    private const int CompressionThreshold = 128;
    private const uint CompressionFlag = 1u << 31;

    // Hard upper bound to avoid pathological allocations from accidental misuse.
    // This is the *uncompressed* payload size (not counting the 4-byte length prefix added by GetBytes()).
    private const int MaxPayloadSize = 64 * 1024 * 1024;

    private byte[] _buffer = new byte[capacity < 0 ? throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.") : capacity];
    private int _offset;

    private void EnsureSpaceAvailable(int space)
    {
        if (space < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(space), "Space cannot be negative.");
        }

        var requiredSizeLong = (long)_offset + space;
        if (requiredSizeLong > MaxPayloadSize)
        {
            throw new InvalidOperationException($"PacketWriter exceeded max payload size ({MaxPayloadSize} bytes). Requested={requiredSizeLong}.");
        }

        var requiredSize = (int)requiredSizeLong;
        if (requiredSize <= _buffer.Length)
        {
            return;
        }

        var doubled = (long)_buffer.Length * 2;
        var desiredCapacityLong = Math.Max(requiredSizeLong, doubled);
        if (desiredCapacityLong > MaxPayloadSize)
        {
            desiredCapacityLong = MaxPayloadSize;
        }

        var desiredCapacity = (int)desiredCapacityLong;

        Array.Resize(ref _buffer, desiredCapacity);
    }

    public byte[] GetBytes()
    {
        if (_offset > CompressionThreshold)
        {
            return GetBytesCompressed();
        }

        var packet = new byte[4 + _offset];

        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), _offset);
        if (_offset > 0)
        {
            _buffer.AsSpan(0, _offset).CopyTo(packet.AsSpan(4));
        }

        return packet;
    }
    
    private byte[] GetBytesCompressed()
    {
        if (_offset < 4)
        {
            throw new InvalidOperationException("Cannot compress a packet without a 4-byte packet id header.");
        }

        var packetId = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(0, 4));

        if ((packetId & CompressionFlag) != 0)
        {
            throw new InvalidOperationException("Packet id already contains the compression flag bit; cannot safely mark as compressed.");
        }

        packetId |= CompressionFlag;

        var uncompressedSize = _offset - 4;
        var compressedBytes = Compress(_buffer, 4, uncompressedSize);
        var compressedSize = 8 + compressedBytes.Length;

        var packet = new byte[4 + compressedSize];

        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), compressedSize);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(4, 4), packetId);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), uncompressedSize);

        if (compressedBytes.Length > 0)
        {
            compressedBytes.CopyTo(packet.AsSpan(12));
        }

        return packet;
    }

    private static byte[] Compress(byte[] src, int offset, int count)
    {
        using var memoryStream = new MemoryStream();

        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(src, offset, count);
        }

        return memoryStream.ToArray();
    }

    public void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return;
        }

        EnsureSpaceAvailable(bytes.Length);

        bytes.CopyTo(_buffer.AsSpan(_offset));

        _offset += bytes.Length;
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        WriteInt32(value.Length);
        WriteRaw(value);
    }

    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteInt32(0);

            return;
        }

        var length = Encoding.UTF8.GetByteCount(value);

        EnsureSpaceAvailable(4 + length);

        WriteInt32(length);

        var bytesWritten = Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_offset));

        _offset += bytesWritten;
    }

    public void WriteChar(char value)
    {
        EnsureSpaceAvailable(sizeof(char));

        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_offset), value);

        _offset += sizeof(char);
    }

    public void WriteByte(byte value)
    {
        EnsureSpaceAvailable(sizeof(byte));

        _buffer[_offset++] = value;
    }

    public void WriteBoolean(bool value)
    {
        WriteByte((byte) (value ? 1 : 0));
    }

    public void WriteInt16(short value)
    {
        EnsureSpaceAvailable(sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(short);
    }

    public void WriteUInt16(ushort value)
    {
        EnsureSpaceAvailable(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(ushort);
    }

    public void WriteInt32(int value)
    {
        EnsureSpaceAvailable(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(int);
    }

    public void WriteEnum<T>(T value) where T : Enum
    {
        // The wire protocol uses 32-bit integers for enum values.
        // Guard against enums backed by 64-bit types (or values that overflow int32).
        var underlying = Enum.GetUnderlyingType(typeof(T));
        if (underlying == typeof(long) || underlying == typeof(ulong))
        {
            throw new NotSupportedException($"Enum '{typeof(T).FullName}' is backed by {underlying.Name}; PacketWriter only supports enums representable as Int32.");
        }

        int intValue;
        try
        {
            intValue = checked(Convert.ToInt32(value));
        }
        catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Enum value for '{typeof(T).FullName}' is not representable as Int32.");
        }

        WriteInt32(intValue);
    }

    public void WriteUInt32(uint value)
    {
        EnsureSpaceAvailable(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(uint);
    }

    public void WriteSingle(float value)
    {
        EnsureSpaceAvailable(sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(float);
    }

    public void WriteInt64(long value)
    {
        EnsureSpaceAvailable(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(long);
    }

    public void WriteUInt64(ulong value)
    {
        EnsureSpaceAvailable(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(ulong);
    }

    public void WriteDouble(double value)
    {
        EnsureSpaceAvailable(sizeof(double));
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(double);
    }
}