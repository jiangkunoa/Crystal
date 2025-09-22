# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the official public source code for Legend of Mir 2 - Crystal, a classic MMORPG server implementation. The project is built using C# with .NET 8.0 and consists of several main components:

- Client: Windows Forms application for the game client
- Server: Server application with database and game logic
- Server.Core: Cross-platform server implementation (new)
- Shared: Common code library shared between client and server
- LibraryEditor: Tools for editing game data files
- AutoPatcherAdmin: Administrative tools for patch management

## Architecture

The solution follows a client-server architecture with:
- Client handling rendering, user input, and network communication with the server
- Server managing game state, player data, and business logic
- Shared library containing common data structures and network protocols

Key technologies:
- .NET 8.0
- Windows Forms for Client UI and original Server UI
- SlimDX for graphics rendering
- WebView2 for modern web integration
- NAudio for audio processing
- log4net for server logging

## Build and Development Commands

### Prerequisites
- Visual Studio 24 or newer with .NET 8.0 SDK
- Required NuGet packages will be restored automatically

### Building the Project
```bash
# Using Visual Studio
# Open Legend of Mir.sln and build solution

# Using dotnet CLI for all projects
dotnet build "Legend of Mir.sln"

# Using dotnet CLI for cross-platform server only
dotnet build Server.Core/Server.Core.csproj

# Using build scripts
./build-server-core.sh    # On Linux/macOS
build-server-core.bat     # On Windows
```

### Running the Applications
```bash
# Run Client (from Build/Client/Debug/)
Client.exe

# Run original Server (from Build/Server/Debug/)
Server.exe

# Run cross-platform Server
Build/Server.Core/Debug/Server.Core.exe

# Or using dotnet CLI
dotnet Build/Server.Core/Debug/Server.Core.dll
```

### Database Setup
The server requires properly configured database files to run:
- Server.MirDB: Main game database with maps, items, monsters, etc.
- Server.MirADB: Accounts database
These files should be placed in the server executable directory or the paths configured in the server settings.

### Logging
The server uses multiple log types:
- Server logs: General server operations
- Chat logs: Player chat messages
- Debug logs: Debug information
- Player logs: Player-specific actions
- Spawn logs: Monster spawn events

Logs are written to the Logs/ directory with daily rotation.

### Command Line Interface
The cross-platform Server.Core includes a comprehensive command-line interface with the following commands:

Available commands:
- `help` - Show help message with all available commands
- `status` - Show server status and statistics including uptime, player count, and performance metrics
- `start` - Start the server (if stopped)
- `stop` - Stop the server (if running)
- `reboot` - Reboot the server
- `players` - List all online players with their details
- `guilds` - List all guilds with leader and member information
- `broadcast <message>` - Broadcast a message to all online players
- `kick <name>` - Kick a player by name
- `ban <name>` - Ban a player by name (IP ban)
- `unban <name>` - Unban a player by name
- `clearbans` - Clear all banned IP addresses
- `maps` - List all loaded maps with size and player count
- `items` - Show item statistics
- `monsters` - Show monster statistics
- `save` - Save all server data to disk
- `quit` or `exit` - Stop server and exit

### Common Development Tasks
1. Building requires all projects in the solution to compile successfully
2. Client and Server share data structures through the Shared project
3. Game data files are typically managed through the LibraryEditor tools
4. Database files need to be set up separately for the Server to run
5. The cross-platform Server.Core uses the same game logic as the original server but without Windows Forms dependencies

## Code Structure
- Client/: Game client implementation with forms and rendering
- Server/: Original server implementation with Windows Forms UI
- Server.Core/: Cross-platform server implementation (new)
- Server.Core/Commands/: Command-line interface commands
- Shared/: Common data structures and network protocols
- LibraryEditor/: Tools for editing game data files
- Build/: Output directory for compiled binaries