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
            // Determine library path based on index and layer
            // This is a simplified version - you may need to adjust based on your specific library structure
            return index switch
            {
                >= 0 and < 100 => layer switch
                {
                    0 => "Map/WemadeMir2/Tiles",      // Back tiles
                    1 => "Map/WemadeMir2/Smtiles",    // Middle tiles
                    2 => "Map/WemadeMir2/Objects",    // Front tiles
                    _ => "Map/WemadeMir2/Tiles"
                },
                >= 100 and < 200 => layer switch
                {
                    0 => "Map/ShandaMir2/Tiles",
                    1 => "Map/ShandaMir2/SmTiles",
                    2 => "Map/ShandaMir2/Objects",
                    _ => "Map/ShandaMir2/Tiles"
                },
                >= 200 and < 300 => layer switch
                {
                    0 => "Map/WemadeMir3/Tilesc",
                    1 => "Map/WemadeMir3/Smtilesc",
                    2 => "Map/WemadeMir3/Objects",
                    _ => "Map/WemadeMir3/Tilesc"
                },
                >= 300 and < 400 => layer switch
                {
                    0 => "Map/ShandaMir3/Tiles",
                    1 => "Map/ShandaMir3/SmTiles",
                    2 => "Map/ShandaMir3/Objects",
                    _ => "Map/ShandaMir3/Tiles"
                },
                _ => layer switch
                {
                    0 => "Map/WemadeMir2/Tiles",
                    1 => "Map/WemadeMir2/Smtiles",
                    2 => "Map/WemadeMir2/Objects",
                    _ => "Map/WemadeMir2/Tiles"
                }
            };
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