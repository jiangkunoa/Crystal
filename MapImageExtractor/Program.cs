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

                using (var fullMapBitmap = new Bitmap(mapWidth, mapHeight))
                using (var graphics = Graphics.FromImage(fullMapBitmap))
                {
                    graphics.Clear(Color.Black);

                    // Draw each cell in correct Z-order: Back -> Middle -> Front
                    foreach (var cellInfo in cellInfos)
                    {
                        // Draw back layer (background tiles)
                        if (cellInfo.TileIndex > 0)
                        {
                            string backLibraryPath = MapParser.GetLibraryPath((short)cellInfo.BackIndex, 0); // 0 for back tiles
                            DrawCellImage(graphics, libraries, backLibraryPath, cellInfo.TileIndex,
                                        cellInfo.X * cellWidth, cellInfo.Y * cellHeight, verbose);
                        }

                        // Draw middle layer (small objects)
                        if (cellInfo.SmObjectIndex > 0)
                        {
                            string middleLibraryPath = MapParser.GetLibraryPath((short)cellInfo.MiddleIndex, 1); // 1 for middle tiles
                            DrawCellImage(graphics, libraries, middleLibraryPath, cellInfo.SmObjectIndex,
                                        cellInfo.X * cellWidth, cellInfo.Y * cellHeight, verbose);
                        }

                        // Draw front layer (objects including blue tiles)
                        if (cellInfo.ObjectIndex > 0)
                        {
                            string frontLibraryPath = MapParser.GetLibraryPath((short)cellInfo.FrontIndex, 2); // 2 for front tiles
                            DrawCellImage(graphics, libraries, frontLibraryPath, cellInfo.ObjectIndex,
                                        cellInfo.X * cellWidth, cellInfo.Y * cellHeight, verbose);
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

        static string FindMapFile(string mapDirectory, string mapName)
        {
            // Try exact filename match first
            string exactPath = Path.Combine(mapDirectory, mapName + ".map");
            if (File.Exists(exactPath))
                return exactPath;

            // Search for map files (case insensitive)
            var mapFiles = Directory.GetFiles(mapDirectory, "*.map", SearchOption.AllDirectories);
            return mapFiles.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Equals(mapName, StringComparison.OrdinalIgnoreCase));
        }
    }
}