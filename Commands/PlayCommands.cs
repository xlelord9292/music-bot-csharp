using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Rest.Entities.Tracks;
using Musico.Services;

namespace Musico.Commands;

public class PlayCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MusicService _musicService;
    private readonly EmbedService _embedService;

    public PlayCommands(MusicService musicService, EmbedService embedService)
    {
        _musicService = musicService;
        _embedService = embedService;
    }

    [SlashCommand("play", "Play a song or add it to the queue")]
    public async Task PlayAsync([Summary("query", "Song name or URL")] string query)
    {
        await DeferAsync();

        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (voiceChannel is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Not Connected", "You must be in a voice channel to use this command!"));
            return;
        }

        var player = await _musicService.GetPlayerAsync(Context.Guild.Id, voiceChannel, (ITextChannel)Context.Channel);
        if (player is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Connection Failed", "Failed to connect to the voice channel. Please try again."));
            return;
        }

        // Determine search mode based on query
        TrackSearchMode? searchMode = null;
        if (query.Contains("soundcloud.com"))
            searchMode = TrackSearchMode.SoundCloud;
        else if (query.Contains("spotify.com"))
            searchMode = TrackSearchMode.Spotify;
        else if (!Uri.IsWellFormedUriString(query, UriKind.Absolute))
            searchMode = TrackSearchMode.YouTube;

        var searchResult = await _musicService.SearchAsync(query, searchMode);

        if (searchResult.IsPlaylist && searchResult.Tracks.Length > 0)
        {
            var firstTrack = searchResult.Tracks[0];
            
            if (player.CurrentTrack is null)
            {
                await _musicService.PlayAsync(player, firstTrack, Context.User.Username);
            }
            else
            {
                await _musicService.AddToQueueAsync(player, firstTrack);
            }

            foreach (var track in searchResult.Tracks.Skip(1))
            {
                await _musicService.AddToQueueAsync(player, track);
            }

            await FollowupAsync(embed: _embedService.CreateSuccessEmbed(
                "Playlist Added",
                $"Added **{searchResult.Tracks.Length}** tracks from **{searchResult.Playlist?.Name ?? "playlist"}** to the queue!"));
        }
        else if (searchResult.Tracks.Length > 0)
        {
            var track = searchResult.Tracks[0];

            if (player.CurrentTrack is null)
            {
                await _musicService.PlayAsync(player, track, Context.User.Username);
                
                await FollowupAsync(embed: _embedService.CreateNowPlayingEmbed(
                    track.Title,
                    track.Author,
                    track.Uri?.ToString() ?? "",
                    track.Duration,
                    track.ArtworkUri?.ToString(),
                    Context.User.Username,
                    player.Queue.Count));
            }
            else
            {
                var position = await _musicService.AddToQueueAsync(player, track);
                
                await FollowupAsync(embed: _embedService.CreateTrackAddedEmbed(
                    track.Title,
                    track.Author,
                    track.Uri?.ToString() ?? "",
                    track.Duration,
                    track.ArtworkUri?.ToString(),
                    position));
            }
        }
        else
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("No Results", $"No results found for: **{query}**"));
        }
    }

    [SlashCommand("search", "Search for tracks and select one")]
    public async Task SearchAsync([Summary("query", "Search query")] string query)
    {
        await DeferAsync();

        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (voiceChannel is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Not Connected", "You must be in a voice channel to use this command!"));
            return;
        }

        var searchResult = await _musicService.SearchAsync(query, TrackSearchMode.YouTube);

        if (searchResult.Tracks.Length == 0)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("No Results", $"No results found for: **{query}**"));
            return;
        }

        var tracks = searchResult.Tracks.Take(5).ToList();
        var results = tracks.Select(t => (t.Title, t.Author, t.Duration, t.Uri?.ToString() ?? "")).ToList();

        var components = new ComponentBuilder();
        for (int i = 0; i < tracks.Count; i++)
        {
            components.WithButton($"{i + 1}", $"search_select_{i}", ButtonStyle.Primary, row: i / 5);
        }
        components.WithButton("Cancel", "search_cancel", ButtonStyle.Danger, row: 1);

        await FollowupAsync(
            embed: _embedService.CreateSearchResultsEmbed(results),
            components: components.Build());
    }

    [SlashCommand("playtop", "Add a song to the top of the queue")]
    public async Task PlayTopAsync([Summary("query", "Song name or URL")] string query)
    {
        await DeferAsync();

        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (voiceChannel is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Not Connected", "You must be in a voice channel to use this command!"));
            return;
        }

        var player = await _musicService.GetPlayerAsync(Context.Guild.Id, voiceChannel, (ITextChannel)Context.Channel);
        if (player is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Connection Failed", "Failed to connect to the voice channel."));
            return;
        }

        TrackSearchMode? topSearchMode = null;
        if (!Uri.IsWellFormedUriString(query, UriKind.Absolute))
            topSearchMode = TrackSearchMode.YouTube;

        var searchResult = await _musicService.SearchAsync(query, topSearchMode);

        if (searchResult.Tracks.Length == 0)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("No Results", $"No results found for: **{query}**"));
            return;
        }

        var track = searchResult.Tracks[0];

        if (player.CurrentTrack is null)
        {
            await _musicService.PlayAsync(player, track, Context.User.Username);
            
            await FollowupAsync(embed: _embedService.CreateNowPlayingEmbed(
                track.Title,
                track.Author,
                track.Uri?.ToString() ?? "",
                track.Duration,
                track.ArtworkUri?.ToString(),
                Context.User.Username,
                player.Queue.Count));
        }
        else
        {
            // Insert at the beginning of the queue
            await player.Queue.InsertAsync(0, new Lavalink4NET.Players.Queued.TrackQueueItem(track));
            
            await FollowupAsync(embed: _embedService.CreateSuccessEmbed(
                "Added to Top",
                $"**[{track.Title}]({track.Uri})** has been added to the top of the queue!"));
        }
    }

    [SlashCommand("playskip", "Play a song immediately, skipping the current track")]
    public async Task PlaySkipAsync([Summary("query", "Song name or URL")] string query)
    {
        await DeferAsync();

        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (voiceChannel is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Not Connected", "You must be in a voice channel to use this command!"));
            return;
        }

        var player = await _musicService.GetPlayerAsync(Context.Guild.Id, voiceChannel, (ITextChannel)Context.Channel);
        if (player is null)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("Connection Failed", "Failed to connect to the voice channel."));
            return;
        }

        TrackSearchMode? skipSearchMode = null;
        if (!Uri.IsWellFormedUriString(query, UriKind.Absolute))
            skipSearchMode = TrackSearchMode.YouTube;

        var searchResult = await _musicService.SearchAsync(query, skipSearchMode);

        if (searchResult.Tracks.Length == 0)
        {
            await FollowupAsync(embed: _embedService.CreateErrorEmbed("No Results", $"No results found for: **{query}**"));
            return;
        }

        var track = searchResult.Tracks[0];
        await _musicService.PlayAsync(player, track, Context.User.Username);

        await FollowupAsync(embed: _embedService.CreateNowPlayingEmbed(
            track.Title,
            track.Author,
            track.Uri?.ToString() ?? "",
            track.Duration,
            track.ArtworkUri?.ToString(),
            Context.User.Username,
            player.Queue.Count));
    }
}
