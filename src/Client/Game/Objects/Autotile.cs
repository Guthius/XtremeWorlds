using Core;
using Core.Globals;
using Core.Interfaces;
using Type = Core.Globals.Type;
using static Core.Globals.Commands;

namespace Client
{

    public class Autotile : IData
    {
        // RPG Maker XP autotile support (96x128 = 3x4 tiles at 32px each).
        // The table below matches the 48 RMXP patterns, with 4 quarter pieces per pattern.
        // Coordinates are pixel offsets relative to the top-left of the 3x4 autotile block.
        private const int RmxpPatternCount = 48;
        private static readonly Type.Point[] RmxpPatternQuarters = new Type.Point[RmxpPatternCount * 4]
        {
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 32, Y = 80 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 48, Y = 80 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 48, Y = 64 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 32, Y = 64 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 0, Y = 64 },
            new Type.Point { X = 16, Y = 64 },
            new Type.Point { X = 0, Y = 80 },
            new Type.Point { X = 16, Y = 80 },
            new Type.Point { X = 0, Y = 64 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 0, Y = 80 },
            new Type.Point { X = 16, Y = 80 },
            new Type.Point { X = 0, Y = 64 },
            new Type.Point { X = 16, Y = 64 },
            new Type.Point { X = 0, Y = 80 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 0, Y = 64 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 0, Y = 80 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 32, Y = 32 },
            new Type.Point { X = 48, Y = 32 },
            new Type.Point { X = 32, Y = 48 },
            new Type.Point { X = 48, Y = 48 },
            new Type.Point { X = 32, Y = 32 },
            new Type.Point { X = 48, Y = 32 },
            new Type.Point { X = 32, Y = 48 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 32, Y = 32 },
            new Type.Point { X = 48, Y = 32 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 48, Y = 48 },
            new Type.Point { X = 32, Y = 32 },
            new Type.Point { X = 48, Y = 32 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 64, Y = 64 },
            new Type.Point { X = 80, Y = 64 },
            new Type.Point { X = 64, Y = 80 },
            new Type.Point { X = 80, Y = 80 },
            new Type.Point { X = 64, Y = 64 },
            new Type.Point { X = 80, Y = 64 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 80 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 64 },
            new Type.Point { X = 64, Y = 80 },
            new Type.Point { X = 80, Y = 80 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 64 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 80 },
            new Type.Point { X = 32, Y = 96 },
            new Type.Point { X = 48, Y = 96 },
            new Type.Point { X = 32, Y = 112 },
            new Type.Point { X = 48, Y = 112 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 48, Y = 96 },
            new Type.Point { X = 32, Y = 112 },
            new Type.Point { X = 48, Y = 112 },
            new Type.Point { X = 32, Y = 96 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 32, Y = 112 },
            new Type.Point { X = 48, Y = 112 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 32, Y = 112 },
            new Type.Point { X = 48, Y = 112 },
            new Type.Point { X = 0, Y = 64 },
            new Type.Point { X = 80, Y = 64 },
            new Type.Point { X = 0, Y = 80 },
            new Type.Point { X = 80, Y = 80 },
            new Type.Point { X = 32, Y = 32 },
            new Type.Point { X = 48, Y = 32 },
            new Type.Point { X = 32, Y = 112 },
            new Type.Point { X = 48, Y = 112 },
            new Type.Point { X = 0, Y = 32 },
            new Type.Point { X = 16, Y = 32 },
            new Type.Point { X = 0, Y = 48 },
            new Type.Point { X = 16, Y = 48 },
            new Type.Point { X = 0, Y = 32 },
            new Type.Point { X = 16, Y = 32 },
            new Type.Point { X = 0, Y = 48 },
            new Type.Point { X = 80, Y = 16 },
            new Type.Point { X = 64, Y = 32 },
            new Type.Point { X = 80, Y = 32 },
            new Type.Point { X = 64, Y = 48 },
            new Type.Point { X = 80, Y = 48 },
            new Type.Point { X = 64, Y = 32 },
            new Type.Point { X = 80, Y = 32 },
            new Type.Point { X = 64, Y = 16 },
            new Type.Point { X = 80, Y = 48 },
            new Type.Point { X = 64, Y = 96 },
            new Type.Point { X = 80, Y = 96 },
            new Type.Point { X = 64, Y = 112 },
            new Type.Point { X = 80, Y = 112 },
            new Type.Point { X = 64, Y = 0 },
            new Type.Point { X = 80, Y = 96 },
            new Type.Point { X = 64, Y = 112 },
            new Type.Point { X = 80, Y = 112 },
            new Type.Point { X = 0, Y = 96 },
            new Type.Point { X = 16, Y = 96 },
            new Type.Point { X = 0, Y = 112 },
            new Type.Point { X = 16, Y = 112 },
            new Type.Point { X = 0, Y = 96 },
            new Type.Point { X = 80, Y = 0 },
            new Type.Point { X = 0, Y = 112 },
            new Type.Point { X = 16, Y = 112 },
            new Type.Point { X = 0, Y = 32 },
            new Type.Point { X = 80, Y = 32 },
            new Type.Point { X = 0, Y = 48 },
            new Type.Point { X = 80, Y = 48 },
            new Type.Point { X = 0, Y = 32 },
            new Type.Point { X = 16, Y = 32 },
            new Type.Point { X = 0, Y = 112 },
            new Type.Point { X = 16, Y = 112 },
            new Type.Point { X = 0, Y = 96 },
            new Type.Point { X = 80, Y = 96 },
            new Type.Point { X = 0, Y = 112 },
            new Type.Point { X = 80, Y = 112 },
            new Type.Point { X = 64, Y = 32 },
            new Type.Point { X = 80, Y = 32 },
            new Type.Point { X = 64, Y = 112 },
            new Type.Point { X = 80, Y = 112 },
            new Type.Point { X = 0, Y = 32 },
            new Type.Point { X = 80, Y = 32 },
            new Type.Point { X = 0, Y = 112 },
            new Type.Point { X = 80, Y = 112 },
            new Type.Point { X = 0, Y = 0 },
            new Type.Point { X = 16, Y = 0 },
            new Type.Point { X = 0, Y = 16 },
            new Type.Point { X = 16, Y = 16 },
        };

        private static int GetRmxpPatternIndex(int layerNum, int x, int y)
        {
            // Adjacent matches
            bool n = CheckTileMatch(layerNum, x, y, x, y - 1);
            bool e = CheckTileMatch(layerNum, x, y, x + 1, y);
            bool s = CheckTileMatch(layerNum, x, y, x, y + 1);
            bool w = CheckTileMatch(layerNum, x, y, x - 1, y);

            // Diagonal matches
            bool nw = CheckTileMatch(layerNum, x, y, x - 1, y - 1);
            bool ne = CheckTileMatch(layerNum, x, y, x + 1, y - 1);
            bool sw = CheckTileMatch(layerNum, x, y, x - 1, y + 1);
            bool se = CheckTileMatch(layerNum, x, y, x + 1, y + 1);

            // Concave corner flags (only meaningful when both adjacent sides are present)
            bool missNw = n && w && !nw;
            bool missNe = n && e && !ne;
            bool missSw = s && w && !sw;
            bool missSe = s && e && !se;

            int sideMask = 0;
            if (n) sideMask |= 8;
            if (e) sideMask |= 4;
            if (s) sideMask |= 2;
            if (w) sideMask |= 1;

            // The pattern index mapping below matches the 48-pattern RMXP autotile layout.
            switch (sideMask)
            {
                case 15: // N E S W
                    return 0
                        + (missNw ? 1 : 0)
                        + (missNe ? 2 : 0)
                        + (missSe ? 4 : 0)
                        + (missSw ? 8 : 0);

                case 14: // N E S
                    return 16
                        + (missNe ? 1 : 0)
                        + (missSe ? 2 : 0);

                case 7: // E S W
                    return 20
                        + (missSe ? 1 : 0)
                        + (missSw ? 2 : 0);

                case 11: // N S W
                    return 24
                        + (missSw ? 1 : 0)
                        + (missNw ? 2 : 0);

                case 13: // N E W
                    return 28
                        + (missNw ? 1 : 0)
                        + (missNe ? 2 : 0);

                case 10: // E W
                    return 32;

                case 5: // N S
                    return 33;

                case 6: // E S
                    return 34 + (missSe ? 1 : 0);

                case 3: // S W
                    return 36 + (missSw ? 1 : 0);

                case 9: // N W
                    return 38 + (missNw ? 1 : 0);

                case 12: // N E
                    return 40 + (missNe ? 1 : 0);

                case 2: // S
                    return 42;

                case 4: // E
                    return 43;

                case 8: // N
                    return 44;

                case 1: // W
                    return 45;

                case 0: // No side connections (optionally allow diagonal-only blends)
                    return (nw || ne || sw || se) ? 46 : 47;

                default:
                    // Remaining side masks aren't expected for RMXP autotiles.
                    // Fall back to the isolated-tile pattern.
                    return 47;
            }
        }

        private static void CalculateRmxp(int layerNum, int x, int y)
        {
            if (Data.Autotile == null || Data.Autotile[x, y].Layer == null)
                return;
            if (layerNum < 0 || layerNum >= Data.Autotile[x, y].Layer.Length)
                return;
            if (Data.Autotile[x, y].Layer[layerNum].Tile == null)
                Data.Autotile[x, y].Layer[layerNum].Tile = new Type.Point[5];

            int patternId = GetRmxpPatternIndex(layerNum, x, y);
            if (patternId < 0 || patternId >= RmxpPatternCount)
                patternId = 47;

            int baseIndex = patternId * 4;
            // Order: 0=NW, 1=NE, 2=SW, 3=SE
            Data.Autotile[x, y].Layer[layerNum].Tile[1] = RmxpPatternQuarters[baseIndex + 0];
            Data.Autotile[x, y].Layer[layerNum].Tile[2] = RmxpPatternQuarters[baseIndex + 1];
            Data.Autotile[x, y].Layer[layerNum].Tile[3] = RmxpPatternQuarters[baseIndex + 2];
            Data.Autotile[x, y].Layer[layerNum].Tile[4] = RmxpPatternQuarters[baseIndex + 3];
        }

        public static void OnClear()
        {
            int x;
            int y;
            int i;

            Data.Autotile = new Type.Autotile[(Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX), (Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY)];

            var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
            for (x = 0; x < count; x++)
            {
                var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                for (y = 0; y < count2; y++)
                {
                    int layerCount = System.Enum.GetValues(typeof(MapLayer)).Length;
                    Data.Autotile[x, y].Layer = new Type.QuarterTile[layerCount];
                    for (i = 0; i < layerCount; i++)
                    {
                        Data.Autotile[x, y].Layer[i].SrcX = new int[5];
                        Data.Autotile[x, y].Layer[i].SrcY = new int[5];
                        Data.Autotile[x, y].Layer[i].Tile = new Type.Point[5];
                    }
                }
            }
        }

        // \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        // All of this code is for auto tiles and the math behind generating them.
        // \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        private static void OnPlace(int layerNum, int x, int y, byte tileQuarter, string autoTileLetter)
        {
            int layerCount = System.Enum.GetValues(typeof(MapLayer)).Length;
            if (layerNum > layerCount)
            {
                layerNum = layerNum - (layerCount);
                // Null guards for extended layer access
                if (Data.Autotile == null) return;
                if (Data.Autotile?[x, y].Layer == null) return;
                if (layerNum < 0 || layerNum >= Data.Autotile[x, y].Layer.Length) return;
                if (Data.Autotile?[x, y].Layer[layerNum].Tile == null)
                    Data.Autotile?[x, y].Layer[layerNum].Tile = new Type.Point[5];
                {
                    if (Data.Autotile != null)
                    {
                        ref var instance = ref Data.Autotile[x, y].Layer[layerNum].Tile[tileQuarter];
                        switch (autoTileLetter ?? "")
                        {
                            case "a":
                            {
                                instance.X = Type.AutoIn[1].X;
                                instance.Y = Type.AutoIn[1].Y;
                                break;
                            }
                            case "b":
                            {
                                instance.X = Type.AutoIn[2].X;
                                instance.Y = Type.AutoIn[2].Y;
                                break;
                            }
                            case "c":
                            {
                                instance.X = Type.AutoIn[3].X;
                                instance.Y = Type.AutoIn[3].Y;
                                break;
                            }
                            case "d":
                            {
                                instance.X = Type.AutoIn[4].X;
                                instance.Y = Type.AutoIn[4].Y;
                                break;
                            }
                            case "e":
                            {
                                instance.X = Type.AutoNw[1].X;
                                instance.Y = Type.AutoNw[1].Y;
                                break;
                            }
                            case "f":
                            {
                                instance.X = Type.AutoNw[2].X;
                                instance.Y = Type.AutoNw[2].Y;
                                break;
                            }
                            case "g":
                            {
                                instance.X = Type.AutoNw[3].X;
                                instance.Y = Type.AutoNw[3].Y;
                                break;
                            }
                            case "h":
                            {
                                instance.X = Type.AutoNw[4].X;
                                instance.Y = Type.AutoNw[4].Y;
                                break;
                            }
                            case "i":
                            {
                                instance.X = Type.AutoNe[1].X;
                                instance.Y = Type.AutoNe[1].Y;
                                break;
                            }
                            case "j":
                            {
                                instance.X = Type.AutoNe[2].X;
                                instance.Y = Type.AutoNe[2].Y;
                                break;
                            }
                            case "k":
                            {
                                instance.X = Type.AutoNe[3].X;
                                instance.Y = Type.AutoNe[3].Y;
                                break;
                            }
                            case "l":
                            {
                                instance.X = Type.AutoNe[4].X;
                                instance.Y = Type.AutoNe[4].Y;
                                break;
                            }
                            case "m":
                            {
                                instance.X = Type.AutoSw[1].X;
                                instance.Y = Type.AutoSw[1].Y;
                                break;
                            }
                            case "n":
                            {
                                instance.X = Type.AutoSw[2].X;
                                instance.Y = Type.AutoSw[2].Y;
                                break;
                            }
                            case "o":
                            {
                                instance.X = Type.AutoSw[3].X;
                                instance.Y = Type.AutoSw[3].Y;
                                break;
                            }
                            case "p":
                            {
                                instance.X = Type.AutoSw[4].X;
                                instance.Y = Type.AutoSw[4].Y;
                                break;
                            }
                            case "q":
                            {
                                instance.X = Type.AutoSe[1].X;
                                instance.Y = Type.AutoSe[1].Y;
                                break;
                            }
                            case "r":
                            {
                                instance.X = Type.AutoSe[2].X;
                                instance.Y = Type.AutoSe[2].Y;
                                break;
                            }
                            case "s":
                            {
                                instance.X = Type.AutoSe[3].X;
                                instance.Y = Type.AutoSe[3].Y;
                                break;
                            }
                            case "t":
                            {
                                instance.X = Type.AutoSe[4].X;
                                instance.Y = Type.AutoSe[4].Y;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // Null guards for normal layer access
                if (Data.Autotile == null) return;
                if (Data.Autotile[x, y].Layer == null) return;
                if (layerNum < 0 || layerNum >= Data.Autotile[x, y].Layer.Length) return;
                if (Data.Autotile[x, y].Layer[layerNum].Tile == null)
                    Data.Autotile[x, y].Layer[layerNum].Tile = new Type.Point[5];
                {
                    ref var instance1 = ref Data.Autotile[x, y].Layer[layerNum].Tile[tileQuarter];
                    switch (autoTileLetter ?? "")
                    {
                        case "a":
                            {
                                instance1.X = Type.AutoIn[1].X;
                                instance1.Y = Type.AutoIn[1].Y;
                                break;
                            }
                        case "b":
                            {
                                instance1.X = Type.AutoIn[2].X;
                                instance1.Y = Type.AutoIn[2].Y;
                                break;
                            }
                        case "c":
                            {
                                instance1.X = Type.AutoIn[3].X;
                                instance1.Y = Type.AutoIn[3].Y;
                                break;
                            }
                        case "d":
                            {
                                instance1.X = Type.AutoIn[4].X;
                                instance1.Y = Type.AutoIn[4].Y;
                                break;
                            }
                        case "e":
                            {
                                instance1.X = Type.AutoNw[1].X;
                                instance1.Y = Type.AutoNw[1].Y;
                                break;
                            }
                        case "f":
                            {
                                instance1.X = Type.AutoNw[2].X;
                                instance1.Y = Type.AutoNw[2].Y;
                                break;
                            }
                        case "g":
                            {
                                instance1.X = Type.AutoNw[3].X;
                                instance1.Y = Type.AutoNw[3].Y;
                                break;
                            }
                        case "h":
                            {
                                instance1.X = Type.AutoNw[4].X;
                                instance1.Y = Type.AutoNw[4].Y;
                                break;
                            }
                        case "i":
                            {
                                instance1.X = Type.AutoNe[1].X;
                                instance1.Y = Type.AutoNe[1].Y;
                                break;
                            }
                        case "j":
                            {
                                instance1.X = Type.AutoNe[2].X;
                                instance1.Y = Type.AutoNe[2].Y;
                                break;
                            }
                        case "k":
                            {
                                instance1.X = Type.AutoNe[3].X;
                                instance1.Y = Type.AutoNe[3].Y;
                                break;
                            }
                        case "l":
                            {
                                instance1.X = Type.AutoNe[4].X;
                                instance1.Y = Type.AutoNe[4].Y;
                                break;
                            }
                        case "m":
                            {
                                instance1.X = Type.AutoSw[1].X;
                                instance1.Y = Type.AutoSw[1].Y;
                                break;
                            }
                        case "n":
                            {
                                instance1.X = Type.AutoSw[2].X;
                                instance1.Y = Type.AutoSw[2].Y;
                                break;
                            }
                        case "o":
                            {
                                instance1.X = Type.AutoSw[3].X;
                                instance1.Y = Type.AutoSw[3].Y;
                                break;
                            }
                        case "p":
                            {
                                instance1.X = Type.AutoSw[4].X;
                                instance1.Y = Type.AutoSw[4].Y;
                                break;
                            }
                        case "q":
                            {
                                instance1.X = Type.AutoSe[1].X;
                                instance1.Y = Type.AutoSe[1].Y;
                                break;
                            }
                        case "r":
                            {
                                instance1.X = Type.AutoSe[2].X;
                                instance1.Y = Type.AutoSe[2].Y;
                                break;
                            }
                        case "s":
                            {
                                instance1.X = Type.AutoSe[3].X;
                                instance1.Y = Type.AutoSe[3].Y;
                                break;
                            }
                        case "t":
                            {
                                instance1.X = Type.AutoSe[4].X;
                                instance1.Y = Type.AutoSe[4].Y;
                                break;
                            }
                    }
                }
            }

        }

        public static void InitAutotiles()
        {
            int x;
            int y;
            int layerNum;
            // Procedure used to cache autotile positions. All positioning is
            // independant from the tileset. Calculations are convoluted and annoying.
            // Maths is not my strong point. Luckily we're caching them so it's a one-off
            // thing when the map is originally loaded. As such optimisation isn't an issue.
            // For simplicity's sake we cache all subtile SOURCE positions in to an array.
            // We also give letters to each subtile for easy rendering tweaks. ;]
            // First, we need to re-size the array

            Data.Autotile = new Type.Autotile[(Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX), (Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY)];
            var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
            for (x = 0; x < count; x++)
            {
                var count3 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                for (y = 0; y < count3; y++)
                {
                    int layerCount = System.Enum.GetValues(typeof(MapLayer)).Length;
                    Data.Autotile[x, y].Layer = new Type.QuarterTile[layerCount];
                    for (int i = 0; i < layerCount; i++)
                    {
                        Data.Autotile[x, y].Layer[i].SrcX = new int[5];
                        Data.Autotile[x, y].Layer[i].SrcY = new int[5];
                        Data.Autotile[x, y].Layer[i].Tile = new Type.Point[5];
                    }
                }
            }

            // Inner tiles (Top right subtile region)
            // NW - a
            Type.AutoIn[1].X = 32;
            Type.AutoIn[1].Y = 0;
            // NE - b
            Type.AutoIn[2].X = 48;
            Type.AutoIn[2].Y = 0;
            // SW - c
            Type.AutoIn[3].X = 32;
            Type.AutoIn[3].Y = 16;
            // SE - d
            Type.AutoIn[4].X = 48;
            Type.AutoIn[4].Y = 16;
            // Outer Tiles - NW (bottom subtile region)
            // NW - e
            Type.AutoNw[1].X = 0;
            Type.AutoNw[1].Y = 32;
            // NE - f
            Type.AutoNw[2].X = 16;
            Type.AutoNw[2].Y = 32;
            // SW - g
            Type.AutoNw[3].X = 0;
            Type.AutoNw[3].Y = 48;
            // SE - h
            Type.AutoNw[4].X = 16;
            Type.AutoNw[4].Y = 48;
            // Outer Tiles - NE (bottom subtile region)
            // NW - i
            Type.AutoNe[1].X = 32;
            Type.AutoNe[1].Y = 32;
            // NE - g
            Type.AutoNe[2].X = 48;
            Type.AutoNe[2].Y = 32;
            // SW - k
            Type.AutoNe[3].X = 32;
            Type.AutoNe[3].Y = 48;
            // SE - l
            Type.AutoNe[4].X = 48;
            Type.AutoNe[4].Y = 48;
            // Outer Tiles - SW (bottom subtile region)
            // NW - m
            Type.AutoSw[1].X = 0;
            Type.AutoSw[1].Y = 64;
            // NE - n
            Type.AutoSw[2].X = 16;
            Type.AutoSw[2].Y = 64;
            // SW - o
            Type.AutoSw[3].X = 0;
            Type.AutoSw[3].Y = 80;
            // SE - p
            Type.AutoSw[4].X = 16;
            Type.AutoSw[4].Y = 80;
            // Outer Tiles - SE (bottom subtile region)
            // NW - q
            Type.AutoSe[1].X = 32;
            Type.AutoSe[1].Y = 64;
            // NE - r
            Type.AutoSe[2].X = 48;
            Type.AutoSe[2].Y = 64;
            // SW - s
            Type.AutoSe[3].X = 32;
            Type.AutoSe[3].Y = 80;
            // SE - t
            Type.AutoSe[4].X = 48;
            Type.AutoSe[4].Y = 80;

            var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
            for (x = 0; x < count2; x++)
            {
                var count3 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                for (y = 0; y < count3; y++)
                {
                    if (Data.Autotile[x, y].Layer == null)
                        return;

                    int layerCount = System.Enum.GetValues(typeof(MapLayer)).Length;
                    for (layerNum = 0; layerNum < layerCount; layerNum++)
                    {
                        // calculate the subtile positions and place them
                        CalculateAutotile(x, y, layerNum);
                        // cache the rendering state of the tiles and set them
                        CacheRenderState(x, y, layerNum);
                    }
                }
            }

        }

        public static void CacheRenderState(int x, int y, int layerNum)
        {
            int quarterNum;

            if (x < 0 | x >= Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX | y < 0 | y >= Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY)
                return;

            // Ensure autotile layer arrays are initialized before dereferencing to avoid CS8602
            if (Data.Autotile == null || Data.Autotile[x, y].Layer == null)
                return;
            if (layerNum < 0 || layerNum >= Data.Autotile[x, y].Layer.Length)
                return;
            if (Data.Autotile[x, y].Layer[layerNum].Tile == null)
                Data.Autotile[x, y].Layer[layerNum].Tile = new Type.Point[5];

            ref var instance = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y];

            // check if the tile can be rendered
            if (instance.Layer[layerNum].Tileset <= 0 | instance.Layer[layerNum].Tileset > GameState.NumTileSets)
            {
                Data.Autotile[x, y].Layer[layerNum].RenderState = GameState.RenderStateNone;
                return;
            }

            // check if it needs to be rendered as an autotile
            if (instance.Layer[layerNum].AutoTile == GameState.AutotileNone | instance.Layer[layerNum].AutoTile == GameState.AutotileFake)
            {
                // default to... default
                Data.Autotile[x, y].Layer[layerNum].RenderState = GameState.RenderStateNormal;
            }
            else
            {
                Data.Autotile[x, y].Layer[layerNum].RenderState = GameState.RenderStateAutotile;
                // cache tileset positioning
                for (quarterNum = 0; quarterNum <= 4; quarterNum++)
                {
                    Data.Autotile[x, y].Layer[layerNum].SrcX[quarterNum] = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[layerNum].X * 32 + Data.Autotile[x, y].Layer[layerNum].Tile[quarterNum].X;
                    Data.Autotile[x, y].Layer[layerNum].SrcY[quarterNum] = Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[layerNum].Y * 32 + Data.Autotile[x, y].Layer[layerNum].Tile[quarterNum].Y;
                }
            }
        }

        private static void CalculateAutotile(int x, int y, int layerNum)
        {
            // Right, so we've split the tile block in to an easy to remember
            // collection of letters. We now need to do the calculations to find
            // out which little lettered block needs to be rendered. We do this
            // by reading the surrounding tiles to check for matches.
            // First we check to make sure an autotile situation is actually there.
            // Then we calculate exactly which situation has arisen.
            // The situations are "inner", "outer", "horizontal", "vertical" and "fill".
            // Exit out if we don't have an autotile

            if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[layerNum].AutoTile == 0)
                return;

            // Okay, we have autotiling but which one?
            switch (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[layerNum].AutoTile)
            {
                // Normal or animated - same difference
                case GameState.AutotileNormal:
                case GameState.AutotileAnim:
                    // North West Quarter
                    CalculateNW_Normal(layerNum, x, y);
                    // North East Quarter
                    CalculateNE_Normal(layerNum, x, y);
                    // South West Quarter
                    CalculateSW_Normal(layerNum, x, y);
                    // South East Quarter
                    CalculateSE_Normal(layerNum, x, y);
                    break;

                // RPG Maker XP
                case GameState.AutotileRpgMakerXp:
                    CalculateRmxp(layerNum, x, y);
                    break;
                // Cliff
                case GameState.AutotileCliff:
                    {
                        // North West Quarter
                        CalculateNW_Cliff(layerNum, x, y);
                        // North East Quarter
                        CalculateNE_Cliff(layerNum, x, y);
                        // South West Quarter
                        CalculateSW_Cliff(layerNum, x, y);
                        // South East Quarter
                        CalculateSE_Cliff(layerNum, x, y);
                        break;
                    }
                // Waterfalls
                case GameState.AutotileWaterfall:
                    {
                        // North West Quarter
                        CalculateNW_Waterfall(layerNum, x, y);
                        // North East Quarter
                        CalculateNE_Waterfall(layerNum, x, y);
                        // South West Quarter
                        CalculateSW_Waterfall(layerNum, x, y);
                        // South East Quarter
                        CalculateSE_Waterfall(layerNum, x, y);
                        break;
                    }
                    // Anything else
            }

        }

        // Normal autotiling
        private static void CalculateNW_Normal(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            var situation = default(byte);

            // North West
            if (CheckTileMatch(layerNum, x, y, x - 1, y - 1))
                tmpTile[1] = true;

            // North
            if (CheckTileMatch(layerNum, x, y, x, y - 1))
                tmpTile[2] = true;

            // West
            if (CheckTileMatch(layerNum, x, y, x - 1, y))
                tmpTile[3] = true;

            // Calculate Situation - Inner
            if (!tmpTile[2] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Horizontal
            if (!tmpTile[2] & tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (tmpTile[2] & !tmpTile[3])
                situation = GameState.AutoVertical;

            // Outer
            if (!tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoOuter;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 1, "e");
                        break;
                    }
                case GameState.AutoOuter:
                    {
                        OnPlace(layerNum, x, y, 1, "a");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 1, "i");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 1, "m");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 1, "q");
                        break;
                    }
            }

        }

        private static void CalculateNE_Normal(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            var situation = default(byte);

            // North
            if (CheckTileMatch(layerNum, x, y, x, y - 1))
                tmpTile[1] = true;

            // North East
            if (CheckTileMatch(layerNum, x, y, x + 1, y - 1))
                tmpTile[2] = true;

            // East
            if (CheckTileMatch(layerNum, x, y, x + 1, y))
                tmpTile[3] = true;

            // Calculate Situation - Inner
            if (!tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Horizontal
            if (!tmpTile[1] & tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoVertical;
            // Outer
            if (tmpTile[1] & !tmpTile[2] & tmpTile[3])
                situation = GameState.AutoOuter;
            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 2, "j");
                        break;
                    }
                case GameState.AutoOuter:
                    {
                        OnPlace(layerNum, x, y, 2, "b");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 2, "f");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 2, "r");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 2, "n");
                        break;
                    }
            }

        }

        private static void CalculateSW_Normal(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            var situation = default(byte);

            // West
            if (CheckTileMatch(layerNum, x, y, x - 1, y))
                tmpTile[1] = true;

            // South West
            if (CheckTileMatch(layerNum, x, y, x - 1, y + 1))
                tmpTile[2] = true;

            // South
            if (CheckTileMatch(layerNum, x, y, x, y + 1))
                tmpTile[3] = true;

            // Calculate Situation - Inner
            if (!tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Horizontal
            if (tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (!tmpTile[1] & tmpTile[3])
                situation = GameState.AutoVertical;

            // Outer
            if (tmpTile[1] & !tmpTile[2] & tmpTile[3])
                situation = GameState.AutoOuter;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 3, "o");
                        break;
                    }
                case GameState.AutoOuter:
                    {
                        OnPlace(layerNum, x, y, 3, "c");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 3, "s");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 3, "g");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 3, "k");
                        break;
                    }
            }

        }

        private static void CalculateSE_Normal(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            var situation = default(byte);

            // South
            if (CheckTileMatch(layerNum, x, y, x, y + 1))
                tmpTile[1] = true;

            // South East
            if (CheckTileMatch(layerNum, x, y, x + 1, y + 1))
                tmpTile[2] = true;

            // East
            if (CheckTileMatch(layerNum, x, y, x + 1, y))
                tmpTile[3] = true;

            // Calculate Situation - Inner
            if (!tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Horizontal
            if (!tmpTile[1] & tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoVertical;

            // Outer
            if (tmpTile[1] & !tmpTile[2] & tmpTile[3])
                situation = GameState.AutoOuter;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 4, "t");
                        break;
                    }
                case GameState.AutoOuter:
                    {
                        OnPlace(layerNum, x, y, 4, "d");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 4, "p");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 4, "l");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 4, "h");
                        break;
                    }
            }

        }

        // Waterfall autotiling
        private static void CalculateNW_Waterfall(int layerNum, int x, int y)
        {
            var tmpTile = default(bool);

            // West
            if (CheckTileMatch(layerNum, x, y, x - 1, y))
                tmpTile = true;

            // Actually place the subtile
            if (tmpTile)
            {
                // Extended
                OnPlace(layerNum, x, y, 1, "i");
            }
            else
            {
                // Edge
                OnPlace(layerNum, x, y, 1, "e");
            }

        }

        private static void CalculateNE_Waterfall(int layerNum, int x, int y)
        {
            var tmpTile = default(bool);

            // East
            if (CheckTileMatch(layerNum, x, y, x + 1, y))
                tmpTile = true;
            // Actually place the subtile
            if (tmpTile)
            {
                // Extended
                OnPlace(layerNum, x, y, 2, "f");
            }
            else
            {
                // Edge
                OnPlace(layerNum, x, y, 2, "j");
            }

        }

        private static void CalculateSW_Waterfall(int layerNum, int x, int y)
        {
            var tmpTile = default(bool);

            // West
            if (CheckTileMatch(layerNum, x, y, x - 1, y))
                tmpTile = true;
            // Actually place the subtile
            if (tmpTile)
            {
                // Extended
                OnPlace(layerNum, x, y, 3, "k");
            }
            else
            {
                // Edge
                OnPlace(layerNum, x, y, 3, "g");
            }

        }

        private static void CalculateSE_Waterfall(int layerNum, int x, int y)
        {
            var tmpTile = default(bool);

            // East
            if (CheckTileMatch(layerNum, x, y, x + 1, y))
                tmpTile = true;
            // Actually place the subtile
            if (tmpTile)
            {
                // Extended
                OnPlace(layerNum, x, y, 4, "h");
            }
            else
            {
                // Edge
                OnPlace(layerNum, x, y, 4, "l");
            }

        }

        // Cliff autotiling
        private static void CalculateNW_Cliff(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            byte situation;

            // North West
            if (CheckTileMatch(layerNum, x, y, x - 1, y - 1))
                tmpTile[1] = true;

            // North
            if (CheckTileMatch(layerNum, x, y, x, y - 1))
                tmpTile[2] = true;

            // West
            if (CheckTileMatch(layerNum, x, y, x - 1, y))
                tmpTile[3] = true;
            situation = GameState.AutoFill;

            // Calculate Situation - Horizontal
            if (!tmpTile[2] & tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (tmpTile[2] & !tmpTile[3])
                situation = GameState.AutoVertical;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Inner
            if (!tmpTile[2] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 1, "e");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 1, "i");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 1, "m");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 1, "q");
                        break;
                    }
            }

        }

        private static void CalculateNE_Cliff(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            byte situation;

            // North
            if (CheckTileMatch(layerNum, x, y, x, y - 1))
                tmpTile[1] = true;

            // North East
            if (CheckTileMatch(layerNum, x, y, x + 1, y - 1))
                tmpTile[2] = true;

            // East
            if (CheckTileMatch(layerNum, x, y, x + 1, y))
                tmpTile[3] = true;
            situation = GameState.AutoFill;

            // Calculate Situation - Horizontal
            if (!tmpTile[1] & tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoVertical;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Inner
            if (!tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 2, "j");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 2, "f");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 2, "r");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 2, "n");
                        break;
                    }
            }

        }

        private static void CalculateSW_Cliff(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            byte situation;

            // West
            if (CheckTileMatch(layerNum, x, y, x - 1, y))
                tmpTile[1] = true;

            // South West
            if (CheckTileMatch(layerNum, x, y, x - 1, y + 1))
                tmpTile[2] = true;

            // South
            if (CheckTileMatch(layerNum, x, y, x, y + 1))
                tmpTile[3] = true;
            situation = GameState.AutoFill;

            // Calculate Situation - Horizontal
            if (tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (!tmpTile[1] & tmpTile[3])
                situation = GameState.AutoVertical;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Inner
            if (!tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoInner;
            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 3, "o");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 3, "s");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 3, "g");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 3, "k");
                        break;
                    }
            }

        }

        private static void CalculateSE_Cliff(int layerNum, int x, int y)
        {
            var tmpTile = new bool[4];
            byte situation;

            // South
            if (CheckTileMatch(layerNum, x, y, x, y + 1))
                tmpTile[1] = true;

            // South East
            if (CheckTileMatch(layerNum, x, y, x + 1, y + 1))
                tmpTile[2] = true;

            // East
            if (CheckTileMatch(layerNum, x, y, x + 1, y))
                tmpTile[3] = true;

            situation = GameState.AutoFill;
            // Calculate Situation -  Horizontal
            if (!tmpTile[1] & tmpTile[3])
                situation = GameState.AutoHorizontal;

            // Vertical
            if (tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoVertical;

            // Fill
            if (tmpTile[1] & tmpTile[2] & tmpTile[3])
                situation = GameState.AutoFill;

            // Inner
            if (!tmpTile[1] & !tmpTile[3])
                situation = GameState.AutoInner;

            // Actually place the subtile
            switch (situation)
            {
                case GameState.AutoInner:
                    {
                        OnPlace(layerNum, x, y, 4, "t");
                        break;
                    }
                case GameState.AutoHorizontal:
                    {
                        OnPlace(layerNum, x, y, 4, "p");
                        break;
                    }
                case GameState.AutoVertical:
                    {
                        OnPlace(layerNum, x, y, 4, "l");
                        break;
                    }
                case GameState.AutoFill:
                    {
                        OnPlace(layerNum, x, y, 4, "h");
                        break;
                    }
            }

        }

        private static bool CheckTileMatch(int layerNum, int x1, int y1, int x2, int y2)
        {
            try
            {
                bool checkTileMatch = default;
                checkTileMatch = true;

                // if it's off the map then set it as autotile and exit out early
                if (x2 < 0 | x2 > Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX | y2 < 0 | y2 > Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY)
                {
                    checkTileMatch = true;
                    return checkTileMatch;
                }

                // fakes ALWAYS return true
                if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x2, y2].Layer[layerNum].AutoTile == GameState.AutotileFake)
                {
                    checkTileMatch = true;
                    return checkTileMatch;
                }

                // check neighbour is an autotile
                if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x2, y2].Layer[layerNum].AutoTile == 0)
                {
                    checkTileMatch = false;
                    return checkTileMatch;
                }

                // check we're a matching
                if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x1, y1].Layer[layerNum].Tileset != Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x2, y2].Layer[layerNum].Tileset)
                {
                    checkTileMatch = false;
                    return checkTileMatch;
                }

                // check tiles match
                if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x1, y1].Layer[layerNum].X != Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x2, y2].Layer[layerNum].X)
                {
                    checkTileMatch = false;
                    return checkTileMatch;
                }
                else if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x1, y1].Layer[layerNum].Y != Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x2, y2].Layer[layerNum].Y)
                {
                    checkTileMatch = false;
                    return checkTileMatch;
                }

                return checkTileMatch;
            }
            catch
            {
                return false;
            }
        }

        public static void OnDraw(int layerNum, int dX, int dY, int quarterNum, int x, int y, int forceFrame = 0, bool strict = true)
        {
            var yOffset = default(int);
            var xOffset = default(int);

            // calculate the offset
            if (forceFrame > 0)
            {
                switch (forceFrame - 1)
                {
                    case 0:
                        {
                            GameState.WaterfallFrame = 1;
                            break;
                        }
                    case 1:
                        {
                            GameState.WaterfallFrame = 2;
                            break;
                        }
                    case 2:
                        {
                            GameState.WaterfallFrame = 0;
                            break;
                        }
                }

                // animate autotiles
                switch (forceFrame - 1)
                {
                    case 0:
                        {
                            GameState.AutoTileFrame = 1;
                            break;
                        }
                    case 1:
                        {
                            GameState.AutoTileFrame = 2;
                            break;
                        }
                    case 2:
                        {
                            GameState.AutoTileFrame = 0;
                            break;
                        }
                }
            }

            switch (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[layerNum].AutoTile)
            {
                case GameState.AutotileWaterfall:
                    {
                        yOffset = (GameState.WaterfallFrame - 1) * 32;
                        break;
                    }
                case GameState.AutotileAnim:
                    {
                        xOffset = GameState.AutoTileFrame * 64;
                        break;
                    }
                case GameState.AutotileCliff:
                    {
                        yOffset = -32;
                        break;
                    }
            }

            if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer is null)
                return;
            string argPath = System.IO.Path.Combine(DataPath.Tilesets, Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[layerNum].Tileset.ToString());
            if (Data.Autotile is null)
                return;
            GameClient.RenderTexture(ref argPath, dX, dY, Data.Autotile[x, y].Layer[layerNum].SrcX[quarterNum] + xOffset, Data.Autotile[x, y].Layer[layerNum].SrcY[quarterNum] + yOffset, 16, 16, 16, 16);
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }
    }
}