using Discord;

namespace Musico.Services;

public class EmbedService
{
    // Musico Brand Colors - Premium aesthetic
    private const string BOT_NAME = "Musico";
    private const string BOT_ICON = "🎵";
    private const string FOOTER_TEXT = "Musico • Premium Music Experience";
    
    // Color palette
    private static readonly Color PrimaryColor = new(138, 43, 226);      // Purple - main brand
    private static readonly Color SuccessColor = new(46, 204, 113);       // Green - success
    private static readonly Color ErrorColor = new(231, 76, 60);          // Red - error
    private static readonly Color InfoColor = new(52, 152, 219);          // Blue - info
    private static readonly Color WarningColor = new(241, 196, 15);       // Yellow - warning
    private static readonly Color NowPlayingColor = new(155, 89, 182);    // Violet - now playing
    private static readonly Color QueueColor = new(52, 73, 94);           // Dark blue - queue

    // Unicode characters for premium look
    private const string PROGRESS_FILLED = "━";
    private const string PROGRESS_EMPTY = "─";
    private const string PROGRESS_HEAD = "⬤";
    private const string BAR_FILLED = "▰";
    private const string BAR_EMPTY = "▱";

    public Embed CreateNowPlayingEmbed(string title, string author, string url, TimeSpan duration, string? thumbnail, string requester, int queueCount)
    {
        var embed = new EmbedBuilder()
            .WithColor(NowPlayingColor)
            .WithAuthor($"{BOT_ICON} Now Playing", null, null)
            .WithTitle(TruncateText(title, 256))
            .WithUrl(url)
            .WithDescription($"**by** `{author}`")
            .AddField("⏱️ Duration", $"`{FormatDuration(duration)}`", true)
            .AddField("📋 In Queue", $"`{queueCount} tracks`", true)
            .AddField("🎧 Requested by", $"`{requester}`", true)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(thumbnail))
            embed.WithThumbnailUrl(thumbnail);

        return embed.Build();
    }

    public Embed CreateTrackAddedEmbed(string title, string author, string url, TimeSpan duration, string? thumbnail, int position)
    {
        var embed = new EmbedBuilder()
            .WithColor(SuccessColor)
            .WithAuthor("✅ Added to Queue", null, null)
            .WithTitle(TruncateText(title, 256))
            .WithUrl(url)
            .WithDescription($"**by** `{author}`")
            .AddField("⏱️ Duration", $"`{FormatDuration(duration)}`", true)
            .AddField("📍 Position", $"`#{position}`", true)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(thumbnail))
            embed.WithThumbnailUrl(thumbnail);

        return embed.Build();
    }

    public Embed CreateQueueEmbed(List<(string Title, string Author, TimeSpan Duration, string Requester)> tracks, int page, int totalPages, string? currentTrack, TimeSpan totalDuration, int totalTracks)
    {
        var sb = new System.Text.StringBuilder();
        
        if (!string.IsNullOrEmpty(currentTrack))
        {
            sb.AppendLine($"🎵 **Now Playing:**");
            sb.AppendLine($"╰ {TruncateText(currentTrack, 45)}");
            sb.AppendLine();
        }

        if (tracks.Count == 0)
        {
            sb.AppendLine("```");
            sb.AppendLine("     📭 The queue is empty!");
            sb.AppendLine("    Use /play to add tracks");
            sb.AppendLine("```");
        }
        else
        {
            sb.AppendLine("**📋 Up Next:**");
            sb.AppendLine("```ml");
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var position = (page - 1) * 10 + i + 1;
                var trackTitle = TruncateText(track.Title, 35);
                var trackAuthor = TruncateText(track.Author, 15);
                sb.AppendLine($"{position,2}. {trackTitle}");
                sb.AppendLine($"    └ {trackAuthor} [{FormatDuration(track.Duration)}]");
            }
            sb.AppendLine("```");
        }

        return new EmbedBuilder()
            .WithColor(QueueColor)
            .WithAuthor($"{BOT_ICON} {BOT_NAME} Queue", null, null)
            .WithDescription(sb.ToString())
            .AddField("📊 Total", $"`{totalTracks}`", true)
            .AddField("⏱️ Duration", $"`{FormatDuration(totalDuration)}`", true)
            .AddField("📄 Page", $"`{page}/{totalPages}`", true)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateSuccessEmbed(string title, string description)
    {
        return new EmbedBuilder()
            .WithColor(SuccessColor)
            .WithAuthor($"✅ {title}", null, null)
            .WithDescription(description)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateErrorEmbed(string title, string description)
    {
        return new EmbedBuilder()
            .WithColor(ErrorColor)
            .WithAuthor($"❌ {title}", null, null)
            .WithDescription(description)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateWarningEmbed(string title, string description)
    {
        return new EmbedBuilder()
            .WithColor(WarningColor)
            .WithAuthor($"⚠️ {title}", null, null)
            .WithDescription(description)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateInfoEmbed(string title, string description)
    {
        return new EmbedBuilder()
            .WithColor(InfoColor)
            .WithAuthor($"ℹ️ {title}", null, null)
            .WithDescription(description)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreatePlayerStatusEmbed(string? currentTrack, bool isPaused, int volume, string loopMode, int queueCount, TimeSpan position, TimeSpan duration, string? thumbnail = null)
    {
        var status = isPaused ? "⏸️ Paused" : "▶️ Playing";
        var progressBar = CreateProgressBar(position, duration, 18);

        var sb = new System.Text.StringBuilder();
        
        if (string.IsNullOrEmpty(currentTrack))
        {
            sb.AppendLine("```");
            sb.AppendLine("   🎵 Nothing is currently playing");
            sb.AppendLine("```");
        }
        else
        {
            sb.AppendLine($"**🎵 {TruncateText(currentTrack, 45)}**");
            sb.AppendLine();
            sb.AppendLine($"{progressBar}");
            sb.AppendLine($"`{FormatDuration(position)}` ━━━━━━━━━━━━━━━━━━ `{FormatDuration(duration)}`");
        }

        var embed = new EmbedBuilder()
            .WithColor(NowPlayingColor)
            .WithAuthor($"{BOT_ICON} {BOT_NAME} Player", null, null)
            .WithDescription(sb.ToString())
            .AddField("📊 Status", $"`{status}`", true)
            .AddField("🔊 Volume", $"`{volume}%`", true)
            .AddField("🔁 Loop", $"`{loopMode}`", true)
            .AddField("📋 Queue", $"`{queueCount} tracks`", true)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(thumbnail))
            embed.WithThumbnailUrl(thumbnail);

        return embed.Build();
    }

    public Embed CreateVolumeEmbed(int volume)
    {
        var volumeBar = CreateVolumeBar(volume);
        var emoji = volume switch
        {
            0 => "🔇",
            < 30 => "🔈",
            < 70 => "🔉",
            _ => "🔊"
        };

        return new EmbedBuilder()
            .WithColor(PrimaryColor)
            .WithAuthor($"{emoji} Volume Adjusted", null, null)
            .WithDescription($"```\n{volumeBar}\n```\n**Volume:** `{volume}%`")
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateSeekEmbed(TimeSpan position, TimeSpan duration)
    {
        var progressBar = CreateProgressBar(position, duration, 18);

        return new EmbedBuilder()
            .WithColor(InfoColor)
            .WithAuthor("⏩ Seeked", null, null)
            .WithDescription($"{progressBar}\n`{FormatDuration(position)}` / `{FormatDuration(duration)}`")
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateLyricsEmbed(string title, string artist, string lyrics)
    {
        if (lyrics.Length > 4000)
            lyrics = lyrics[..4000] + "\n\n*[Lyrics truncated...]*";

        return new EmbedBuilder()
            .WithColor(PrimaryColor)
            .WithAuthor($"📝 Lyrics", null, null)
            .WithTitle(title)
            .WithDescription(lyrics)
            .WithFooter($"{FOOTER_TEXT} • Artist: {artist}", null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateHelpEmbed(string? avatarUrl = null)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("```ansi");
        sb.AppendLine("\u001b[1;35m══════════════════════════════════════\u001b[0m");
        sb.AppendLine($"\u001b[1;37m       🎵 {BOT_NAME} Command List       \u001b[0m");
        sb.AppendLine("\u001b[1;35m══════════════════════════════════════\u001b[0m");
        sb.AppendLine("```");

        var embed = new EmbedBuilder()
            .WithColor(PrimaryColor)
            .WithAuthor($"{BOT_ICON} {BOT_NAME} Help", avatarUrl, null)
            .WithDescription(sb.ToString())
            .AddField("🎵 Playback",
                "`/play` `/pause` `/resume` `/stop`\n`/skip` `/seek` `/replay` `/volume`", true)
            .AddField("📋 Queue",
                "`/queue` `/nowplaying` `/shuffle`\n`/clear` `/remove` `/move` `/skipto`", true)
            .AddField("🔁 Loop & More",
                "`/loop` `/playtop` `/playskip`\n`/search` `/247` `/disconnect`", true)
            .AddField("🎛️ Audio Filters",
                "`/bassboost` `/nightcore` `/vaporwave`\n`/speed` `/pitch` `/rotation` `/tremolo`", true)
            .AddField("ℹ️ Utility",
                "`/ping` `/stats` `/help`\n`/invite` `/serverinfo`", true)
            .AddField("🔧 Advanced",
                "`/vibrato` `/clearfilters`\n`/forceskip`", true)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(avatarUrl))
            embed.WithThumbnailUrl(avatarUrl);

        return embed.Build();
    }

    public Embed CreateStatsEmbed(int serverCount, int playerCount, long memoryUsage, TimeSpan uptime, int totalTracks, int latency, string? avatarUrl = null)
    {
        var memoryMb = memoryUsage / 1024 / 1024;
        var uptimeStr = FormatUptime(uptime);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("```ansi");
        sb.AppendLine("\u001b[1;35m╔══════════════════════════════════════╗\u001b[0m");
        sb.AppendLine($"\u001b[1;37m         📊 {BOT_NAME} Statistics         \u001b[0m");
        sb.AppendLine("\u001b[1;35m╚══════════════════════════════════════╝\u001b[0m");
        sb.AppendLine("```");

        var embed = new EmbedBuilder()
            .WithColor(PrimaryColor)
            .WithAuthor($"{BOT_ICON} {BOT_NAME} Stats", avatarUrl, null)
            .WithDescription(sb.ToString())
            .AddField("🏠 Servers", $"```{serverCount:N0}```", true)
            .AddField("🎵 Players", $"```{playerCount:N0}```", true)
            .AddField("📀 Tracks", $"```{totalTracks:N0}```", true)
            .AddField("📡 Latency", $"```{latency}ms```", true)
            .AddField("💾 Memory", $"```{memoryMb} MB```", true)
            .AddField("⏰ Uptime", $"```{uptimeStr}```", true)
            .WithFooter($"{FOOTER_TEXT} • v1.0.0", null)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(avatarUrl))
            embed.WithThumbnailUrl(avatarUrl);

        return embed.Build();
    }

    public Embed CreatePingEmbed(int websocketLatency, long messageLatency, string? avatarUrl = null)
    {
        var wsStatus = websocketLatency switch
        {
            < 100 => "🟢 Excellent",
            < 200 => "🟡 Good",
            < 400 => "🟠 Fair",
            _ => "🔴 Poor"
        };

        var msgStatus = messageLatency switch
        {
            < 100 => "🟢 Excellent",
            < 200 => "🟡 Good",
            < 400 => "🟠 Fair",
            _ => "🔴 Poor"
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("```ansi");
        sb.AppendLine("\u001b[1;32m  ╔═══════════════════════════════════╗\u001b[0m");
        sb.AppendLine("\u001b[1;37m            🏓 Pong!                  \u001b[0m");
        sb.AppendLine("\u001b[1;32m  ╚═══════════════════════════════════╝\u001b[0m");
        sb.AppendLine("```");

        return new EmbedBuilder()
            .WithColor(SuccessColor)
            .WithAuthor($"{BOT_ICON} {BOT_NAME} Latency", avatarUrl, null)
            .WithDescription(sb.ToString())
            .AddField("📡 WebSocket", $"```{websocketLatency}ms```{wsStatus}", true)
            .AddField("💬 Response", $"```{messageLatency}ms```{msgStatus}", true)
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateSearchResultsEmbed(List<(string Title, string Author, TimeSpan Duration, string Url)> results)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("```ml");
        for (int i = 0; i < Math.Min(results.Count, 5); i++)
        {
            var track = results[i];
            var trackTitle = TruncateText(track.Title, 40);
            var trackAuthor = TruncateText(track.Author, 20);
            sb.AppendLine($"{i + 1}. {trackTitle}");
            sb.AppendLine($"   └ {trackAuthor} [{FormatDuration(track.Duration)}]");
        }
        sb.AppendLine("```");

        return new EmbedBuilder()
            .WithColor(InfoColor)
            .WithAuthor($"🔎 Search Results", null, null)
            .WithDescription(sb.ToString())
            .WithFooter($"{FOOTER_TEXT} • Select using buttons below", null)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed CreateInviteEmbed(string inviteUrl, string? avatarUrl = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("```ansi");
        sb.AppendLine("\u001b[1;35m╔══════════════════════════════════════╗\u001b[0m");
        sb.AppendLine($"\u001b[1;37m        🎵 Invite {BOT_NAME}!              \u001b[0m");
        sb.AppendLine("\u001b[1;35m╚══════════════════════════════════════╝\u001b[0m");
        sb.AppendLine("```");
        sb.AppendLine("**Bring premium music to your server!**");
        sb.AppendLine();
        sb.AppendLine("**Required Permissions:**");
        sb.AppendLine("```");
        sb.AppendLine("• Connect to Voice Channels");
        sb.AppendLine("• Speak in Voice Channels");
        sb.AppendLine("• Send Messages");
        sb.AppendLine("• Embed Links");
        sb.AppendLine("• Use Slash Commands");
        sb.AppendLine("```");

        var embed = new EmbedBuilder()
            .WithColor(PrimaryColor)
            .WithAuthor($"{BOT_ICON} {BOT_NAME}", avatarUrl, null)
            .WithDescription(sb.ToString())
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(avatarUrl))
            embed.WithThumbnailUrl(avatarUrl);

        return embed.Build();
    }

    public Embed CreateFilterEmbed(string filterName, string status, string emoji)
    {
        return new EmbedBuilder()
            .WithColor(PrimaryColor)
            .WithAuthor($"{emoji} {filterName}", null, null)
            .WithDescription($"**Status:** {status}")
            .WithFooter(FOOTER_TEXT, null)
            .WithCurrentTimestamp()
            .Build();
    }

    // Helper methods
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return duration.ToString(@"h\:mm\:ss");
        return duration.ToString(@"m\:ss");
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }

    private static string CreateProgressBar(TimeSpan current, TimeSpan total, int length)
    {
        var progress = total.TotalSeconds > 0 ? current.TotalSeconds / total.TotalSeconds : 0;
        var filled = (int)(progress * length);
        var empty = length - filled - 1;

        var bar = new string(PROGRESS_FILLED[0], filled) + PROGRESS_HEAD + new string(PROGRESS_EMPTY[0], Math.Max(0, empty));
        return bar;
    }

    private static string CreateVolumeBar(int volume)
    {
        var normalized = volume * 15 / 150;
        var filled = Math.Min(normalized, 15);
        var empty = 15 - filled;
        return new string(BAR_FILLED[0], filled) + new string(BAR_EMPTY[0], empty);
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "Unknown";
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
