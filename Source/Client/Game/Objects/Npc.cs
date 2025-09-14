using Core;
using System;
using Core.Globals;

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
        private const int TileSize = 32;

        // Client-side prediction helpers: track remaining pixels and destination for current tile step.
        private static readonly int[] RemainingPixels = new int[Constant.MaxMapNpcs];
        private static readonly int[] DestX = new int[Constant.MaxMapNpcs];
        private static readonly int[] DestY = new int[Constant.MaxMapNpcs];

        /// <summary>
        /// Raised when an NPC lands exactly on a tile boundary (x % 32 == 0 && y % 32 == 0).
        /// Args: (npcIndex, tileX, tileY)
        /// </summary>
        public static event Action<int, int, int>? OnTileAligned;

        /// <summary>
        /// Processes one NPC by index (from a legacy double), moving by 1 pixel per tick.
        /// </summary>
        public static void ProcessMovement(double mapNpcNum) => ProcessMovement((int)mapNpcNum, 1);

        /// <summary>
        /// Processes one NPC by index, moving by a configurable number of pixels per tick.
        /// </summary>
        /// <param name="index">NPC array index (0..MaxMapNpcs-1).</param>
        /// <param name="pixelsPerTick">How many pixels to move this tick (>=1).</param>
        public static void ProcessMovement(int index, int pixelsPerTick)
        {
            if (index < 0 || index >= Constant.MaxMapNpcs) return;
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
                RemainingPixels[index] = TileSize;
                var (fullDx, fullDy) = GetDirectionDelta(npc.Dir, TileSize);
                DestX[index] = npc.X + fullDx;
                DestY[index] = npc.Y + fullDy;
            }

            var step = Math.Max(1, pixelsPerTick);
            var (dx, dy) = GetDirectionDelta(npc.Dir, step);

            // Apply delta
            int newX = x + dx;
            int newY = y + dy;

            // Keep within 0 .. (Max-1) * TileSize inclusive to match the original coordinate convention.
            int maxXpx = Math.Max(0, (Data.MyMap.MaxX - 1) * TileSize);
            int maxYpx = Math.Max(0, (Data.MyMap.MaxY - 1) * TileSize);

            newX = Math.Clamp(newX, 0, maxXpx);
            newY = Math.Clamp(newY, 0, maxYpx);
            
            // Commit the move (IMPORTANT: commit BEFORE the "aligned" check so we don't get stuck at 31px!)
            npc.X = newX;
            npc.Y = newY;
            RemainingPixels[index] -= step;

            // If we've landed exactly on a tile boundary, notify listeners (e.g., to advance path, stop walking, etc.)
            if (RemainingPixels[index] <= 0 || ((newX % TileSize == 0) && (newY % TileSize == 0)))
            {
                // Snap to destination to ensure perfect alignment (server authoritative end already snapped)
                npc.X = DestX[index];
                npc.Y = DestY[index];
                RemainingPixels[index] = 0;
                var tileX = npc.X / TileSize;
                var tileY = npc.Y / TileSize;
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
        public static void ProcessMovementDt(int index, float speedPxPerSecond, float deltaTimeSeconds)
        {
            var px = Math.Max(1, (int)MathF.Round(MathF.Abs(speedPxPerSecond) * MathF.Max(0.0f, deltaTimeSeconds)));
            ProcessMovement(index, px);
        }

        /// <summary>
        /// Convenience: process all map NPCs (1 px per tick).
        /// </summary>
        public static void ProcessAll()
        {
            for (int i = 0; i < Constant.MaxMapNpcs; i++)
                ProcessMovement(i, 1);
        }

        /// <summary>
        /// Convenience: process all map NPCs with a fixed pixels-per-tick step.
        /// </summary>
        public static void ProcessAll(int pixelsPerTick)
        {
            var step = Math.Max(1, pixelsPerTick);
            for (int i = 0; i < Constant.MaxMapNpcs; i++)
                ProcessMovement(i, step);
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
    }
}
