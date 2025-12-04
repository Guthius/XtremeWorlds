using Core.Configurations;
using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class MapProjectile : IData
    {
        public static void OnClear(int projectileNum)
        {
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].ProjectileNum = -1;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Owner = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].OwnerType = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].X = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Y = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Dir = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Vx = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Vy = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].FreeAim = 0;
            Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Timer = 0;
        }


        public static void OnDraw(int projectileNum)
        {
            Core.Globals.Type.Rect rec;
            int x;
            int y;
            int sprite;

            // Defensive: ensure projectile index within bounds
            if (projectileNum < 0 || projectileNum >= Variables.MaxProjectiles)
            {
                return;
            }

            // Defensive: ensure player index and map index are valid before indexing map projectile array
            if (GameState.MyIndex < 0 || GameState.MyIndex > Variables.MaxPlayers)
            {
                return;
            }

            int mapId = Data.Player[GameState.MyIndex].Map;
            if (mapId < 0 || mapId >= Data.MapProjectile.GetLength(0))
            {
                return;
            }

            Projectile.OnStream(projectileNum);

            x = (int)Math.Floor((double)Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].X / Constants.TileSize);
            y = (int)Math.Floor((double)Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Y / Constants.TileSize);

            // Check if its been going for over 1 minute, if so clear.
            if (Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Timer < General.GetTickCount())
                return;

            if (x > Data.MyMap.MaxX | x < 0)
                return;

            if (y > Data.MyMap.MaxY | y < 0)
                return;

            int projectile = Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].ProjectileNum;
            if (projectile < 0 || projectile >= Data.Projectile.Length)
            {
                return;
            }

            sprite = Data.Projectile[projectile].Sprite;
            if (sprite < 1 || sprite > GameState.NumProjectiles)
            {
                return;
            }

            // src rect
            rec.Top = 0d;
            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Projectiles, sprite.ToString()));
            if (gfxInfo == null)
            {
                return;
            }

            rec.Bottom = gfxInfo.Height;
            // 8-direction spritesheet assumed with 8 columns in order:
            // 0: Up, 1: Down, 2: Left, 3: Right, 4: UpRight, 5: UpLeft, 6: DownRight, 7: DownLeft
            // If the sheet has fewer than 8 columns, fall back to 4-direction mapping.
            int col = 0;
            var mp = Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum];
            var dir = mp.Dir;
            int cols = Math.Max(1, gfxInfo.Width / Constants.TileSize);
            bool eightDirEnabled = SettingsManager.Instance.SpriteDirections >= 8;
            if (cols >= 8 && eightDirEnabled)
            {
                switch (dir)
                {
                    case (byte)Direction.Up: col = 0; break;
                    case (byte)Direction.Down: col = 1; break;
                    case (byte)Direction.Left: col = 2; break;
                    case (byte)Direction.Right: col = 3; break;
                    case (byte)Direction.UpRight: col = 4; break;
                    case (byte)Direction.UpLeft: col = 5; break;
                    case (byte)Direction.DownRight: col = 6; break;
                    case (byte)Direction.DownLeft: col = 7; break;
                    default: col = 1; break; // default to Down
                }
            }
            else
            {
                // 4-dir fallback (Up=0, Down=1, Left=2, Right=3) — diagonals map to nearest cardinal
                switch (dir)
                {
                    case (byte)Direction.Down:
                    case (byte)Direction.DownLeft:
                    case (byte)Direction.DownRight:
                        col = 1; break;
                    case (byte)Direction.Right:
                        col = 3; break;
                    case (byte)Direction.Left:
                        col = 2; break;
                    case (byte)Direction.UpRight:
                    case (byte)Direction.UpLeft:
                    case (byte)Direction.Up:
                    default:
                        col = 0; break;
                }
            }
            rec.Left = Constants.TileSize * Math.Clamp(col, 0, cols - 1);

            rec.Right = rec.Left + Constants.TileSize;

            // Convert coordinates
            x = GameLogic.ConvertMapX(Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].X);
            y = GameLogic.ConvertMapY(Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Y);

            // Render texture
            string argPath = System.IO.Path.Combine(DataPath.Projectiles, sprite.ToString());
            GameClient.RenderTexture(ref argPath, x, y, (int)rec.Left, (int)rec.Top, 32, 32, 32, 32);
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            for (int i = 0; i < Data.MapProjectile.GetLength(1); i++)
                OnClear(i);
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }
    }
}
