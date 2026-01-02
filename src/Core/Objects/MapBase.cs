using Core.Globals;
using System;
using System.Collections.Generic;

namespace Core.Objects
{
    public class MapBase
    {
        public string Name;
        public string Music;
        public int Revision;
        public byte Moral;
        public int Tileset;

        // Per-map camera zoom bounds.
        // Client clamps camera zoom to this range and initializes zoom to MinZoom when the map loads.
        public float MinZoom;
        public float MaxZoom;

        public int Up;
        public int Down;
        public int Left;
        public int Right;
        public int BootMap;
        public byte BootX;
        public byte BootY;
        public byte MaxX;
        public byte MaxY;
        public Core.Globals.Type.Tile[,] Tile;
        public int[] Npc;
        public int EventCount;
        public Core.Globals.Type.Event[] Event;
        public byte Weather;
        public int Fog;
        public int WeatherIntensity;
        public byte FogOpacity;
        public byte FogSpeed;
        public bool MapTint;
        public byte MapTintR;
        public byte MapTintG;
        public byte MapTintB;
        public byte MapTintA;
        public byte Panorama;
        public byte Parallax;
        public byte Brightness;
        public int Shop;
        public bool NoRespawn;
        public bool Indoors;

        public static List<MapBase> Instance { get; private set; } = new List<MapBase>();

        public MapBase()
        {
            Name = "";
            Music = "";
            Tileset = 1;
            MaxX = Variables.MaxMapX;
            MaxY = Variables.MaxMapY;

            // Defaults match existing clamp behavior.
            MinZoom = 0.5f;
            MaxZoom = 4.0f;

            Npc = new int[Variables.MaxMapNpcs];
            for (var i = 0; i < Npc.Length; i++)
            {
                Npc[i] = -1;
            }

            Tile = new Core.Globals.Type.Tile[MaxX, MaxY];
            var layerCount = Enum.GetValues(typeof(MapLayer)).Length;
            for (var x = 0; x < MaxX; x++)
            {
                for (var y = 0; y < MaxY; y++)
                {
                    Tile[x, y].Layer = new Core.Globals.Type.Layer[layerCount];
                }
            }

            EventCount = 0;
            Event = Array.Empty<Core.Globals.Type.Event>();
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
            {
                Instance[index] = new MapBase();
            }
        }

        public static void OnClear()
        {
            for (var i = 0; i < Instance.Count; i++)
                OnClear(i);
        }
    }
}
