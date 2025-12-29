using Core.Globals;
using Core.Interfaces;
using CSScripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using static Core.Globals.Type;
using Core.Objects;

namespace Server
{
    public class Map : MapBase, IAsyncData
    {
        private static Core.Globals.Type.Tile[,] CreateTile(byte maxX, byte maxY)
        {
            var tile = new Core.Globals.Type.Tile[maxX, maxY];
            EnsureTileLayers(tile);
            return tile;
        }

        private static void EnsureTileLayers(Core.Globals.Type.Tile[,] tile)
        {
            var layerCount = Enum.GetValues(typeof(MapLayer)).Length;
            for (var x = 0; x < tile.GetLength(0); x++)
            {
                for (var y = 0; y < tile.GetLength(1); y++)
                {
                    tile[x, y].Layer ??= new Core.Globals.Type.Layer[layerCount];
                }
            }
        }

        public static void OnSave(int index)
        {
            string json = JsonConvert.SerializeObject(Server.Map.Instance[index]).ToString();

            if (Database.RowExists(index, "map"))
            {
                Database.UpdateRow(index, json, "map", "data");
            }
            else
            {
                Database.InsertRow(index, json, "map");
            }
        }

        public static System.Threading.Tasks.Task OnLoadAllAsync()
        {
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxMaps), OnLoadAsync);
        }

        public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
        {
            string baseDir;

            baseDir = AppDomain.CurrentDomain.BaseDirectory;   

            // Construct the path to the "maps" directory
            string mapsDir = Path.Combine(baseDir, "maps");
            if (!Directory.Exists(mapsDir))
            {
                Directory.CreateDirectory(mapsDir);
            }

            string xwMapsDir = Path.Combine(mapsDir, "xw");
            if (!Directory.Exists(xwMapsDir))
            {
                Directory.CreateDirectory(xwMapsDir);
            }

            string csMapsDir = Path.Combine(mapsDir, "cs");
            if (!Directory.Exists(csMapsDir))
            {
                Directory.CreateDirectory(csMapsDir);
            }

            string sdMapDir = Path.Combine(mapsDir, "sd");
            if (!Directory.Exists(sdMapDir))
            {
                Directory.CreateDirectory(sdMapDir);
            }

            if (System.IO.File.Exists(xwMapsDir + @"\map" + index + ".dat"))
            {
                var xwMap = LoadXwMap(mapsDir + @"\map" + index + ".dat");
                Server.Map.Instance[index] = MapFromXwMap(xwMap);
                return;
            }

            if (File.Exists(csMapsDir + @"\map" + index + ".ini"))
            {
                var csMap = LoadCsMap(csMapsDir + @"\map" + index + ".ini");
                Server.Map.Instance[index] = MapFromCsMap(csMap);
                return;
            }

            var mapPath = Path.Combine(sdMapDir, index + ".map");
            if (File.Exists(mapPath))
            {
                SdMap sdMap = LoadSdMap(mapPath);
                Server.Map.Instance[index] = MapFromSdMap(sdMap);
                return;
            }

            JObject data;

            data = await Database.SelectRowAsync(index, "map", "data");

            if (data is null)
            {
                if (Server.Map.Instance.Count <= index)
                {
                    Server.Map.Instance.Add(new Map());
                }
                OnClear(index);
                return;
            }

            var mapData = JObject.FromObject(data).ToObject<Map>();
            Server.Map.Instance.Add(mapData ?? new Map());

            MapResource.OnUpdate(index);
        }

        public static CsMap LoadCsMap(string fileName)
        {
            long i;
            long x;
            long y;
            var csMap = new CsMap();

            // General
            {
                var instance = csMap.MapData;
                instance.Name = Database.GetVar(fileName, "General", "Name");
                instance.Music = Database.GetVar(fileName, "General", "Music");
                instance.Moral = Convert.ToByte(Database.GetVar(fileName, "General", "Moral"));
                instance.Up = Convert.ToInt32(Database.GetVar(fileName, "General", "Up"));
                instance.Down = Convert.ToInt32(Database.GetVar(fileName, "General", "Down"));
                instance.Left = Convert.ToInt32(Database.GetVar(fileName, "General", "Left"));
                instance.Right = Convert.ToInt32(Database.GetVar(fileName, "General", "Right"));
                instance.BootMap = Convert.ToInt32(Database.GetVar(fileName, "General", "BootMap"));
                instance.BootX = Convert.ToByte(Database.GetVar(fileName, "General", "BootX"));
                instance.BootY = Convert.ToByte(Database.GetVar(fileName, "General", "BootY"));
                instance.MaxX = Convert.ToByte(Database.GetVar(fileName, "General", "MaxX"));
                instance.MaxY = Convert.ToByte(Database.GetVar(fileName, "General", "MaxY"));

                instance.Weather = Convert.ToInt32(Database.GetVar(fileName, "General", "Weather"));
                instance.WeatherIntensity = Convert.ToInt32(Database.GetVar(fileName, "General", "WeatherIntensity"));

                instance.Fog = Convert.ToInt32(Database.GetVar(fileName, "General", "Fog"));
                instance.FogSpeed = Convert.ToInt32(Database.GetVar(fileName, "General", "FogSpeed"));
                instance.FogOpacity = Convert.ToInt32(Database.GetVar(fileName, "General", "FogOpacity"));

                instance.Red = Convert.ToInt32(Database.GetVar(fileName, "General", "Red"));
                instance.Green = Convert.ToInt32(Database.GetVar(fileName, "General", "Green"));
                instance.Blue = Convert.ToInt32(Database.GetVar(fileName, "General", "Blue"));
                instance.Alpha = Convert.ToInt32(Database.GetVar(fileName, "General", "Alpha"));

                instance.BossNpc = Convert.ToInt32(Database.GetVar(fileName, "General", "BossNpc"));
            }

            // Redim the map
            csMap.Tile = new CsTile[csMap.MapData.MaxX, csMap.MapData.MaxY];

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                // Assuming Core.Constant.MAX_X and Core.Constant.MAX_Y are the dimensions of your map
                int maxX = csMap.MapData.MaxX;
                int maxY = csMap.MapData.MaxY;

                for (x = 0L; x < maxX; x++)
                {
                    for (y = 0L; y < maxY; y++)
                    {
                        csMap.Tile[x, y].Autotile = new byte[Enum.GetValues(typeof(MapLayer)).Length];
                        csMap.Tile[x, y].Layer = new CsTileType[Enum.GetValues(typeof(MapLayer)).Length];

                        var instance1 = csMap.Tile[x, y];
                        instance1.Type = binaryReader.ReadByte();
                        instance1.Data1 = binaryReader.ReadInt32();
                        instance1.Data2 = binaryReader.ReadInt32();
                        instance1.Data3 = binaryReader.ReadInt32();
                        instance1.Data4 = binaryReader.ReadInt32();
                        instance1.Data5 = binaryReader.ReadInt32();

                        for (i = 0L; i < Enum.GetValues(typeof(MapLayer)).Length; i++)
                            instance1.Autotile[i] = binaryReader.ReadByte();
                        instance1.DirBlock = binaryReader.ReadByte();

                        for (i = 0L; i < Enum.GetValues(typeof(MapLayer)).Length; i++)
                        {
                            instance1.Layer[i].TileSet = binaryReader.ReadInt32();
                            instance1.Layer[i].X = binaryReader.ReadInt32();
                            instance1.Layer[i].Y = binaryReader.ReadInt32();
                        }
                    }
                }
            }

            return csMap;
        }

        public static XwMap LoadXwMap(string fileName)
        {
            var encoding = new ASCIIEncoding();
            var xwMap = new XwMap
            {
                Tile = new XwTile[16, 12],
                Npc = new long[Core.Globals.Variables.MaxMapNpcs]
            };

            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                using (var reader = new BinaryReader(fs))
                {
                    // OFFSET 0: The first 20 bytes are the map name.
                    xwMap.Name = encoding.GetString(reader.ReadBytes(20));

                    // OFFSET 20: The revision is stored here @ 4 bytes.
                    xwMap.Revision = reader.ReadInt32();

                    // OFFSET 24: Contains the map moral as a byte.
                    xwMap.Moral = reader.ReadByte();

                    // OFFSET 25: Stored as 2 bytes, the map UP.
                    xwMap.Up = reader.ReadInt16();

                    // OFFSET 27: Stored as 2 bytes, the map DOWN.
                    xwMap.Down = reader.ReadInt16();

                    // OFFSET 29: Stored as 2 bytes, the map LEFT.
                    xwMap.Left = reader.ReadInt16();

                    // OFFSET 31: Stored as 2 bytes, the map RIGHT.
                    xwMap.Right = reader.ReadInt16();

                    // OFFSET 33: Stored as 2 bytes, the map music.
                    xwMap.Music = reader.ReadInt16();

                    // OFFSET 35: Stored as 2 bytes, the Boot MyMap.
                    xwMap.BootMap = reader.ReadInt16();

                    // OFFSET 37: Stored as a single byte, the boot X
                    xwMap.BootX = reader.ReadByte();

                    // OFFSET 38: Stored as a single byte, the boot Y
                    xwMap.BootY = reader.ReadByte();

                    // OFFSET 39: Stored as two bytes, the Shop Id.
                    xwMap.Shop = reader.ReadInt16();

                    // OFFSET 41: Stored as a single byte, is the map indoors?
                    xwMap.Indoors = (byte)(reader.ReadByte() == 1 ? 1 : 0);

                    // Now, we decode the Tiles
                    for (int y = 0; y < 11; y++)
                    {
                        for (int x = 0; x < 15; x++)
                        {
                            xwMap.Tile[x, y].Ground = reader.ReadInt16(); // 42
                            xwMap.Tile[x, y].Mask = reader.ReadInt16(); // 44
                            xwMap.Tile[x, y].MaskAnim = reader.ReadInt16(); // 46
                            xwMap.Tile[x, y].Fringe = reader.ReadInt16(); // 48
                            xwMap.Tile[x, y].Type = (XwTileType)reader.ReadByte(); // 50
                            xwMap.Tile[x, y].Data1 = reader.ReadInt16(); // 51
                            xwMap.Tile[x, y].Data2 = reader.ReadInt16(); // 53
                            xwMap.Tile[x, y].Data3 = reader.ReadInt16(); // 55
                            xwMap.Tile[x, y].Type2 = (XwTileType)reader.ReadByte(); // 57
                            xwMap.Tile[x, y].Data1_2 = reader.ReadInt16(); // 59
                            xwMap.Tile[x, y].Data2_2 = reader.ReadInt16(); // 61
                            xwMap.Tile[x, y].Data3_2 = reader.ReadInt16(); // 63
                            xwMap.Tile[x, y].Mask2 = reader.ReadInt16(); // 64
                            xwMap.Tile[x, y].Mask2Anim = reader.ReadInt16(); // 66
                            xwMap.Tile[x, y].FringeAnim = reader.ReadInt16(); // 68
                            xwMap.Tile[x, y].Roof = reader.ReadInt16(); // 70
                            xwMap.Tile[x, y].Fringe2Anim = reader.ReadInt16(); // 72
                        }
                    }

                    for (int i = 0; i <= 14; i++)
                        xwMap.Npc[i] = reader.ReadInt32();
                }
            }

            return xwMap;
        }

        private static Tile ConvertXwTileToTile(XwTile xwTile)
        {
            var tile = new Tile
            {
                Layer = new Layer[System.Enum.GetValues(typeof(MapLayer)).Length]
            };

            // Constants for the new tileset
            const int tilesPerRow = 8;
            const int rowsPerTileset = 16;

            // Process each layer
            for (int i = (int)MapLayer.Ground; i < Enum.GetValues(typeof(MapLayer)).Length; i++)
            {
                int tileNumber = 0;

                // Select the appropriate tile number for each layer
                switch ((MapLayer)i)
                {
                    case MapLayer.Ground:
                        tileNumber = xwTile.Ground;
                        break;
                    case MapLayer.Mask:
                        tileNumber = xwTile.Mask;
                        break;
                    case MapLayer.MaskAnimation:
                        tileNumber = xwTile.MaskAnim;
                        break;
                    case MapLayer.Cover:
                        tileNumber = xwTile.Mask2;
                        break;
                    case MapLayer.CoverAnimation:
                        tileNumber = xwTile.Mask2Anim;
                        break;
                    case MapLayer.Fringe:
                        tileNumber = xwTile.Fringe;
                        break;
                    case MapLayer.FringeAnimation:
                        tileNumber = xwTile.FringeAnim;
                        break;
                    case MapLayer.Roof:
                        tileNumber = xwTile.Roof;
                        break;
                    case MapLayer.RoofAnimation:
                        tileNumber = xwTile.Fringe2Anim;
                        break;
                }

                // Ensure tileNumber is non-negative
                if (tileNumber > 0)
                {
                    tile.Layer[i].Tileset = (int)(Math.Floor(tileNumber / (double)tilesPerRow / rowsPerTileset) + 1);
                    tile.Layer[i].Y = (int)(Math.Floor(tileNumber / (double)tilesPerRow) % rowsPerTileset);
                    tile.Layer[i].X = tileNumber % tilesPerRow;
                }
            }

            // Copy over additional data fields
            tile.Data1 = xwTile.Data1;
            tile.Data2 = xwTile.Data2;
            tile.Data3 = xwTile.Data3;
            tile.Data1_2 = xwTile.Data1_2;
            tile.Data2_2 = xwTile.Data2_2;
            tile.Data3_2 = xwTile.Data3_2;
            tile.Type = ToTileType(xwTile.Type);
            tile.Type2 = ToTileType(xwTile.Type2);

            return tile;
        }

        public static TileType ToTileType(XwTileType xwTileType)
        {
            string name = Enum.GetName(typeof(XwTileType), xwTileType);
            return name switch
            {
                "None" => TileType.None,
                "Block" => TileType.Blocked,
                "Warp" => TileType.Warp,
                "Item" => TileType.Item,
                "NpcAvoid" => TileType.NpcAvoid,
                "NpcSpawn" => TileType.NpcSpawn,
                "Shop" => TileType.Shop,
                "Heal" => TileType.Heal,
                "Damage" => TileType.Trap,
                "NoCrossing" => TileType.NoCrossing,
                "Key" => TileType.Key,
                "KeyOpen" => TileType.KeyOpen,
                "Door" => TileType.Door,
                "WalkThrough" => TileType.WalkThrough,
                "Arena" => TileType.Arena,
                "Roof" => TileType.Roof,
                _ => TileType.None // Default for unmapped types (e.g., Sign, DirectionBlock)
            };
        }

        public static Map MapFromXwMap(XwMap xwMap)
        {
            var map = new Map();

            map.Tile = new Tile[16, 12];
            map.Npc = new int[Core.Globals.Variables.MaxMapNpcs];
            map.Name = xwMap.Name;
            map.Music = "Music" + xwMap.Music.ToString() + ".mid";
            map.Revision = (int)xwMap.Revision;
            map.Moral = xwMap.Moral;
            map.Up = xwMap.Up;
            map.Down = xwMap.Down;
            map.Left = xwMap.Left;
            map.Right = xwMap.Right;
            map.BootMap = xwMap.BootMap;
            map.BootX = xwMap.BootX;
            map.BootY = xwMap.BootY;
            map.Shop = xwMap.Shop;

            // Convert Byte to Boolean (False if 0, True otherwise)
            map.Indoors = xwMap.Indoors != 0;

            // Loop through each tile in xwMap and copy the data to map
            for (int y = 0; y < 11; y++)
            {
                for (int x = 0; x < 15; x++)
                    map.Tile[x, y] = ConvertXwTileToTile(xwMap.Tile[x, y]);
            }

            // Npc array conversion (Long to Integer), if necessary
            //if (xwMap.Npc is not null)
            //{
            //    map.Npc = Array.ConvertAll(xwMap.Npc, i => (int)i);
            //}

            for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                map.Npc[i] = -1;
            }

            map.Weather = xwMap.Weather;
            map.NoRespawn = xwMap.Respawn == 0;
            map.MaxX = 15;
            map.MaxY = 11;

            return map;
        }

        public static SdMap LoadSdMap(string fileName)
        {
            // Load XML content
            string xmlContent = File.ReadAllText(fileName);
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xmlContent);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Invalid XML format in {fileName}.", ex);
            }

            SdMap sdMap = new SdMap();
            if (doc == null || doc.Root == null)
            {
                throw new InvalidDataException("XML document is empty or has no root element.");
            }

            var root = doc.Root;

            // Helper to get element by name and throw if missing
            string GetElementValue(string name)
            {
                var el = root.Element(name);
                if (el == null)
                    return "0";
                return el.Value.Trim();
            }

            sdMap.Revision = int.Parse(GetElementValue("Revision"));
            //sdMap.Tileset = int.Parse(GetElementValue("Tileset"));
            sdMap.Name = GetElementValue("Name");
            sdMap.Music = Path.GetFileName(GetElementValue("Music"));

            // Parse connections
            var connectionsNode = root.Element("Border");
            if (connectionsNode == null)
                throw new InvalidDataException("Missing 'Connections' element in XML document.");

            var connections = connectionsNode.Elements().ToList();
            if (connections.Count >= 4)
            {
                if (connectionsNode == null)
                    throw new InvalidDataException("Missing 'Border' element in XML document.");

                var connectionInts = connectionsNode.Elements("int").ToList();
                if (connectionInts.Count >= 4)
                {
                    sdMap.Up = int.Parse(connectionInts[0]?.Value?.Trim() ?? throw new InvalidDataException("Missing 'Up' in Border."));
                    sdMap.Down = int.Parse(connectionInts[1]?.Value?.Trim() ?? throw new InvalidDataException("Missing 'Down' in Border."));
                    sdMap.Left = int.Parse(connectionInts[2]?.Value?.Trim() ?? throw new InvalidDataException("Missing 'Left' in Border."));
                    sdMap.Right = int.Parse(connectionInts[3]?.Value?.Trim() ?? throw new InvalidDataException("Missing 'Right' in Border."));
                }
                else
                {
                    throw new InvalidDataException("Invalid Border data: not enough <int> elements.");
                }
            }
            else
            {
                throw new InvalidDataException("Invalid connections data.");
            }

            // Parse dimensions
            sdMap.MaxX = int.Parse(GetElementValue("Width"));
            sdMap.MaxY = int.Parse(GetElementValue("Height"));

            // Parse warp data (support multiple <WarpData> nodes)
            var warpDataParent = root.Element("WarpData");
            if (warpDataParent != null)
            {
                var warpDataList = warpDataParent.Elements("WarpData").ToList();
                if (warpDataList?.Count > 0)
                {
                    // If there are multiple <WarpData> nodes, pick the first one for backward compatibility
                    var warpNode = warpDataList[0];
                    var posElement = warpNode.Element("Position");
                    var destElement = warpNode.Element("WarpDest");

                    // Extract Position data
                    var posX = int.Parse(posElement?.Element("X")?.Value?.Trim() ?? throw new InvalidDataException("Missing Position X in warp data."));
                    var posY = int.Parse(posElement?.Element("Y")?.Value?.Trim() ?? throw new InvalidDataException("Missing Position Y in warp data."));

                    // Extract WarpDest data
                    var destX = int.Parse(destElement?.Element("X")?.Value?.Trim() ?? throw new InvalidDataException("Missing WarpDest X in warp data."));
                    var destY = int.Parse(destElement?.Element("Y")?.Value?.Trim() ?? throw new InvalidDataException("Missing WarpDest Y in warp data."));

                    // Extract MapID data
                    var mapId = int.Parse(warpNode.Element("MapID")?.Value?.Trim() ?? throw new InvalidDataException("Missing MapID in warp data."));

                    sdMap.Warp = new SdWarpData
                    {
                        Pos = new SdWarpPos
                        {
                            X = posX,
                            Y = posY
                        },
                        WarpDes = new SdWarpDes
                        {
                            X = destX,
                            Y = destY
                        },
                        MapId = mapId
                    };
                }
            }

            // Parse layer data
            var mapGridNode = root.Element("MapGrid");
            if (mapGridNode == null)
            {
                throw new InvalidDataException("Invalid map data: 'MapGrid' node missing.");
            }
            var layersNode = mapGridNode.Element("Layers");
            if (layersNode == null)
            {
                throw new InvalidDataException("Invalid layer data: 'Layers' node missing.");
            }

            var mapLayers = new List<Core.Globals.Type.SdMapLayer>();

            // There may be multiple <MapLayer> nodes
            foreach (var mapLayersNode in layersNode.Elements("MapLayer"))
            {
                // Extract Layer Name
                var layerNameElement = mapLayersNode.Element("Name");
                string layerName = layerNameElement != null ? layerNameElement.Value.Trim() : "";

                // Extract ArrayOfMapTile
                var tilesElement = mapLayersNode.Element("Tiles");
                var arrayOfMapTileElement = tilesElement != null ? tilesElement.Element("ArrayOfMapTile") : null;

                var tiles = new List<SdMapTile>();

                if (arrayOfMapTileElement != null)
                {
                    // Each child is a MapTile element
                    foreach (var tileElement in arrayOfMapTileElement.Elements())
                    {
                        int tileIndex = 0;
                        if (int.TryParse(tileElement.Value.Trim(), out tileIndex))
                        {
                            tiles.Add(new SdMapTile { TileIndex = tileIndex });
                        }
                    }
                }

                // Add this layer to the list
                var sdMapLayer = new Core.Globals.Type.SdMapLayer
                {
                    Name = layerName,
                    Tiles = new Core.Globals.Type.SdTile
                    {
                        ArrayOfMapTile = tiles
                    }
                };
                mapLayers.Add(sdMapLayer);
            }

            // Create layer structure
            sdMap.MapLayer = new SdLayer
            {
                MapLayer = mapLayers
            };

            return sdMap;
        }

        public static Map MapFromCsMap(CsMap csMap)
        {
            var mwMap = new Map
            {
                Name = csMap.MapData.Name,
                MaxX = csMap.MapData.MaxX,
                MaxY = csMap.MapData.MaxY,
                BootMap = csMap.MapData.BootMap,
                BootX = csMap.MapData.BootX,
                BootY = csMap.MapData.BootY,
                Moral = csMap.MapData.Moral,
                Music = csMap.MapData.Music,
                Fog = csMap.MapData.Fog,
                Weather = (byte)csMap.MapData.Weather,
                WeatherIntensity = csMap.MapData.WeatherIntensity,
                Up = csMap.MapData.Up,
                Down = csMap.MapData.Down,
                Left = csMap.MapData.Left,
                Right = csMap.MapData.Right,
                MapTintA = (byte)csMap.MapData.Alpha,
                MapTintR = (byte)csMap.MapData.Red,
                MapTintG = (byte)csMap.MapData.Green,
                MapTintB = (byte)csMap.MapData.Blue,
                FogOpacity = (byte)csMap.MapData.FogOpacity,
                FogSpeed = (byte)csMap.MapData.FogSpeed,
                Tile = new Tile[csMap.MapData.MaxX, csMap.MapData.MaxY],
                Npc = new int[Core.Globals.Variables.MaxMapNpcs]
            };

            var layerCount = Enum.GetValues(typeof(MapLayer)).Length;

            for (int x = 0; x < mwMap.MaxX; x++)
                {
                for (int y = 0; y < mwMap.MaxY; y++)
                {
                    mwMap.Tile[x, y].Layer = new Core.Globals.Type.Layer[layerCount];
                    mwMap.Tile[x, y].Data1 = csMap.Tile[x, y].Data1;
                    mwMap.Tile[x, y].Data2 = csMap.Tile[x, y].Data2;
                    mwMap.Tile[x, y].Data3 = csMap.Tile[x, y].Data3;
                    mwMap.Tile[x, y].DirBlock = csMap.Tile[x, y].DirBlock;

                    for (int i = (int)MapLayer.Ground; i < layerCount; i++)
                    {
                        mwMap.Tile[x, y].Layer[i].X = csMap.Tile[x, y].Layer[i].X;
                        mwMap.Tile[x, y].Layer[i].Y = csMap.Tile[x, y].Layer[i].Y;
                        mwMap.Tile[x, y].Layer[i].Tileset = csMap.Tile[x, y].Layer[i].TileSet;
                        mwMap.Tile[x, y].Layer[i].AutoTile = csMap.Tile[x, y].Autotile[i];
                    }
                }
            }

            for (int i = 0; i < 30; i++)
            {
                mwMap.Npc[i] = csMap.MapData.Npc[i];
            }

            return mwMap;
        }

        private static Map MapFromSdMap(SdMap sdMap)
        {
            var mwMap = new Map();

            mwMap.Name = sdMap.Name;
            mwMap.Music = sdMap.Music;
            mwMap.Revision = sdMap.Revision;

            mwMap.Up = sdMap.Up;
            mwMap.Down = sdMap.Down;
            mwMap.Left = sdMap.Left;
            mwMap.Right = sdMap.Right;

            mwMap.Tileset = sdMap.Tileset;
            mwMap.MaxX = (byte)sdMap.MaxX;
            mwMap.MaxY = (byte)sdMap.MaxY;

            int layerCount = sdMap.MapLayer.MapLayer.Count;
            int mapLayerEnumCount = Enum.GetValues(typeof(MapLayer)).Length;
            mwMap.Tile = new Tile[mwMap.MaxX, mwMap.MaxY];

            // Initialize all tiles and their layers
            for (int y = 0; y < mwMap.MaxY; y++)
            {
                for (int x = 0; x < mwMap.MaxX; x++)
                {
                    mwMap.Tile[x, y].Layer = new Layer[mapLayerEnumCount];
                }
            }

            // Fill in tile data for each layer
            for (int i = 0; i < layerCount; i++)
            {
                var layer = sdMap.MapLayer.MapLayer[i];
                var tiles = layer.Tiles.ArrayOfMapTile;
                int tileCounter = 0;
                for (int y = 0; y < mwMap.MaxY; y++)
                {
                    for (int x = 0; x < mwMap.MaxX; x++)
                    {
                        if (tileCounter < tiles.Count)
                        {
                            int tileIndex = tiles[tileCounter].TileIndex;
                            int targetLayer = i;

                            // Move the layer up for animation layers
                            switch (i)
                            {
                                case (int)Core.Globals.SdMapLayer.Mask2:
                                    targetLayer = (int)MapLayer.Cover;
                                    break;
                                case (int)Core.Globals.SdMapLayer.Fringe:
                                    targetLayer = (int)MapLayer.Fringe;
                                    break;
                                case (int)Core.Globals.SdMapLayer.Fringe2:
                                    targetLayer = (int)MapLayer.Roof;
                                    break;
                            }
                            mwMap.Tile[x, y].Layer[targetLayer].X = tileIndex % 12;
                            mwMap.Tile[x, y].Layer[targetLayer].Y = (tileIndex - mwMap.Tile[x, y].Layer[targetLayer].X) / 12;
                            mwMap.Tile[x, y].Layer[targetLayer].Tileset = 1;
                        }
                        tileCounter++;
                    }
                }
            }

            mwMap.Npc = new int[Core.Globals.Variables.MaxMapNpcs];

            for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
            {
                mwMap.Npc[i] = -1;
            }
            return mwMap;
        }

    }
}
