using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;

namespace MapImageExtractor
{
    public sealed class MLibrary
    {
        public const string Extention = ".Lib";
        public const int LibVersion = 3;

        private readonly string _fileName;

        private MImage[] _images;
        private int[] _indexList;
        private int _count;
        private bool _initialized;

        private BinaryReader _reader;
        private FileStream _fStream;

        public MLibrary(string filename)
        {
            _fileName = Path.ChangeExtension(filename, Extention);
        }

        public void Initialize()
        {
            _initialized = true;

            if (!File.Exists(_fileName))
                return;

            try
            {
                _fStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
                _reader = new BinaryReader(_fStream);
                int currentVersion = _reader.ReadInt32();
                if (currentVersion < 2)
                {
                    Console.WriteLine($"Wrong version, expecting lib version: {LibVersion} found version: {currentVersion}.");
                    return;
                }
                _count = _reader.ReadInt32();

                int frameSeek = 0;
                if (currentVersion >= 3)
                {
                    frameSeek = _reader.ReadInt32();
                }

                _images = new MImage[_count];
                _indexList = new int[_count];

                for (int i = 0; i < _count; i++)
                    _indexList[i] = _reader.ReadInt32();

                // Skip frame data for now
                if (currentVersion >= 3 && frameSeek > 0)
                {
                    _fStream.Seek(frameSeek, SeekOrigin.Begin);
                    var frameCount = _reader.ReadInt32();
                    if (frameCount > 0)
                    {
                        _fStream.Seek(frameCount * 5, SeekOrigin.Current); // Skip frame data
                    }
                }
            }
            catch (Exception ex)
            {
                _initialized = false;
                Console.WriteLine($"Failed to initialize library {_fileName}: {ex.Message}");
                throw;
            }
        }

        private bool CheckImage(int index)
        {
            if (!_initialized)
                Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return false;

            if (_images[index] == null)
            {
                _fStream.Position = _indexList[index];
                _images[index] = new MImage(_reader);
            }

            MImage mi = _images[index];
            if (!mi.TextureValid)
            {
                if ((mi.Width == 0) || (mi.Height == 0))
                    return false;
                _fStream.Seek(_indexList[index] + 17, SeekOrigin.Begin);
                mi.CreateTexture(_reader);
            }

            return true;
        }

        public bool ExtractImage(int index, string outputPath)
        {
            if (!CheckImage(index))
                return false;

            MImage mi = _images[index];
            return mi.SaveAsPng(outputPath);
        }

        public MImage GetImage(int index)
        {
            if (!CheckImage(index))
                return null;

            return _images[index];
        }

        public int GetImageCount()
        {
            if (!_initialized)
                Initialize();

            return _count;
        }

        public Point GetOffSet(int index)
        {
            if (!_initialized) Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return Point.Empty;

            if (_images[index] == null)
            {
                _fStream.Seek(_indexList[index], SeekOrigin.Begin);
                _images[index] = new MImage(_reader);
            }

            return new Point(_images[index].X, _images[index].Y);
        }

        public Size GetSize(int index)
        {
            if (!_initialized) Initialize();
            if (_images == null || index < 0 || index >= _images.Length)
                return Size.Empty;

            if (_images[index] == null)
            {
                _fStream.Seek(_indexList[index], SeekOrigin.Begin);
                _images[index] = new MImage(_reader);
            }

            return new Size(_images[index].Width, _images[index].Height);
        }

        public void Dispose()
        {
            _reader?.Close();
            _fStream?.Close();
            _reader?.Dispose();
            _fStream?.Dispose();
        }
    }
}