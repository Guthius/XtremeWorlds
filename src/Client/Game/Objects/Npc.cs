using Client.Game.UI;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using System;

namespace Client
{
    /// <summary>
    /// NPC movement processing (tile-based with pixel offsets).
    /// Improvements:
    /// - Safer bounds checks + clamping (no silent early-returns).
    /// - Correctly commits the final step onto a tile boundary (fixes "stuck at 31 px" issue).
    /// - Optional per-tick pixel step and delta-time overloads.
    /// - Helper utilities for bulk updates and tile alignment callbacks.
    /// - Clear, centralized constants (TileSize).
    /// </summary>
    public static class Npc
    {
        // Client-side prediction helpers: track remaining pixels and destination for current tile step.
        private static readonly int[] RemainingPixels = new int[Variables.MaxMapNpcs];
        private static readonly int[] DestX = new int[Variables.MaxMapNpcs];
        private static readonly int[] DestY = new int[Variables.MaxMapNpcs];

        // Run animation finishing support: after movement stops, keep rendering the run segment
        // until the current cycle completes (based on Steps and 250ms cadence).
        private static readonly long[] StopTick = new long[Variables.MaxMapNpcs];
        private static readonly long[] FinishUntil = new long[Variables.MaxMapNpcs];
        private const int StepsCadenceMs = 250; // matches Loop.cs _tmr250 cadence

        /// <summary>
        /// Call when an NPC begins moving (SNpcMove). Clears any pending finish-tail.
        /// </summary>
        public static void MarkMoveStart(int index)
        {
            if (index < 0 || index >= Variables.MaxMapNpcs) return;
            StopTick[index] = 0;
            FinishUntil[index] = 0;
        }

        /// <summary>
        /// Initializes a new 1-tile step starting from the given start position and direction.
        /// Also clears any pending finish-tail.
        /// </summary>
        public static void StartStep(int index, int startX, int startY, byte dir)
        {
            if (index < 0 || index >= Variables.MaxMapNpcs) return;
            // Reset finish-tail
            StopTick[index] = 0;
            FinishUntil[index] = 0;
            // Initialize movement bookkeeping
            RemainingPixels[index] = Constants.TileSize;
            var (fullDx, fullDy) = GetDirectionDelta(dir, Constants.TileSize);
            DestX[index] = startX + fullDx;
            DestY[index] = startY + fullDy;
        }

        /// <summary>
        /// Call when an NPC stops moving (SNpcDir). Records the stop time; actual finish window is computed lazily in Draw.
        /// </summary>
        public static void MarkMoveStop(int index)
        {
            if (index < 0 || index >= Variables.MaxMapNpcs) return;
            StopTick[index] = General.GetTickCount();
            FinishUntil[index] = 0; // will be set on first ShouldRenderRun call
        }

        /// <summary>
        /// Snap NPC to its last planned tile destination (used when receiving SNpcDir at tile end).
        /// </summary>
        public static void SnapToDest(int index)
        {
            if (index < 0 || index >= Variables.MaxMapNpcs) return;
            if (Data.MyMapNpc == null) return;
            ref var npc = ref Data.MyMapNpc[index];
            if (RemainingPixels[index] > 0)
            {
                npc.X = DestX[index];
                npc.Y = DestY[index];
                RemainingPixels[index] = 0;
            }
        }

        /// <summary>
        /// Determines if we should keep rendering the run segment after movement stopped
        /// to complete the current run cycle. Uses Steps modulo runFrames to compute remaining frames.
        /// </summary>
        /// <param name="index">NPC index</param>
        /// <param name="runFrames">Number of frames in the run segment (>=1)</param>
        /// <param name="tick">Current tick count</param>
        /// <param name="steps">Current Steps counter for this NPC</param>
        public static bool ShouldRenderRun(int index, int runFrames, long tick, int steps)
        {
            if (index < 0 || index >= Variables.MaxMapNpcs) return false;
            if (runFrames <= 1) return false; // nothing to finish
            if (Data.MyMapNpc[index].Moving != 0) return false; // currently moving, not finishing

            // If we haven't observed a stop, nothing to finish
            long stoppedAt = StopTick[index];
            if (stoppedAt <= 0) return false;

            // Initialize finish window if not yet computed
            if (FinishUntil[index] <= 0)
            {
                int shown = steps % runFrames;
                int remaining = (runFrames - shown) % runFrames;
                if (remaining <= 0)
                {
                    // No remaining frames; nothing to finish
                    StopTick[index] = 0;
                    return false;
                }
                FinishUntil[index] = stoppedAt + remaining * StepsCadenceMs;
            }

            if (tick < FinishUntil[index])
            {
                return true;
            }

            // Finished tail; clear markers
            StopTick[index] = 0;
            FinishUntil[index] = 0;
            return false;
        }

        /// <summary>
        /// Raised when an NPC lands exactly on a tile boundary (x % 32 == 0 && y % 32 == 0).
        /// Args: (npcIndex, tileX, tileY)
        /// </summary>
        public static event Action<int, int, int>? OnTileAligned;

        /// <summary>
        /// Processes one NPC by index (from a legacy double), moving by 1 pixel per tick.
        /// </summary>
        public static void OnMove(double mapNpcNum) => OnMove((int)mapNpcNum, 1);

        /// <summary>
        /// Processes one NPC by index, moving by a configurable number of pixels per tick.
        /// </summary>
        /// <param name="index">NPC array index (0..MaxMapNpcs-1).</param>
        /// <param name="pixelsPerTick">How many pixels to move this tick (>=1).</param>
        public static void OnMove(int index, int pixelsPerTick)
        {
            if (index < 0 || index >= Variables.MaxMapNpcs) return;
            if (Data.MyMapNpc == null) return;

            ref var npc = ref Data.MyMapNpc[index];

            // Only process active walking state
            if (npc.Moving != (byte)MovementState.Walking)
            {
                RemainingPixels[index] = 0;
                return;
            }

            // Current pixel position
            int x = npc.X;
            int y = npc.Y;

            // Initialize a new tile step if just started (RemainingPixels == 0)
            if (RemainingPixels[index] <= 0)
            {
                RemainingPixels[index] = Constants.TileSize;
                var (fullDx, fullDy) = GetDirectionDelta(npc.Dir, Constants.TileSize);
                DestX[index] = npc.X + fullDx;
                DestY[index] = npc.Y + fullDy;
            }

            var step = Math.Max(1, pixelsPerTick);
            var (dx, dy) = GetDirectionDelta(npc.Dir, step);

            // Apply delta
            int newX = x + dx;
            int newY = y + dy;

            // Keep within 0 .. (Max-1) * TileSize inclusive to match the original coordinate convention.
            int maxXpx = Math.Max(0, (Data.MyMap.MaxX - 1) * Constants.TileSize);
            int maxYpx = Math.Max(0, (Data.MyMap.MaxY - 1) * Constants.TileSize);

            newX = Math.Clamp(newX, 0, maxXpx);
            newY = Math.Clamp(newY, 0, maxYpx);
            
            // Commit the move (IMPORTANT: commit BEFORE the "aligned" check so we don't get stuck at 31px!)
            npc.X = newX;
            npc.Y = newY;
            RemainingPixels[index] -= step;

            // If we've landed exactly on a tile boundary, notify listeners (e.g., to advance path, stop walking, etc.)
            if (RemainingPixels[index] <= 0 || ((newX % Constants.TileSize == 0) && (newY % Constants.TileSize == 0)))
            {
                // Snap to destination to ensure perfect alignment (server authoritative end already snapped)
                npc.X = DestX[index];
                npc.Y = DestY[index];
                RemainingPixels[index] = 0;
                var tileX = npc.X / Constants.TileSize;
                var tileY = npc.Y / Constants.TileSize;
                OnTileAligned?.Invoke(index, tileX, tileY);

                // If your project requires stopping at tile boundaries, do it here:
                // npc.Moving = (byte)MovementState.Idle; // <-- uncomment & adjust if you have an Idle/Standing state
            }
        }

        /// <summary>
        /// Delta-time aware variant (moves by (speedPxPerSec * dt) rounded to at least 1 px).
        /// </summary>
        /// <param name="index">NPC index.</param>
        /// <param name="speedPxPerSecond">Speed in pixels per second.</param>
        /// <param name="deltaTimeSeconds">Elapsed seconds since last tick.</param>
        public static void OnMoveDt(int index, float speedPxPerSecond, float deltaTimeSeconds)
        {
            var px = Math.Max(1, (int)MathF.Round(MathF.Abs(speedPxPerSecond) * MathF.Max(0.0f, deltaTimeSeconds)));
            OnMove(index, px);
        }

        /// <summary>
        /// Convenience: process all map NPCs (1 px per tick).
        /// </summary>
        public static void ProcessAll()
        {
            for (int i = 0; i < Variables.MaxMapNpcs; i++)
                OnMove(i, 1);
        }

        /// <summary>
        /// Convenience: process all map NPCs with a fixed pixels-per-tick step.
        /// </summary>
        public static void ProcessAll(int pixelsPerTick)
        {
            var step = Math.Max(1, pixelsPerTick);
            for (int i = 0; i < Variables.MaxMapNpcs; i++)
                OnMove(i, step);
        }

        /// <summary>
        /// Converts a Direction enum value into a pixel delta scaled by step.
        /// </summary>
        private static (int dx, int dy) GetDirectionDelta(int dirValue, int step)
        {
            // The Direction enum is assumed to match the original code.
            // Up/Down change Y, Left/Right change X.
            switch ((Direction)dirValue)
            {
                case Direction.Up: return (0, -step);
                case Direction.Down: return (0, step);
                case Direction.Left: return (-step, 0);
                case Direction.Right: return (step, 0);
                default: return (0, 0);
            }
        }


        public static void OnClearAll()
        {
            Data.Npc = new Core.Globals.Type.Npc[Variables.MaxNpcs];

            for (int i = 0; i < Variables.MaxNpcs; i++)
                OnClear(i);

        }

        public static void OnClear(int index)
        {
            int statCount = Enum.GetValues(typeof(Stat)).Length;
            Data.Npc[index].AttackSay = "";
            Data.Npc[index].Name = "";
            Data.Npc[index] = default;
            Data.Npc[index].Stat = new byte[statCount];
            Data.Npc[index].DropChance = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].DropItem = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].DropItemValue = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].Skill = new byte[Core.Globals.Variables.MaxNpcSkills];
            GameState.NpcLoaded[index] = 0;
        }

        public static void OnStream(int npcNum)
        {
            if (npcNum >= 0 && string.IsNullOrEmpty(Data.Npc[npcNum].Name) && GameState.NpcLoaded[npcNum] == 0)
            {
                GameState.NpcLoaded[(int)npcNum] = 1;
                Sender.SendRequestNpc(npcNum);
            }
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
            int screenX = GameLogic.ConvertMapX(baseWorldX);
            int drawX = (int)(screenX + (Constants.TileSize - size.X) / 2);

            int spriteNum = Data.Npc[(int)npcNum].Sprite;
            if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
            {
                // No valid graphic: render just above feet similar to player fallback
                int feetScreenY = GameLogic.ConvertMapY(baseWorldY);
                textY = feetScreenY - 16;
                TextRenderer.OnRender(name, drawX, textY, color, backColor);
                return;
            }

            var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteNum.ToString()));
            if (gfxInfo == null || gfxInfo.Height <= 0)
            {
                int feetScreenY = GameLogic.ConvertMapY(baseWorldY);
                textY = feetScreenY - 16;
                TextRenderer.OnRender(name, drawX, textY, color, backColor);
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
            TextRenderer.OnRender(name, drawX, textY, color, backColor);
        }
    }
}
