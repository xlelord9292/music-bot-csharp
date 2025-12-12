using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Musico.Services;

public class BotHostedService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<BotHostedService> _logger;
    private const string BOT_NAME = "Musico";

    public BotHostedService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<BotHostedService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _interactions.Log += LogAsync;

        // Handle interaction created - fire and forget for performance
        _client.InteractionCreated += HandleInteractionAsync;

        // Load command modules
        await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Discord token not found in environment variables!");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _logger.LogInformation($"{BOT_NAME} started successfully!");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        _logger.LogInformation($"{BOT_NAME} stopped.");
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation($"Logged in as {_client.CurrentUser.Username}");
        _logger.LogInformation($"Serving {_client.Guilds.Count} guilds");

        // Register commands globally
        await _interactions.RegisterCommandsGloballyAsync();
        _logger.LogInformation("Slash commands registered globally!");

        // Set bot status with rotating activities
        await UpdatePresenceAsync();
    }

    private async Task UpdatePresenceAsync()
    {
        await _client.SetActivityAsync(new Game($"/play | {_client.Guilds.Count} servers", ActivityType.Listening));
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            
            // Execute command asynchronously for better performance
            _ = Task.Run(async () =>
            {
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess && result.Error != InteractionCommandError.UnknownCommand)
                {
                    _logger.LogWarning($"Command failed: {result.ErrorReason}");
                    
                    if (interaction.Type == InteractionType.ApplicationCommand && !interaction.HasResponded)
                    {
                        await interaction.RespondAsync($"❌ An error occurred: {result.ErrorReason}", ephemeral: true);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");
        }
        
        await Task.CompletedTask;
    }

    private Task LogAsync(LogMessage log)
    {
        var severity = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(severity, log.Exception, "[{Source}] {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }
}
