using System.Security.Cryptography;
using Server.Net;

namespace Server.Game.Net;

public sealed class GameSession(int id, INetworkChannel channel, GameSessionManager sessionManager) : IDisposable
{
    private const int InitialBufferSize = 0xFFFF;
    private const int MaxBufferSize = 64 * 1024 * 1024; // 64 MiB safety cap

    private readonly GamePacketParser _parser = new();
    private byte[] _buffer = new byte[InitialBufferSize];
    private int _bufferOffset;
    private bool _disposed;

    public int Id { get; } = id;
    public INetworkChannel Channel { get; } = channel;
    public Aes Aes { get; set; } = Aes.Create();

    public byte[] Decrypt(byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return Array.Empty<byte>();
        }

        // CBC requires whole blocks. If the client sent plaintext (or used the wrong key/IV),
        // the length is often not a multiple of 16.
        if ((bytes.Length & 0x0F) != 0)
        {
            return Array.Empty<byte>();
        }

        try
        {
            using var aes = Aes.Create();

            aes.Key = Aes.Key;
            aes.IV = Aes.IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);

            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();

            return memoryStream.ToArray();
        }
        catch (CryptographicException)
        {
            // Bad/early ciphertext should never crash the server. Callers can treat empty as "decrypt failed".
            return Array.Empty<byte>();
        }
    }
    
    public async ValueTask ParseAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        if (bytes.Length <= 0)
            return;

        // Ensure capacity for incoming bytes (allow dynamic growth beyond initial size)
        var required = _bufferOffset + bytes.Length;
        if (required > _buffer.Length)
        {
            var newCapacity = Math.Max(required, _buffer.Length * 2);
            if (newCapacity > MaxBufferSize)
            {
                throw new InvalidOperationException($"Receive buffer exceeded max size ({MaxBufferSize} bytes)");
            }

            Array.Resize(ref _buffer, newCapacity);
        }

        bytes.Span.CopyTo(_buffer.AsSpan(_bufferOffset));

        _bufferOffset += bytes.Length;
        if (_bufferOffset == 0)
        {
            return;
        }

        var consumed = await _parser.Parse(this, _buffer.AsMemory(0, _bufferOffset), cancellationToken).ConfigureAwait(false);
        if (consumed == 0)
        {
            return;
        }

        var bytesLeft = _bufferOffset - consumed;
        if (bytesLeft > 0)
        {
            _buffer.AsSpan(consumed, bytesLeft).CopyTo(_buffer.AsSpan(0));
        }

        _bufferOffset = bytesLeft;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        Aes.Dispose();
        
        sessionManager.Destroy(this);
    }
}