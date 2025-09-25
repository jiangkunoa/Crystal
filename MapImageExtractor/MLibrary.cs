using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Numerics;
using System.Text.RegularExpressions;
using Client.MirObjects;

namespace MapImageExtractor
{
    public static class Libraries
    {
        public static bool Loaded;
        public static int Count, Progress;

        public const string DataPath = @".\Data\",
                            MapPath = @".\Map\",
                            SoundPath = @".\Sound\",
                            ExtraDataPath = @".\Data\Extra\",
                            ShadersPath = @".\Data\Shaders\",
                            MonsterPath = @".\Data\Monster\",
                            GatePath = @".\Data\Gate\",
                            FlagPath = @".\Data\Flag\",
                            SiegePath = @".\Data\Siege\",
                            NPCPath = @".\Data\NPC\",
                            CArmourPath = @".\Data\CArmour\",
                            CWeaponPath = @".\Data\CWeapon\",
							CWeaponEffectPath = @".\Data\CWeaponEffect\",
							CHairPath = @".\Data\CHair\",
                            AArmourPath = @".\Data\AArmour\",
                            AWeaponPath = @".\Data\AWeapon\",
                            AHairPath = @".\Data\AHair\",
                            ARArmourPath = @".\Data\ARArmour\",
                            ARWeaponPath = @".\Data\ARWeapon\",
                            ARHairPath = @".\Data\ARHair\",
                            CHumEffectPath = @".\Data\CHumEffect\",
                            AHumEffectPath = @".\Data\AHumEffect\",
                            ARHumEffectPath = @".\Data\ARHumEffect\",
                            MountPath = @".\Data\Mount\",
                            FishingPath = @".\Data\Fishing\",
                            PetsPath = @".\Data\Pet\",
                            TransformPath = @".\Data\Transform\",
                            TransformMountsPath = @".\Data\TransformRide2\",
                            TransformEffectPath = @".\Data\TransformEffect\",
                            TransformWeaponEffectPath = @".\Data\TransformWeaponEffect\",
                            MouseCursorPath = @".\Data\Cursors\",
                            ResourcePath = @".\DirectX\",
                            UserDataPath = @".\Data\UserData\";
        
        public static readonly MLibrary
            ChrSel = new MLibrary(DataPath + "ChrSel"),
            Prguse = new MLibrary(DataPath + "Prguse"),
            Prguse2 = new MLibrary(DataPath + "Prguse2"),
            Prguse3 = new MLibrary(DataPath + "Prguse3"),
            BuffIcon = new MLibrary(DataPath + "BuffIcon"),
            Help = new MLibrary(DataPath + "Help"),
            MiniMap = new MLibrary(DataPath + "MMap"),
            MapLinkIcon = new MLibrary(DataPath + "MapLinkIcon"),
            Title = new MLibrary(DataPath + "Title"),
            MagIcon = new MLibrary(DataPath + "MagIcon"),
            MagIcon2 = new MLibrary(DataPath + "MagIcon2"),
            Magic = new MLibrary(DataPath + "Magic"),
            Magic2 = new MLibrary(DataPath + "Magic2"),
            Magic3 = new MLibrary(DataPath + "Magic3"),
            Effect = new MLibrary(DataPath + "Effect"),
            MagicC = new MLibrary(DataPath + "MagicC"),
            GuildSkill = new MLibrary(DataPath + "GuildSkill"),
            Weather = new MLibrary(DataPath + "Weather");

        public static readonly MLibrary
            Background = new MLibrary(DataPath + "Background");


        public static readonly MLibrary
            Dragon = new MLibrary(DataPath + "Dragon");

        //Map
        public static readonly MLibrary[] MapLibs = new MLibrary[400];

        //Items
        public static readonly MLibrary
            Items = new MLibrary(DataPath + "Items"),
            StateItems = new MLibrary(DataPath + "StateItem"),
            FloorItems = new MLibrary(DataPath + "DNItems");

        //Deco
        public static readonly MLibrary
            Deco = new MLibrary(DataPath + "Deco");

        public static MLibrary[] CArmours,
                                          CWeapons,
										  CWeaponEffect,
										  CHair,
                                          CHumEffect,
                                          AArmours,
                                          AWeaponsL,
                                          AWeaponsR,
                                          AHair,
                                          AHumEffect,
                                          ARArmours,
                                          ARWeapons,
                                          ARWeaponsS,
                                          ARHair,
                                          ARHumEffect,
                                          Monsters,
                                          Gates,
                                          Flags,
                                          Siege,
                                          Mounts,
                                          NPCs,
                                          Fishing,
                                          Pets,
                                          Transform,
                                          TransformMounts,
                                          TransformEffect,
                                          TransformWeaponEffect;

        static Libraries()
        {
            //Wiz/War/Tao
            InitLibrary(ref CArmours, CArmourPath, "00");
            InitLibrary(ref CHair, CHairPath, "00");
            InitLibrary(ref CWeapons, CWeaponPath, "00");
            InitLibrary(ref CWeaponEffect, CWeaponEffectPath, "00");
            InitLibrary(ref CHumEffect, CHumEffectPath, "00");

            //Assassin
            InitLibrary(ref AArmours, AArmourPath, "00");
            InitLibrary(ref AHair, AHairPath, "00");
            InitLibrary(ref AWeaponsL, AWeaponPath, "00", " L");
            InitLibrary(ref AWeaponsR, AWeaponPath, "00", " R");
            InitLibrary(ref AHumEffect, AHumEffectPath, "00");

            //Archer
            InitLibrary(ref ARArmours, ARArmourPath, "00");
            InitLibrary(ref ARHair, ARHairPath, "00");
            InitLibrary(ref ARWeapons, ARWeaponPath, "00");
            InitLibrary(ref ARWeaponsS, ARWeaponPath, "00", " S");
            InitLibrary(ref ARHumEffect, ARHumEffectPath, "00");

            //Other
            InitLibrary(ref Monsters, MonsterPath, "000");
            InitLibrary(ref Gates, GatePath, "00");
            InitLibrary(ref Flags, FlagPath, "00");
            InitLibrary(ref Siege, SiegePath, "00");
            InitLibrary(ref NPCs, NPCPath, "00");
            InitLibrary(ref Mounts, MountPath, "00");
            InitLibrary(ref Fishing, FishingPath, "00");
            InitLibrary(ref Pets, PetsPath, "00");
            InitLibrary(ref Transform, TransformPath, "00");
            InitLibrary(ref TransformMounts, TransformMountsPath, "00");
            InitLibrary(ref TransformEffect, TransformEffectPath, "00");
            InitLibrary(ref TransformWeaponEffect, TransformWeaponEffectPath, "00");

            #region Maplibs
            //wemade mir2 (allowed from 0-99)
            MapLibs[0] = new MLibrary(DataPath + "Map\\WemadeMir2\\Tiles");
            MapLibs[1] = new MLibrary(DataPath + "Map\\WemadeMir2\\Smtiles");
            MapLibs[2] = new MLibrary(DataPath + "Map\\WemadeMir2\\Objects");
            for (int i = 2; i < 28; i++)
            {
                MapLibs[i + 1] = new MLibrary(DataPath + "Map\\WemadeMir2\\Objects" + i.ToString());
            }
            MapLibs[90] = new MLibrary(DataPath + "Map\\WemadeMir2\\Objects_32bit");

            //shanda mir2 (allowed from 100-199)
            MapLibs[100] = new MLibrary(DataPath + "Map\\ShandaMir2\\Tiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[100 + i] = new MLibrary(DataPath + "Map\\ShandaMir2\\Tiles" + (i + 1));
            }
            MapLibs[110] = new MLibrary(DataPath + "Map\\ShandaMir2\\SmTiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[110 + i] = new MLibrary(DataPath + "Map\\ShandaMir2\\SmTiles" + (i + 1));
            }
            MapLibs[120] = new MLibrary(DataPath + "Map\\ShandaMir2\\Objects");
            for (int i = 1; i < 31; i++)
            {
                MapLibs[120 + i] = new MLibrary(DataPath + "Map\\ShandaMir2\\Objects" + (i + 1));
            }
            MapLibs[190] = new MLibrary(DataPath + "Map\\ShandaMir2\\AniTiles1");
            //wemade mir3 (allowed from 200-299)
            string[] Mapstate = { "", "wood\\", "sand\\", "snow\\", "forest\\"};
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[200 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tilesc");
                MapLibs[201 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles30c");
                MapLibs[202 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles5c");
                MapLibs[203 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Smtilesc");
                MapLibs[204 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Housesc");
                MapLibs[205 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Cliffsc");
                MapLibs[206 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Dungeonsc");
                MapLibs[207 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Innersc");
                MapLibs[208 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Furnituresc");
                MapLibs[209 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Wallsc");
                MapLibs[210 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "smObjectsc");
                MapLibs[211 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Animationsc");
                MapLibs[212 +(i*15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object1c");
                MapLibs[213 + (i * 15)] = new MLibrary(DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object2c");
            }
            Mapstate = new string[] { "", "wood", "sand", "snow", "forest"};
            //shanda mir3 (allowed from 300-399)
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[300 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Tilesc" + Mapstate[i]);
                MapLibs[301 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Tiles30c" + Mapstate[i]);
                MapLibs[302 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Tiles5c" + Mapstate[i]);
                MapLibs[303 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Smtilesc" + Mapstate[i]);
                MapLibs[304 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Housesc" + Mapstate[i]);
                MapLibs[305 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Cliffsc" + Mapstate[i]);
                MapLibs[306 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Dungeonsc" + Mapstate[i]);
                MapLibs[307 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Innersc" + Mapstate[i]);
                MapLibs[308 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Furnituresc" + Mapstate[i]);
                MapLibs[309 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Wallsc" + Mapstate[i]);
                MapLibs[310 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "smObjectsc" + Mapstate[i]);
                MapLibs[311 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Animationsc" + Mapstate[i]);
                MapLibs[312 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Object1c" + Mapstate[i]);
                MapLibs[313 + (i * 15)] = new MLibrary(DataPath + "Map\\ShandaMir3\\" + "Object2c" + Mapstate[i]);
            }
            #endregion

            LoadLibraries();

            Thread thread = new Thread(LoadGameLibraries) { IsBackground = true };
            thread.Start();
        }

        static void InitLibrary(ref MLibrary[] library, string path, string toStringValue, string suffix = "")
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var allFiles = Directory.GetFiles(path, "*" + suffix + MLibrary.Extention, SearchOption.TopDirectoryOnly).OrderBy(x => int.Parse(Regex.Match(x, @"\d+").Value));

            var lastFile = allFiles.Count() > 0 ? Path.GetFileName(allFiles.Last()) : "0";

            var count = int.Parse(Regex.Match(lastFile, @"\d+").Value) + 1;

            library = new MLibrary[count];

            for (int i = 0; i < count; i++)
            {
                library[i] = new MLibrary(path + i.ToString(toStringValue) + suffix);
            }
        }

        static void LoadLibraries()
        {
            ChrSel.Initialize();
            Progress++;

            Prguse.Initialize();
            Progress++;

            Prguse2.Initialize();
            Progress++;

            Prguse3.Initialize();
            Progress++;

            Title.Initialize();
            Progress++;
        }

        private static void LoadGameLibraries()
        {
            Count = MapLibs.Length + Monsters.Length + Gates.Length + Flags.Length + Siege.Length + NPCs.Length + CArmours.Length +
                CHair.Length + CWeapons.Length + CWeaponEffect.Length + AArmours.Length + AHair.Length + AWeaponsL.Length + AWeaponsR.Length +
                ARArmours.Length + ARHair.Length + ARWeapons.Length + ARWeaponsS.Length +
                CHumEffect.Length + AHumEffect.Length + ARHumEffect.Length + Mounts.Length + Fishing.Length + Pets.Length +
                Transform.Length + TransformMounts.Length + TransformEffect.Length + TransformWeaponEffect.Length + 18;

            Dragon.Initialize();
            Progress++;

            BuffIcon.Initialize();
            Progress++;

            Help.Initialize();
            Progress++;

            MiniMap.Initialize();
            Progress++;
            MapLinkIcon.Initialize();
            Progress++;

            MagIcon.Initialize();
            Progress++;
            MagIcon2.Initialize();
            Progress++;

            Magic.Initialize();
            Progress++;
            Magic2.Initialize();
            Progress++;
            Magic3.Initialize();
            Progress++;
            MagicC.Initialize();
            Progress++;

            Effect.Initialize();
            Progress++;

            Weather.Initialize();
            Progress++;

            GuildSkill.Initialize();
            Progress++;

            Background.Initialize();
            Progress++;

            Deco.Initialize();
            Progress++;

            Items.Initialize();
            Progress++;
            StateItems.Initialize();
            Progress++;
            FloorItems.Initialize();
            Progress++;

            for (int i = 0; i < MapLibs.Length; i++)
            {
                if (MapLibs[i] == null)
                    MapLibs[i] = new MLibrary("");
                else
                    MapLibs[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Monsters.Length; i++)
            {
                Monsters[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Gates.Length; i++)
            {
                Gates[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Flags.Length; i++)
            {
                Flags[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Siege.Length; i++)
            {
                Siege[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < NPCs.Length; i++)
            {
                NPCs[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < CArmours.Length; i++)
            {
                CArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHair.Length; i++)
            {
                CHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CWeapons.Length; i++)
            {
                CWeapons[i].Initialize();
                Progress++;
            }

			for (int i = 0; i < CWeaponEffect.Length; i++)
			{
				CWeaponEffect[i].Initialize();
				Progress++;
			}

			for (int i = 0; i < AArmours.Length; i++)
            {
                AArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHair.Length; i++)
            {
                AHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsL.Length; i++)
            {
                AWeaponsL[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsR.Length; i++)
            {
                AWeaponsR[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARArmours.Length; i++)
            {
                ARArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHair.Length; i++)
            {
                ARHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeapons.Length; i++)
            {
                ARWeapons[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeaponsS.Length; i++)
            {
                ARWeaponsS[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHumEffect.Length; i++)
            {
                CHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHumEffect.Length; i++)
            {
                AHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHumEffect.Length; i++)
            {
                ARHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Mounts.Length; i++)
            {
                Mounts[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < Fishing.Length; i++)
            {
                Fishing[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Pets.Length; i++)
            {
                Pets[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Transform.Length; i++)
            {
                Transform[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformEffect.Length; i++)
            {
                TransformEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformWeaponEffect.Length; i++)
            {
                TransformWeaponEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformMounts.Length; i++)
            {
                TransformMounts[i].Initialize();
                Progress++;
            }
            
            Loaded = true;
        }

    }

    public sealed class MLibrary
    {
        public const string Extention = ".Lib";
        public const int LibVersion = 3;

        private readonly string _fileName;

        private MImage[] _images;
        private FrameSet _frames;
        private int[] _indexList;
        private int _count;
        private bool _initialized;

        private BinaryReader _reader;
        private FileStream _fStream;

        public FrameSet Frames
        {
            get { return _frames; }
        }

        public MLibrary(string filename)
        {
            _fileName = Path.ChangeExtension(filename, Extention);
        }

        public String GetFileName()
        {
            return _fileName;
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
                    Console.WriteLine("ERROR: Wrong version, expecting lib version: " + LibVersion.ToString() + " found version: " + currentVersion.ToString() + ".", _fileName);
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

                if (currentVersion >= 3)
                {
                    _fStream.Seek(frameSeek, SeekOrigin.Begin);

                    var frameCount = _reader.ReadInt32();

                    if (frameCount > 0)
                    {
                        _frames = new FrameSet();
                        for (int i = 0; i < frameCount; i++)
                        {
                            _frames.Add((MirAction)_reader.ReadByte(), new Frame(_reader));
                        }
                    }
                }
            }
            catch (Exception)
            {
                _initialized = false;
                throw;
            }
        }

        public MImage GetImage(int index)
        {
            if (!CheckImage(index))
                return null;

            return _images[index];
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
        public Size GetTrueSize(int index)
        {
            if (!_initialized)
                Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return Size.Empty;

            if (_images[index] == null)
            {
                _fStream.Position = _indexList[index];
                _images[index] = new MImage(_reader);
            }
            MImage mi = _images[index];
            if (mi.TrueSize.IsEmpty)
            {
                if (!mi.TextureValid)
                {
                    if ((mi.Width == 0) || (mi.Height == 0))
                        return Size.Empty;

                    _fStream.Seek(_indexList[index] + 17, SeekOrigin.Begin);
                    mi.CreateTexture(_reader);
                }
                return mi.GetTrueSize();
            }
            return mi.TrueSize;
        }

        public void Draw(int index, int x, int y)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)x, (float)y, 0.0F), Color.White);
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Point point, Color colour, bool offSet = false)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void Draw(int index, Point point, Color colour, bool offSet, float opacity)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // DXManager.DrawOpaque(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour, opacity); 
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawBlend(int index, Point point, Color colour, bool offSet = false, float rate = 1)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // bool oldBlend = DXManager.Blending;
            // DXManager.SetBlend(true, rate);
            //
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);
            //
            // DXManager.SetBlend(oldBlend);
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Rectangle section, Point point, Color colour, bool offSet)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);


            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // if (section.Right > mi.Width)
            //     section.Width -= section.Right - mi.Width;
            //
            // if (section.Bottom > mi.Height)
            //     section.Height -= section.Bottom - mi.Height;
            //
            // DXManager.Draw(mi.Image, section, new Vector3((float)point.X, (float)point.Y, 0.0F), colour);
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Rectangle section, Point point, Color colour, float opacity)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];


            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // if (section.Right > mi.Width)
            //     section.Width -= section.Right - mi.Width;
            //
            // if (section.Bottom > mi.Height)
            //     section.Height -= section.Bottom - mi.Height;
            //
            // DXManager.DrawOpaque(mi.Image, section, new Vector3((float)point.X, (float)point.Y, 0.0F), colour, opacity); 
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Point point, Size size, Color colour)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + size.Width < 0 || point.Y + size.Height < 0)
            //     return;
            //
            // float scaleX = (float)size.Width / mi.Width;
            // float scaleY = (float)size.Height / mi.Height;
            //
            // Matrix matrix = Matrix.Scaling(scaleX, scaleY, 0);
            // DXManager.Sprite.Transform = matrix;
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X / scaleX, (float)point.Y / scaleY, 0.0F), Color.White); 
            //
            // DXManager.Sprite.Transform = Matrix.Identity;
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawTinted(int index, Point point, Color colour, Color Tint, bool offSet = false)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);
            //
            // if (mi.HasMask)
            // {
            //     DXManager.Draw(mi.MaskImage, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), Tint);
            // }
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawUp(int index, int x, int y)
        {
            // if (x >= Settings.ScreenWidth)
            //     return;
            //
            // if (!CheckImage(index))
            //     return;
            //
            // MImage mi = _images[index];
            // y -= mi.Height;
            // if (y >= Settings.ScreenHeight)
            //     return;
            // if (x + mi.Width < 0 || y + mi.Height < 0)
            //     return;
            //
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3(x, y, 0.0F), Color.White);
            //
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void DrawUpBlend(int index, Point point)
        {
            // if (!CheckImage(index))
            //     return;
            //
            // MImage mi = _images[index];
            //
            // point.Y -= mi.Height;
            //
            //
            // if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
            //     return;
            //
            // bool oldBlend = DXManager.Blending;
            // DXManager.SetBlend(true, 1);
            //
            // DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), Color.White);
            //
            // DXManager.SetBlend(oldBlend);
            // mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public bool VisiblePixel(int index, Point point, bool accuate)
        {
            if (!CheckImage(index))
                return false;

            if (accuate)
                return _images[index].VisiblePixel(point);

            int accuracy = 2;

            for (int x = -accuracy; x <= accuracy; x++)
                for (int y = -accuracy; y <= accuracy; y++)
                    if (_images[index].VisiblePixel(new Point(point.X + x, point.Y + y)))
                        return true;

            return false;
        }
    }

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

        public long CleanTime;
        public Size TrueSize;

        public unsafe byte* Data;

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

        public unsafe void CreateTexture(BinaryReader reader)
        {
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

        public unsafe bool VisiblePixel(Point p)
        {
            if (p.X < 0 || p.Y < 0 || p.X >= Width || p.Y >= Height)
                return false;

            int w = Width;

            bool result = false;
            if (Data != null)
            {
                int x = p.X;
                int y = p.Y;
                
                int index = (y * (w << 2)) + (x << 2) + 3;
                
                byte col = Data[index];

                if (col == 0) return false;
                else return true;
            }
            return result;
        }

        public Size GetTrueSize()
        {
            if (TrueSize != Size.Empty) return TrueSize;

            int l = 0, t = 0, r = Width, b = Height;

            bool visible = false;
            for (int x = 0; x < r; x++)
            {
                for (int y = 0; y < b; y++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;
                }

                if (!visible) continue;

                l = x;
                break;
            }

            visible = false;
            for (int y = 0; y < b; y++)
            {
                for (int x = l; x < r; x++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;

                }
                if (!visible) continue;

                t = y;
                break;
            }

            visible = false;
            for (int x = r - 1; x >= l; x--)
            {
                for (int y = 0; y < b; y++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;
                }

                if (!visible) continue;

                r = x + 1;
                break;
            }

            visible = false;
            for (int y = b - 1; y >= t; y--)
            {
                for (int x = l; x < r; x++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;

                }
                if (!visible) continue;

                b = y + 1;
                break;
            }

            TrueSize = Rectangle.FromLTRB(l, t, r, b).Size;

            return TrueSize;
        }

        

        private static void DecompressImage(byte[] data, Stream destination)
        {
            using (var stream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                stream.CopyTo(destination);
            }
        }
    }
}
