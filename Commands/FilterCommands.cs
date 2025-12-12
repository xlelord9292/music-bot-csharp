using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Filters;
using Musico.Services;

namespace Musico.Commands;

public class FilterCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MusicService _musicService;
    private readonly EmbedService _embedService;

    public FilterCommands(MusicService musicService, EmbedService embedService)
    {
        _musicService = musicService;
        _embedService = embedService;
    }

    [SlashCommand("bassboost", "Apply bass boost effect")]
    public async Task BassBoostAsync([Summary("level", "Bass boost level")] BassBoostLevel level = BassBoostLevel.Medium)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        // Use LowPass filter as an alternative bass boost effect
        if (level == BassBoostLevel.Off)
        {
            player.Filters.LowPass = null;
        }
        else
        {
            var smoothing = level switch
            {
                BassBoostLevel.Low => 10f,
                BassBoostLevel.Medium => 15f,
                BassBoostLevel.High => 20f,
                BassBoostLevel.Extreme => 25f,
                _ => 15f
            };
            player.Filters.LowPass = new LowPassFilterOptions(smoothing);
        }
        await player.Filters.CommitAsync();

        var emoji = level switch
        {
            BassBoostLevel.Off => "🔇",
            BassBoostLevel.Low => "🔈",
            BassBoostLevel.Medium => "🔉",
            BassBoostLevel.High => "🔊",
            BassBoostLevel.Extreme => "💥",
            _ => "🎵"
        };

        await RespondAsync(embed: _embedService.CreateFilterEmbed("Bass Boost", $"Set to **{level}**", emoji));
    }

    [SlashCommand("nightcore", "Apply nightcore effect")]
    public async Task NightcoreAsync([Summary("enabled", "Enable or disable")] bool enabled = true)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (enabled)
        {
            player.Filters.Timescale = new TimescaleFilterOptions(1.25f, 1.25f, 1f);
        }
        else
        {
            player.Filters.Timescale = null;
        }
        await player.Filters.CommitAsync();

        var status = enabled ? "**Enabled**" : "**Disabled**";
        await RespondAsync(embed: _embedService.CreateFilterEmbed("Nightcore", status, enabled ? "🌙" : "🔇"));
    }

    [SlashCommand("vaporwave", "Apply vaporwave effect")]
    public async Task VaporwaveAsync([Summary("enabled", "Enable or disable")] bool enabled = true)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (enabled)
        {
            player.Filters.Timescale = new TimescaleFilterOptions(0.8f, 0.8f, 1f);
        }
        else
        {
            player.Filters.Timescale = null;
        }
        await player.Filters.CommitAsync();

        var status = enabled ? "**Enabled**" : "**Disabled**";
        await RespondAsync(embed: _embedService.CreateFilterEmbed("Vaporwave", status, enabled ? "🌊" : "🔇"));
    }

    [SlashCommand("speed", "Change playback speed")]
    public async Task SpeedAsync([Summary("rate", "Speed multiplier (0.5 - 2.0)")] float rate)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        rate = Math.Clamp(rate, 0.5f, 2.0f);
        player.Filters.Timescale = new TimescaleFilterOptions(rate, 1f, 1f);
        await player.Filters.CommitAsync();

        await RespondAsync(embed: _embedService.CreateFilterEmbed("Speed", $"Set to **{rate}x**", "⚡"));
    }

    [SlashCommand("pitch", "Change playback pitch")]
    public async Task PitchAsync([Summary("level", "Pitch multiplier (0.5 - 2.0)")] float level)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        level = Math.Clamp(level, 0.5f, 2.0f);
        player.Filters.Timescale = new TimescaleFilterOptions(1f, level, 1f);
        await player.Filters.CommitAsync();

        await RespondAsync(embed: _embedService.CreateFilterEmbed("Pitch", $"Set to **{level}x**", "🎼"));
    }

    [SlashCommand("rotation", "Apply rotation (8D audio) effect")]
    public async Task RotationAsync([Summary("frequency", "Rotation frequency in Hz")] float frequency = 0.2f)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (frequency <= 0)
        {
            player.Filters.Rotation = null;
        }
        else
        {
            frequency = Math.Clamp(frequency, 0.01f, 1f);
            player.Filters.Rotation = new RotationFilterOptions(frequency);
        }
        await player.Filters.CommitAsync();

        var status = frequency <= 0 ? "**Disabled**" : $"Set to **{frequency}Hz**";
        await RespondAsync(embed: _embedService.CreateFilterEmbed("8D Audio", status, frequency <= 0 ? "🔇" : "🎧"));
    }

    [SlashCommand("tremolo", "Apply tremolo effect")]
    public async Task TremoloAsync(
        [Summary("frequency", "Tremolo frequency")] float frequency = 4f,
        [Summary("depth", "Tremolo depth (0-1)")] float depth = 0.5f)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        frequency = Math.Clamp(frequency, 0.1f, 20f);
        depth = Math.Clamp(depth, 0.01f, 1f);

        player.Filters.Tremolo = new TremoloFilterOptions(frequency, depth);
        await player.Filters.CommitAsync();

        await RespondAsync(embed: _embedService.CreateFilterEmbed("Tremolo", $"Frequency: **{frequency}** | Depth: **{depth}**", "🎸"));
    }

    [SlashCommand("vibrato", "Apply vibrato effect")]
    public async Task VibratoAsync(
        [Summary("frequency", "Vibrato frequency")] float frequency = 4f,
        [Summary("depth", "Vibrato depth (0-1)")] float depth = 0.5f)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        frequency = Math.Clamp(frequency, 0.1f, 14f);
        depth = Math.Clamp(depth, 0.01f, 1f);

        player.Filters.Vibrato = new VibratoFilterOptions(frequency, depth);
        await player.Filters.CommitAsync();

        await RespondAsync(embed: _embedService.CreateFilterEmbed("Vibrato", $"Frequency: **{frequency}** | Depth: **{depth}**", "🎻"));
    }

    [SlashCommand("karaoke", "Apply karaoke effect (reduce vocals)")]
    public async Task KaraokeAsync([Summary("enabled", "Enable or disable")] bool enabled = true)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (enabled)
        {
            player.Filters.Karaoke = new KaraokeFilterOptions(3f, 1f, 1f, 1f);
        }
        else
        {
            player.Filters.Karaoke = null;
        }
        await player.Filters.CommitAsync();

        var status = enabled ? "**Enabled**" : "**Disabled**";
        await RespondAsync(embed: _embedService.CreateFilterEmbed("Karaoke", status, enabled ? "🎤" : "🔇"));
    }

    [SlashCommand("clearfilters", "Remove all audio filters")]
    public async Task ClearFiltersAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        player.Filters.Equalizer = null;
        player.Filters.Timescale = null;
        player.Filters.Tremolo = null;
        player.Filters.Vibrato = null;
        player.Filters.Rotation = null;
        player.Filters.Karaoke = null;
        player.Filters.LowPass = null;
        player.Filters.ChannelMix = null;
        player.Filters.Distortion = null;
        await player.Filters.CommitAsync();

        await RespondAsync(embed: _embedService.CreateFilterEmbed("Filters Cleared", "All audio filters have been removed", "🧹"));
    }
}

public enum BassBoostLevel
{
    Off,
    Low,
    Medium,
    High,
    Extreme
}