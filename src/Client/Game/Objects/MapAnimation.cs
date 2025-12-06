using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using static Core.Globals.Command;

namespace Client
{
    public class MapAnimation : IData
    {
        public static byte Index;
        public static Core.Globals.Type.MapAnimation[]? Instance;

        public static void OnDraw(int index, int layer)
        {
            // Validate instance and animation
            if (MapAnimation.Instance == null || index < 0 || index >= MapAnimation.Instance.Length)
                return;

            if (GameState.MyEditorType == EditorType.Map && GameState.MapEditorTab != (byte)MapEditorTab.Tiles)
                return;

            ref var instance = ref MapAnimation.Instance[index];
            int anim = instance.Animation;
            if (anim < 0 || anim >= Variables.MaxAnimations)
                return;

            // Validate layer and arrays
            if (layer < 0)
                return;

            var animation = Animation.Instance?[anim];
            if (animation?.Sprite == null || animation.Frames == null || instance.Used == null || instance.FrameIndex == null)
                return;

            if (animation.Sprite.Length <= layer || animation.Frames.Length <= layer || instance.Used.Length <= layer || instance.FrameIndex.Length <= layer)
                return;

            if (!instance.Used[layer])
                return;

            int sprite = animation.Sprite[layer];
            if (sprite < 1 || sprite > GameState.NumAnimations)
                return;

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Animations, sprite.ToString()));
            if (gfxInfo == null)
                return;

            // Texture and frame layout (5 columns typical; dynamic height supported)
            int totalWidth = gfxInfo.Width;
            int totalHeight = gfxInfo.Height;
            int columns = animation.Frames[layer];
            if (columns <= 0)
                return;

            int frameWidth = (int)Math.Round(totalWidth / (double)columns);
            if (frameWidth <= 0)
                return;

            // Estimate row count from width-derived frame size, then compute frameHeight from total height
            int rows = Math.Max(1, (int)Math.Round(totalHeight / (double)frameWidth));
            int frameHeight = rows > 0 ? (int)Math.Round(totalHeight / (double)rows) : 0;
            if (frameHeight <= 0)
                return;
            int frameCount = Math.Max(1, rows * columns);

            // Frame index (1-based in state, convert to 0-based for drawing)
            int id1 = instance.FrameIndex[layer];
            if (id1 <= 0) id1 = 1;
            if (id1 > frameCount) id1 = frameCount;
            int zeroIndex = id1 - 1;

            int column = columns > 0 ? zeroIndex % columns : 0;
            int row = columns > 0 ? zeroIndex / columns : 0;

            var sRect = new Rectangle(column * frameWidth, row * frameHeight, frameWidth, frameHeight);

            // Determine draw position
            int x;
            int y;
            if (instance.LockType > 0)
            {
                int lockindex = instance.LockIndex;
                var point = GetLockedPosition(index, lockindex, frameWidth, frameHeight);
                x = point.X;
                y = point.Y;
            }
            else
            {
                x = (int)Math.Round(instance.X * 32 + 16 - frameWidth / 2d);
                y = (int)Math.Round(instance.Y * 32 + 16 - frameHeight / 2d);
            }

            x = GameLogic.ConvertMapX(x);
            y = GameLogic.ConvertMapY(y);

            string argPath = System.IO.Path.Combine(DataPath.Animations, sprite.ToString());
            GameClient.RenderTexture(ref argPath, x, y, sRect.X, sRect.Y, frameWidth, frameHeight, frameWidth, frameHeight);
        }

        private static Point GetLockedPosition(int index, int lockindex, int width, int height)
        {
            int x = 0;
            int y = 0;

            if (MapAnimation.Instance == null || index < 0 || index >= MapAnimation.Instance.Length)
                return new Point(x, y);

            byte lockType = MapAnimation.Instance[index].LockType;

            switch (lockType)
            {
                case (byte)TargetType.Player:
                    {
                        if (lockindex >= 0 && lockindex < Variables.MaxPlayers &&
                            IsPlaying(lockindex) && GetPlayerMap(lockindex) == GetPlayerMap(GameState.MyIndex))
                        {
                            x = (int)Math.Round(GetPlayerX(lockindex) + 16 - width / 2d);
                            y = (int)Math.Round(GetPlayerY(lockindex) + 16 - height / 2d);
                        }
                        break;
                    }
                case (byte)TargetType.Npc:
                    {
                        if (Data.MyMapNpc != null && lockindex >= 0 && lockindex < Data.MyMapNpc.Length)
                        {
                            var npc = Data.MyMapNpc[lockindex];
                            var vit = npc.Vital;
                            bool hasVitals = vit != null && vit.Length > (int)Vital.Health;
                            if (npc.Num >= 0 && hasVitals && vit![(int)Vital.Health] > 0)
                            {
                                x = (int)Math.Round(npc.X + 16 - width / 2d);
                                y = (int)Math.Round(npc.Y + 16 - height / 2d);
                            }
                        }
                        break;
                    }
            }

            return new Point(x, y);
        }

        public static void OnUpdate(int index)
        {
            // Validate instance and animation index
            if (Instance == null || index < 0 || index >= Instance.Length)
                return;

            ref var mapInstance = ref Instance[index];
            int anim = mapInstance.Animation;
            if (anim < 0 || anim >= Animation.Instance.Count)
                return;

            var instance = Animation.Instance[anim];

            // Advance each layer independently with strict bounds checks
            for (int layer = 0; layer <= 1; layer++)
            {
                // Ensure all arrays we index are present and sized
                var spriteArr = instance.Sprite;
                var framesArr = instance.Frames;
                var loopTimeArr = instance.LoopTime;
                var loopCountArr = instance.LoopCount;
                var usedArr = mapInstance.Used;
                var frameIndexArr = mapInstance.FrameIndex;
                var loopIndexArr = mapInstance.LoopIndex;
                var timerArr = mapInstance.Timer;

                if (spriteArr == null || framesArr == null || loopTimeArr == null || loopCountArr == null ||
                    usedArr == null || frameIndexArr == null || loopIndexArr == null || timerArr == null)
                    continue;

                if (spriteArr.Length <= layer || framesArr.Length <= layer || loopTimeArr.Length <= layer ||
                    loopCountArr.Length <= layer || usedArr.Length <= layer || frameIndexArr.Length <= layer ||
                    loopIndexArr.Length <= layer || timerArr.Length <= layer)
                    continue;

                if (!usedArr[layer])
                    continue;

                int sprite = spriteArr[layer];
                if (sprite < 1 || sprite > GameState.NumAnimations)
                {
                    usedArr[layer] = false;
                    continue;
                }

                var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Animations, sprite.ToString()));
                int columns = framesArr[layer];
                if (gfxInfo == null || columns <= 0)
                {
                    usedArr[layer] = false;
                    continue;
                }

                int totalWidth = gfxInfo.Width;
                int totalHeight = gfxInfo.Height;
                int frameWidth = (int)Math.Round(totalWidth / (double)columns);
                int rows = Math.Max(1, (int)Math.Round(totalHeight / (double)frameWidth));
                int frameHeight = rows > 0 ? (int)Math.Round(totalHeight / (double)rows) : 0;
                int frameCount = Math.Max(1, rows * columns);

                int loopTime = loopTimeArr[layer];
                if (frameIndexArr[layer] == 0) frameIndexArr[layer] = 1;
                if (loopIndexArr[layer] == 0) loopIndexArr[layer] = 1;

                if (timerArr[layer] + loopTime <= General.GetTickCount())
                {
                    if (frameIndexArr[layer] >= frameCount)
                    {
                        loopIndexArr[layer]++;
                        if (loopIndexArr[layer] > loopCountArr[layer])
                        {
                            usedArr[layer] = false;
                        }
                        else
                        {
                            frameIndexArr[layer] = 1;
                            var sound = instance.Sound;
                            if (!string.IsNullOrEmpty(sound))
                                Audio.PlaySound(sound, mapInstance.X, mapInstance.Y);
                        }
                    }
                    else
                    {
                        frameIndexArr[layer]++;
                    }
                    timerArr[layer] = General.GetTickCount();
                }
            }

            // If neither layer is used, clear the instance
            if (mapInstance.Used != null && mapInstance.Used.Length > 1 && !mapInstance.Used[0] && !mapInstance.Used[1])
                MapAnimation.OnClear(index);
        }

        public static int OnPlay(int sprite, int layer, int data, byte x, byte y)
        {
            if (sprite == 0)
                return 0;

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Animations, sprite.ToString()));

            if (gfxInfo == null)
                return 0;

            // Get dimensions and column count from controls and graphic info
            int totalWidth = gfxInfo.Width;
            int totalHeight = gfxInfo.Height;
            int columns = Animation.Instance[data].Frames[layer];
            int frameWidth = columns > 0 ? (int)Math.Round(totalWidth / (double)columns) : 0;
            int rows = frameWidth > 0 ? Math.Max(1, (int)Math.Round(totalHeight / (double)frameWidth)) : 1;
            int frameHeight = rows > 0 ? (int)Math.Round(totalHeight / (double)rows) : 0;
            int frameCount = rows * Math.Max(1, columns);

            OnCreate(data, x, y);
            return Animation.Instance[data].LoopTime[layer] * frameCount * Animation.Instance[data].LoopCount[layer];
        }

        public static void OnCreate(int animationNum, byte x, byte y)
        {
            string sound;
            Index = (byte)(Index + 1);
            if (Index >= byte.MaxValue)
                Index = 1;
            {
                // Ensure AnimInstance is initialized
                if (Instance == null)
                    MapAnimation.OnClear(Index);

                // Safety: if still null, bail
                if (Instance == null)
                    return;

                ref var instance = ref Instance[Index];
                // Ensure per-instance arrays exist and have at least 2 layers
                instance.Timer ??= new int[2];
                instance.Used ??= new bool[2];
                instance.LoopIndex ??= new int[2];
                instance.FrameIndex ??= new int[2];
                instance.Animation = animationNum;
                instance.X = x;
                instance.Y = y;
                instance.LockType = 0;
                instance.LockIndex = 0;
                instance.Used[0] = true;
                instance.Used[1] = true;

                sound = Animation.Instance[instance.Animation].Sound;
                if (!string.IsNullOrEmpty(sound))
                    Audio.PlaySound(sound, instance.X, instance.Y);
            }
        }

        public static void OnReset()
        {
            int i;

            Instance = new Core.Globals.Type.MapAnimation[(byte.MaxValue)];

            for (i = 0; i < byte.MaxValue; i++)
            {
                for (int x = 0; x <= 1; x++)
                    Instance[i].Timer = new int[x + 1];

                for (int x = 0; x <= 1; x++)
                    Instance[i].Used = new bool[x + 1];

                for (int x = 0; x <= 1; x++)
                    Instance[i].LoopIndex = new int[x + 1];

                for (int x = 0; x <= 1; x++)
                    Instance[i].FrameIndex = new int[x + 1];

                OnClear(i);
            }
        }

        public static void OnClear(int index)
        {
            if (Instance == null || index < 0 || index >= Instance.Length)
                return;

            ref var instance = ref Instance[index];
            instance.Animation = -1;
            instance.X = 0;
            instance.Y = 0;

            if (instance.Used != null)
            {
                for (int i = 0; i < instance.Used.Length; i++)
                    instance.Used[i] = false;
            }

            if (instance.Timer != null)
            {
                for (int i = 0; i < instance.Timer.Length; i++)
                    instance.Timer[i] = 0;
            }

            if (instance.FrameIndex != null)
            {
                for (int i = 0; i < instance.FrameIndex.Length; i++)
                    instance.FrameIndex[i] = 0;
            }

            instance.LockType = 0;
            instance.LockIndex = 0;
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
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
    }
}
