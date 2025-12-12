using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetEnv;
using Musico.Services;
using System.Net;

namespace Musico;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load environment variables
        Env.Load();

        // Optimize thread pool for high performance
        ThreadPool.SetMinThreads(100, 100);
        
        // Enable high performance socket mode
        ServicePointManager.DefaultConnectionLimit = 100;
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.UseNagleAlgorithm = false;

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Discord client configuration - optimized for performance
                services.AddSingleton(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds | 
                                    GatewayIntents.GuildVoiceStates,
                    LogLevel = LogSeverity.Warning,
                    AlwaysDownloadUsers = false,
                    MessageCacheSize = 0,  // Disable message cache for memory efficiency
                    ConnectionTimeout = 30000,
                    HandlerTimeout = 3000,
                    UseInteractionSnowflakeDate = false,
                    UseSystemClock = true
                });

                // Discord socket client
                services.AddSingleton<DiscordSocketClient>();

                // Interaction service for slash commands - optimized
                services.AddSingleton(x => new InteractionService(
                    x.GetRequiredService<DiscordSocketClient>(),
                    new InteractionServiceConfig
                    {
                        LogLevel = LogSeverity.Warning,
                        DefaultRunMode = RunMode.Async,
                        UseCompiledLambda = true,  // Faster command execution
                        AutoServiceScopes = true
                    }));

                // Lavalink4NET configuration for V4 - optimized
                services.AddLavalink();
                services.ConfigureLavalink(options =>
                {
                    var host = Environment.GetEnvironmentVariable("LAVALINK_HOST") ?? "localhost";
                    var port = Environment.GetEnvironmentVariable("LAVALINK_PORT") ?? "2333";
                    var secure = Environment.GetEnvironmentVariable("LAVALINK_SECURE")?.ToLower() == "true";
                    var protocol = secure ? "https" : "http";
                    var wsProtocol = secure ? "wss" : "ws";
                    
                    options.BaseAddress = new Uri($"{protocol}://{host}:{port}");
                    options.WebSocketUri = new Uri($"{wsProtocol}://{host}:{port}/v4/websocket");
                    options.Passphrase = Environment.GetEnvironmentVariable("LAVALINK_PASSWORD") ?? "youshallnotpass";
                    options.ReadyTimeout = TimeSpan.FromSeconds(10);
                    options.ResumptionOptions = new Lavalink4NET.LavalinkSessionResumptionOptions(TimeSpan.FromSeconds(60));
                });

                // Custom services
                services.AddSingleton<EmbedService>();
                services.AddSingleton<MusicService>();
                
                // Hosted service to start the bot
                services.AddHostedService<BotHostedService>();
            })
            .Build();

        await host.RunAsync();
    }
}
