using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Rest.Entities.Tracks;
using Musico.Services;

namespace Musico.Commands;

public class QueueCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MusicService _musicService;
    private readonly EmbedService _embedService;

    public QueueCommands(MusicService musicService, EmbedService embedService)
    {
        _musicService = musicService;
        _embedService = embedService;
    }

    [SlashCommand("skip", "Skip the current track")]
    public async Task SkipAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        var currentTrack = player.CurrentTrack;
        if (currentTrack is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Nothing Playing", "There is nothing currently playing!"), ephemeral: true);
            return;
        }

        await _musicService.SkipAsync(player);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Skipped", $"⏭️ Skipped **{currentTrack.Title}**"));
    }

    [SlashCommand("stop", "Stop playback and clear the queue")]
    public async Task StopAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        await _musicService.StopAsync(player);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Stopped", "⏹️ Playback stopped and queue cleared."));
    }

    [SlashCommand("pause", "Pause the current track")]
    public async Task PauseAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (await _musicService.PauseAsync(player))
        {
            await RespondAsync(embed: _embedService.CreateSuccessEmbed("Paused", "⏸️ Playback paused. Use `/resume` to continue."));
        }
        else
        {
            await RespondAsync(embed: _embedService.CreateWarningEmbed("Already Paused", "The player is already paused!"), ephemeral: true);
        }
    }

    [SlashCommand("resume", "Resume the paused track")]
    public async Task ResumeAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (await _musicService.ResumeAsync(player))
        {
            await RespondAsync(embed: _embedService.CreateSuccessEmbed("Resumed", "▶️ Playback resumed!"));
        }
        else
        {
            await RespondAsync(embed: _embedService.CreateWarningEmbed("Not Paused", "The player is not paused!"), ephemeral: true);
        }
    }

    [SlashCommand("queue", "View the current queue")]
    public async Task QueueAsync([Summary("page", "Page number")] int page = 1)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        var queueItems = player.Queue.ToList();
        var totalTracks = queueItems.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalTracks / 10.0));
        page = Math.Clamp(page, 1, totalPages);

        var pageItems = queueItems
            .Skip((page - 1) * 10)
            .Take(10)
            .Select(x => (x.Track!.Title, x.Track.Author, x.Track.Duration, ""))
            .ToList();

        var totalDuration = TimeSpan.FromTicks(queueItems.Sum(x => x.Track?.Duration.Ticks ?? 0));
        if (player.CurrentTrack is not null)
            totalDuration += player.CurrentTrack.Duration - (player.Position?.Position ?? TimeSpan.Zero);

        var components = new ComponentBuilder();
        if (totalPages > 1)
        {
            components.WithButton("◀️", $"queue_prev_{page}", ButtonStyle.Secondary, disabled: page <= 1);
            components.WithButton($"{page}/{totalPages}", "queue_page", ButtonStyle.Primary, disabled: true);
            components.WithButton("▶️", $"queue_next_{page}", ButtonStyle.Secondary, disabled: page >= totalPages);
        }

        await RespondAsync(
            embed: _embedService.CreateQueueEmbed(pageItems, page, totalPages, player.CurrentTrack?.Title, totalDuration, totalTracks),
            components: components.Build());
    }

    [SlashCommand("nowplaying", "Show the currently playing track")]
    public async Task NowPlayingAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player?.CurrentTrack is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Nothing Playing", "There is nothing currently playing!"), ephemeral: true);
            return;
        }

        var track = player.CurrentTrack;
        var position = player.Position?.Position ?? TimeSpan.Zero;
        var settings = _musicService.GetOrCreateSettings(Context.Guild.Id);

        await RespondAsync(embed: _embedService.CreatePlayerStatusEmbed(
            track.Title,
            player.State == Lavalink4NET.Players.PlayerState.Paused,
            (int)(player.Volume * 100),
            settings.LoopMode.ToString(),
            player.Queue.Count,
            position,
            track.Duration));
    }

    [SlashCommand("volume", "Set the player volume (0-150)")]
    public async Task VolumeAsync([Summary("level", "Volume level (0-150)")] int level)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        level = Math.Clamp(level, 0, 150);
        await _musicService.SetVolumeAsync(player, level / 100f);
        await RespondAsync(embed: _embedService.CreateVolumeEmbed(level));
    }

    [SlashCommand("seek", "Seek to a position in the current track")]
    public async Task SeekAsync([Summary("position", "Position in seconds or format mm:ss")] string position)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player?.CurrentTrack is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Nothing Playing", "There is nothing currently playing!"), ephemeral: true);
            return;
        }

        TimeSpan seekPosition;
        if (int.TryParse(position, out var seconds))
        {
            seekPosition = TimeSpan.FromSeconds(seconds);
        }
        else if (TimeSpan.TryParse(position, out var parsed))
        {
            seekPosition = parsed;
        }
        else
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Invalid Format", "Please provide position as seconds (e.g., `90`) or as mm:ss (e.g., `1:30`)"), ephemeral: true);
            return;
        }

        if (seekPosition > player.CurrentTrack.Duration)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Invalid Position", "Position exceeds track duration!"), ephemeral: true);
            return;
        }

        await _musicService.SeekAsync(player, seekPosition);
        await RespondAsync(embed: _embedService.CreateSeekEmbed(seekPosition, player.CurrentTrack.Duration));
    }

    [SlashCommand("shuffle", "Shuffle the queue")]
    public async Task ShuffleAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (player.Queue.Count < 2)
        {
            await RespondAsync(embed: _embedService.CreateWarningEmbed("Not Enough Tracks", "Need at least 2 tracks in queue to shuffle!"), ephemeral: true);
            return;
        }

        await _musicService.ShuffleAsync(player);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Shuffled", $"🔀 Shuffled **{player.Queue.Count}** tracks in the queue!"));
    }

    [SlashCommand("loop", "Set the loop mode")]
    public async Task LoopAsync([Summary("mode", "Loop mode")] LoopMode mode)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        var settings = _musicService.GetOrCreateSettings(Context.Guild.Id);
        settings.LoopMode = mode;

        var modeText = mode switch
        {
            LoopMode.None => "🔁 Loop mode disabled",
            LoopMode.Track => "🔂 Now looping the current track",
            LoopMode.Queue => "🔁 Now looping the entire queue",
            _ => "Loop mode updated"
        };

        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Loop Mode", modeText));
    }

    [SlashCommand("clear", "Clear the queue")]
    public async Task ClearAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        var count = player.Queue.Count;
        await _musicService.ClearQueueAsync(player);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Queue Cleared", $"🗑️ Removed **{count}** tracks from the queue."));
    }

    [SlashCommand("remove", "Remove a track from the queue")]
    public async Task RemoveAsync([Summary("position", "Position in queue (1-based)")] int position)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (position < 1 || position > player.Queue.Count)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Invalid Position", $"Please provide a position between 1 and {player.Queue.Count}"), ephemeral: true);
            return;
        }

        var track = player.Queue[position - 1];
        await _musicService.RemoveAtAsync(player, position - 1);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Track Removed", $"🗑️ Removed **{track.Track?.Title}** from the queue."));
    }

    [SlashCommand("move", "Move a track to a different position")]
    public async Task MoveAsync(
        [Summary("from", "Current position (1-based)")] int from,
        [Summary("to", "New position (1-based)")] int to)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (from < 1 || from > player.Queue.Count || to < 1 || to > player.Queue.Count)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Invalid Position", $"Positions must be between 1 and {player.Queue.Count}"), ephemeral: true);
            return;
        }

        if (from == to)
        {
            await RespondAsync(embed: _embedService.CreateWarningEmbed("Same Position", "The positions are the same!"), ephemeral: true);
            return;
        }

        var track = player.Queue[from - 1];
        await player.Queue.RemoveAtAsync(from - 1);
        await player.Queue.InsertAsync(to - 1, track);

        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Track Moved", $"↕️ Moved **{track.Track?.Title}** from position #{from} to #{to}"));
    }

    [SlashCommand("skipto", "Skip to a specific track in the queue")]
    public async Task SkipToAsync([Summary("position", "Position in queue (1-based)")] int position)
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("No Player", "There is no active player in this server!"), ephemeral: true);
            return;
        }

        if (position < 1 || position > player.Queue.Count)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Invalid Position", $"Please provide a position between 1 and {player.Queue.Count}"), ephemeral: true);
            return;
        }

        var targetTrack = player.Queue[position - 1];
        
        for (int i = 0; i < position - 1; i++)
        {
            await player.Queue.RemoveAtAsync(0);
        }

        await _musicService.SkipAsync(player);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Skipped To", $"⏭️ Skipped to **{targetTrack.Track?.Title}**"));
    }

    [SlashCommand("replay", "Replay the current track from the beginning")]
    public async Task ReplayAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player?.CurrentTrack is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Nothing Playing", "There is nothing currently playing!"), ephemeral: true);
            return;
        }

        await _musicService.SeekAsync(player, TimeSpan.Zero);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Replaying", $"🔄 Replaying **{player.CurrentTrack.Title}** from the beginning!"));
    }

    [SlashCommand("disconnect", "Disconnect the bot from voice")]
    public async Task DisconnectAsync()
    {
        var player = await _musicService.GetExistingPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await RespondAsync(embed: _embedService.CreateErrorEmbed("Not Connected", "The bot is not connected to any voice channel!"), ephemeral: true);
            return;
        }

        await _musicService.DisconnectAsync(player);
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("Disconnected", "👋 Left the voice channel. Goodbye!"));
    }

    [SlashCommand("247", "Toggle 24/7 mode")]
    public async Task Toggle247Async()
    {
        var settings = _musicService.GetOrCreateSettings(Context.Guild.Id);
        settings.Is247Mode = !settings.Is247Mode;

        var status = settings.Is247Mode ? "enabled" : "disabled";
        var emoji = settings.Is247Mode ? "🌙" : "☀️";
        await RespondAsync(embed: _embedService.CreateSuccessEmbed("24/7 Mode", $"{emoji} 24/7 mode has been **{status}**"));
    }
}
