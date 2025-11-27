using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public static class MapProjectile
    {
        public static void Clear(int mapNum, int mapProjectileNum)
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

    }
}
