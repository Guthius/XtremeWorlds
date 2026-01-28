using Core.Globals;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Core.Objects
{
    public class AnimationBase
    {
        public byte Count = 2;

        public static bool[] IsStreaming { get; set; } = new bool[Core.Globals.Variables.MaxAnimations];
        public static bool[] IsChanged { get; set; } = new bool[Core.Globals.Variables.MaxAnimations];

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

        public string Name = string.Empty;
        public string Sound = string.Empty;
        public int[] Sprite = Array.Empty<int>();
        public int[] Frames = Array.Empty<int>();
        public int[] LoopCount = Array.Empty<int>();
        public int[] LoopTime = Array.Empty<int>();

        public static List<AnimationBase> Instance { get; private set; } = new List<AnimationBase>();

        public static void OnClearChanged()
        {
            IsChanged = new bool[Core.Globals.Variables.MaxAnimations];
            IsStreaming = new bool[Core.Globals.Variables.MaxAnimations];
        }

        public static void OnClear()
        {
            for (int i = 0; i < Instance.Count; i++)
                OnClear(i);
        }

        public static void OnClear(int index)
        {
            if (index < 0 || index >= Instance.Count)
                return;
            Instance[index] = new AnimationBase();
            IsChanged[index] = false;
            IsStreaming[index] = false;
        }
    }
}
