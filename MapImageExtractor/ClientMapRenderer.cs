using System.Drawing;
using System.Drawing.Imaging;

namespace MapImageExtractor
{
    public class ClientMapRenderer
    {
        private const int CellWidth = 48;
        private const int CellHeight = 32;

        public static void RenderFullMap(MapReader mapReader, string outputDirectory, bool verbose = false)
        {
            try
            {
                Console.WriteLine($"Rendering full map with Client logic: {mapReader.Width}x{mapReader.Height}");

                // Calculate map dimensions
                int mapWidth = mapReader.Width * CellWidth;
                int mapHeight = mapReader.Height * CellHeight;
                try
                {
                    // Create bitmap with scaled dimensions
                    using (var fullMapBitmap = new Bitmap(mapWidth, mapHeight))
                    using (var graphics = Graphics.FromImage(fullMapBitmap))
                    {
                        graphics.Clear(Color.Black);
                        while (!Libraries.Loaded)
                        {
                            Console.WriteLine("Waiting for libraries to load...");
                            Thread.Sleep(100);
                        }
                        // Draw each cell in correct Z-order: Back -> Middle -> Front
                        // DrawBackLayer(mapReader, graphics, verbose);
                        // DrawMiddleLayer(mapReader, graphics, verbose);
                        DrawFrontLayer(mapReader, graphics, verbose);

                        // Save full map image
                        string fullMapPath = Path.Combine(outputDirectory, mapReader.GetFileName() + "_full_map.png");
                        fullMapBitmap.Save(fullMapPath, ImageFormat.Png);
                        Console.WriteLine($"Full map image saved to: {fullMapPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating bitmap: {ex.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering full map: {ex.Message}");
                if (verbose)
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void DrawBackLayer(MapReader mapReader, Graphics graphics, bool verbose)
        {
            Console.WriteLine("Drawing back layer...");
            for (int y = 0; y < mapReader.Height; y++)
            {
                for (int x = 0; x < mapReader.Width; x++)
                {
                    var cell = mapReader.MapCells[x, y];
                    if ((cell.BackImage == 0) || (cell.BackIndex == -1)) continue;

                    int index = (cell.BackImage & 0x1FFFFFFF) - 1;
                    MLibrary mLibrary = Libraries.MapLibs[cell.BackIndex];
                    MImage image = mLibrary.GetImage(index);
                    if (image != null) {
                        // Calculate scaled position with offset
                        int drawX = ((x * CellWidth + image.X));
                        int drawY = ((y * CellHeight + image.Y));
                        graphics.DrawImage(image.Image, drawX, drawY);
                        Console.WriteLine($"Drawn back tile at ({x}, {y})");
                    }
                }
            }
        }

        private static void DrawMiddleLayer(MapReader mapReader, Graphics graphics, bool verbose)
        {
            Console.WriteLine("Drawing middle layer...");
            
            for (int y = 0; y < mapReader.Height; y++)
            {
                for (int x = 0; x < mapReader.Width; x++)
                {
                    var cell = mapReader.MapCells[x, y];
                    int index = cell.MiddleImage - 1;
                    if ((index < 0) || (cell.MiddleIndex == -1)) continue;
                    if (cell.MiddleIndex >= 0) //M2P '> 199' changed to '>= 0' to include mir2 libraries. Fixes middle layer tile strips draw. Also changed in 'Draw mir3 middle layer' bellow.
                    {
                        //mir3 mid layer is same level as front layer not real middle + it cant draw index -1 so 2 birds in one stone :p
                        Size s = Libraries.MapLibs[cell.MiddleIndex].GetSize(index);

                        if ((s.Width != CellWidth || s.Height != CellHeight) &&
                            ((s.Width != CellWidth * 2) || (s.Height != CellHeight * 2))) continue;
                    }
                    // Libraries.MapLibs[cell.MiddleIndex].Draw(index, drawX, drawY);
                    MLibrary mLibrary = Libraries.MapLibs[cell.MiddleIndex];
                    MImage image = mLibrary.GetImage(index);
                    if (image != null) {
                        // Calculate scaled position with offset
                        int drawX = ((x * CellWidth + image.X));
                        int drawY = ((y * CellHeight + image.Y));
                        graphics.DrawImage(image.Image, drawX, drawY);
                        Console.WriteLine($"Drawn middle tile at ({x}, {y})");
                    }
                }
            }
        }

        private static void DrawFrontLayer(MapReader mapReader, Graphics graphics, bool verbose)
        {
            Console.WriteLine("Drawing front layer...");

            for (int y = 0; y < mapReader.Height; y++)
            {
                for (int x = 0; x < mapReader.Width; x++)
                {
                    var cell = mapReader.MapCells[x, y];
                    int index = (cell.FrontImage & 0x7FFF) - 1;
                    if (index == -1) continue;
                    int fileIndex = cell.FrontIndex;
                    if (fileIndex == -1) continue;
                    Size s = Libraries.MapLibs[fileIndex].GetSize(index);
                    if (fileIndex == 200) continue; //fixes random bad spots on old school 4.map
                    if (cell.DoorIndex > 0)
                    {
                        // Door DoorInfo = GetDoor(cell.DoorIndex);
                        // if (DoorInfo == null)
                        // {
                        //     DoorInfo = new Door() { index = cell.DoorIndex, DoorState = 0, ImageIndex = 0, LastTick = CMain.Time };
                        //     Doors.Add(DoorInfo);
                        // }
                        // else
                        // {
                        //     if (DoorInfo.DoorState != 0)
                        //     {
                        //         index += (DoorInfo.ImageIndex + 1) * cell.DoorOffset;//'bad' code if you want to use animation but it's gonna depend on the animation > has to be custom designed for the animtion
                        //     }
                        // }
                    }
                    
                    if (index < 0 || ((s.Width != CellWidth || s.Height != CellHeight) && ((s.Width != CellWidth * 2) || (s.Height != CellHeight * 2)))) continue;
                    MLibrary mLibrary = Libraries.MapLibs[fileIndex];
                    MImage image = mLibrary.GetImage(index);
                    if (image != null) {
                        // Calculate scaled position with offset
                        int drawX = ((x * CellWidth + image.X));
                        int drawY = ((y * CellHeight + image.Y));
                        graphics.DrawImage(image.Image, drawX, drawY);
                        Console.WriteLine($"Drawn front tile at ({x}, {y}), index: {index}, size: {image.Width}, size: {image.Height}");
                    }
                }
            }
        }
    }
}