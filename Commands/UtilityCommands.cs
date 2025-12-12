using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Musico.Services;
using System.Diagnostics;

namespace Musico.Commands;

public class UtilityCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MusicService _musicService;
    private readonly EmbedService _embedService;
    private readonly DiscordSocketClient _client;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public UtilityCommands(MusicService musicService, EmbedService embedService, DiscordSocketClient client)
    {
        _musicService = musicService;
        _embedService = embedService;
        _client = client;
    }

    [SlashCommand("ping", "Check the bot's latency")]
    public async Task PingAsync()
    {
        var sw = Stopwatch.StartNew();
        await DeferAsync();
        sw.Stop();

        var avatarUrl = _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl();
        await FollowupAsync(embed: _embedService.CreatePingEmbed(_client.Latency, sw.ElapsedMilliseconds, avatarUrl));
    }

    [SlashCommand("stats", "View bot statistics")]
    public async Task StatsAsync()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var memoryUsage = Process.GetCurrentProcess().WorkingSet64;
        var playerCount = _musicService.GetActivePlayerCount();
        var avatarUrl = _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl();

        await RespondAsync(embed: _embedService.CreateStatsEmbed(
            _client.Guilds.Count,
            playerCount,
            memoryUsage,
            uptime,
            _musicService.TotalTracksPlayed,
            _client.Latency,
            avatarUrl));
    }

    [SlashCommand("help", "Show all available commands")]
    public async Task HelpAsync()
    {
        var avatarUrl = _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl();
        await RespondAsync(embed: _embedService.CreateHelpEmbed(avatarUrl));
    }

    [SlashCommand("invite", "Get the bot invite link")]
    public async Task InviteAsync()
    {
        var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={_client.CurrentUser.Id}&permissions=3147776&scope=bot%20applications.commands";
        var avatarUrl = _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl();

        var components = new ComponentBuilder()
            .WithButton("Invite Musico", style: ButtonStyle.Link, url: inviteUrl, emote: new Emoji("🎵"))
            .WithButton("Support Server", style: ButtonStyle.Link, url: "https://discord.gg/musico", emote: new Emoji("💬"))
            .Build();

        await RespondAsync(embed: _embedService.CreateInviteEmbed(inviteUrl, avatarUrl), components: components);
    }

    [SlashCommand("serverinfo", "Get information about the server")]
    public async Task ServerInfoAsync()
    {
        var guild = Context.Guild;
        var owner = guild.Owner;

        var embed = new EmbedBuilder()
            .WithColor(new Color(138, 43, 226))
            .WithAuthor("🏠 Server Information", null, null)
            .WithTitle(guild.Name)
            .WithThumbnailUrl(guild.IconUrl ?? "")
            .AddField("👑 Owner", $"`{owner?.Username ?? "Unknown"}`", true)
            .AddField("👥 Members", $"`{guild.MemberCount:N0}`", true)
            .AddField("📅 Created", $"<t:{guild.CreatedAt.ToUnixTimeSeconds()}:R>", true)
            .AddField("💬 Text", $"`{guild.TextChannels.Count}`", true)
            .AddField("🔊 Voice", $"`{guild.VoiceChannels.Count}`", true)
            .AddField("🎭 Roles", $"`{guild.Roles.Count}`", true)
            .AddField("🆔 Server ID", $"`{guild.Id}`", false)
            .WithFooter("Musico • Premium Music Experience")
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("userinfo", "Get information about a user")]
    public async Task UserInfoAsync([Summary("user", "The user to get info about")] IUser? user = null)
    {
        user ??= Context.User;
        var guildUser = user as IGuildUser;

        var embed = new EmbedBuilder()
            .WithColor(new Color(138, 43, 226))
            .WithAuthor("👤 User Information", null, null)
            .WithTitle(user.Username)
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .AddField("📛 Display Name", $"`{user.GlobalName ?? user.Username}`", true)
            .AddField("🆔 User ID", $"`{user.Id}`", true)
            .AddField("🤖 Bot", $"`{(user.IsBot ? "Yes" : "No")}`", true)
            .AddField("📅 Account Created", $"<t:{user.CreatedAt.ToUnixTimeSeconds()}:R>", true);

        if (guildUser is not null)
        {
            embed.AddField("📥 Joined Server", guildUser.JoinedAt.HasValue 
                ? $"<t:{guildUser.JoinedAt.Value.ToUnixTimeSeconds()}:R>" 
                : "`Unknown`", true);
            
            if (guildUser.Nickname is not null)
                embed.AddField("📝 Nickname", $"`{guildUser.Nickname}`", true);
        }

        embed.WithFooter("Musico • Premium Music Experience")
            .WithCurrentTimestamp();

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("avatar", "Get a user's avatar")]
    public async Task AvatarAsync([Summary("user", "The user to get avatar of")] IUser? user = null)
    {
        user ??= Context.User;

        var avatarUrl = user.GetAvatarUrl(ImageFormat.Auto, 1024) ?? user.GetDefaultAvatarUrl();

        var embed = new EmbedBuilder()
            .WithColor(new Color(138, 43, 226))
            .WithAuthor("🖼️ Avatar", null, null)
            .WithTitle(user.Username)
            .WithImageUrl(avatarUrl)
            .WithFooter("Musico • Premium Music Experience")
            .WithCurrentTimestamp()
            .Build();

        var components = new ComponentBuilder()
            .WithButton("Open Original", style: ButtonStyle.Link, url: avatarUrl, emote: new Emoji("🔗"))
            .Build();

        await RespondAsync(embed: embed, components: components);
    }
}
