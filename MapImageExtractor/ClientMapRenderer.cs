using System.Drawing;
using System.Drawing.Imaging;

namespace MapImageExtractor
{
    public class ClientMapRenderer
    {
        private const int CellWidth = 48;
        private const int CellHeight = 32;

        public static void RenderFullMap(MapReader mapReader, Dictionary<string, MLibrary> libraries, string outputPath, bool verbose = false)
        {
            try
            {
                Console.WriteLine($"Rendering full map with Client logic: {mapReader.Width}x{mapReader.Height}");

                // Calculate map dimensions
                int mapWidth = mapReader.Width * CellWidth;
                int mapHeight = mapReader.Height * CellHeight;

                // Check if the map size is too large and reduce it if necessary
                const int maxDimension = 32767; // Maximum bitmap dimension
                int scaledMapWidth = mapWidth;
                int scaledMapHeight = mapHeight;

                if (scaledMapWidth > maxDimension || scaledMapHeight > maxDimension)
                {
                    Console.WriteLine($"Map size ({scaledMapWidth}x{scaledMapHeight}) is too large, scaling down...");
                    double scale = Math.Min((double)maxDimension / scaledMapWidth, (double)maxDimension / scaledMapHeight);
                    scaledMapWidth = (int)(scaledMapWidth * scale);
                    scaledMapHeight = (int)(scaledMapHeight * scale);
                    Console.WriteLine($"Scaled map size: {scaledMapWidth}x{scaledMapHeight}");
                }

                try
                {
                    // Create bitmap with scaled dimensions
                    using (var fullMapBitmap = new Bitmap(scaledMapWidth, scaledMapHeight))
                    using (var graphics = Graphics.FromImage(fullMapBitmap))
                    {
                        graphics.Clear(Color.Black);

                        // Calculate scale factors
                        float scaleX = (float)scaledMapWidth / mapWidth;
                        float scaleY = (float)scaledMapHeight / mapHeight;

                        // Draw each cell in correct Z-order: Back -> Middle -> Front
                        DrawBackLayer(mapReader, libraries, graphics, scaleX, scaleY, verbose);
                        DrawMiddleLayer(mapReader, libraries, graphics, scaleX, scaleY, verbose);
                        DrawFrontLayer(mapReader, libraries, graphics, scaleX, scaleY, verbose);

                        // Save full map image
                        string fullMapPath = Path.Combine(outputPath, "full_map_client.png");
                        fullMapBitmap.Save(fullMapPath, ImageFormat.Png);
                        Console.WriteLine($"Full map image saved to: {fullMapPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating bitmap: {ex.Message}");
                    // Try creating a smaller version
                    RenderReducedMap(mapReader, libraries, outputPath, verbose);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering full map: {ex.Message}");
                if (verbose)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void DrawBackLayer(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            Graphics graphics, float scaleX, float scaleY, bool verbose)
        {
            Console.WriteLine("Drawing back layer...");

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
                            // Calculate scaled position with offset
                            int drawX = (int)((x * CellWidth + image.X) * scaleX);
                            int drawY = (int)((y * CellHeight + image.Y) * scaleY);
                            int scaledWidth = (int)(image.Image.Width * scaleX);
                            int scaledHeight = (int)(image.Image.Height * scaleY);

                            graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);

                            if (verbose && (x * mapReader.Height + y) % 10000 == 0)
                                Console.WriteLine($"Drawn back tile at ({x}, {y})");
                        }
                    }
                }
            }
        }

        private static void DrawMiddleLayer(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            Graphics graphics, float scaleX, float scaleY, bool verbose)
        {
            Console.WriteLine("Drawing middle layer...");

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
                            // Calculate scaled position with offset
                            int drawX = (int)((x * CellWidth + image.X) * scaleX);
                            int drawY = (int)((y * CellHeight + image.Y) * scaleY);
                            int scaledWidth = (int)(image.Image.Width * scaleX);
                            int scaledHeight = (int)(image.Image.Height * scaleY);

                            // Apply size filtering like Client project
                            if ((image.Width != CellWidth || image.Height != CellHeight) &&
                                (image.Width != CellWidth * 2 || image.Height != CellHeight * 2))
                            {
                                // For non-standard sizes, adjust position like Client's DrawUp
                                drawY -= (int)(image.Height * scaleY);
                            }

                            graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);

                            if (verbose && (x * mapReader.Height + y) % 10000 == 0)
                                Console.WriteLine($"Drawn middle tile at ({x}, {y})");
                        }
                    }
                }
            }
        }

        private static void DrawFrontLayer(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            Graphics graphics, float scaleX, float scaleY, bool verbose)
        {
            Console.WriteLine("Drawing front layer...");

            for (int y = 0; y < mapReader.Height; y++)
            {
                for (int x = 0; x < mapReader.Width; x++)
                {
                    var cell = mapReader.MapCells[x, y];
                    int index = (cell.FrontImage & 0x7FFF) - 1;

                    if (index < 0 || cell.FrontIndex == -1) continue;

                    // Handle door index like Client project
                    if (cell.DoorIndex > 0)
                    {
                        // Use door offset to adjust index
                        index += cell.DoorOffset;
                    }

                    // Skip file index 200 like Client project
                    if (cell.FrontIndex == 200) continue;

                    string frontLibraryPath = MapParser.GetLibraryPath(cell.FrontIndex, 2);
                    string libKey = frontLibraryPath.ToLower();

                    if (libraries.ContainsKey(libKey))
                    {
                        var library = libraries[libKey];
                        var image = library.GetImage(index);
                        if (image != null && image.Image != null)
                        {
                            // Calculate scaled position with offset
                            int drawX = (int)((x * CellWidth + image.X) * scaleX);
                            int drawY = (int)((y * CellHeight + image.Y) * scaleY);
                            int scaledWidth = (int)(image.Image.Width * scaleX);
                            int scaledHeight = (int)(image.Image.Height * scaleY);

                            // Apply size filtering like Client project
                            if ((image.Width != CellWidth || image.Height != CellHeight) &&
                                (image.Width != CellWidth * 2 || image.Height != CellHeight * 2))
                            {
                                // For non-standard sizes, adjust position like Client's DrawUp
                                drawY -= (int)(image.Height * scaleY);
                            }

                            graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);

                            if (verbose && (x * mapReader.Height + y) % 10000 == 0)
                                Console.WriteLine($"Drawn front tile at ({x}, {y})");
                        }
                    }
                }
            }
        }

        private static void RenderReducedMap(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            string outputPath, bool verbose)
        {
            try
            {
                Console.WriteLine("Creating reduced map image with Client logic...");

                // Create a smaller bitmap (1/4 size)
                int reducedWidth = mapReader.Width * CellWidth / 4;
                int reducedHeight = mapReader.Height * CellHeight / 4;

                using (var fullMapBitmap = new Bitmap(reducedWidth, reducedHeight))
                using (var graphics = Graphics.FromImage(fullMapBitmap))
                {
                    graphics.Clear(Color.Black);
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                    // Draw each cell in correct Z-order: Back -> Middle -> Front
                    DrawReducedBackLayer(mapReader, libraries, graphics, verbose);
                    DrawReducedMiddleLayer(mapReader, libraries, graphics, verbose);
                    DrawReducedFrontLayer(mapReader, libraries, graphics, verbose);

                    // Save full map image
                    string fullMapPath = Path.Combine(outputPath, "full_map_reduced_client.png");
                    fullMapBitmap.Save(fullMapPath, ImageFormat.Png);
                    Console.WriteLine($"Reduced full map image saved to: {fullMapPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating reduced map image: {ex.Message}");
            }
        }

        private static void DrawReducedBackLayer(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            Graphics graphics, bool verbose)
        {
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
                            int drawX = (x * CellWidth + image.X) / 4;
                            int drawY = (y * CellHeight + image.Y) / 4;
                            int scaledWidth = Math.Max(1, image.Image.Width / 4);
                            int scaledHeight = Math.Max(1, image.Image.Height / 4);

                            graphics.DrawImage(image.Image, drawX, drawY, scaledWidth, scaledHeight);
                        }
                    }
                }
            }
        }

        private static void DrawReducedMiddleLayer(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            Graphics graphics, bool verbose)
        {
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
                            int drawX = (x * CellWidth + image.X) / 4;
                            int drawY = (y * CellHeight + image.Y) / 4;
                            int scaledWidth = Math.Max(1, image.Image.Width / 4);
                            int scaledHeight = Math.Max(1, image.Image.Height / 4);

                            // Check if image has standard size
                            if ((image.Width == CellWidth && image.Height == CellHeight) ||
                                (image.Width == CellWidth * 2 && image.Height == CellHeight * 2))
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
        }

        private static void DrawReducedFrontLayer(MapReader mapReader, Dictionary<string, MLibrary> libraries,
            Graphics graphics, bool verbose)
        {
            for (int y = 0; y < mapReader.Height; y++)
            {
                for (int x = 0; x < mapReader.Width; x++)
                {
                    var cell = mapReader.MapCells[x, y];
                    int index = (cell.FrontImage & 0x7FFF) - 1;

                    if (index < 0 || cell.FrontIndex == -1) continue;

                    // Handle door index like Client project
                    if (cell.DoorIndex > 0)
                    {
                        // Use door offset to adjust index
                        index += cell.DoorOffset;
                    }

                    // Skip file index 200 like Client project
                    if (cell.FrontIndex == 200) continue;

                    string frontLibraryPath = MapParser.GetLibraryPath(cell.FrontIndex, 2);
                    string libKey = frontLibraryPath.ToLower();

                    if (libraries.ContainsKey(libKey))
                    {
                        var library = libraries[libKey];
                        var image = library.GetImage(index);
                        if (image != null && image.Image != null)
                        {
                            // Calculate position with offset and scale
                            int drawX = (x * CellWidth + image.X) / 4;
                            int drawY = (y * CellHeight + image.Y) / 4;
                            int scaledWidth = Math.Max(1, image.Image.Width / 4);
                            int scaledHeight = Math.Max(1, image.Image.Height / 4);

                            // Check if image has standard size
                            if ((image.Width == CellWidth && image.Height == CellHeight) ||
                                (image.Width == CellWidth * 2 && image.Height == CellHeight * 2))
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
        }
    }
}