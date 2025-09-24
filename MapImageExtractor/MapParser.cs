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

        public class MapCellInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int BackIndex { get; set; }
            public int TileIndex { get; set; }
            public int MiddleIndex { get; set; }
            public int SmObjectIndex { get; set; }
            public int FrontIndex { get; set; }
            public int ObjectIndex { get; set; }
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
                // Use the Client's MapReader for parsing
                var mapReader = new MapReader(mapFilePath);

                // Convert CellInfo to MapImageInfo
                var cellInfos = new List<MapCellInfo>(); // Not used here but needed for conversion
                ConvertCellInfoToMapInfo(mapReader, imageInfos, cellInfos, dataPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing map file: {ex.Message}");
            }

            return imageInfos.DistinctBy(x => x.Index).ToList();
        }

        public static (List<MapImageInfo> imageInfos, List<MapCellInfo> cellInfos, int width, int height) ParseMapFileWithCells(string mapFilePath, string dataPath)
        {
            var imageInfos = new List<MapImageInfo>();
            var cellInfos = new List<MapCellInfo>();

            if (!File.Exists(mapFilePath))
            {
                Console.WriteLine($"Map file not found: {mapFilePath}");
                return (imageInfos, cellInfos, 0, 0);
            }

            try
            {
                // Use the Client's MapReader for parsing
                var mapReader = new MapReader(mapFilePath);
                int width = mapReader.Width;
                int height = mapReader.Height;

                Console.WriteLine($"Map dimensions: {width}x{height}");

                // Convert CellInfo to MapCellInfo and MapImageInfo
                ConvertCellInfoToMapInfo(mapReader, imageInfos, cellInfos, dataPath);

                Console.WriteLine($"Cell count: {cellInfos.Count}, Image count: {imageInfos.Count}");
                return (imageInfos.DistinctBy(x => x.Index).ToList(), cellInfos, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing map file: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (imageInfos, cellInfos, 0, 0);
            }
        }

        private static void ConvertCellInfoToMapInfo(MapReader mapReader, List<MapImageInfo> imageInfos, List<MapCellInfo> cellInfos, string dataPath)
        {
            var addedIndices = new HashSet<string>(); // To avoid duplicates

            for (int x = 0; x < mapReader.Width; x++)
            {
                for (int y = 0; y < mapReader.Height; y++)
                {
                    var cell = mapReader.MapCells[x, y];

                    // Add cell info
                    var cellInfo = new MapCellInfo
                    {
                        X = x,
                        Y = y,
                        BackIndex = cell.BackIndex,
                        TileIndex = cell.BackImage,
                        MiddleIndex = cell.MiddleIndex,
                        SmObjectIndex = cell.MiddleImage,
                        FrontIndex = cell.FrontIndex,
                        ObjectIndex = cell.FrontImage
                    };
                    cellInfos.Add(cellInfo);

                    // Add image info for back tiles - apply mask
                    int backImageIndex = (cell.BackImage & 0x1FFFFFFF) - 1;
                    if (cell.BackIndex >= 0 && backImageIndex >= 0)
                    {
                        string libraryPath = GetLibraryPath(cell.BackIndex, 0); // 0 for back tiles
                        string key = $"{libraryPath}_{backImageIndex}";
                        if (!addedIndices.Contains(key))
                        {
                            AddImageInfo(imageInfos, backImageIndex, libraryPath, dataPath);
                            addedIndices.Add(key);
                        }
                    }

                    // Add image info for middle tiles
                    int middleImageIndex = cell.MiddleImage - 1;
                    if (cell.MiddleIndex >= 0 && middleImageIndex >= 0)
                    {
                        string libraryPath = GetLibraryPath(cell.MiddleIndex, 1); // 1 for middle tiles
                        string key = $"{libraryPath}_{middleImageIndex}";
                        if (!addedIndices.Contains(key))
                        {
                        AddImageInfo(imageInfos, middleImageIndex, libraryPath, dataPath);
                            addedIndices.Add(key);
                        }
                    }

                    // Add image info for front tiles - apply mask
                    int frontImageIndex = (cell.FrontImage & 0x7FFF) - 1;
                    if (cell.FrontIndex >= 0 && frontImageIndex >= 0)
                    {
                        string libraryPath = GetLibraryPath(cell.FrontIndex, 2); // 2 for front tiles
                        string key = $"{libraryPath}_{frontImageIndex}";
                        if (!addedIndices.Contains(key))
                        {
                            AddImageInfo(imageInfos, frontImageIndex, libraryPath, dataPath);
                            addedIndices.Add(key);
                        }
                    }
                }
            }
        }

        public static string GetLibraryPath(short index, int layer)
        {
            // Recreate the exact library path mapping from Client project
            if (index >= 0 && index < 100)
            {
                // Wemade Mir2 (0-99)
                return layer switch
                {
                    0 => "Map/WemadeMir2/Tiles",           // Back tiles
                    1 => "Map/WemadeMir2/Smtiles",         // Middle tiles
                    2 => index == 2 ? "Map/WemadeMir2/Objects" : $"Map/WemadeMir2/Objects{index - 1}", // Front tiles
                    _ => "Map/WemadeMir2/Tiles"
                };
            }
            else if (index >= 100 && index < 200)
            {
                // Shanda Mir2 (100-199)
                return layer switch
                {
                    0 => index == 100 ? "Map/ShandaMir2/Tiles" : $"Map/ShandaMir2/Tiles{index - 99}",     // Back tiles
                    1 => index == 110 ? "Map/ShandaMir2/SmTiles" : $"Map/ShandaMir2/SmTiles{index - 109}", // Middle tiles
                    2 => index >= 120 && index < 151 ? $"Map/ShandaMir2/Objects{index - 119}" : "Map/ShandaMir2/Objects", // Front tiles
                    _ => "Map/ShandaMir2/Tiles"
                };
            }
            else if (index >= 200 && index < 299)
            {
                // Wemade Mir3 (200-299)
                int stateGroup = (index - 200) / 15;
                int stateIndex = (index - 200) % 15;
                string[] mapStates = { "", "wood/", "sand/", "snow/", "forest/" };
                string statePrefix = stateGroup < mapStates.Length ? mapStates[stateGroup] : "";

                return stateIndex switch
                {
                    0 => $"Map/WemadeMir3/{statePrefix}Tilesc",
                    1 => $"Map/WemadeMir3/{statePrefix}Tiles30c",
                    2 => $"Map/WemadeMir3/{statePrefix}Tiles5c",
                    3 => $"Map/WemadeMir3/{statePrefix}Smtilesc",
                    4 => $"Map/WemadeMir3/{statePrefix}Housesc",
                    5 => $"Map/WemadeMir3/{statePrefix}Cliffsc",
                    6 => $"Map/WemadeMir3/{statePrefix}Dungeonsc",
                    7 => $"Map/WemadeMir3/{statePrefix}Innersc",
                    8 => $"Map/WemadeMir3/{statePrefix}Furnituresc",
                    9 => $"Map/WemadeMir3/{statePrefix}Wallsc",
                    10 => $"Map/WemadeMir3/{statePrefix}smObjectsc",
                    11 => $"Map/WemadeMir3/{statePrefix}Animationsc",
                    12 => $"Map/WemadeMir3/{statePrefix}Object1c",
                    13 => $"Map/WemadeMir3/{statePrefix}Object2c",
                    _ => $"Map/WemadeMir3/{statePrefix}Tilesc"
                };
            }
            else if (index >= 300 && index < 399)
            {
                // Shanda Mir3 (300-399)
                int stateGroup = (index - 300) / 15;
                int stateIndex = (index - 300) % 15;
                string[] mapStates = { "", "wood", "sand", "snow", "forest" };
                string stateSuffix = stateGroup < mapStates.Length ? mapStates[stateGroup] : "";

                return stateIndex switch
                {
                    0 => $"Map/ShandaMir3/Tilesc{stateSuffix}",
                    1 => $"Map/ShandaMir3/Tiles30c{stateSuffix}",
                    2 => $"Map/ShandaMir3/Tiles5c{stateSuffix}",
                    3 => $"Map/ShandaMir3/Smtilesc{stateSuffix}",
                    4 => $"Map/ShandaMir3/Housesc{stateSuffix}",
                    5 => $"Map/ShandaMir3/Cliffsc{stateSuffix}",
                    6 => $"Map/ShandaMir3/Dungeonsc{stateSuffix}",
                    7 => $"Map/ShandaMir3/Innersc{stateSuffix}",
                    8 => $"Map/ShandaMir3/Furnituresc{stateSuffix}",
                    9 => $"Map/ShandaMir3/Wallsc{stateSuffix}",
                    10 => $"Map/ShandaMir3/smObjectsc{stateSuffix}",
                    11 => $"Map/ShandaMir3/Animationsc{stateSuffix}",
                    12 => $"Map/ShandaMir3/Object1c{stateSuffix}",
                    13 => $"Map/ShandaMir3/Object2c{stateSuffix}",
                    _ => $"Map/ShandaMir3/Tilesc{stateSuffix}"
                };
            }
            else
            {
                // Default fallback
                return layer switch
                {
                    0 => "Map/WemadeMir2/Tiles",
                    1 => "Map/WemadeMir2/Smtiles",
                    2 => "Map/WemadeMir2/Objects",
                    _ => "Map/WemadeMir2/Tiles"
                };
            }
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