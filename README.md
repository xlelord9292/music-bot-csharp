# 🎵 Musico - Discord Music Bot

A high-performance Discord music bot built with C# using Discord.NET and Lavalink4NET v4.

## ✨ Features

- **🎶 Music Playback** - Play music from YouTube, SoundCloud, Spotify, and more
- **📋 Queue Management** - Full queue control with shuffle, loop, and move
- **🎛️ Audio Filters** - Bass boost, nightcore, vaporwave, 8D audio, and more
- **⚡ High Performance** - Built for speed with async/await patterns
- **🎨 Beautiful Embeds** - Elegant and informative message embeds
- **🔧 Slash Commands** - Modern Discord slash command interface

## 📝 Commands

### Music Commands
| Command | Description |
|---------|-------------|
| `/play <query>` | Play a song or add to queue |
| `/search <query>` | Search and select from results |
| `/playtop <query>` | Add song to top of queue |
| `/playskip <query>` | Play immediately, skip current |

### Queue Commands
| Command | Description |
|---------|-------------|
| `/skip` | Skip current track |
| `/stop` | Stop and clear queue |
| `/pause` | Pause playback |
| `/resume` | Resume playback |
| `/queue [page]` | View queue |
| `/nowplaying` | Show current track |
| `/shuffle` | Shuffle the queue |
| `/loop <mode>` | Set loop mode |
| `/clear` | Clear the queue |
| `/remove <pos>` | Remove track from queue |
| `/move <from> <to>` | Move track position |
| `/skipto <pos>` | Skip to position |
| `/replay` | Restart current track |
| `/volume <0-150>` | Set volume |
| `/seek <position>` | Seek in track |
| `/247` | Toggle 24/7 mode |
| `/disconnect` | Leave voice channel |

### Filter Commands
| Command | Description |
|---------|-------------|
| `/bassboost <level>` | Bass boost (off/low/medium/high/extreme) |
| `/nightcore` | Nightcore effect |
| `/vaporwave` | Vaporwave effect |
| `/speed <rate>` | Playback speed (0.5-2.0) |
| `/pitch <level>` | Pitch adjustment |
| `/rotation <freq>` | 8D audio effect |
| `/tremolo` | Tremolo effect |
| `/vibrato` | Vibrato effect |
| `/karaoke` | Reduce vocals |
| `/clearfilters` | Remove all filters |

### Utility Commands
| Command | Description |
|---------|-------------|
| `/ping` | Check latency |
| `/stats` | Bot statistics |
| `/help` | Show commands |
| `/invite` | Get invite link |
| `/serverinfo` | Server information |
| `/userinfo [user]` | User information |
| `/avatar [user]` | Get avatar |

## 🚀 Setup

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Lavalink v4 server
- Discord Bot Token

### Configuration

1. Edit the `.env` file with your credentials:
```env
DISCORD_TOKEN=your_discord_bot_token
LAVALINK_HOST=lava-v4.ajieblogs.eu.org
LAVALINK_PORT=80
LAVALINK_PASSWORD=https://dsc.gg/ajidevserver
LAVALINK_SECURE=false
```

### Running the Bot

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run

# OR YOU CAN USE DOCKER
docker-compose up -d --build
```

## 📦 Dependencies

- **Discord.NET** - Discord API wrapper
- **Lavalink4NET** - Lavalink client for .NET
- **Microsoft.Extensions.Hosting** - Generic host builder
- **DotNetEnv** - Environment variable loader

## 🏗️ Project Structure

```
Musico/
├── Program.cs              # Entry point
├── .env                    # Environment configuration
├── Musico.csproj          # Project file
├── Commands/
│   ├── PlayCommands.cs    # Play-related commands
│   ├── QueueCommands.cs   # Queue management commands
│   ├── FilterCommands.cs  # Audio filter commands
│   └── UtilityCommands.cs # Utility commands
└── Services/
    ├── BotHostedService.cs # Bot lifecycle management
    ├── MusicService.cs     # Music player service
    └── EmbedService.cs     # Embed builder service
```
