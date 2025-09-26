using System.Drawing;
using System.Drawing.Imaging;

namespace MapImageExtractor
{
    public class ClientMapRenderer
    {
        private const int CellWidth = 48;
        private const int CellHeight = 32;
        
        private static Dictionary<String, MImage> cachedImages = new Dictionary<String, MImage>();

        public static void RenderFullMap(MapReader mapReader, string outputDirectory, bool verbose = false)
        {
            try
            {
                Console.WriteLine($"Rendering full map with Client logic: {mapReader.Width}x{mapReader.Height}");

                // Calculate map dimensions
                int mapWidth = mapReader.Width * CellWidth;
                int mapHeight = mapReader.Height * CellHeight;
                Console.WriteLine($"Original dimensions: {mapWidth}x{mapHeight}");

                // Check if dimensions are too large and scale down if necessary
                int maxDimension = 20000; // Use a safer maximum dimension
                float scale = 1.0f;
                if (mapWidth > maxDimension || mapHeight > maxDimension)
                {
                    scale = Math.Min((float)maxDimension / mapWidth, (float)maxDimension / mapHeight);
                    mapWidth = (int)(mapWidth * scale);
                    mapHeight = (int)(mapHeight * scale);
                    Console.WriteLine($"Map dimensions too large, scaling down by factor of {scale:F2}");
                    Console.WriteLine($"New dimensions: {mapWidth}x{mapHeight}");
                }
                else
                {
                    Console.WriteLine($"Dimensions are within limits");
                }

                try
                {
                    // Create bitmap with scaled dimensions
                    Console.WriteLine($"Attempting to create bitmap with dimensions: {mapWidth}x{mapHeight}");
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
                        DrawBackLayer(mapReader, graphics);
                        DrawMiddleLayer(mapReader, graphics);
                        DrawFrontLayer(mapReader, graphics);

                        // Save full map image
                        string fullMapPath = Path.Combine(outputDirectory, mapReader.GetFileName() + "_full_map.png");
                        fullMapBitmap.Save(fullMapPath, ImageFormat.Png);
                        Console.WriteLine($"Full map image saved to: {fullMapPath}");
                    }
                    //图片保存
                    foreach (var item in cachedImages)
                    {
                        string[] strings = item.Key.Split("/");
                        for (var i = 0; i < strings.Length - 1; i++)
                        {
                            string dirName = strings[i];
                            Directory.CreateDirectory(Path.Combine(outputDirectory, dirName));
                        }
                        item.Value.Image.Save(Path.Combine(outputDirectory, item.Key + ".png"), ImageFormat.Png);
                        
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

        private static MImage GetSaveImage(int libIndex, int imgIndex)
        {
            MLibrary mLibrary = Libraries.MapLibs[libIndex];
            MImage image = mLibrary.GetImage(imgIndex);
            if (image != null)
            {
                string cacheKey = $"{libIndex}/{imgIndex}";
                if (!cachedImages.ContainsKey(cacheKey))
                {
                    cachedImages.Add(cacheKey, image);
                }
                // image.Image.Save(Path.Combine(outputDirectory, $"{libIndex}_{imgIndex}.png"), ImageFormat.Png);
            }
            return image;
        }
        
        private static void DrawBackLayer(MapReader mapReader, Graphics graphics)
        {
            Console.WriteLine("Drawing back layer...");
            for (int y = 0; y < mapReader.Height; y++)
            {
                if (y <= 0 || y % 2 == 1) continue;
                int drawY = ((y * CellHeight));
                for (int x = 0; x < mapReader.Width; x++)
                {
                    if (x <= 0 || x % 2 == 1) continue;
                    int drawX = ((x * CellWidth));
                    
                    var cell = mapReader.MapCells[x, y];
                    if ((cell.BackImage == 0) || (cell.BackIndex == -1)) continue;

                    int index = (cell.BackImage & 0x1FFFFFFF) - 1;
                    MImage image = GetSaveImage(cell.BackIndex, index);
                    if (image != null) {
                        // Calculate scaled position with offset
                        
                        graphics.DrawImage(image.Image, drawX, drawY);
                        // Console.WriteLine($"Drawn back tile at ({x}, {y})");
                    }
                }
            }
        }

        private static void DrawMiddleLayer(MapReader mapReader, Graphics graphics)
        {
            Console.WriteLine("Drawing middle layer...");
            int skippedCount = 0;
            int drawnCount = 0;

            for (int y = 0; y < mapReader.Height; y++)
            {
                if (y <= 0) continue;
                int drawY = y * CellHeight;
                for (int x = 0; x < mapReader.Width; x++)
                {
                    if (x < 0) continue;
                    int drawX = x * CellWidth;
                    var cell = mapReader.MapCells[x, y];
                    int index = cell.MiddleImage - 1;
                    if ((index < 0) || (cell.MiddleIndex == -1)) continue;
                    if (cell.MiddleIndex >= 0) //M2P '> 199' changed to '>= 0' to include mir2 libraries. Fixes middle layer tile strips draw. Also changed in 'Draw mir3 middle layer' bellow.
                    {
                        //mir3 mid layer is same level as front layer not real middle + it cant draw index -1 so 2 birds in one stone :p
                        Size s = Libraries.MapLibs[cell.MiddleIndex].GetSize(index);
                        if ((s.Width != CellWidth || s.Height != CellHeight) &&
                            ((s.Width != CellWidth * 2) || (s.Height != CellHeight * 2))) {
                            Console.WriteLine($"Skipped middle tile at ({x}, {y}), index: {index}, size: {s.Width}x{s.Height}");
                            skippedCount++;
                            continue;
                        }
                    }
                    // Libraries.MapLibs[cell.MiddleIndex].Draw(index, drawX, drawY);
                    MImage image = GetSaveImage(cell.MiddleIndex, index);
                    if (image != null) {
                        graphics.DrawImage(image.Image, drawX, drawY);
                        drawnCount++;
                    }
                }
            }
            Console.WriteLine($"Middle layer: {drawnCount} tiles drawn, {skippedCount} tiles skipped due to size");
        }

        private static void DrawFrontLayer(MapReader mapReader, Graphics graphics)
        {
            Console.WriteLine("Drawing front layer...");
            int skippedCount = 0;
            int drawnCount = 0;

            for (int y = 0; y < mapReader.Height; y++)
            {
                if (y <= 0) continue;
                int drawY = y * CellHeight;
                for (int x = 0; x < mapReader.Width; x++)
                {
                    if (x < 0) continue;
                    int drawX = x * CellWidth;
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

                    if (index < 0 || ((s.Width != CellWidth || s.Height != CellHeight) && ((s.Width != CellWidth * 2) || (s.Height != CellHeight * 2)))) {
                        // Console.WriteLine($"Skipped front tile at ({x}, {y}), index: {index}, size: {s.Width}x{s.Height}");
                        skippedCount++;
                        continue;
                    }
                    MImage image = GetSaveImage(fileIndex, index);
                    if (image != null) {
                        // Calculate scaled position with offset
                        graphics.DrawImage(image.Image, drawX, drawY);
                        // Console.WriteLine($"Drawn front tile at ({x}, {y}), index: {index}, size: {image.Width}x{image.Height}");
                        drawnCount++;
                    }
                }
            }
            Console.WriteLine($"Front layer: {drawnCount} tiles drawn, {skippedCount} tiles skipped due to size");
            int frontDrawn2 = 0;
            for (int y = 0; y < mapReader.Height; y++)
            {
                if (y <= 0) continue;
                int drawY = (y + 1) * CellHeight;
                for (int x = 0; x < mapReader.Width; x++)
                {
                    if (x < 0) continue;
                    int drawX = x * CellWidth;
                    var cell = mapReader.MapCells[x, y];
                    int index = (cell.FrontImage & 0x7FFF) - 1;
                    if (index < 0) continue;
                    int fileIndex = cell.FrontIndex;
                    if (fileIndex == -1) continue;
                    byte animation = cell.FrontAnimationFrame;
                    bool blend;
                    if ((animation & 0x80) > 0)
                    {
                        blend = true;
                        animation &= 0x7F;
                    }
                    else
                        blend = false;


                    if (animation > 0)
                    {
                        byte animationTick = cell.FrontAnimationTick;
                        // index += (AnimationCount % (animation + (animation * animationTick))) / (1 + animationTick);
                        index += (1 % (animation + (animation * animationTick))) / (1 + animationTick);
                    }


                    // if (cell.DoorIndex > 0)
                    // {
                    //     Door DoorInfo = GetDoor(cell.DoorIndex);
                    //     if (DoorInfo == null)
                    //     {
                    //         DoorInfo = new Door()
                    //         {
                    //             index = cell.DoorIndex, DoorState = 0, ImageIndex = 0, LastTick = CMain.Time
                    //         };
                    //         Doors.Add(DoorInfo);
                    //     }
                    //     else
                    //     {
                    //         if (DoorInfo.DoorState != 0)
                    //         {
                    //             index += (DoorInfo.ImageIndex + 1) *
                    //                      cell
                    //                          .DoorOffset; //'bad' code if you want to use animation but it's gonna depend on the animation > has to be custom designed for the animtion
                    //         }
                    //     }
                    // }

                    Size s = Libraries.MapLibs[fileIndex].GetSize(index);
                    if (s.Width == CellWidth && s.Height == CellHeight && animation == 0) continue;
                    if ((s.Width == CellWidth * 2) && (s.Height == CellHeight * 2) && (animation == 0)) continue;

                    MImage image = GetSaveImage(fileIndex, index);
                    if (blend)
                    {
                        if (fileIndex == 14 || fileIndex == 27 || (fileIndex > 99 & fileIndex < 199))
                            // 暂不支持的
                            // Libraries.MapLibs[fileIndex].DrawBlend(index, new Point(drawX, drawY - (3 * CellHeight)),
                            //     Color.White, true);
                            DrawBlend();
                        else
                            // 暂不支持的
                            // Libraries.MapLibs[fileIndex].DrawBlend(index, new Point(drawX, drawY - s.Height),
                            //     Color.White, (index >= 2723 && index <= 2732));
                            DrawBlend();
                    }
                    else
                    {
                        if (fileIndex == 28 && Libraries.MapLibs[fileIndex].GetOffSet(index) != Point.Empty)
                            Draw(graphics, image, drawX, drawY - CellHeight, Color.White, true);
                        else
                            Draw(graphics, image, drawX, drawY - s.Height, Color.White, false);
                    }

                    frontDrawn2 += 1;
                }
            }
            Console.WriteLine($"Drawing finished. frontDrawn2: {frontDrawn2}");
        }
        
        private static void DrawBlend()
        {
            Console.WriteLine("Drawing blend...暂不支持...............");
        }
        
        private static void Draw(Graphics graphics, MImage image, int x, int y, Color color, bool offSet)
        {

            if (offSet)
            {
                Console.WriteLine($"Drawing offset image at {x}, {y}");
                x += image.X;
                y += image.Y;
            }

            if (image == null || image.Image == null)
            {
                Console.WriteLine($"Image is null at {x}, {y}");
                return;
            }
            graphics.DrawImage(image.Image, x, y);
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;

            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        
    }
}