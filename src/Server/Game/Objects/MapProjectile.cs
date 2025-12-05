using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class MapProjectile : IData
    {
        public static void OnClear(int mapNum, int mapProjectileNum)
        {
            ref var mp = ref Data.MapProjectile[mapNum, mapProjectileNum];
            mp.ProjectileNum = -1;
            mp.Owner = 0;
            mp.OwnerType = 0;
            mp.X = 0;
            mp.Y = 0;
            mp.Dir = 0;
            mp.Vx = 0;
            mp.Vy = 0;
            mp.FreeAim = 0;
            mp.AccX = 0;
            mp.AccY = 0;
            mp.DestX = 0;
            mp.DestY = 0;
            mp.SkillId = -1;
            mp.Range = 0;
            mp.TravelTime = 0;
            mp.Timer = 0;

            NetworkSend.SendProjectileToMap(mapNum, mapProjectileNum);
        }

        public static void OnClear(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            for (int mapNum = 0; mapNum < Core.Globals.Variables.MaxMaps; mapNum++)
            {
                for (int mapProjectileNum = 0; mapProjectileNum < Data.MapProjectile.GetLength(1); mapProjectileNum++)
                {
                    OnClear(mapNum, mapProjectileNum);
                }
            }
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }
    }
}
