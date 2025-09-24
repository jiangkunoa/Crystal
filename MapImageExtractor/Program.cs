using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace MapImageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string mapName = args[0];
            string outputDirectory = "./output";
            string dataDirectory = "./Data";
            string mapDirectory = "./Map";
            bool verbose = false;
            bool outputFullMap = false;

            // Parse simple command line arguments
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length)
                    outputDirectory = args[++i];
                else if (args[i] == "-d" && i + 1 < args.Length)
                    dataDirectory = args[++i];
                else if (args[i] == "--map-path" && i + 1 < args.Length)
                    mapDirectory = args[++i];
                else if (args[i] == "-v")
                    verbose = true;
                else if (args[i] == "--full-map")
                    outputFullMap = true;
            }

            Run(mapName, outputDirectory, dataDirectory, mapDirectory, verbose, outputFullMap);
        }

        static void PrintUsage()
        {
            Console.WriteLine("Map Image Extractor - Extract images from Legend of Mir 2 maps");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("  MapImageExtractor <map_name> [options]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("  -o <dir>       Output directory (default: ./output)");
            Console.WriteLine("  -d <dir>       Data directory (default: ./Data)");
            Console.WriteLine("  --map-path <dir> Map directory (default: ./Map)");
            Console.WriteLine("  -v             Enable verbose output");
            Console.WriteLine("  --full-map     Output full map image");
        }

        static void Run(string mapName, string outputDirectory, string dataDirectory, string mapDirectory, bool verbose, bool outputFullMap)
        {
            try
            {
                Console.WriteLine($"Map Image Extractor - Processing map: {mapName}");

                // Create output directory
                string outputPath = Path.Combine(outputDirectory, mapName);
                string imagesPath = Path.Combine(outputPath, "images");
                Directory.CreateDirectory(imagesPath);

                // Find map file
                string mapFilePath = FindMapFile(mapDirectory, mapName);
                if (mapFilePath == null)
                {
                    Console.WriteLine($"Map file for '{mapName}' not found in {mapDirectory}");
                    return;
                }

                Console.WriteLine($"Found map file: {mapFilePath}");

                // Parse map to get image information
                var imageInfos = MapParser.ParseMapFile(mapFilePath, dataDirectory);
                Console.WriteLine($"Found {imageInfos.Count} image references in map");

                if (imageInfos.Count == 0)
                {
                    Console.WriteLine("No images found to extract");
                    return;
                }

                // Extract images
                int successCount = 0;
                var libraries = new Dictionary<string, MLibrary>();

                foreach (var imageInfo in imageInfos)
                {
                    try
                    {
                        if (verbose)
                            Console.WriteLine($"Processing image {imageInfo.Index} from {imageInfo.Library}");

                        string libKey = imageInfo.Library.ToLower();
                        if (!libraries.ContainsKey(libKey))
                        {
                            string libPath = Path.Combine(dataDirectory, imageInfo.Library);
                            var library = new MLibrary(libPath);
                            library.Initialize();
                            libraries[libKey] = library;
                        }

                        var lib = libraries[libKey];

                        // Create subfolder for each library
                        string libraryFolder = Path.Combine(imagesPath, Path.GetFileName(imageInfo.Library));
                        Directory.CreateDirectory(libraryFolder);

                        string outputFile = Path.Combine(libraryFolder, imageInfo.FileName);

                        if (lib.ExtractImage(imageInfo.Index, outputFile))
                        {
                            successCount++;
                            if (verbose)
                                Console.WriteLine($"✓ Extracted {imageInfo.FileName}");

                            // Update image info with actual dimensions
                            var image = lib.GetImage(imageInfo.Index);
                            if (image != null)
                            {
                                imageInfo.Width = image.Width;
                                imageInfo.Height = image.Height;
                                imageInfo.OffsetX = image.X;
                                imageInfo.OffsetY = image.Y;
                            }
                        }
                        else
                        {
                            if (verbose)
                                Console.WriteLine($"✗ Failed to extract {imageInfo.FileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting image {imageInfo.Index}: {ex.Message}");
                    }
                }

                // Create full map image if requested
                if (outputFullMap)
                {
                    Console.WriteLine("Creating full map image...");
                    CreateFullMapImage(mapFilePath, dataDirectory, outputPath, libraries, verbose);
                }

                // Save parameters file
                MapParser.SaveParameters(imageInfos, outputPath, mapName);

                Console.WriteLine($"\nExtraction completed!");
                Console.WriteLine($"Successfully extracted: {successCount}/{imageInfos.Count} images");
                Console.WriteLine($"Output directory: {outputPath}");
                string paramFile = Path.Combine(outputPath, "parameters.json");
                Console.WriteLine($"Parameters file: {paramFile}");

                // Clean up libraries
                foreach (var library in libraries.Values)
                {
                    library.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                if (verbose)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static void CreateFullMapImage(string mapFilePath, string dataDirectory, string outputPath, Dictionary<string, MLibrary> libraries, bool verbose)
        {
            try
            {
                Console.WriteLine($"Creating full map image for: {mapFilePath}");
                // Parse map with cell information
                var (imageInfos, cellInfos, width, height) = MapParser.ParseMapFileWithCells(mapFilePath, dataDirectory);

                Console.WriteLine($"Map dimensions: {width}x{height}");
                Console.WriteLine($"Cell count: {cellInfos.Count}, Image count: {imageInfos.Count}");

                Console.WriteLine($"Parse result - Width: {width}, Height: {height}, Cells: {cellInfos.Count}, Images: {imageInfos.Count}");

                if (width <= 0 || height <= 0)
                {
                    Console.WriteLine("Cannot create full map image: invalid map dimensions");
                    return;
                }

                if (cellInfos.Count == 0)
                {
                    Console.WriteLine("Cannot create full map image: no cell data found");
                    return;
                }

                // Preload required libraries
                var requiredLibraries = imageInfos.Select(i => i.Library.ToLower()).Distinct().ToList();
                foreach (var libPath in requiredLibraries)
                {
                    string fullLibPath = Path.Combine(dataDirectory, libPath);
                    string libKey = libPath.ToLower();
                    if (!libraries.ContainsKey(libKey))
                    {
                        try
                        {
                            var library = new MLibrary(fullLibPath);
                            library.Initialize();
                            libraries[libKey] = library;
                        }
                        catch (Exception ex)
                        {
                            if (verbose)
                                Console.WriteLine($"Warning: Failed to load library {libPath}: {ex.Message}");
                        }
                    }
                }

                // Create full map image
                // For simplicity, we'll assume each cell is 48x32 pixels (standard tile size)
                const int cellWidth = 48;
                const int cellHeight = 32;
                int mapWidth = width * cellWidth;
                int mapHeight = height * cellHeight;

                // Check if the map size is too large and reduce it if necessary
                const int maxDimension = 32767; // Maximum bitmap dimension
                if (mapWidth > maxDimension || mapHeight > maxDimension)
                {
                    Console.WriteLine($"Map size ({mapWidth}x{mapHeight}) is too large, scaling down...");
                    double scale = Math.Min((double)maxDimension / mapWidth, (double)maxDimension / mapHeight);
                    mapWidth = (int)(mapWidth * scale);
                    mapHeight = (int)(mapHeight * scale);
                    Console.WriteLine($"Scaled map size: {mapWidth}x{mapHeight}");
                }

                // Use MapReader to get proper cell info
                var mapReader = new MapReader(mapFilePath);

                try
                {
                    using (var fullMapBitmap = new Bitmap(mapWidth, mapHeight))
                    using (var graphics = Graphics.FromImage(fullMapBitmap))
                    {
                        graphics.Clear(Color.Black);

                        // Draw each cell in correct Z-order: Back -> Middle -> Front
                        // First draw all back tiles (no size filtering for back tiles)
                        for (int y = 0; y < mapReader.Height; y++)
                        {
                            for (int x = 0; x < mapReader.Width; x++)
                            {
                                var cell = mapReader.MapCells[x, y];
                                if ((cell.BackImage == 0) || (cell.BackIndex == -1)) continue;

                                int index = (cell.BackImage & 0x1FFFFFFF) - 1;
                                string backLibraryPath = MapParser.GetLibraryPath(cell.BackIndex, 0);
                                string libKey = backLibraryPath.ToLower();

                                if (libraries.ContainsKey(libKey))
                                {
                                    var library = libraries[libKey];
                                    var image = library.GetImage(index);
                                    if (image != null && image.Image != null)
                                    {
                                        // Calculate position with offset - match Client project exactly
                                        int drawX = x * cellWidth + image.X;
                                        int drawY = y * cellHeight + image.Y;
                                        graphics.DrawImage(image.Image, drawX, drawY);
                                    }
                                }
                            }
                        }

                        // Then draw all middle tiles with special handling for non-standard sizes
                        for (int y = 0; y < mapReader.Height; y++)
                        {
                            for (int x = 0; x < mapReader.Width; x++)
                            {
                                var cell = mapReader.MapCells[x, y];
                                int index = cell.MiddleImage - 1;

                                if (index < 0 || cell.MiddleIndex == -1) continue;

                                string middleLibraryPath = MapParser.GetLibraryPath(cell.MiddleIndex, 1);
                                string libKey = middleLibraryPath.ToLower();

                                if (libraries.ContainsKey(libKey))
                                {
                                    var library = libraries[libKey];
                                    var image = library.GetImage(index);
                                    if (image != null && image.Image != null)
                                    {
                                        // Calculate position with offset - match Client project exactly
                                        int drawX = x * cellWidth + image.X;
                                        int drawY = y * cellHeight + image.Y;

                                        // Check if image has standard size
                                        if ((image.Width == cellWidth && image.Height == cellHeight) ||
                                            (image.Width == cellWidth * 2 && image.Height == cellHeight * 2))
                                        {
                                            // Draw standard size images normally
                                            graphics.DrawImage(image.Image, drawX, drawY);
                                        }
                                        else
                                        {
                                            // For non-standard sizes, draw with special positioning (like Client's DrawUp)
                                            // Move image up by its height to match Client's DrawUp behavior
                                            int adjustedY = drawY - image.Height;
                                            graphics.DrawImage(image.Image, drawX, adjustedY);
                                        }
                                    }
                                }
                            }
                        }

                        // Finally draw all front tiles with special handling for non-standard sizes
                        for (int y = 0; y < mapReader.Height; y++)
                        {
                            for (int x = 0; x < mapReader.Width; x++)
                            {
                                var cell = mapReader.MapCells[x, y];
                                int index = (cell.FrontImage & 0x7FFF) - 1;

                                if (index == -1 || cell.FrontIndex == -1) continue;

                                string frontLibraryPath = MapParser.GetLibraryPath(cell.FrontIndex, 2);
                                string libKey = frontLibraryPath.ToLower();

                                if (libraries.ContainsKey(libKey))
                                {
                                    var library = libraries[libKey];
                                    var image = library.GetImage(index);
                                    if (image != null && image.Image != null)
                                    {
                                        // Calculate position with offset - match Client project exactly
                                        int drawX = x * cellWidth + image.X;
                                        int drawY = y * cellHeight + image.Y;

                                        // Check if image has standard size
                                        if ((image.Width == cellWidth && image.Height == cellHeight) ||
                                            (image.Width == cellWidth * 2 && image.Height == cellHeight * 2))
                                        {
                                            // Draw standard size images normally
                                            graphics.DrawImage(image.Image, drawX, drawY);
                                        }
                                        else
                                        {
                                            // For non-standard sizes, draw with special positioning
                                            // Move image up by its height to match Client's behavior
                                            int adjustedY = drawY - image.Height;
                                            graphics.DrawImage(image.Image, drawX, adjustedY);
                                        }
                                    }
                                }
                            }
                        }

                        // Save full map image
                        string fullMapPath = Path.Combine(outputPath, "full_map.png");
                        fullMapBitmap.Save(fullMapPath, ImageFormat.Png);
                        Console.WriteLine($"Full map image saved to: {fullMapPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating bitmap: {ex.Message}");
                    // Try creating a smaller version
                    CreateReducedMapImage(mapFilePath, dataDirectory, outputPath, libraries, verbose, mapReader, cellWidth, cellHeight);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating full map image: {ex.Message}");
                if (verbose)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static void DrawCellImage(Graphics graphics, Dictionary<string, MLibrary> libraries,
                                string libraryPath, int imageIndex, int x, int y, bool verbose)
        {
            try
            {
                string libKey = libraryPath.ToLower();
                if (libraries.ContainsKey(libKey))
                {
                    var library = libraries[libKey];
                    var image = library.GetImage(imageIndex);
                    if (image != null && image.Image != null)
                    {
                        // Calculate position with offset
                        int drawX = x + image.X;
                        int drawY = y + image.Y;
                        graphics.DrawImage(image.Image, drawX, drawY);
                    }
                }
            }
            catch (Exception ex)
            {
                if (verbose)
                    Console.WriteLine($"Warning: Failed to draw image {imageIndex} from {libraryPath}: {ex.Message}");
            }
        }

        static void CreateReducedMapImage(string mapFilePath, string dataDirectory, string outputPath, Dictionary<string, MLibrary> libraries, bool verbose, MapReader mapReader, int cellWidth, int cellHeight)
        {
            try
            {
                Console.WriteLine("Creating reduced map image...");

                // Create a smaller bitmap (1/4 size)
                int reducedWidth = mapReader.Width * cellWidth / 4;
                int reducedHeight = mapReader.Height * cellHeight / 4;

                using (var fullMapBitmap = new Bitmap(reducedWidth, reducedHeight))
                using (var graphics = Graphics.FromImage(fullMapBitmap))
                {
                    graphics.Clear(Color.Black);
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                    // Draw each cell in correct Z-order: Back -> Middle -> Front
                    // First draw all back tiles (no size filtering for back tiles)
                    for (int y = 0; y < mapReader.Height; y++)
                    {
                        for (int x = 0; x < mapReader.Width; x++)
                        {
                            var cell = mapReader.MapCells[x, y];
                            if ((cell.BackImage == 0) || (cell.BackIndex == -1)) continue;

                            int index = (cell.BackImage & 0x1FFFFFFF) - 1;
                            string backLibraryPath = MapParser.GetLibraryPath(cell.BackIndex, 0);
                            string libKey = backLibraryPath.ToLower();

                            if (libraries.ContainsKey(libKey))
                            {
                                var library = libraries[libKey];
                                var image = library.GetImage(index);
                                if (image != null && image.Image != null)
                                {
                                    // Calculate position with offset and scale
                                    int drawX = (x * cellWidth + image.X) / 4;
                                    int drawY = (y * cellHeight + image.Y) / 4;
                                    int scaledWidth = Math.Max(1, image.Image.Width / 4);
                                    int scaledHeight = Math.Max(1, image.Image.Height / 4);
                                    graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);
                                }
                            }
                        }
                    }

                    // Then draw all middle tiles with special handling for non-standard sizes
                    for (int y = 0; y < mapReader.Height; y++)
                    {
                        for (int x = 0; x < mapReader.Width; x++)
                        {
                            var cell = mapReader.MapCells[x, y];
                            int index = cell.MiddleImage - 1;

                            if (index < 0 || cell.MiddleIndex == -1) continue;

                            string middleLibraryPath = MapParser.GetLibraryPath(cell.MiddleIndex, 1);
                            string libKey = middleLibraryPath.ToLower();

                            if (libraries.ContainsKey(libKey))
                            {
                                var library = libraries[libKey];
                                var image = library.GetImage(index);
                                if (image != null && image.Image != null)
                                {
                                    // Calculate position with offset and scale
                                    int drawX = (x * cellWidth + image.X) / 4;
                                    int drawY = (y * cellHeight + image.Y) / 4;
                                    int scaledWidth = Math.Max(1, image.Image.Width / 4);
                                    int scaledHeight = Math.Max(1, image.Image.Height / 4);

                                    // Check if image has standard size
                                    if ((image.Width == cellWidth && image.Height == cellHeight) ||
                                        (image.Width == cellWidth * 2 && image.Height == cellHeight * 2))
                                    {
                                        // Draw standard size images normally
                                        graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);
                                    }
                                    else
                                    {
                                        // For non-standard sizes, draw with special positioning (like Client's DrawUp)
                                        // Move image up by its height to match Client's DrawUp behavior
                                        int adjustedY = (drawY * 4 - image.Height) / 4;
                                        graphics.DrawImage(image.Image, drawX, adjustedY, scaledWidth, scaledHeight);
                                    }
                                }
                            }
                        }
                    }

                    // Finally draw all front tiles with special handling for non-standard sizes
                    for (int y = 0; y < mapReader.Height; y++)
                    {
                        for (int x = 0; x < mapReader.Width; x++)
                        {
                            var cell = mapReader.MapCells[x, y];
                            int index = (cell.FrontImage & 0x7FFF) - 1;

                            if (index == -1 || cell.FrontIndex == -1) continue;

                            string frontLibraryPath = MapParser.GetLibraryPath(cell.FrontIndex, 2);
                            string libKey = frontLibraryPath.ToLower();

                            if (libraries.ContainsKey(libKey))
                            {
                                var library = libraries[libKey];
                                var image = library.GetImage(index);
                                if (image != null && image.Image != null)
                                {
                                    // Calculate position with offset and scale
                                    int drawX = (x * cellWidth + image.X) / 4;
                                    int drawY = (y * cellHeight + image.Y) / 4;
                                    int scaledWidth = Math.Max(1, image.Image.Width / 4);
                                    int scaledHeight = Math.Max(1, image.Image.Height / 4);

                                    // Check if image has standard size
                                    if ((image.Width == cellWidth && image.Height == cellHeight) ||
                                        (image.Width == cellWidth * 2 && image.Height == cellHeight * 2))
                                    {
                                        // Draw standard size images normally
                                        graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);
                                    }
                                    else
                                    {
                                        // For non-standard sizes, draw with special positioning
                                        // Move image up by its height to match Client's behavior
                                        int adjustedY = (drawY * 4 - image.Height) / 4;
                                        graphics.DrawImage(image.Image, drawX, adjustedY, scaledWidth, scaledHeight);
                                    }
                                }
                            }
                        }
                    }

                    // Save full map image
                    string fullMapPath = Path.Combine(outputPath, "full_map_reduced.png");
                    fullMapBitmap.Save(fullMapPath, ImageFormat.Png);
                    Console.WriteLine($"Reduced full map image saved to: {fullMapPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating reduced map image: {ex.Message}");
            }
        }

        static string FindMapFile(string mapDirectory, string mapName)
        {
            try
            {
                Console.WriteLine($"Searching for map '{mapName}' in directory '{mapDirectory}'");

                // If mapName already contains .map extension, don't add it again
                string fileName = mapName.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ?
                    mapName : mapName + ".map";

                // Try exact filename match first
                string exactPath = Path.Combine(mapDirectory, fileName);
                Console.WriteLine($"Checking exact path: {exactPath}");
                if (File.Exists(exactPath))
                {
                    Console.WriteLine($"Found map at exact path: {exactPath}");
                    return exactPath;
                }

                // Try with just the map name directly
                if (File.Exists(mapName))
                {
                    Console.WriteLine($"Found map at direct path: {mapName}");
                    return mapName;
                }

                // Search for map files (case insensitive)
                if (Directory.Exists(mapDirectory))
                {
                    var mapFiles = Directory.GetFiles(mapDirectory, "*.map", SearchOption.AllDirectories);
                    Console.WriteLine($"Found {mapFiles.Length} map files in directory");
                    var result = mapFiles.FirstOrDefault(f =>
                        Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileNameWithoutExtension(f).Equals(mapName, StringComparison.OrdinalIgnoreCase));
                    if (result != null)
                    {
                        Console.WriteLine($"Found map by search: {result}");
                        return result;
                    }
                }
                else
                {
                    Console.WriteLine($"Map directory does not exist: {mapDirectory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for map file: {ex.Message}");
            }

            return null;
        }
    }
}