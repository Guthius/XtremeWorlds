using Core.Globals;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using static Core.Globals.Type;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Core.Configurations;
using Client.Game.UI;

namespace Client
{
    public class MapNpc : IData
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

  public static void OnDrawName(int mapNpcNum)
        {
            int textY;
            var color = default(Color);
            var backColor = default(Color);

            double npcNum = Data.MyMapNpc[mapNpcNum].Num;

            if (npcNum < 0 | npcNum > Variables.MaxNpcs) return;
            if (EditorType.Map == GameState.MyEditorType) return;

            switch (Data.Npc[(int)npcNum].Behavior)
            {
                case 0: color = Color.Red; backColor = Color.Black; break;
                case 1: color = Color.Green; backColor = Color.Black; break;
                case 2: color = Color.Yellow; backColor = Color.Black; break;
            }

            var remaining = Data.MyMapNpc[mapNpcNum].DeathTimer - General.GetTickCount() / 1000;
            if (remaining < 0) remaining = 0;

            var name = remaining > 0 ? $"{remaining}..." : Data.Npc[(int)npcNum].Name;

            int baseWorldX = Data.MyMapNpc[mapNpcNum].X;
            int baseWorldY = Data.MyMapNpc[mapNpcNum].Y;
            
            if (name == null) return;

            // X position: match player name centering over the tile
            var size = TextRenderer.Fonts[Font.Georgia].MeasureString(name);
            var padding = GameLogic.ConvertMapX((int)size.X / 6);
            var drawX = (int)(baseWorldX + (Constants.TileSize - size.X) / 2 + padding);

            int spriteNum = Data.Npc[(int)npcNum].Sprite;
            if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
            {
                // No valid graphic: render just above feet similar to player fallback
                int screenY = GameLogic.ConvertMapY(baseWorldY);
                textY = screenY - 16;
                TextRenderer.OnDraw(name, drawX, textY, color, backColor);
                return;
            }

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, spriteNum.ToString()));
            if (gfxInfo == null || gfxInfo.Height <= 0)
            {
                int screenY = GameLogic.ConvertMapY(baseWorldY);
                textY = screenY - 16;
                TextRenderer.OnDraw(name, drawX, textY, color, backColor);
                return;
            }

            int configuredDirs = SettingsManager.Instance.SpriteDirections;
            if (configuredDirs <= 0) configuredDirs = 4;
            configuredDirs = Math.Max(1, configuredDirs);
            int directionRows = 1;
            if (gfxInfo.Height % configuredDirs == 0) directionRows = configuredDirs;
            else if (configuredDirs != 8 && gfxInfo.Height % 8 == 0) directionRows = 8;
            else if (configuredDirs != 4 && gfxInfo.Height % 4 == 0) directionRows = 4;

            int frameHeight = gfxInfo.Height / directionRows;
            if (frameHeight <= 0) frameHeight = 32;

            int spriteTopWorldY = baseWorldY;
            if (frameHeight > 32) spriteTopWorldY = baseWorldY - (frameHeight - 32);

            int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);

            // Y position: mirror player Y logic (label-style just above head)
            int textPixelHeight = (int)Math.Ceiling(TextRenderer.Fonts[Font.Georgia].LineSpacing * TextRenderer.BaseScale);
            int margin = 8;
            textY = spriteTopScreenY - textPixelHeight + margin;
            TextRenderer.OnDraw(name, drawX, textY, color, backColor);
        }

        public static void OnDraw(int mapNpcNum)
        {
            // Segmented NPC draw mirroring player logic (Idle/Run/Attack) with fallback.
            byte anim = 0; // frame within chosen segment
            int x;
            int y;
            int sprite;
            int spriteLeft = 0; // direction row
            Rectangle rect;
            int attackSpeed = 1000; // attack duration (ms) for one full NPC attack animation cycle

            // Check if Npc exists
            if (Data.MyMapNpc[(int)mapNpcNum].Num < 0 ||
                Data.MyMapNpc[(int)mapNpcNum].Num > Variables.MaxNpcs)
                return;

            if (EditorType.Map == GameState.MyEditorType)
                return;

            x = (int)Math.Floor((double)Data.MyMapNpc[(int)mapNpcNum].X / Constants.TileSize);
            y = (int)Math.Floor((double)Data.MyMapNpc[(int)mapNpcNum].Y / Constants.TileSize);

            // Ensure Npc is within the tile view range
            if (x < GameState.TileView.Left |
                x > GameState.TileView.Right)
                return;

            if (y < GameState.TileView.Top |
                y > GameState.TileView.Bottom)
                return;

            // Stream Npc if not yet loaded
            Npc.OnStream((int)Data.MyMapNpc[(int)mapNpcNum].Num);

            if (Data.MyMapNpc[(int)mapNpcNum].Num < 0 ||
                Data.MyMapNpc[(int)mapNpcNum].Num > Variables.MaxNpcs)
                return;
                
            // Get the sprite of the Npc
            sprite = Data.Npc[(int)Data.MyMapNpc[(int)mapNpcNum].Num].Sprite;

            // Validate sprite
            if (sprite < 1 | sprite > GameState.NumCharacters)
                return;

            // Timing flags
            long tick = General.GetTickCount();
            bool isAttacking = Data.MyMapNpc[mapNpcNum].Attacking == 1; // treat full attack duration as attack
            bool provisionalMoving = Data.MyMapNpc[mapNpcNum].Moving != 0;

            // Reset attacking state if attack timer has passed
            {
                ref var instance = ref Data.MyMapNpc[(int)mapNpcNum];
                if (instance.AttackTimer + attackSpeed < General.GetTickCount())
                {
                    instance.Attacking = 0;
                    instance.AttackTimer = 0;
                }
            }

            // Segmentation logic
            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, sprite.ToString()));
            if (gfxInfo == null) return;
            int directionRows = GameClient.ComputeDirectionRows(gfxInfo.Height, Math.Max(1, SettingsManager.Instance.SpriteDirections));

            // Map direction to row after computing available rows
            spriteLeft = GameClient.MapDirectionToRow((Direction)Data.MyMapNpc[(int)mapNpcNum].Dir, directionRows);
            int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
            int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
            int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
            int expectedTotalColumns = idleFrames + runFrames + attackFrames;

            int frameHeight = gfxInfo.Height / directionRows;
            if (frameHeight <= 0) return;
            int autoColsBySquare = frameHeight > 0 ? gfxInfo.Width / frameHeight : 0;
            if (autoColsBySquare <= 0) autoColsBySquare = 1;
            bool widthDivisible = expectedTotalColumns > 0 && gfxInfo.Width % expectedTotalColumns == 0;
            int candidateFrameWidth = widthDivisible ? gfxInfo.Width / expectedTotalColumns : 0;
            
            // Relaxed segmentation: if width is divisible by expected columns we segment, even if not perfectly square.
            bool canSegment = widthDivisible; // old heuristic removed to prevent cycling through all segments linearly
            int frameColumnsForWidth = canSegment ? expectedTotalColumns : autoColsBySquare; // legacy fallback

            // Dynamic segment ordering via settings
            string orderCsv = SettingsManager.Instance.SpriteSegmentOrder ?? "idle,run,attack";
            var tokens = orderCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 3)
                tokens = new[] { "idle", "run", "attack" };
            for (int i = 0; i < tokens.Length; i++) tokens[i] = tokens[i].Trim().ToLowerInvariant();
            if (!(tokens.Contains("idle") && tokens.Contains("run") && tokens.Contains("attack")))
                tokens = new[] { "idle", "run", "attack" };
            int runningOffset = 0;
            int idleOffset = 0, runOffset = 0, attackOffset = 0;
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (t == "idle") idleOffset = runningOffset;
                else if (t == "run") runOffset = runningOffset;
                else if (t == "attack") attackOffset = runningOffset;
                if (t == "idle") runningOffset += idleFrames;
                else if (t == "run") runningOffset += runFrames;
                else if (t == "attack") runningOffset += attackFrames;
            }

            // Moving only meaningful if segmented sheet
            bool isMoving = provisionalMoving && !isAttacking && canSegment;

            // Determine frame inside its segment (Steps driven for run; idle frame stays 0)
            if (canSegment)
            {
                if (isAttacking)
                {
                    long elapsed = tick - Data.MyMapNpc[mapNpcNum].AttackTimer;
                    if (elapsed < 0) elapsed = 0;
                    long duration = attackSpeed;
                    if (duration <= 0) duration = 1;
                    if (elapsed >= duration) elapsed = duration - 1;
                    double ratio = elapsed / (double)duration;
                    int frame = (int)(ratio * attackFrames);
                    if (frame >= attackFrames) frame = attackFrames - 1;
                    anim = (byte)frame;
                }
                else if (isMoving)
                {
                    anim = (byte)(Data.MyMapNpc[mapNpcNum].Steps % runFrames);
                }
                else
                {
                    anim = (byte)(Data.MyMapNpc[mapNpcNum].Steps % idleFrames); // idle animated
                }
            }
            else
            {
                anim = (byte)(Data.MyMapNpc[mapNpcNum].Steps % frameColumnsForWidth);
            }

            // Frame placement
            int segmentOffset = 0;
            if (canSegment)
            {
                if (isAttacking) segmentOffset = attackOffset;
                else if (isMoving) segmentOffset = runOffset;
                else segmentOffset = idleOffset;
            }
            
            int frameColumn = Math.Min(frameColumnsForWidth - 1, segmentOffset + anim);
            double frameWidth = gfxInfo.Width / (double)frameColumnsForWidth;
            double frameHeightD = frameHeight;
            rect = new Rectangle(
                (int)Math.Round(frameColumn * frameWidth),
                (int)Math.Round(spriteLeft * frameHeightD),
                (int)Math.Round(frameWidth),
                (int)Math.Round(frameHeightD));

            // X/Y positioning
            x = (int)Math.Round(Data.MyMapNpc[mapNpcNum].X - (gfxInfo.Width / (double)frameColumnsForWidth - 32d) / 2d);
            if ((gfxInfo.Height / directionRows) > 32)
                y = (int)Math.Round(Data.MyMapNpc[mapNpcNum].Y - (gfxInfo.Height / (double)directionRows - 32d));
            else
                y = Data.MyMapNpc[mapNpcNum].Y;

            GameClient.DrawCharacterSprite(sprite, x, y, rect);
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            throw new NotImplementedException();
        }

        public static void OnStream(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }
    }
}
