using System;
using System.Drawing;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using static Core.Globals.Commands;
using Core.Configurations;
using Type = Core.Globals.Type;
using Core.Interfaces;
using Core.Objects;

namespace Client
{
    public class Projectile : ProjectileBase, IStreamable
    {
        #region Database

        public static void OnStream(int index)
{           if (index < 0 || index >= Core.Globals.Variables.MaxProjectiles) return;
            if (Projectile.Instance.Count <= index)
                Sender.SendRequestProjectile(index);
        }

        #endregion
    }
}