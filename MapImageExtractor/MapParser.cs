using System.Text.Json;

namespace MapImageExtractor
{
    public class MapParser
    {
        public class MapImageInfo
        {
            public int Index { get; set; }
            public MLibrary Library { get; set; } = null!;
            public int Width { get; set; }
            public int Height { get; set; }
            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
        }

        public class MapCellInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            
            required 
            public CellInfo CellInfo { get; set; }
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
                        CellInfo = cell
                    };
                    cellInfos.Add(cellInfo);
                    {
                        // Add image info for back tiles - apply mask
                        if ((cell.BackImage == 0) || (cell.BackIndex == -1)) continue;
                        int index = (cell.BackImage & 0x1FFFFFFF) - 1;
                        MLibrary mLibrary = Libraries.MapLibs[cell.BackIndex];
                        var image = mLibrary.GetImage(index);
                        string fileName = mLibrary.GetFileName();
                        string key = $"{fileName}_{index}";
                        Console.WriteLine($"Add image info for back tile: {fileName}_{index}, size: {image.Width}x{image.Height}");
                        if (!addedIndices.Contains(key))
                        {
                            AddImageInfo(imageInfos, index, mLibrary);
                            addedIndices.Add(key);
                        }
                    }
                    // TODO JKF
                    // // Add image info for middle tiles
                    // int middleImageIndex = cell.MiddleImage - 1;
                    // if (cell.MiddleIndex >= 0 && middleImageIndex >= 0)
                    // {
                    //     string libraryPath = GetLibraryPath(cell.MiddleIndex, 1); // 1 for middle tiles
                    //     string key = $"{libraryPath}_{middleImageIndex}";
                    //     if (!addedIndices.Contains(key))
                    //     {
                    //     AddImageInfo(imageInfos, middleImageIndex, libraryPath, dataPath);
                    //         addedIndices.Add(key);
                    //     }
                    // }
                    //
                    // // Add image info for front tiles - apply mask
                    // int frontImageIndex = (cell.FrontImage & 0x7FFF) - 1;
                    // if (cell.FrontIndex >= 0 && frontImageIndex >= 0)
                    // {
                    //     string libraryPath = GetLibraryPath(cell.FrontIndex, 2); // 2 for front tiles
                    //     string key = $"{libraryPath}_{frontImageIndex}";
                    //     if (!addedIndices.Contains(key))
                    //     {
                    //         AddImageInfo(imageInfos, frontImageIndex, libraryPath, dataPath);
                    //         addedIndices.Add(key);
                    //     }
                    // }
                }
            }
        }


        private static void AddImageInfo(List<MapImageInfo> imageInfos, int index, MLibrary library)
        {
            var info = new MapImageInfo
            {
                Index = index,
                Library = library,
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