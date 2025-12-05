using Microsoft.Extensions.Hosting;

namespace Server.Services;

public sealed class ConsoleInputService : BackgroundService
{
    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Console.IsInputRedirected)
        {
            return;
        }
        
        await using var stream = Console.OpenStandardInput();

        using var streamReader = new StreamReader(stream);

        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await streamReader.ReadLineAsync(stoppingToken);
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var command = line.Split(' ');
            if (command.Length < 1)
            {
                continue;
            }

            await General.HandlePlayerCommandAsync(command);
        }
    }
}