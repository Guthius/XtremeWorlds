using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class MapProjectile
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

    }
}
