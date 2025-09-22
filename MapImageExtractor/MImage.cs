using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;

namespace MapImageExtractor
{
    public sealed class MImage
    {
        public short Width, Height, X, Y, ShadowX, ShadowY;
        public byte Shadow;
        public int Length;

        public bool TextureValid;
        public Bitmap Image;

        //layer 2:
        public short MaskWidth, MaskHeight, MaskX, MaskY;
        public int MaskLength;

        public Bitmap MaskImage;
        public Boolean HasMask;

        public MImage(BinaryReader reader)
        {
            //read layer 1
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            ShadowX = reader.ReadInt16();
            ShadowY = reader.ReadInt16();
            Shadow = reader.ReadByte();
            Length = reader.ReadInt32();

            //check if there's a second layer and read it
            HasMask = ((Shadow >> 7) == 1) ? true : false;
            if (HasMask)
            {
                reader.ReadBytes(Length);
                MaskWidth = reader.ReadInt16();
                MaskHeight = reader.ReadInt16();
                MaskX = reader.ReadInt16();
                MaskY = reader.ReadInt16();
                MaskLength = reader.ReadInt32();
            }
        }

        public void CreateTexture(BinaryReader reader)
        {
            if (Width <= 0 || Height <= 0)
                return;

            // Read and decompress image data
            byte[] compressedData = reader.ReadBytes(Length);
            byte[] imageData = DecompressImage(compressedData);

            // Create bitmap from ARGB data
            Image = CreateBitmapFromArgbData(imageData, Width, Height);

            if (HasMask)
            {
                // Skip mask header and read mask data
                reader.ReadBytes(12);
                byte[] compressedMaskData = reader.ReadBytes(Length);
                byte[] maskData = DecompressImage(compressedMaskData);
                MaskImage = CreateBitmapFromArgbData(maskData, Width, Height);
            }

            TextureValid = true;
        }

        private Bitmap CreateBitmapFromArgbData(byte[] argbData, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(argbData, 0, bitmapData.Scan0, argbData.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public bool SaveAsPng(string filePath)
        {
            if (!TextureValid || Image == null)
                return false;

            try
            {
                Image.Save(filePath, ImageFormat.Png);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SaveMaskAsPng(string filePath)
        {
            if (!TextureValid || !HasMask || MaskImage == null)
                return false;

            try
            {
                MaskImage.Save(filePath, ImageFormat.Png);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] DecompressImage(byte[] image)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(image), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public void Dispose()
        {
            Image?.Dispose();
            MaskImage?.Dispose();
            Image = null;
            MaskImage = null;
        }
    }
}