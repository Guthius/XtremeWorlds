using System;
using System.Drawing;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using static Core.Globals.Command;
using Core.Configurations;
using Type = Core.Globals.Type;
using Core.Interfaces;

namespace Client
{
    public class Projectile : IData
    {
        #region Database

        public static void OnReset()
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
                Sender.SendRequestProjectile(projectileNum);
            }
        }

        public static void OnDraw(int index)
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

        #endregion
    }
}