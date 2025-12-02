using Core.Globals;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Client
{
    public class MapNpc
    {
        public static void OnClear(int index)
        {
            ref var instance = ref Data.MyMapNpc[index];
            instance.Attacking = 0;
            instance.AttackTimer = 0;
            instance.Dir = 0;
            instance.Moving = 0;
            instance.Num = -1;
            instance.SkillBuffer = -1;
            instance.Steps = 0;
            instance.Target = 0;
            instance.TargetType = 0;
            instance.Vital = new int[Enum.GetValues(typeof(Vital)).Length];
            for (int i = 0; i < Enum.GetValues(typeof(Vital)).Length; i++)
            {
                instance.Vital[i] = 0;
            }

            instance.X = 0;
            instance.Y = 0;
        }
    }
}
