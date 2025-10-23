using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using static Core.Globals.Command;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Type = Core.Globals.Type;

namespace Client
{
    public class Animation
    {

        #region Drawing
        public static void Draw(int index, int layer)
        {
            // Validate instance and animation
            if (AnimInstance == null || index < 0 || index >= AnimInstance.Length)
                return;

            if (GameState.MyEditorType == EditorType.Map && GameState.MapEditorTab != (byte)MapEditorTab.Tiles)
                return;

            ref var inst = ref AnimInstance[index];
            int animIdx = inst.Animation;
            if (animIdx < 0 || animIdx >= Data.Animation.Length)
                return;

            // Validate layer and arrays
            if (layer < 0)
                return;

            var anim = Data.Animation[animIdx];
            if (anim.Sprite == null || anim.Frames == null || inst.Used == null || inst.FrameIndex == null)
                return;

            if (anim.Sprite.Length <= layer || anim.Frames.Length <= layer || inst.Used.Length <= layer || inst.FrameIndex.Length <= layer)
                return;

            if (!inst.Used[layer])
                return;

            int sprite = anim.Sprite[layer];
            if (sprite < 1 || sprite > GameState.NumAnimations)
                return;

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Animations, sprite.ToString()));
            if (gfxInfo == null)
                return;

            // Texture and frame layout (5 columns typical; dynamic height supported)
            int totalWidth = gfxInfo.Width;
            int totalHeight = gfxInfo.Height;
            int columns = anim.Frames[layer];
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
            int idx1 = inst.FrameIndex[layer];
            if (idx1 <= 0) idx1 = 1;
            if (idx1 > frameCount) idx1 = frameCount;
            int zeroIndex = idx1 - 1;

            int column = columns > 0 ? zeroIndex % columns : 0;
            int row = columns > 0 ? zeroIndex / columns : 0;

            var sRect = new Rectangle(column * frameWidth, row * frameHeight, frameWidth, frameHeight);

            // Determine draw position
            int x;
            int y;
            if (inst.LockType > 0)
            {
                int lockindex = inst.LockIndex;
                var point = GetLockedPosition(index, lockindex, frameWidth, frameHeight);
                x = point.X;
                y = point.Y;
            }
            else
            {
                x = (int)Math.Round(inst.X * 32 + 16 - frameWidth / 2d);
                y = (int)Math.Round(inst.Y * 32 + 16 - frameHeight / 2d);
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

            if (AnimInstance == null || index < 0 || index >= AnimInstance.Length)
                return new Point(x, y);

            byte lockType = AnimInstance[index].LockType;

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

        public static void CheckAnimInstance(int index)
        {
            // Validate instance and animation index
            if (AnimInstance == null || index < 0 || index >= AnimInstance.Length)
                return;

            ref var inst = ref AnimInstance[index];
            int animIdx = inst.Animation;
            if (animIdx < 0 || animIdx >= Data.Animation.Length)
                return;

            var anim = Data.Animation[animIdx];
            StreamAnimation(animIdx);

            // Advance each layer independently with strict bounds checks
            for (int layer = 0; layer <= 1; layer++)
            {
                // Ensure all arrays we index are present and sized
                var spriteArr = anim.Sprite;
                var framesArr = anim.Frames;
                var loopTimeArr = anim.LoopTime;
                var loopCountArr = anim.LoopCount;
                var usedArr = inst.Used;
                var frameIndexArr = inst.FrameIndex;
                var loopIndexArr = inst.LoopIndex;
                var timerArr = inst.Timer;

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
                            var sound = anim.Sound;
                            if (!string.IsNullOrEmpty(sound))
                                Sound.PlaySound(sound, inst.X, inst.Y);
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
            if (inst.Used != null && inst.Used.Length > 1 && !inst.Used[0] && !inst.Used[1])
                ClearAnimInstance(index);
        }

        public static int PlayAnimation(int sprite, int layer, int data, byte x, byte y)
        {
            Animation.StreamAnimation(data);

            if (sprite == 0)
                return 0;

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(DataPath.Animations, sprite.ToString()));

            if (gfxInfo == null)
                return 0;        

            // Get dimensions and column count from controls and graphic info
            int totalWidth = gfxInfo.Width;
            int totalHeight = gfxInfo.Height;
            int columns = Data.Animation[data].Frames[layer];
            int frameWidth = columns > 0 ? (int)Math.Round(totalWidth / (double)columns) : 0;
            int rows = frameWidth > 0 ? Math.Max(1, (int)Math.Round(totalHeight / (double)frameWidth)) : 1;
            int frameHeight = rows > 0 ? (int)Math.Round(totalHeight / (double)rows) : 0;
            int frameCount = rows * Math.Max(1, columns);

            Animation.CreateAnimation(data, x, y);
            return Data.Animation[data].LoopTime[layer] * frameCount * Data.Animation[data].LoopCount[layer];
        }

        public static void CreateAnimation(int animationNum, byte x, byte y)
        {
            string sound;
            AnimationIndex = (byte)(AnimationIndex + 1);
            if (AnimationIndex >= byte.MaxValue)
                AnimationIndex = 1;

            {
                // Ensure AnimInstance is initialized
                if (AnimInstance == null)
                    ClearAnimInstances();

                // Safety: if still null, bail
                if (AnimInstance == null)
                    return;

                ref var withBlock = ref AnimInstance[AnimationIndex];
                // Ensure per-instance arrays exist and have at least 2 layers
                withBlock.Timer ??= new int[2];
                withBlock.Used ??= new bool[2];
                withBlock.LoopIndex ??= new int[2];
                withBlock.FrameIndex ??= new int[2];
                withBlock.Animation = animationNum;
                withBlock.X = x;
                withBlock.Y = y;
                withBlock.LockType = 0;
                withBlock.LockIndex = 0;
                withBlock.Used[0] = true;
                withBlock.Used[1] = true;

                sound = Data.Animation[withBlock.Animation].Sound;
                if (!string.IsNullOrEmpty(sound))
                    Sound.PlaySound(sound, withBlock.X, withBlock.Y);
            }
        }

        #endregion

        #region Globals

        public static byte AnimationIndex;
        public static Type.AnimInstance[]? AnimInstance;

        #endregion

        #region Database

        public static void ClearAnimation(int index)
        {
            Data.Animation[index] = default;
            Data.Animation[index] = new Type.Animation();

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].Sprite = new int[x + 1];

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].Frames = new int[x + 1];

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].Frames[x] = 5;

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].LoopCount = new int[x + 1];

            for (int x = 0; x <= 1; x++)
                Data.Animation[index].LoopTime = new int[x + 1];

            Data.Animation[index].Name = "";
            Data.Animation[index].LoopCount[0] = 1;
            Data.Animation[index].LoopCount[1] = 1;
            Data.Animation[index].LoopTime[0] = 1;
            Data.Animation[index].LoopTime[1] = 1;
            GameState.AnimationLoaded[index] = 0;
        }

        public static void ClearAnimations()
        {
            int i;

            Data.Animation = new Type.Animation[Variables.MaxAnimations];

            for (i = 0; i < Variables.MaxAnimations; i++)
                ClearAnimation(i);
        }

        public static void ClearAnimInstances()
        {
            int i;

            AnimInstance = new Type.AnimInstance[(byte.MaxValue)];

            for (i = 0; i < byte.MaxValue; i++)
            {
                for (int x = 0; x <= 1; x++)
                    AnimInstance[i].Timer = new int[x + 1];

                for (int x = 0; x <= 1; x++)
                    AnimInstance[i].Used = new bool[x + 1];

                for (int x = 0; x <= 1; x++)
                    AnimInstance[i].LoopIndex = new int[x + 1];

                for (int x = 0; x <= 1; x++)
                    AnimInstance[i].FrameIndex = new int[x + 1];

                ClearAnimInstance(i);
            }
        }

        public static void ClearAnimInstance(int index)
        {
            if (AnimInstance == null || index < 0 || index >= AnimInstance.Length)
                return;

            ref var inst = ref AnimInstance[index];
            inst.Animation = -1;
            inst.X = 0;
            inst.Y = 0;

            if (inst.Used != null)
            {
                for (int i = 0; i < inst.Used.Length; i++)
                    inst.Used[i] = false;
            }

            if (inst.Timer != null)
            {
                for (int i = 0; i < inst.Timer.Length; i++)
                    inst.Timer[i] = 0;
            }

            if (inst.FrameIndex != null)
            {
                for (int i = 0; i < inst.FrameIndex.Length; i++)
                    inst.FrameIndex[i] = 0;
            }

            inst.LockType = 0;
            inst.LockIndex = 0;
        }

        public static void StreamAnimation(int animationNum)
        {
            if (animationNum >= 0 && string.IsNullOrEmpty(Data.Animation[animationNum].Name) && GameState.AnimationLoaded[animationNum] == 0)
            {
                GameState.AnimationLoaded[animationNum] = 1;
                SendRequestAnimation(animationNum);
            }
        }

        #endregion

        #region Incoming Traffic

        public static void Packet_UpdateAnimation(ReadOnlyMemory<byte> data)
        {
            int n;
            int i;
            var buffer = new PacketReader(data);

            n = buffer.ReadInt32();
            // Update the Animation
            for (i = 0; i < Data.Animation[n].Frames.Length; i++)
                Data.Animation[n].Frames[i] = buffer.ReadInt32();

            for (i = 0; i < Data.Animation[n].LoopCount.Length; i++)
                Data.Animation[n].LoopCount[i] = buffer.ReadInt32();

            for (i = 0; i < Data.Animation[n].LoopTime.Length; i++)
                Data.Animation[n].LoopTime[i] = buffer.ReadInt32();

            Data.Animation[n].Name = buffer.ReadString();
            Data.Animation[n].Sound = buffer.ReadString();

            for (i = 0; i < Data.Animation[n].Sprite.Length; i++)
                Data.Animation[n].Sprite[i] = buffer.ReadInt32();
        }

        public static void Packet_Animation(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);

            AnimationIndex = (byte)(AnimationIndex + 1);
            if (AnimationIndex >= byte.MaxValue)
                AnimationIndex = 1;

            {
                if (AnimInstance == null)
                    ClearAnimInstances();
                if (AnimInstance == null)
                    return;

                ref var withBlock = ref AnimInstance[AnimationIndex];
                withBlock.Timer ??= new int[2];
                withBlock.Used ??= new bool[2];
                withBlock.LoopIndex ??= new int[2];
                withBlock.FrameIndex ??= new int[2];
                withBlock.Animation = buffer.ReadInt32();
                withBlock.X = buffer.ReadInt32();
                withBlock.Y = buffer.ReadInt32();
                withBlock.LockType = (byte)buffer.ReadInt32();
                withBlock.LockIndex = buffer.ReadInt32();
                withBlock.Used[0] = true;
                withBlock.Used[1] = true;
            }
        }

        #endregion
        #region Outgoing Traffic

        public static void SendRequestAnimation(int animationNum)
        {
            var packetWriter = new PacketWriter(8);

            packetWriter.WriteEnum(Packets.ClientPackets.CRequestAnimation);
            packetWriter.WriteInt32(animationNum);

            Network.Send(packetWriter);
        }

        #endregion

    }
}