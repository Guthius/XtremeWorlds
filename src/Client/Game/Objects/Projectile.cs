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

        #region Database

        public static void OnClearAll()
        {
            int i;

            for (i = 0; i < Variables.MaxProjectiles; i++)
                OnClear(i);
        }

        public static void OnClear(int index)
        {
            Data.Projectile[index].Name = "";
            Data.Projectile[index].Sprite = 0;
            Data.Projectile[index].Range = 0;
            Data.Projectile[index].Speed = 0;
            Data.Projectile[index].Damage = 0;
            Data.Projectile[index].Animation = -1;
        }

        public static void OnStream(int projectileNum)
        {
            if (projectileNum >= 0 & string.IsNullOrEmpty(Data.Projectile[projectileNum].Name) && GameState.ProjectileLoaded[projectileNum] == 0)
            {
                GameState.ProjectileLoaded[projectileNum] = 1;
                SendRequestProjectile(projectileNum);
            }
        }

        #endregion
    }
}