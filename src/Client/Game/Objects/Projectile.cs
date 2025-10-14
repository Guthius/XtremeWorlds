using System;
using System.Drawing;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using static Core.Globals.Command;
using Core.Configurations;
using Type = Core.Globals.Type;

namespace Client
{
    public class Projectile
    {
        #region Sending

        public static void SendRequestEditProjectiles()
        {
            var packetWriter = new PacketWriter(4);

            packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditProjectile);

            Network.Send(packetWriter);
        }

        public static void SendSaveProjectile(int projectileNum)
        {
            var packetWriter = new PacketWriter();

            packetWriter.WriteEnum(Packets.ClientPackets.CSaveProjectile);
            packetWriter.WriteInt32(projectileNum);
            packetWriter.WriteString(Data.Projectile[projectileNum].Name);
            packetWriter.WriteInt32(Data.Projectile[projectileNum].Sprite);
            packetWriter.WriteInt32(Data.Projectile[projectileNum].Range);
            packetWriter.WriteInt32(Data.Projectile[projectileNum].Speed);
            packetWriter.WriteInt32(Data.Projectile[projectileNum].Damage);
            packetWriter.WriteInt32(Data.Projectile[projectileNum].Animation);

            Network.Send(packetWriter);
        }

        public static void SendRequestProjectile(int projectileNum)
        {
            var packetWriter = new PacketWriter(8);

            packetWriter.WriteEnum(Packets.ClientPackets.CRequestProjectile);
            packetWriter.WriteInt32(projectileNum);

            Network.Send(packetWriter);
        }

        public static void SendClearProjectile(int projectileNum, int collisionindex, byte collisionType, int collisionZone)
        {
            var packetWriter = new PacketWriter(20);

            packetWriter.WriteEnum(Packets.ClientPackets.CClearProjectile);
            packetWriter.WriteInt32(projectileNum);
            packetWriter.WriteInt32(collisionindex);
            packetWriter.WriteInt32(collisionType);
            packetWriter.WriteInt32(collisionZone);

            Network.Send(packetWriter);
        }

        #endregion

        #region Recieving

        public static void HandleUpdateProjectile(ReadOnlyMemory<byte> data)
        {
            int projectileNum;
            var buffer = new PacketReader(data);
            projectileNum = buffer.ReadInt32();

            Data.Projectile[projectileNum].Name = buffer.ReadString();
            Data.Projectile[projectileNum].Sprite = buffer.ReadInt32();
            Data.Projectile[projectileNum].Range = (byte) buffer.ReadInt32();
            Data.Projectile[projectileNum].Speed = buffer.ReadInt32();
            Data.Projectile[projectileNum].Damage = buffer.ReadInt32();
            Data.Projectile[projectileNum].Animation = buffer.ReadInt32();
        }

        public static void HandleMapProjectile(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);
            int i = buffer.ReadInt32();

            {
                ref var withBlock = ref Data.MapProjectile[Data.Player[GameState.MyIndex].Map, i];
                withBlock.ProjectileNum = buffer.ReadInt32();
                withBlock.Owner = buffer.ReadInt32();
                withBlock.OwnerType = buffer.ReadByte();
                withBlock.Dir = buffer.ReadByte();
                withBlock.X = buffer.ReadInt32();
                withBlock.Y = buffer.ReadInt32();
                // New free-aim fields
                withBlock.Vx = buffer.ReadInt16();
                withBlock.Vy = buffer.ReadInt16();
                withBlock.FreeAim = buffer.ReadByte();
                withBlock.Range = 0;
                withBlock.Timer = General.GetTickCount() + 60000;
            }
        }

        #endregion

        #region Database

        public static void ClearProjectile()
        {
            int i;

            for (i = 0; i < Constant.MaxProjectiles; i++)
                ClearProjectile(i);
        }

        public static void ClearProjectile(int index)
        {
            Data.Projectile[index].Name = "";
            Data.Projectile[index].Sprite = 0;
            Data.Projectile[index].Range = 0;
            Data.Projectile[index].Speed = 0;
            Data.Projectile[index].Damage = 0;
            Data.Projectile[index].Animation = -1;
        }

        public static void ClearMapProjectile(int projectileNum)
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

        public static void StreamProjectile(int projectileNum)
        {
            if (projectileNum >= 0 & string.IsNullOrEmpty(Data.Projectile[projectileNum].Name) && GameState.ProjectileLoaded[projectileNum] == 0)
            {
                GameState.ProjectileLoaded[projectileNum] = 1;
                SendRequestProjectile(projectileNum);
            }
        }

        #endregion

        #region Drawing

        public static void DrawProjectile(int projectileNum)
        {
            Type.Rect rec;
            int x;
            int y;
            int sprite;

            // Defensive: ensure projectile index within bounds
            if (projectileNum < 0 || projectileNum >= Constant.MaxProjectiles)
            {
                return;
            }

            // Defensive: ensure player index and map index are valid before indexing map projectile array
            if (GameState.MyIndex < 0 || GameState.MyIndex > Constant.MaxPlayers)
            {
                return;
            }
            
            int mapId = Data.Player[GameState.MyIndex].Map;
            if (mapId < 0 || mapId >= Data.MapProjectile.GetLength(0))
            {
                return;
            }

            StreamProjectile(projectileNum);

            x = (int)Math.Floor((double)Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].X / 32);
            y = (int)Math.Floor((double)Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Y / 32);

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
            int cols = Math.Max(1, gfxInfo.Width / GameState.SizeX);
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
            rec.Left = GameState.SizeX * Math.Clamp(col, 0, cols - 1);
           
            rec.Right = rec.Left + GameState.SizeX;
            
            // Convert coordinates
            x = GameLogic.ConvertMapX(Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].X);
            y = GameLogic.ConvertMapY(Data.MapProjectile[Data.Player[GameState.MyIndex].Map, projectileNum].Y);

            // Render texture
            string argPath = System.IO.Path.Combine(DataPath.Projectiles, sprite.ToString());
            GameClient.RenderTexture(ref argPath, x, y, (int) rec.Left, (int) rec.Top, 32, 32, 32, 32);
        }

        #endregion
    }
}