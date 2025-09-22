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
            }

            Run(mapName, outputDirectory, dataDirectory, mapDirectory, verbose);
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
        }

        static void Run(string mapName, string outputDirectory, string dataDirectory, string mapDirectory, bool verbose)
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