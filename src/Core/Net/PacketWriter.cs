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

        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Fastest))
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

#if DEBUG
    [Obsolete("WriteInt16 requires a 'short'. Use WriteByte for 1-byte values.", true)]
    public void WriteInt16(byte value) => throw new NotSupportedException();

    [Obsolete("WriteInt16 requires a 'short'. Choose the correct width explicitly.", true)]
    public void WriteInt16(sbyte value) => throw new NotSupportedException();
#endif

    public void WriteUInt16(ushort value)
    {
        EnsureSpaceAvailable(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(ushort);
    }

#if DEBUG
    [Obsolete("WriteUInt16 requires a 'ushort'. Use WriteByte for 1-byte values.", true)]
    public void WriteUInt16(byte value) => throw new NotSupportedException();

    [Obsolete("WriteUInt16 requires a 'ushort'. Use WriteChar for 'char' values.", true)]
    public void WriteUInt16(char value) => throw new NotSupportedException();
#endif

    public void WriteInt32(int value)
    {
        EnsureSpaceAvailable(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(int);
    }

#if DEBUG
    [Obsolete("WriteInt32 requires an 'int'. Use WriteByte/WriteUInt16/WriteInt16 for smaller widths (or cast to int explicitly if the wire type is Int32).", true)]
    public void WriteInt32(byte value) => throw new NotSupportedException();

    [Obsolete("WriteInt32 requires an 'int'. Choose the correct width explicitly.", true)]
    public void WriteInt32(sbyte value) => throw new NotSupportedException();

    [Obsolete("WriteInt32 requires an 'int'. Use WriteInt16 for 2-byte signed values (or cast to int explicitly if the wire type is Int32).", true)]
    public void WriteInt32(short value) => throw new NotSupportedException();

    [Obsolete("WriteInt32 requires an 'int'. Use WriteUInt16 for 2-byte unsigned values (or cast to int explicitly if the wire type is Int32).", true)]
    public void WriteInt32(ushort value) => throw new NotSupportedException();

    [Obsolete("WriteInt32 requires an 'int'. Use WriteChar for 'char' values.", true)]
    public void WriteInt32(char value) => throw new NotSupportedException();
#endif

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

#if DEBUG
    [Obsolete("WriteUInt32 requires a 'uint'. Use WriteByte/WriteUInt16 for smaller widths (or cast to uint explicitly if the wire type is UInt32).", true)]
    public void WriteUInt32(byte value) => throw new NotSupportedException();

    [Obsolete("WriteUInt32 requires a 'uint'. Use WriteUInt16 for 2-byte unsigned values (or cast to uint explicitly if the wire type is UInt32).", true)]
    public void WriteUInt32(ushort value) => throw new NotSupportedException();

    [Obsolete("WriteUInt32 requires a 'uint'. Use WriteChar for 'char' values.", true)]
    public void WriteUInt32(char value) => throw new NotSupportedException();
#endif

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

#if DEBUG
    [Obsolete("WriteInt64 requires a 'long'. Use WriteInt32 for 4-byte signed values (or cast to long explicitly if the wire type is Int64).", true)]
    public void WriteInt64(int value) => throw new NotSupportedException();

    [Obsolete("WriteInt64 requires a 'long'. Use WriteUInt32 for 4-byte unsigned values (or cast to long explicitly if the wire type is Int64).", true)]
    public void WriteInt64(uint value) => throw new NotSupportedException();

    [Obsolete("WriteInt64 requires a 'long'. Use WriteInt16 for 2-byte signed values.", true)]
    public void WriteInt64(short value) => throw new NotSupportedException();

    [Obsolete("WriteInt64 requires a 'long'. Use WriteUInt16 for 2-byte unsigned values.", true)]
    public void WriteInt64(ushort value) => throw new NotSupportedException();

    [Obsolete("WriteInt64 requires a 'long'. Use WriteByte for 1-byte values.", true)]
    public void WriteInt64(byte value) => throw new NotSupportedException();
#endif

    public void WriteUInt64(ulong value)
    {
        EnsureSpaceAvailable(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(ulong);
    }

#if DEBUG
    [Obsolete("WriteUInt64 requires an 'ulong'. Use WriteUInt32 for 4-byte unsigned values (or cast to ulong explicitly if the wire type is UInt64).", true)]
    public void WriteUInt64(uint value) => throw new NotSupportedException();

    [Obsolete("WriteUInt64 requires an 'ulong'. Use WriteInt32 for 4-byte signed values (or cast to ulong explicitly if the wire type is UInt64).", true)]
    public void WriteUInt64(int value) => throw new NotSupportedException();

    [Obsolete("WriteUInt64 requires an 'ulong'. Use WriteUInt16 for 2-byte unsigned values.", true)]
    public void WriteUInt64(ushort value) => throw new NotSupportedException();

    [Obsolete("WriteUInt64 requires an 'ulong'. Use WriteByte for 1-byte values.", true)]
    public void WriteUInt64(byte value) => throw new NotSupportedException();
#endif

    public void WriteDouble(double value)
    {
        EnsureSpaceAvailable(sizeof(double));
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_offset), value);
        _offset += sizeof(double);
    }

#if DEBUG
    [Obsolete("WriteDouble requires a 'double'. Use WriteSingle for 4-byte floats.", true)]
    public void WriteDouble(float value) => throw new NotSupportedException();
#endif
}