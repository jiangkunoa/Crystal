using log4net;
using System.Reflection;
using Server.MirEnvir;
using Server.Library.Utils;
using Server;

namespace Server.Core
{
    class Program
    {
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
                var envir = Envir.Main;

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
                envir.Start();

                Console.WriteLine("Server started successfully.");
                Console.WriteLine("Press Ctrl+C to stop the server.");

                // Display some server info
                DisplayServerInfo();

                // Wait for shutdown signal
                var tcs = new TaskCompletionSource<bool>();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    tcs.TrySetResult(true);
                };

                // Start message processing and stats display
                var messageTask = ProcessMessages();
                var statsTask = DisplayStatsPeriodically(envir, tcs.Task);

                await tcs.Task;

                Console.WriteLine("Stopping server...");
                // Stop the server
                envir.Stop();
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

        static void DisplayServerInfo()
        {
            Console.WriteLine($"Version path: {Settings.VersionPath}");
            Console.WriteLine($"Check version: {Settings.CheckVersion}");
            Console.WriteLine($"Multithreaded: {Settings.Multithreaded}");
            Console.WriteLine($"Thread limit: {Settings.ThreadLimit}");
            Console.WriteLine($"IP Address: {Settings.IPAddress}");
            Console.WriteLine($"Port: {Settings.Port}");
        }

        static async Task ProcessMessages()
        {
            while (true)
            {
                await Task.Delay(100); // Check for messages every 100ms

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

        static async Task DisplayStatsPeriodically(Envir envir, Task shutdownTask)
        {
            while (!shutdownTask.IsCompleted)
            {
                await Task.Delay(5000); // Update every 5 seconds
                if (envir.Running)
                {
                    Console.WriteLine($"[Stats] Players: {envir.Players.Count}, " +
                                    $"Monsters: {envir.MonsterCount}, " +
                                    $"Connections: {envir.Connections.Count}");
                }
            }
        }
    }
}