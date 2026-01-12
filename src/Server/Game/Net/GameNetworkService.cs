using System.Security.Cryptography;
using Server.Game.Net.Protocol;
using Server.Net;

namespace Server.Game.Net;

public sealed class GameNetworkService : NetworkService<GameSession>
{
    public override Task OnConnectedAsync(GameSession session, CancellationToken cancellationToken)
    {
        session.Aes = Aes.Create();
        Console.WriteLine($"Client connected (id={session.Id}); sending SAes");
        session.Channel.Send(new AesPacket(session.Aes.Key, session.Aes.IV));

        return Task.CompletedTask;
    }

    public override async System.Threading.Tasks.Task OnDisconnectedAsync(GameSession session, CancellationToken cancellationToken)
    {
        await Server.Player.OnExit(session.Id);
    }

    public override Task OnBytesReceivedAsync(GameSession session, ReadOnlySpan<byte> bytes, CancellationToken cancellationToken)
    {
        try
        {
            // Cannot await with ReadOnlySpan<byte> parameter; copy to heap memory first.
            var data = bytes.ToArray();
            var task = session.ParseAsync(data, cancellationToken).AsTask();
            _ = task.ContinueWith(t =>
            {
                Console.WriteLine($"Parse error from {session.Channel.IpAddress} (id={session.Id}): {t.Exception?.GetBaseException().Message}");
                session.Channel.Close();
            }, TaskContinuationOptions.OnlyOnFaulted);
            return task;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parse error from {session.Channel.IpAddress} (id={session.Id}): {ex.Message}");
            session.Channel.Close();
            return Task.CompletedTask;
        }
    }
}