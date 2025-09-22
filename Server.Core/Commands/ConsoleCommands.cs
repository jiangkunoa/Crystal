using Server.MirEnvir;
using Server.MirDatabase;
using System.Text;

namespace Server.Core.Commands
{
    public class ConsoleCommands
    {
        private readonly Envir _envir;

        public ConsoleCommands(Envir envir)
        {
            _envir = envir;
        }

        public void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  help            - Show this help message");
            Console.WriteLine("  status          - Show server status and statistics");
            Console.WriteLine("  start           - Start the server");
            Console.WriteLine("  stop            - Stop the server");
            Console.WriteLine("  reboot          - Reboot the server");
            Console.WriteLine("  players         - List online players");
            Console.WriteLine("  guilds          - List guilds");
            Console.WriteLine("  broadcast <msg> - Broadcast message to all players");
            Console.WriteLine("  kick <name>     - Kick player by name");
            Console.WriteLine("  ban <name>      - Ban player by name");
            Console.WriteLine("  unban <name>    - Unban player by name");
            Console.WriteLine("  clearbans       - Clear all banned IPs");
            Console.WriteLine("  maps            - List loaded maps");
            Console.WriteLine("  items           - Show item statistics");
            Console.WriteLine("  monsters        - Show monster statistics");
            Console.WriteLine("  save            - Save server data");
            Console.WriteLine("  quit            - Stop server and exit");
            Console.WriteLine("  exit            - Stop server and exit");
        }

        public void ShowStatus()
        {
            Console.WriteLine("=== Server Status ===");
            Console.WriteLine($"Running: {_envir.Running}");
            Console.WriteLine($"Players Online: {_envir.Players.Count}");
            Console.WriteLine($"Monsters: {_envir.MonsterCount}");
            Console.WriteLine($"Connections: {_envir.Connections.Count}");
            Console.WriteLine($"Blocked IPs: {Envir.IPBlocks.Count(x => x.Value > _envir.Now)}");
            Console.WriteLine($"Uptime: {_envir.Stopwatch.ElapsedMilliseconds / 1000 / 60 / 60 / 24}d:{_envir.Stopwatch.ElapsedMilliseconds / 1000 / 60 / 60 % 24}h:{_envir.Stopwatch.ElapsedMilliseconds / 1000 / 60 % 60}m:{_envir.Stopwatch.ElapsedMilliseconds / 1000 % 60}s");

            // Simplified version without LastRunTime to avoid conflicts
            if (Settings.Multithreaded && _envir.MobThreads != null)
            {
                Console.WriteLine("Multithreaded mode enabled");
            }
            else
            {
                Console.WriteLine("Single thread mode");
            }
        }

        public void ListPlayers()
        {
            Console.WriteLine("=== Online Players ===");
            if (_envir.Players.Count == 0)
            {
                Console.WriteLine("No players online.");
                return;
            }

            Console.WriteLine($"{"Index",-6} {"Name",-15} {"Level",-5} {"Class",-10} {"Gender",-8} {"Map",-20}");
            Console.WriteLine(new string('-', 70));

            for (int i = 0; i < _envir.Players.Count; i++)
            {
                var player = _envir.Players[i];
                var mapName = GetMapTitleByIndex(player.CurrentMapIndex);
                Console.WriteLine($"{player.Info.Index,-6} {player.Info.Name,-15} {player.Info.Level,-5} {player.Info.Class,-10} {player.Info.Gender,-8} {mapName,-20}");
            }
        }

        public void ListGuilds()
        {
            Console.WriteLine("=== Guilds ===");
            if (_envir.GuildList.Count == 0)
            {
                Console.WriteLine("No guilds found.");
                return;
            }

            Console.WriteLine($"{"Name",-20} {"Leader",-15} {"Members",-8} {"Level",-6} {"Gold",-15}");
            Console.WriteLine(new string('-', 70));

            foreach (var guild in _envir.GuildList)
            {
                // Get leader from the first rank (typically rank 0 is leader)
                string leaderName = "Unknown";
                int memberCount = 0;

                if (guild.Ranks.Count > 0)
                {
                    var leaderRank = guild.Ranks[0];
                    if (leaderRank.Members.Count > 0)
                    {
                        leaderName = leaderRank.Members[0].Name;
                    }
                    memberCount = guild.Ranks.Sum(r => r.Members.Count);
                }

                Console.WriteLine($"{guild.Name,-20} {leaderName,-15} {memberCount,-8} {guild.Level,-6} {guild.Gold,-15:N0}");
            }
        }

        public void BroadcastMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("Usage: broadcast <message>");
                return;
            }

            foreach (var player in _envir.Players)
            {
                player.ReceiveChat(message, ChatType.Announcement);
            }

            MessageQueue.Instance.EnqueueChat(message);
            Console.WriteLine($"Broadcast message sent: {message}");
        }

        public void KickPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Console.WriteLine("Usage: kick <player name>");
                return;
            }

            var player = _envir.Players.FirstOrDefault(p => p.Info.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (player == null)
            {
                Console.WriteLine($"Player '{playerName}' not found.");
                return;
            }

            player.Connection.SendDisconnect(0);
            Console.WriteLine($"Player '{playerName}' has been kicked.");
        }

        public void BanPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Console.WriteLine("Usage: ban <player name>");
                return;
            }

            var player = _envir.Players.FirstOrDefault(p => p.Info.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (player == null)
            {
                Console.WriteLine($"Player '{playerName}' not found.");
                return;
            }

            // Ban the player's IP
            if (player.Connection != null && player.Connection.IPAddress != null)
            {
                Envir.IPBlocks[player.Connection.IPAddress] = DateTime.UtcNow.AddYears(5);
                player.Connection.SendDisconnect(0);
                Console.WriteLine($"Player '{playerName}' has been banned (IP: {player.Connection.IPAddress}).");
            }
            else
            {
                Console.WriteLine($"Could not ban player '{playerName}' - no IP address found.");
            }
        }

        public void UnbanPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Console.WriteLine("Usage: unban <player name>");
                return;
            }

            // Find the player's account and unban it
            var account = _envir.AccountList.FirstOrDefault(a => a.Characters.Any(c => c.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
            if (account == null)
            {
                Console.WriteLine($"Player '{playerName}' not found.");
                return;
            }

            account.Banned = false;
            account.BanReason = string.Empty;
            account.ExpiryDate = DateTime.MinValue;
            Console.WriteLine($"Player '{playerName}' has been unbanned.");
        }

        public void ClearBans()
        {
            var count = Envir.IPBlocks.Count;
            Envir.IPBlocks.Clear();
            Console.WriteLine($"Cleared {count} banned IP addresses.");
        }

        public void ListMaps()
        {
            Console.WriteLine("=== Loaded Maps ===");
            if (_envir.MapInfoList.Count == 0)
            {
                Console.WriteLine("No maps loaded.");
                return;
            }

            Console.WriteLine($"{"Index",-6} {"File Name",-20} {"Title",-25} {"Size",-12} {"Players",-8}");
            Console.WriteLine(new string('-', 80));

            foreach (var mapInfo in _envir.MapInfoList)
            {
                var map = _envir.MapList.FirstOrDefault(m => m.Info == mapInfo);
                var playerCount = map != null ? map.Players.Count : 0;
                var size = map != null ? $"{map.Width}x{map.Height}" : "Unknown";
                Console.WriteLine($"{mapInfo.Index,-6} {mapInfo.FileName,-20} {mapInfo.Title,-25} {size,-12} {playerCount,-8}");
            }
        }

        public void ShowItemStats()
        {
            Console.WriteLine("=== Item Statistics ===");
            Console.WriteLine($"Total Items: {_envir.ItemInfoList.Count}");
            Console.WriteLine($"Total Drop Items: {_envir.ItemInfoList.Count(i => i.Type == ItemType.Nothing)}");
            Console.WriteLine($"Weapons: {_envir.ItemInfoList.Count(i => i.Type == ItemType.Weapon)}");
            Console.WriteLine($"Armors: {_envir.ItemInfoList.Count(i => i.Type == ItemType.Armour)}");
            Console.WriteLine($"Potions: {_envir.ItemInfoList.Count(i => i.Type == ItemType.Potion)}");
            Console.WriteLine($"Scrolls: {_envir.ItemInfoList.Count(i => i.Type == ItemType.Scroll)}");
        }

        public void ShowMonsterStats()
        {
            Console.WriteLine("=== Monster Statistics ===");
            Console.WriteLine($"Total Monsters: {_envir.MonsterInfoList.Count}");
            Console.WriteLine($"Monster Count (in game): {_envir.MonsterCount}");
        }

        public void SaveData()
        {
            try
            {
                _envir.SaveAccounts();
                _envir.SaveDB(); // Save main database
                Console.WriteLine("Server data saved successfully.");
                Console.WriteLine("Note: Guilds, goods, and conquests are saved automatically.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        private string GetMapTitleByIndex(int mapIndex)
        {
            var mapInfo = _envir.MapInfoList.FirstOrDefault(m => m.Index == mapIndex);
            return mapInfo?.Title ?? "Unknown";
        }
    }
}