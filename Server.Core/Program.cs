using log4net;
using System.Reflection;
using Server.MirEnvir;
using Server.Library.Utils;
using Server;
using Server.Core.Commands;
using System.Collections.Concurrent;

namespace Server.Core
{
    class Program
    {
        private static Envir _envir;
        private static bool _running = true;
        private static ConsoleCommands _commands;
        private static readonly ConcurrentQueue<string> _commandQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// The main entry point for the cross-platform server application.
        /// </summary>
        static async Task Main(string[] args)
        {
            // Initialize server environment
            Packet.IsServer = true;

            // Configure logging
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            Console.WriteLine("Starting Legend of Mir 2 - Crystal Server (Cross-Platform)");
            Console.WriteLine("========================================================");

            try
            {
                // Load server settings
                Console.WriteLine("Loading server settings...");
                Settings.Load();
                Console.WriteLine("Server settings loaded successfully.");

                // Start the server environment
                _envir = Envir.Main;

                // Initialize commands
                _commands = new ConsoleCommands(_envir);

                Console.WriteLine("Initializing server environment...");
                Console.WriteLine("Loading database...");

                // Load database (similar to what SMain does)
                var editEnvir = Envir.Edit;
                var loaded = editEnvir.LoadDB();

                if (!loaded)
                {
                    Console.WriteLine("Failed to load database!");
                    return;
                }

                Console.WriteLine($"Database loaded successfully. Maps: {editEnvir.MapInfoList.Count}, Items: {editEnvir.ItemInfoList.Count}, Monsters: {editEnvir.MonsterInfoList.Count}");

                // Start the server
                Console.WriteLine("Starting server...");
                _envir.Start();

                Console.WriteLine("Server started successfully.");
                Console.WriteLine("Type 'help' for available commands or 'quit' to stop the server.");

                // Display some server info
                _commands.ShowStatus();

                // Start background tasks
                _ = ProcessMessages();
                _ = ProcessConsoleInput();
                _ = DisplayStatsPeriodically();

                // Wait for shutdown
                while (_running)
                {
                    await Task.Delay(100);
                }

                Console.WriteLine("Stopping server...");
                // Stop the server
                _envir.Stop();
                Settings.Save();
                Console.WriteLine("Server stopped.");
            }
            catch (Exception ex)
            {
                Logger.GetLogger().Error(ex);
                Console.WriteLine($"Server error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static async Task ProcessConsoleInput()
        {
            while (_running)
            {
                var input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    _commandQueue.Enqueue(input.Trim());
                }
                await Task.Delay(10);
            }
        }

        static async Task ProcessMessages()
        {
            while (_running)
            {
                await Task.Delay(100); // Check for messages every 100ms

                // Process commands
                while (_commandQueue.TryDequeue(out var command))
                {
                    await ProcessCommand(command);
                }

                // Process regular messages
                while (!MessageQueue.Instance.MessageLog.IsEmpty)
                {
                    string message;
                    if (MessageQueue.Instance.MessageLog.TryDequeue(out message))
                    {
                        Console.Write("[Server] ");
                        Console.WriteLine(message.TrimEnd());
                    }
                }

                // Process debug messages
                while (!MessageQueue.Instance.DebugLog.IsEmpty)
                {
                    string message;
                    if (MessageQueue.Instance.DebugLog.TryDequeue(out message))
                    {
                        Console.Write("[Debug] ");
                        Console.WriteLine(message.TrimEnd());
                    }
                }

                // Process chat messages
                while (!MessageQueue.Instance.ChatLog.IsEmpty)
                {
                    string message;
                    if (MessageQueue.Instance.ChatLog.TryDequeue(out message))
                    {
                        Console.Write("[Chat] ");
                        Console.WriteLine(message.TrimEnd());
                    }
                }
            }
        }

        static async Task ProcessCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var command = parts[0].ToLower();
            var args = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();

            switch (command)
            {
                case "help":
                    _commands.ShowHelp();
                    break;
                case "status":
                    _commands.ShowStatus();
                    break;
                case "start":
                    if (!_envir.Running)
                    {
                        _envir.Start();
                        Console.WriteLine("Server started.");
                    }
                    else
                    {
                        Console.WriteLine("Server is already running.");
                    }
                    break;
                case "stop":
                    if (_envir.Running)
                    {
                        _envir.Stop();
                        Console.WriteLine("Server stopped.");
                    }
                    else
                    {
                        Console.WriteLine("Server is not running.");
                    }
                    break;
                case "reboot":
                    Console.WriteLine("Rebooting server...");
                    _envir.Reboot();
                    break;
                case "players":
                    _commands.ListPlayers();
                    break;
                case "guilds":
                    _commands.ListGuilds();
                    break;
                case "broadcast":
                    _commands.BroadcastMessage(string.Join(" ", args));
                    break;
                case "kick":
                    if (args.Length > 0)
                        _commands.KickPlayer(args[0]);
                    else
                        Console.WriteLine("Usage: kick <player name>");
                    break;
                case "ban":
                    if (args.Length > 0)
                        _commands.BanPlayer(args[0]);
                    else
                        Console.WriteLine("Usage: ban <player name>");
                    break;
                case "unban":
                    if (args.Length > 0)
                        _commands.UnbanPlayer(args[0]);
                    else
                        Console.WriteLine("Usage: unban <player name>");
                    break;
                case "clearbans":
                    _commands.ClearBans();
                    break;
                case "maps":
                    _commands.ListMaps();
                    break;
                case "items":
                    _commands.ShowItemStats();
                    break;
                case "monsters":
                    _commands.ShowMonsterStats();
                    break;
                case "save":
                    _commands.SaveData();
                    break;
                case "quit":
                case "exit":
                    _running = false;
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }

        static async Task DisplayStatsPeriodically()
        {
            while (_running)
            {
                await Task.Delay(30000); // Update every 30 seconds
                if (_envir.Running)
                {
                    Console.WriteLine($"[Stats] Players: {_envir.Players.Count}, " +
                                    $"Monsters: {_envir.MonsterCount}, " +
                                    $"Connections: {_envir.Connections.Count}");
                }
            }
        }
    }
}