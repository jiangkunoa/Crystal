using System.Text.Json;

namespace MapImageExtractor
{
    public class MapParser
    {
        public class MapImageInfo
        {
            public int Index { get; set; }
            public string Library { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public int Width { get; set; }
            public int Height { get; set; }
            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
        }

        public static List<MapImageInfo> ParseMapFile(string mapFilePath, string dataPath)
        {
            var imageInfos = new List<MapImageInfo>();

            if (!File.Exists(mapFilePath))
            {
                Console.WriteLine($"Map file not found: {mapFilePath}");
                return imageInfos;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(mapFilePath);
                int mapType = FindMapType(fileBytes);

                switch (mapType)
                {
                    case 0: // Wemade Mir2 format
                        ParseWemadeMir2Format(fileBytes, imageInfos, dataPath);
                        break;
                    case 1: // Wemade 2010 format
                    case 2: // Shanda format
                    case 3: // Shanda 2012 format
                    case 4: // Wemade antihack format
                    case 5: // Wemade Mir3 format
                    case 6: // Shanda Mir3 format
                    case 7: // 3/4 Heroes format
                    case 100: // C# custom format
                        ParseGenericFormat(fileBytes, imageInfos, dataPath, mapType);
                        break;
                    default:
                        Console.WriteLine($"Unknown map format: {mapType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing map file: {ex.Message}");
            }

            return imageInfos.DistinctBy(x => x.Index).ToList();
        }

        private static int FindMapType(byte[] input)
        {
            // C# custom map format
            if ((input[2] == 0x43) && (input[3] == 0x23))
                return 100;

            // Wemade mir3 maps have no title, they just start with blank bytes
            if (input[0] == 0)
                return 5;

            // Shanda mir3 maps start with title: (C) SNDA, MIR3.
            if ((input[0] == 0x0F) && (input[5] == 0x53) && (input[14] == 0x33))
                return 6;

            // Wemade antihack map (laby maps) title start with: Mir2 AntiHack
            if ((input[0] == 0x15) && (input[4] == 0x32) && (input[6] == 0x41) && (input[19] == 0x31))
                return 4;

            // Wemade 2010 map format, title starts with: Map 2010 Ver 1.0
            if ((input[0] == 0x10) && (input[2] == 0x61) && (input[7] == 0x31) && (input[14] == 0x31))
                return 1;

            // Shanda's 2012 format and older formats
            if ((input[4] == 0x0F) || (input[4] == 0x03) && (input[18] == 0x0D) && (input[19] == 0x0A))
            {
                int W = input[0] + (input[1] << 8);
                int H = input[2] + (input[3] << 8);
                if (input.Length > (52 + (W * H * 14)))
                    return 3;
                else
                    return 2;
            }

            // 3/4 Heroes map format
            if ((input[0] == 0x0D) && (input[1] == 0x4C) && (input[7] == 0x20) && (input[11] == 0x6D))
                return 7;

            return 0; // Default to Wemade Mir2
        }

        private static void ParseWemadeMir2Format(byte[] fileBytes, List<MapImageInfo> imageInfos, string dataPath)
        {
            int offset = 52;
            int width = BitConverter.ToInt16(fileBytes, 0);
            int height = BitConverter.ToInt16(fileBytes, 2);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Extract image indices from map data
                    int tileIndex = BitConverter.ToInt16(fileBytes, offset);
                    offset += 2;

                    int smObjectIndex = BitConverter.ToInt16(fileBytes, offset);
                    offset += 2;

                    int objectIndex = BitConverter.ToInt16(fileBytes, offset);
                    offset += 2;

                    // Skip door index and light bytes
                    offset += 4;

                    // Add valid image indices
                    AddImageInfo(imageInfos, tileIndex & 0x7FFF, "Map/WemadeMir2/Tiles", dataPath);
                    AddImageInfo(imageInfos, smObjectIndex & 0x7FFF, "Map/WemadeMir2/Smtiles", dataPath);
                    AddImageInfo(imageInfos, objectIndex & 0x7FFF, "Map/WemadeMir2/Objects", dataPath);
                }
            }
        }

        private static void ParseGenericFormat(byte[] fileBytes, List<MapImageInfo> imageInfos, string dataPath, int mapType)
        {
            // Generic parsing for other formats - extract potential image indices
            // This is a simplified approach; real implementation would need format-specific parsing

            int width = BitConverter.ToInt16(fileBytes, 0);
            int height = BitConverter.ToInt16(fileBytes, 2);

            // Scan for potential image indices in the file
            for (int i = 0; i < fileBytes.Length - 1; i++)
            {
                ushort potentialIndex = BitConverter.ToUInt16(fileBytes, i);

                // Check if this looks like a valid image index (within reasonable range)
                if (potentialIndex > 0 && potentialIndex < 10000)
                {
                    // Try different library paths based on map type
                    string[] possibleLibraries = mapType switch
                    {
                        5 or 6 => new[] { "Map/WemadeMir3/Tilesc", "Map/WemadeMir3/Smtilesc", "Map/WemadeMir3/Objects" },
                        _ => new[] { "Map/ShandaMir2/Tiles", "Map/ShandaMir2/SmTiles", "Map/ShandaMir2/Objects" }
                    };

                    foreach (var library in possibleLibraries)
                    {
                        AddImageInfo(imageInfos, potentialIndex, library, dataPath);
                    }
                }
            }
        }

        private static void AddImageInfo(List<MapImageInfo> imageInfos, int index, string libraryPath, string dataPath)
        {
            if (index <= 0) return;

            // Check if library file exists
            string libPath = Path.Combine(dataPath, libraryPath + MLibrary.Extention);
            if (!File.Exists(libPath))
                return;

            var info = new MapImageInfo
            {
                Index = index,
                Library = libraryPath,
                FileName = $"{index:D4}.png"  // Simple filename with index only
            };

            imageInfos.Add(info);
        }

        public static void SaveParameters(List<MapImageInfo> imageInfos, string outputPath, string mapName)
        {
            var parameters = new
            {
                MapName = mapName,
                TotalImages = imageInfos.Count,
                Images = imageInfos
            };

            string json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outputPath, "parameters.json"), json);
        }
    }
}