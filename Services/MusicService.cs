using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;

namespace Musico.Services;

public sealed class MusicService
{
    private readonly IAudioService _audioService;
    private readonly EmbedService _embedService;
    private readonly Dictionary<ulong, PlayerSettings> _playerSettings = new();
    private int _totalTracksPlayed = 0;

    public int TotalTracksPlayed => _totalTracksPlayed;

    public MusicService(IAudioService audioService, EmbedService embedService)
    {
        _audioService = audioService;
        _embedService = embedService;
    }

    public async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(ulong guildId, IVoiceChannel voiceChannel, ITextChannel textChannel, bool connectToVoiceChannel = true)
    {
        var playerOptions = new QueuedLavalinkPlayerOptions
        {
            DisconnectOnStop = false,
            SelfDeaf = true,
            SelfMute = false
        };

        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

        var result = await _audioService.Players.RetrieveAsync<QueuedLavalinkPlayer, QueuedLavalinkPlayerOptions>(guildId, voiceChannel.Id, CreatePlayerAsync, Options.Create(playerOptions), retrieveOptions);

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Player;
    }

    private static ValueTask<QueuedLavalinkPlayer> CreatePlayerAsync(IPlayerProperties<QueuedLavalinkPlayer, QueuedLavalinkPlayerOptions> properties, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new QueuedLavalinkPlayer(properties));
    }

    public async ValueTask<QueuedLavalinkPlayer?> GetExistingPlayerAsync(ulong guildId)
    {
        var player = await _audioService.Players.GetPlayerAsync<QueuedLavalinkPlayer>(guildId);
        return player;
    }

    public async Task<TrackLoadResult> SearchAsync(string query, TrackSearchMode? searchMode = null)
    {
        if (searchMode.HasValue)
        {
            return await _audioService.Tracks.LoadTracksAsync(query, searchMode.Value);
        }
        return await _audioService.Tracks.LoadTracksAsync(query, TrackSearchMode.None);
    }

    public async Task<bool> PlayAsync(QueuedLavalinkPlayer player, LavalinkTrack track, string requester)
    {
        try
        {
            await player.PlayAsync(track);
            _totalTracksPlayed++;
            
            // Store requester info
            if (!_playerSettings.ContainsKey(player.GuildId))
                _playerSettings[player.GuildId] = new PlayerSettings();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> AddToQueueAsync(QueuedLavalinkPlayer player, LavalinkTrack track)
    {
        await player.Queue.AddAsync(new TrackQueueItem(track));
        return player.Queue.Count;
    }

    public async Task<bool> SkipAsync(QueuedLavalinkPlayer player)
    {
        if (player.CurrentTrack is null && player.Queue.IsEmpty)
            return false;

        await player.SkipAsync();
        return true;
    }

    public async Task<bool> StopAsync(QueuedLavalinkPlayer player)
    {
        await player.StopAsync();
        await player.Queue.ClearAsync();
        return true;
    }

    public async Task<bool> PauseAsync(QueuedLavalinkPlayer player)
    {
        if (player.State == PlayerState.Paused)
            return false;

        await player.PauseAsync();
        return true;
    }

    public async Task<bool> ResumeAsync(QueuedLavalinkPlayer player)
    {
        if (player.State != PlayerState.Paused)
            return false;

        await player.ResumeAsync();
        return true;
    }

    public async Task<bool> SeekAsync(QueuedLavalinkPlayer player, TimeSpan position)
    {
        if (player.CurrentTrack is null)
            return false;

        await player.SeekAsync(position);
        return true;
    }

    public async Task SetVolumeAsync(QueuedLavalinkPlayer player, float volume)
    {
        await player.SetVolumeAsync(volume);
    }

    public async Task ShuffleAsync(QueuedLavalinkPlayer player)
    {
        await player.Queue.ShuffleAsync();
    }

    public async Task ClearQueueAsync(QueuedLavalinkPlayer player)
    {
        await player.Queue.ClearAsync();
    }

    public async Task<bool> RemoveAtAsync(QueuedLavalinkPlayer player, int index)
    {
        if (index < 0 || index >= player.Queue.Count)
            return false;

        await player.Queue.RemoveAtAsync(index);
        return true;
    }

    public PlayerSettings GetOrCreateSettings(ulong guildId)
    {
        if (!_playerSettings.ContainsKey(guildId))
            _playerSettings[guildId] = new PlayerSettings();
        
        return _playerSettings[guildId];
    }

    public async Task DisconnectAsync(QueuedLavalinkPlayer player)
    {
        await player.DisconnectAsync();
    }

    public int GetActivePlayerCount()
    {
        return _audioService.Players.Players.Count();
    }
}

public class PlayerSettings
{
    public LoopMode LoopMode { get; set; } = LoopMode.None;
    public bool Is247Mode { get; set; } = false;
    public string? LastRequester { get; set; }
}

public enum LoopMode
{
    None,
    Track,
    Queue
}
