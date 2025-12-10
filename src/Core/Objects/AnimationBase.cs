using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Core.Objects
{
    public class AnimationBase : IData
    {
        public byte Count = 2;

        public static bool[] IsChanged { get; set; } = new bool[Variables.MaxAnimations];

        public AnimationBase()
        {
            for (int x = 0; x < Count; x++)
            {
                Sprite = new int[x + 1];
                Frames = new int[x + 1];
                Frames[x] = 5;
                LoopCount = new int[x + 1];
                LoopTime = new int[x + 1];
                LoopCount[x] = 1;
                LoopTime[x] = 1;
            }
        }

        public string Name;
        public string Sound;
        public int[] Sprite;
        public int[] Frames;
        public int[] LoopCount;
        public int[] LoopTime;

        public static List<AnimationBase> Instance { get; private set; } = new List<AnimationBase>();
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
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new AnimationBase();
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }
    }
}
