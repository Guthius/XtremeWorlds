using System.Reflection;
using Core.Globals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using Server.Services;

// Get the directory where the executable is located
var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

// Create builder with content root set to executable's directory
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = exeDir,
    Args = args
});

// Configure services and logging
builder.Services.AddHostedService<GameService>();
builder.Services.AddSingleton<IPlayerService, PlayerService>();
builder.Services.AddNetworkService<GameSession, GameSessionManager, GameNetworkService>();
builder.Services.AddHostedService<ConsoleInputService>();
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    Directory.CreateDirectory(DataPath.Logs);
    var hasConsoleSink = builder.Configuration
        .GetSection("Serilog:WriteTo")
        .GetChildren()
        .Any(x => string.Equals(x["Name"], "Console", StringComparison.OrdinalIgnoreCase));

    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services);

    if (!hasConsoleSink)
    {
        loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
    }

    loggerConfiguration.WriteTo.File(
        path: DataPath.Logs,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true
    );
});
var app = builder.Build();
await app.RunAsync();
