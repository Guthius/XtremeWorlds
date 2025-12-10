using System.Data;
using Core;
using System.Net.Security;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core.Globals;
using Core.Net;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;
using Microsoft.Xna.Framework;
using Core.Configurations;
using Core.Interfaces;
using Core.Objects;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Client
{
    public class Player : PlayerBase
    {
        #region Database

        public static void OnClear(int index)
        {
            Data.TradeTheirOffer = new Type.Item[Core.Globals.Variables.MaxInventory];
            Data.TradeYourOffer = new Type.Item[Core.Globals.Variables.MaxInventory];

            for (int x = 0; x < Core.Globals.Variables.MaxInventory; x++)
            {
                Data.TradeTheirOffer[x].Num = -1;
                Data.TradeYourOffer[x].Num = -1;
            }
  
            Trade.InTrade = -1;
        }

        #endregion

        #region Movement

        public static void CheckMovement()
        {
            // Guard against invalid player or map state
            if (GameState.MyIndex < 0 || GameState.MyIndex >= Core.Globals.Variables.MaxPlayers)
                return;

            int mapIdx = GetPlayerMap(GameState.MyIndex);

            if (mapIdx < 0 || mapIdx >= Data.Map.Length)
                return;

            if (Data.MyMap.MaxX <= 0 || Data.MyMap.MaxY <= 0)
                return;

            // Always refresh facing immediately based on current key state (diagonals prioritized)
            RefreshFacingFromKeys(sendIfChanged: true);

            if (IsTryingToMove())
            {
                bool started = CanMove();
                if (started)
                {
                    Player.Instance[GameState.MyIndex].Moving = (byte)(GameState.VbKeyShift ? MovementState.Walking : MovementState.Running);
                    Sender.SendPlayerMove();
                }
                else if (Player.Instance[GameState.MyIndex].IsMoving)
                {
                    // Keep sending movement while mid‑tile to keep server in sync (optional; can throttle later)
                    Sender.SendPlayerMove();
                }

                // Warp detection with bounds checks to avoid out-of-range
                int tx = GetPlayerX(GameState.MyIndex);
                int ty = GetPlayerY(GameState.MyIndex);
                if (tx >= 0 && ty >= 0 && tx < Data.MyMap.MaxX && ty < Data.MyMap.MaxY)
                {
                    var tile = Data.MyMap.Tile[tx, ty];
                    if (tile.Type == TileType.Warp || tile.Type2 == TileType.Warp)
                        GameState.GettingMap = true;
                }
            }
        }

        private static void RefreshFacingFromKeys(bool sendIfChanged)
        {
            int newDir = -1;
            bool up = GameState.DirUp;
            bool down = GameState.DirDown;
            bool left = GameState.DirLeft;
            bool right = GameState.DirRight;

            // Diagonals first
            if (up && right)
                newDir = (int)Direction.UpRight;
            else if (up && left)
                newDir = (int)Direction.UpLeft;
            else if (down && right)
                newDir = (int)Direction.DownRight;
            else if (down && left)
                newDir = (int)Direction.DownLeft;
            else if (up)
                newDir = (int)Direction.Up;
            else if (down)
                newDir = (int)Direction.Down;
            else if (left)
                newDir = (int)Direction.Left;
            else if (right)
                newDir = (int)Direction.Right;
            else
                return; // no input

            if (newDir >= 0)
            {
                // Always update local facing immediately
                if (Player.Instance[GameState.MyIndex].Dir != newDir)
                {
                    Player.Instance[GameState.MyIndex].Dir = (byte)newDir;
                }
                // Send dir every frame while keys are held to eliminate visual lag between transitions
                Sender.SendPlayerDir();
            }
        }

        public static bool IsTryingToMove()
        {
            bool isTryingToMove = default;

            if (GameState.DirUp | GameState.DirDown | GameState.DirLeft | GameState.DirRight)
            {
                isTryingToMove = true;
            }
            else
            {
                if (Player.Instance[GameState.MyIndex].IsMoving)
                {
                    Sender.SendStopPlayerMove();
                    Player.Instance[GameState.MyIndex].IsMoving = false;
                }
                // Always ensure numeric Moving flag (animation driver) is cleared whenever no movement keys are down
                Player.Instance[GameState.MyIndex].Moving = 0; // 0 = idle
            }

            return isTryingToMove;
        }

        public static bool CanMove()
        {
            bool canMove = false;
            int d;

            if (GetPlayerX(GameState.MyIndex) < 0 || GetPlayerX(GameState.MyIndex) >= Data.MyMap.MaxX || GetPlayerY(GameState.MyIndex) < 0 || GetPlayerY(GameState.MyIndex) >= Data.MyMap.MaxY)
            {
                return canMove;
            }

            if (Event.HoldPlayer)
            {
                return canMove;
            }

            var remaining = (int) (Player.Instance[GameState.MyIndex].DeathTimer - General.GetTickCount()) / 1000;
            if (remaining < 0) remaining = 0;

            if (remaining > 0)
            {
                return canMove;
            }

            if (GameState.GettingMap)
            {
                return canMove;
            }

            // Make sure they haven't just casted a skill
            if (GameState.SkillBuffer >= 0)
            {
                Sender.SendCancelCast();
            }

            // make sure they're not stunned
            if (GameState.StunDuration > 0)
            {
                return canMove;
            }

            if (Event.InEvent)
            {
                return canMove;
            }

            if (!GameState.InSmallChat)
            {
                return canMove;
            }

            if (Trade.InTrade >= 0)
            {
                Sender.SendDeclineTrade();
            }

            if (GameState.InShop >= 0)
            {
                Shop.OnClose();
            }

            if (GameState.InBank)
            {
                Sender.SendCloseBank();
            }

            d = GetPlayerDir(GameState.MyIndex);

            switch (d)
            {
                case (int) Direction.Up:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Up == 0 && GetPlayerY(GameState.MyIndex) <= 0)
                    {
                        GameState.DirUp = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.Down:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Down == 0 && GetPlayerY(GameState.MyIndex) >= Data.MyMap.MaxY)
                    {
                        GameState.DirDown = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Up);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.Left:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Left == 0 && GetPlayerX(GameState.MyIndex) <= 0)
                    {
                        GameState.DirLeft = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.Right:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerX(GameState.MyIndex) >= Data.MyMap.MaxX)
                    {
                        GameState.DirRight = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Left);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.UpLeft:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Up == 0 && Data.Map[GetPlayerMap(GameState.MyIndex)].Left == 0 && GetPlayerY(GameState.MyIndex) <= 0 & GetPlayerX(GameState.MyIndex) <= 0)
                    {
                        GameState.DirUp = false;
                        GameState.DirDown = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                        GameState.DirLeft = false;
                        GameState.DirRight = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Down == 0 && GetPlayerY(GameState.MyIndex) <= 0)
                    {
                        GameState.DirUp = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerX(GameState.MyIndex) <= 0)
                    {
                        GameState.DirLeft = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.UpRight:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Up == 0 && Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerY(GameState.MyIndex) >= Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) >= Data.MyMap.MaxX)
                    {
                        GameState.DirUp = false;
                        GameState.DirDown = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                        GameState.DirRight = false;
                        GameState.DirLeft = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Left);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Up == 0 && GetPlayerY(GameState.MyIndex) <= 0)
                    {
                        GameState.DirUp = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerX(GameState.MyIndex) <= 0)
                    {
                        GameState.DirLeft = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.DownLeft:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Up == 0 && Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerY(GameState.MyIndex) >= Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) < 0)
                    {
                        GameState.DirDown = false;
                        GameState.DirUp = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Up);
                        GameState.DirLeft = false;
                        GameState.DirRight = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Up == 0 && GetPlayerY(GameState.MyIndex) <= 0)
                    {
                        GameState.DirDown = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Up);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerX(GameState.MyIndex) <= 0)
                    {
                        GameState.DirLeft = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                        return canMove;
                    }

                    break;
                }

                case (int) Direction.DownRight:
                {
                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Down == 0 && Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerY(GameState.MyIndex) >= Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) >= Data.MyMap.MaxX)
                    {
                        GameState.DirDown = false;
                        GameState.DirUp = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Up);
                        GameState.DirRight = false;
                        GameState.DirLeft = true;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Left);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Down == 0 && GetPlayerY(GameState.MyIndex) >= Data.MyMap.MaxY)
                    {
                        GameState.DirDown = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Up);
                        return canMove;
                    }

                    if (Data.Map[GetPlayerMap(GameState.MyIndex)].Right == 0 && GetPlayerX(GameState.MyIndex) >= Data.MyMap.MaxX)
                    {
                        GameState.DirRight = false;
                        SetPlayerDir(GameState.MyIndex, (int) Direction.Left);
                        return canMove;
                    }

                    break;
                }
            }

            if (GameState.DirUp && !GameState.DirDown && !GameState.DirLeft && !GameState.DirRight)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Up);
                if (GetPlayerY(GameState.MyIndex) > 0)
                {
                    if (OnCheckDir((byte) Direction.Up))
                    {
                        if (d != (int) Direction.Up)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Up > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            if (GameState.DirDown && !GameState.DirUp && !GameState.DirLeft && !GameState.DirRight)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                if (GetPlayerY(GameState.MyIndex) < Data.MyMap.MaxY - 1)
                {
                    if (OnCheckDir((byte) Direction.Down))
                    {
                        if (d != (int) Direction.Down)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Down > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            if (GameState.DirLeft && !GameState.DirUp && !GameState.DirDown && !GameState.DirRight)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Left);
                if (GetPlayerX(GameState.MyIndex) > 0)
                {
                    if (OnCheckDir((byte) Direction.Left))
                    {
                        if (d != (int) Direction.Left)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Left > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            if (GameState.DirRight && !GameState.DirUp && !GameState.DirDown && !GameState.DirLeft)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                if (GetPlayerX(GameState.MyIndex) < Data.MyMap.MaxX)
                {
                    if (OnCheckDir((byte) Direction.Right))
                    {
                        if (d != (int) Direction.Right)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Right > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            // Check for diagonal movements first
            if (GameState.DirUp && GameState.DirRight && !GameState.DirLeft && !GameState.DirDown)
            {
                if (GetPlayerY(GameState.MyIndex) > 0 & GetPlayerX(GameState.MyIndex) < Data.MyMap.MaxX)
                {
                    SetPlayerDir(GameState.MyIndex, (int) Direction.UpRight);
                    if (OnCheckDir((byte)Direction.UpRight))
                    {
                        if (d != (int)Direction.UpRight)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Up > 0 & Data.MyMap.Right > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }
            else if (GameState.DirUp && GameState.DirLeft && !GameState.DirRight && !GameState.DirDown)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.UpLeft);
                if (GetPlayerY(GameState.MyIndex) > 0 & GetPlayerX(GameState.MyIndex) > 0)
                {
                    if (OnCheckDir((byte) Direction.UpLeft))
                    {
                        if (d != (int) Direction.UpLeft)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Up > 0 & Data.MyMap.Left > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }
            else if (GameState.DirDown && GameState.DirRight && !GameState.DirLeft && !GameState.DirUp)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.DownRight);
                if (GetPlayerY(GameState.MyIndex) < Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) < Data.MyMap.MaxX)
                {
                    if (OnCheckDir((byte) Direction.DownRight))
                    {
                        if (d != (int) Direction.DownRight)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Down > 0 & Data.MyMap.Right > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }
            else if (GameState.DirDown && GameState.DirLeft && !GameState.DirRight && !GameState.DirUp)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.DownLeft);
                if (GetPlayerY(GameState.MyIndex) < Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) > 0)
                {
                    if (OnCheckDir((byte) Direction.DownLeft))
                    {
                        if (d != (int) Direction.DownLeft)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Down > 0 & Data.MyMap.Left > 0)
                {
                    Sender.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            canMove = true;
            return canMove;
        }

        public static bool OnCheckDir(byte direction)
        {
            bool OnCheckDir = default;
            var x = default(int);
            var y = default(int);
            int i;

            if (GetPlayerX(GameState.MyIndex) >= Data.Map[GetPlayerMap(GameState.MyIndex)].MaxX || GetPlayerY(GameState.MyIndex) >= Data.Map[GetPlayerMap(GameState.MyIndex)].MaxY)
            {
                OnCheckDir = true;
                return OnCheckDir;
            }

            // check directional blocking
            if (GameLogic.IsDirBlocked(ref Data.MyMap.Tile[GetPlayerX(GameState.MyIndex), GetPlayerY(GameState.MyIndex)].DirBlock, ref direction))
            {
                OnCheckDir = true;
                return OnCheckDir;
            }

            switch (direction)
            {
                case (byte) Direction.Up:
                {
                    x = GetPlayerX(GameState.MyIndex);
                    y = GetPlayerY(GameState.MyIndex) - 1;
                    break;
                }
                case (byte) Direction.Down:
                {
                    x = GetPlayerX(GameState.MyIndex);
                    y = GetPlayerY(GameState.MyIndex) + 1;
                    break;
                }
                case (byte) Direction.Left:
                {
                    x = GetPlayerX(GameState.MyIndex) - 1;
                    y = GetPlayerY(GameState.MyIndex);
                    break;
                }
                case (byte) Direction.Right:
                {
                    x = GetPlayerX(GameState.MyIndex) + 1;
                    y = GetPlayerY(GameState.MyIndex);
                    break;
                }
                case (byte) Direction.UpLeft:
                {
                    x = GetPlayerX(GameState.MyIndex) - 1;
                    y = GetPlayerY(GameState.MyIndex) - 1;
                    break;
                }
                case (byte) Direction.UpRight:
                {
                    x = GetPlayerX(GameState.MyIndex) + 1;
                    y = GetPlayerY(GameState.MyIndex) - 1;
                    break;
                }
                case (byte) Direction.DownLeft:
                {
                    x = GetPlayerX(GameState.MyIndex) - 1;
                    y = GetPlayerY(GameState.MyIndex) + 1;
                    break;
                }
                case (byte) Direction.DownRight:
                {
                    x = GetPlayerX(GameState.MyIndex) + 1;
                    y = GetPlayerY(GameState.MyIndex) + 1;
                    break;
                }
            }

            if (x < 0 || y < 0 || x >= Data.MyMap.MaxX || y >= Data.MyMap.MaxY)
            {
                OnCheckDir = true;
                return OnCheckDir;
            }

            // Check to see if the map tile is blocked or not
            if (Data.MyMap.Tile[x, y].Type == TileType.Blocked | Data.MyMap.Tile[x, y].Type2 == TileType.Blocked)
            {
                OnCheckDir = true;
                return OnCheckDir;
            }

            // Check to see if the map tile is tree or not
            if (Data.MyMap.Tile[x, y].Type == TileType.Resource | Data.MyMap.Tile[x, y].Type2 == TileType.Resource)
            {
                OnCheckDir = true;
                return OnCheckDir;
            }

            // Check to see if a player is already on that tile
            if (Data.MyMap.Moral > 0)
            {
                if (Moral.Instance[Data.MyMap.Moral].PlayerBlock)
                {
                    for (i = 0; i < Core.Globals.Variables.MaxPlayers; i++)
                    {
                        if (IsPlaying(i))
                        {
                            if (Player.Instance[i].X == x & Player.Instance[i].Y == y)
                            {
                                OnCheckDir = true;
                                return OnCheckDir;
                            }
                        }
                    }
                }

                // Check to see if a Npc is already on that tile
                if (Moral.Instance[Data.MyMap.Moral].NpcBlock)
                {
                    for (i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
                    {
                        if (Data.MyMapNpc[i].Num >= 0 & Data.MyMapNpc[i].X == x & Data.MyMapNpc[i].Y == y)
                        {
                            OnCheckDir = true;
                            return OnCheckDir;
                        }
                    }
                }
            }

            var loopTo = GameState.CurrentEvents;
            for (i = 0; i < loopTo; i++)
            {
                if (Data.MapEvents?[i].Visible == true)
                {
                    if (Data.MapEvents[i].X == x & Data.MapEvents[i].Y == y)
                    {
                        if (Data.MapEvents[i].WalkThrough == 0)
                        {
                            OnCheckDir = true;
                            return OnCheckDir;
                        }
                    }
                }
            }

            return OnCheckDir;
        }

        /// <summary>
        /// Update player facing based on mouse cursor screen position. Converts current player world position
        /// to screen center and sets an 8-direction facing, then sends direction packet.
        /// </summary>
        public static void UpdateFacingFromMouse(int mouseScreenX, int mouseScreenY)
        {
            if (GameState.MyIndex < 0 | GameState.MyIndex > Core.Globals.Variables.MaxPlayers) return;
            int playerScreenX = GameLogic.ConvertMapX(GetPlayerRawX(GameState.MyIndex)) + Constants.TileSize / 2;
            int playerScreenY = GameLogic.ConvertMapY(GetPlayerRawY(GameState.MyIndex)) + Constants.TileSize / 2;
            int dx = mouseScreenX - playerScreenX;
            int dy = mouseScreenY - playerScreenY; // positive downwards
            if (dx == 0 && dy == 0) return;
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI; // degrees
            Direction dir;
            if (angle > -22.5 && angle <= 22.5) dir = Direction.Right;
            else if (angle > 22.5 && angle <= 67.5) dir = Direction.DownRight;
            else if (angle > 67.5 && angle <= 112.5) dir = Direction.Down;
            else if (angle > 112.5 && angle <= 157.5) dir = Direction.DownLeft;
            else if (angle > 157.5 || angle <= -157.5) dir = Direction.Left;
            else if (angle > -157.5 && angle <= -112.5) dir = Direction.UpLeft;
            else if (angle > -112.5 && angle <= -67.5) dir = Direction.Up;
            else dir = Direction.UpRight;
            if (Player.Instance[GameState.MyIndex].Dir != (byte)dir)
            {
                Player.Instance[GameState.MyIndex].Dir = (byte)dir;
                Sender.SendPlayerDir();
            }
        }

        public static void OnMove(int index)
        {
            // BUGFIX: Previously gated all players' pixel movement on the LOCAL player's IsMoving flag,
            // causing remote players to slide only while you moved (and freeze otherwise), producing
            // desynced positions and camera jitter when targeting them. Now use each player's own flag.
            if (!IsPlaying(index)) return;
            if (!Player.Instance[index].IsMoving) return;

            // Update per‑pixel offsets based on direction.
            // NOTE: This assumes 1px per tick step. If variable speed is desired later, introduce a per-player speed.
            switch (GetPlayerDir(index))
            {
                case (int)Direction.Up:
                    Player.Instance[index].Y -= 1;
                    break;
                case (int)Direction.Down:
                    Player.Instance[index].Y += 1;
                    break;
                case (int)Direction.Left:
                    Player.Instance[index].X -= 1;
                    break;
                case (int)Direction.Right:
                    Player.Instance[index].X += 1;
                    break;
                case (int)Direction.UpRight:
                    Player.Instance[index].X += 1;
                    Player.Instance[index].Y -= 1;
                    break;
                case (int)Direction.UpLeft:
                    Player.Instance[index].X -= 1;
                    Player.Instance[index].Y -= 1;
                    break;
                case (int)Direction.DownRight:
                    Player.Instance[index].X += 1;
                    Player.Instance[index].Y += 1;
                    break;
                case (int)Direction.DownLeft:
                    Player.Instance[index].X -= 1;
                    Player.Instance[index].Y += 1;
                    break;
            }
        }

        public static void OnCheckAttack(bool mouse = false)
        {
            int attackSpeed;
            var x = default(int);
            var y = default(int);

            if (GameState.VbKeyControl || mouse)
            {
                if (GameState.MyIndex < 0 | GameState.MyIndex > Core.Globals.Variables.MaxPlayers)
                    return;

                if (Event.InEvent)
                    return;

                // If server is holding the player (e.g., on death), block attacks and show a quick local message
                if (Event.HoldPlayer)
                {
                    return;
                }

                var remaining = (int) (Player.Instance[GameState.MyIndex].DeathTimer - General.GetTickCount()) / 1000;
                if (remaining < 0) remaining = 0;

                if (remaining > 0)
                {
                    return;
                }

                if (GameState.SkillBuffer >= 0)
                    return; // currently casting a skill, can't attack

                if (GameState.StunDuration > 0)
                    return; // stunned, can't attack

                // speed from weapon
                if (GetPlayerPaperdoll(GameState.MyIndex, Equipment.Weapon) >= 0)
                {
                    attackSpeed = Item.Instance[GetPlayerPaperdoll(GameState.MyIndex, Equipment.Weapon)].Speed * 1000;
                }
                else
                {
                    attackSpeed = 1000;
                }

                if (Player.Instance[GameState.MyIndex].AttackTimer + attackSpeed < General.GetTickCount())
                {
                    if (Player.Instance[GameState.MyIndex].Attacking == 0)
                    {
                        {
                            var instance = Player.Instance[GameState.MyIndex];
                            instance.Attacking = 1;
                            instance.AttackTimer = General.GetTickCount();
                        }

                        // If weapon has a projectile, send mouse-aimed attack with world pixel coords
                        int weapon = GetPlayerPaperdoll(GameState.MyIndex, Equipment.Weapon);
                        if (mouse && weapon >= 0 && Item.Instance[weapon].Projectile >= 0)
                        {
                            // Compute world pixel coordinates of mouse relative to map origin
                            int worldX = (int)GameState.Camera.Left + GameState.CurMouseXGame;
                            int worldY = (int)GameState.Camera.Top + GameState.CurMouseYGame;
                            Sender.SendMouseAttack(worldX, worldY);
                        }
                        else
                        {
                            Sender.SendAttack();
                        }
                    }
                }

                switch (Player.Instance[GameState.MyIndex].Dir)
                {
                    case (byte) Direction.Up:
                    {
                        x = GetPlayerRawX(GameState.MyIndex);
                        y = GetPlayerRawY(GameState.MyIndex) - Constants.TileSize;
                        break;
                    }

                    case (byte) Direction.Down:
                    {
                        x = GetPlayerRawX(GameState.MyIndex);
                        y = GetPlayerRawY(GameState.MyIndex) + Constants.TileSize;
                        break;
                    }

                    case (byte) Direction.Left:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) - Constants.TileSize;
                        y = GetPlayerRawY(GameState.MyIndex);
                        break;
                    }
                    case (byte) Direction.Right:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) + Constants.TileSize;
                        y = GetPlayerRawY(GameState.MyIndex);
                        break;
                    }

                    case (byte) Direction.UpRight:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) + Constants.TileSize;
                        y = GetPlayerRawY(GameState.MyIndex) - Constants.TileSize;
                        break;
                    }

                    case (byte) Direction.UpLeft:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) - Constants.TileSize;
                        y = GetPlayerRawY(GameState.MyIndex) - Constants.TileSize;
                        break;
                    }

                    case (byte) Direction.DownRight:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) + Constants.TileSize;
                        y = GetPlayerRawY(GameState.MyIndex) + Constants.TileSize;
                        break;
                    }

                    case (byte) Direction.DownLeft:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) - Constants.TileSize;
                        y = GetPlayerRawY(GameState.MyIndex) + Constants.TileSize;
                        break;
                    }
                }

                if (General.GetTickCount() > Player.Instance[GameState.MyIndex].EventTimer)
                {
                    for (int i = 0, loopTo = GameState.CurrentEvents; i < loopTo; i++)
                    {
                        if (Data.MapEvents?.Length < GameState.CurrentEvents)
                            break;

                        if (Data.MapEvents?[i].Visible == true)
                        {
                            // Check for 32 pixels around the map event
                            int eventX = Data.MapEvents[i].X;
                            int eventY = Data.MapEvents[i].Y;
                            // Assume eventX and eventY are in pixel coordinates
                            // If they are in tile coordinates, multiply by tile size (e.g., 32)
                            int px = x;
                            int py = y;
                            // If x/y are tile coordinates, multiply by tile size
                            // For now, assume all are pixel coordinates
                            if (Math.Abs(px - eventX) <= Constants.TileSize && Math.Abs(py - eventY) <= Constants.TileSize)
                            {
                                var packetWriter = new PacketWriter(8);

                                packetWriter.WriteEnum(Packets.ClientPackets.CEvent);
                                packetWriter.WriteInt32(i);

                                Network.Send(packetWriter);

                                Player.Instance[GameState.MyIndex].EventTimer = General.GetTickCount() + 200;
                            }
                        }
                    }
                }
            }
        }

        public static void CastSkill(int skillSlot)
        {
            // Check for subscript out of range
            if (skillSlot < 0 | skillSlot > Core.Globals.Variables.MaxPlayerSkills)
                return;

            if (Player.Instance[GameState.MyIndex].Skill[skillSlot].Cd > 0)
            {
                TextRenderer.AddText("Skill has not cooled down yet!", (int) ColorName.BrightRed);
                return;
            }

            if (Player.Instance[GameState.MyIndex].Skill[skillSlot].Num < 0)
                return;

            // Check if player has enough MP
            if (GetPlayerVital(GameState.MyIndex,Core.Globals.Vital.Mana) < Data.Skill[Player.Instance[GameState.MyIndex].Skill[skillSlot].Num].MpCost)
            {
                TextRenderer.AddText("Not enough mana to cast " + Data.Skill[Player.Instance[GameState.MyIndex].Skill[skillSlot].Num].Name + ".", (int) ColorName.BrightRed);
                return;
            }

            if (Player.Instance[GameState.MyIndex].Skill[skillSlot].Num >= 0)
            {
                if (General.GetTickCount() > Player.Instance[GameState.MyIndex].AttackTimer + 1000)
                {
                    if (Player.Instance[GameState.MyIndex].Moving == 0)
                    {
                        if (Data.MyMap.Moral >= 0)
                        {
                            if (Moral.Instance[Data.MyMap.Moral].CanCast)
                            {
                                Sender.SendCast(skillSlot);
                            }
                            else
                            {
                                TextRenderer.AddText("Cannot cast here!", (int) ColorName.BrightRed);
                            }
                        }
                    }
                    else
                    {
                        TextRenderer.AddText("Cannot cast while walking!", (int) ColorName.BrightRed);
                    }
                }
            }
            else
            {
                TextRenderer.AddText("No skill here.", (int) ColorName.BrightRed);
            }
        }

        public static int FindSkill(int skillNum)
        {
            int findSkill = default;
            int i;

            findSkill = 0;

            // Check for subscript out of range
            if (skillNum < 0 | skillNum > Core.Globals.Variables.MaxSkills)
            {
                return findSkill;
            }

            for (i = 0; i < Core.Globals.Variables.MaxPlayerSkills; i++)
            {
                // Check to see if the player has the skill
                if (GetPlayerSkill(GameState.MyIndex, i) == skillNum)
                {
                    findSkill = i;
                    return findSkill;
                }
            }

            return findSkill;
        }

        public static void OnDrawName(int index)
        {
            var color = default(Color);
            var backColor = default(Color);

            if (!GetPlayerPk(index))
            {
                switch (GetPlayerAccess(index))
                {
                    case (int)AccessLevel.Player: color = Color.White; backColor = Color.Black; break;
                    case (int)AccessLevel.Moderator: color = Color.Cyan; backColor = Color.White; break;
                    case (int)AccessLevel.Mapper: color = Color.Green; backColor = Color.Black; break;
                    case (int)AccessLevel.Developer: color = Color.Blue; backColor = Color.Black; break;
                    case (int)AccessLevel.Owner: color = Color.Yellow; backColor = Color.Black; break;
                }
            }
            else
            {
                color = Color.Red;
            }

            var remaining = (Player.Instance[index].DeathTimer - General.GetTickCount()) / 1000;
            if (remaining < 0) remaining = 0;
            var name = remaining > 0 ? $"{remaining}..." : Player.Instance[index].Name;

            // X position: keep current label-style centering over the tile
            var playerWorldX = GetPlayerRawX(index);
            var playerWorldY = GetPlayerRawY(index);
            var playerScreenX = GameLogic.ConvertMapX(playerWorldX);

            var size = TextRenderer.Fonts[Font.Georgia].MeasureString(name);
            var padding = (int)(size.X / 6);
            var drawX = (int)(playerScreenX + (Constants.TileSize - size.X) / 2 + padding);

            // Y position: mirror NPC/event logic using sprite graphics when available
            int textY;
            int spriteNum = Player.Instance[index].Sprite;

            if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
            {
                // No valid graphic: render at feet (just above base tile)
                textY = GameLogic.ConvertMapY(playerWorldY) - 16;
            }
            else
            {
                var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, spriteNum.ToString()));
                if (gfxInfo == null || gfxInfo.Height <= 0)
                {
                    // Missing or invalid graphic: fallback to feet
                    textY = GameLogic.ConvertMapY(playerWorldY) - 16;
                }
                else
                {
                    int configuredDirs = SettingsManager.Instance.SpriteDirections;
                    if (configuredDirs <= 0) configuredDirs = 4;
                    configuredDirs = Math.Max(1, configuredDirs);
                    int directionRows = 1;
                    if (gfxInfo.Height % configuredDirs == 0) directionRows = configuredDirs;
                    else if (configuredDirs != 8 && gfxInfo.Height % 8 == 0) directionRows = 8;
                    else if (configuredDirs != 4 && gfxInfo.Height % 4 == 0) directionRows = 4;

                    int frameHeight = gfxInfo.Height / directionRows;
                    if (frameHeight <= 0) frameHeight = 32;

                    int spriteTopWorldY = playerWorldY;
                    if (frameHeight > 32) spriteTopWorldY = playerWorldY - (frameHeight - 32);

                    int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);
                    int textPixelHeight = (int)Math.Ceiling(TextRenderer.Fonts[Font.Georgia].LineSpacing * TextRenderer.BaseScale);
                    int margin = 8;
                    textY = spriteTopScreenY - textPixelHeight + margin;
                }
            }

            TextRenderer.OnDraw(name, drawX, textY, color, backColor, Font.Georgia);
        }

         public static void OnDraw(int index)
        {
            // Expect sprite sheet columns grouped evenly into 3 segments: Idle, Run, Attack.
            // If columns not divisible by 3, we fallback to original linear usage.
            byte anim; // frame index within chosen segment
            int x;
            int y;
            int spriteNum;
            var spriteleft = default(int);
            int attackSpeed; // attack speed duration (ms) controlling full attack cycle length
            Rectangle rect;

            spriteNum = GetPlayerSprite(index);

            if (index < 0 | index > Core.Globals.Variables.MaxPlayers)
                return;

            if (spriteNum <= 0 | spriteNum > GameState.NumCharacters)
                return;

            // Derive attack speed duration (ms). If stored as seconds, multiply here; if already ms, keep as-is.
            if (GetPlayerPaperdoll(index, Equipment.Weapon) >= 0)
            {
                attackSpeed = Item.Instance[GetPlayerPaperdoll(index, Equipment.Weapon)].Speed;
                if (attackSpeed < 50) attackSpeed *= 1000; // heuristic: treat tiny values as seconds, convert to ms
            }
            else
            {
                attackSpeed = 1000;
            }

            long tick = General.GetTickCount();
            bool isAttacking = Player.Instance[index].Attacking == 1; // full attack state
            bool provisionalMoving = Player.Instance[index].IsMoving; // raw flag from network
            anim = 0; // will be set after texture info (need framesPerSegment)

            // Check to see if we want to stop making him attack
            {
                var instance = Player.Instance[index];
                if (instance.AttackTimer + attackSpeed < General.GetTickCount())
                {
                    instance.Attacking = 0;
                    instance.AttackTimer = 0;
                }
            }

            // Dynamic row index from direction
            // We'll compute directionRows below once gfxInfo known; use placeholder for now

            var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, spriteNum.ToString()));
            if (gfxInfo == null)
            {
                // Handle the case where the graphic information is not found
                return;
            }

            int directionRows = GameClient.ComputeDirectionRows(gfxInfo.Height, Math.Max(1, SettingsManager.Instance.SpriteDirections)); // dynamic rows (supports 4/8/1)
            spriteleft = GameClient.MapDirectionToRow((Direction)GetPlayerDir(index), directionRows);

            // Determine segment frame counts (allow variable counts)
            int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
            int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
            int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
            int[] segmentLengths = { idleFrames, runFrames, attackFrames };
            int expectedTotalColumns = idleFrames + runFrames + attackFrames;

            // Derive frameHeight from vertical directional stacking
            int frameHeight = gfxInfo.Height / directionRows;
            if (frameHeight <= 0) return; // safety

            // Heuristic for legacy sheets: sprites are usually square per frame
            int autoColsBySquare = frameHeight > 0 ? gfxInfo.Width / frameHeight : 0;
            if (autoColsBySquare <= 0) autoColsBySquare = 1;

            // Candidate segmented frame width if we attempted 3 segments
            bool widthDivisible = expectedTotalColumns > 0 && gfxInfo.Width % expectedTotalColumns == 0;
            int candidateFrameWidth = widthDivisible ? gfxInfo.Width / expectedTotalColumns : 0;

            // Relax segmentation: any width-divisible sheet is treated as segmented.
            bool canSegment = widthDivisible;
            bool hasThreeSegments = canSegment;

            int frameColumnsForWidth = canSegment ? expectedTotalColumns : autoColsBySquare;

            // Dynamic segment ordering via settings (e.g. "idle,run,attack" or "attack,idle,run")
            string orderCsv = SettingsManager.Instance.SpriteSegmentOrder ?? "idle,run,attack";
            var tokens = orderCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 3)
                tokens = new[] { "idle", "run", "attack" };
            // Normalize & validate
            for (int i = 0; i < tokens.Length; i++) tokens[i] = tokens[i].Trim().ToLowerInvariant();
            // Ensure all three unique expected names present; else fallback default
            if (!(tokens.Contains("idle") && tokens.Contains("run") && tokens.Contains("attack")))
                tokens = new[] { "idle", "run", "attack" };

            // Build offsets based on order sequence
            int runningOffset = 0;
            int idleOffset = 0, runOffset = 0, attackOffset = 0;
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (t == "idle") idleOffset = runningOffset;
                else if (t == "run") runOffset = runningOffset;
                else if (t == "attack") attackOffset = runningOffset;
                // advance by that segment's length
                if (t == "idle") runningOffset += idleFrames;
                else if (t == "run") runningOffset += runFrames;
                else if (t == "attack") runningOffset += attackFrames;
            }

            // Moving only meaningful if segmented sheet
            bool isMoving = provisionalMoving && !isAttacking && hasThreeSegments;

            // Determine frame inside its segment (Steps driven for run; idle frame stays 0)
            if (hasThreeSegments)
            {
                if (isAttacking)
                {
                    // Time-based mapping: elapsed over attackSpeed spans exactly one full attack frame cycle
                    long elapsed = tick - Player.Instance[index].AttackTimer;
                    if (elapsed < 0) elapsed = 0;
                    long duration = attackSpeed;
                    if (duration <= 0) duration = 1;
                    if (elapsed >= duration) elapsed = duration - 1; // clamp
                    double ratio = elapsed / (double)duration; // 0.. <1
                    int frame = (int)(ratio * attackFrames);
                    if (frame >= attackFrames) frame = attackFrames - 1;
                    anim = (byte)frame;
                }
                else if (isMoving)
                {
                    // Run anim: tied to movement steps only
                    int len = segmentLengths[1];
                    anim = (byte)(Player.Instance[index].Steps % len);
                }
                else
                {
                    // Idle: animate through idle frames using Steps
                    int len = segmentLengths[0];
                    anim = (byte)(Player.Instance[index].Steps % len);
                }
            }
            else
            {
                // Legacy: single segment; Steps only advance while moving so idle shows frame 0
                anim = (byte)(Player.Instance[index].Steps % frameColumnsForWidth); // legacy: cycles while idle too
            }
            // Calculate the X
            x = (int)Math.Round(Player.Instance[index].X - (gfxInfo.Width / (double)frameColumnsForWidth - 32d) / 2d);

            // Is the player's height more than 32..?
            if ((gfxInfo.Height / directionRows) > 32)
            {
                // Create a 32 pixel offset for larger sprites
                y = (int)Math.Round(GetPlayerRawY(index) - (gfxInfo.Height / (double)directionRows - 32d));
            }
            else
            {
                // Proceed as normal
                y = GetPlayerRawY(index);
            }

            int frameColumn;
            int segmentOffset = 0;
            if (hasThreeSegments)
            {
                if (isAttacking) segmentOffset = attackOffset;
                else if (isMoving) segmentOffset = runOffset;
                else segmentOffset = idleOffset;
            }
            frameColumn = Math.Min(frameColumnsForWidth - 1, segmentOffset + anim);
            double frameWidth = gfxInfo.Width / (double)frameColumnsForWidth;
            double frameHeightD = frameHeight; // already computed above
            rect = new Rectangle((int)Math.Round(frameColumn * frameWidth),
                (int)Math.Round(spriteleft * frameHeightD), (int)Math.Round(frameWidth),
                (int)Math.Round(frameHeightD));

            // render the actual sprite
            // DrawShadow(x, y + 16)
            if (GetPlayerDir(index) == (byte)Direction.Up)
            {
                GameClient.DrawCharacterSprite(spriteNum, x, y, rect);
            }

            // check for paperdolling with directional draw order rules
            // Rule: draw weapon first when facing up (behind), draw weapon last when facing down (in front)
            var dirVal = (Direction)GetPlayerDir(index);
            Equipment[] eqOrder = new[] { Equipment.Weapon, Equipment.Armor, Equipment.Helmet, Equipment.Shield };

            // Treat diagonals as their vertical tendency
            bool isUp = dirVal == Direction.Up || dirVal == Direction.UpLeft || dirVal == Direction.UpRight;
            bool isDown = dirVal == Direction.Down || dirVal == Direction.DownLeft || dirVal == Direction.DownRight;

            if (isDown)
            {
                // Move weapon to the end so it draws on top
                eqOrder = new[] { Equipment.Armor, Equipment.Helmet, Equipment.Shield, Equipment.Weapon };
            }
            else if (isUp)
            {
                // Ensure weapon is first so it draws behind
                eqOrder = new[] { Equipment.Weapon, Equipment.Armor, Equipment.Helmet, Equipment.Shield };
            }

            foreach (var eq in eqOrder)
            {
                if (GetPlayerPaperdoll(index, eq) >= 0)
                {
                    var itemIndex = GetPlayerPaperdoll(index, eq);
                    var paperId = Item.Instance[itemIndex].Paperdoll;
                    if (paperId > 0)
                    {
                        // Pass segment context so equipment animates consistently with base sprite.
                        GameClient.DrawPaperdoll(x, y, paperId, anim, spriteleft, isMoving, isAttacking);
                    }
                }
            }

            if (GetPlayerDir(index) != (byte)Direction.Up)
            {
                GameClient.DrawCharacterSprite(spriteNum, x, y, rect);
            }

            // Check to see if we want to stop showing emote
            {
                var instance = Player.Instance[index];
                if (instance.EmoteTimer < General.GetTickCount())
                {
                    instance.Emote = 0;
                    instance.EmoteTimer = 0;
                }
            }

            // check for emotes
            if (Player.Instance[GameState.MyIndex].Emote > 0)
            {
                GameClient.DrawEmote(x, y, Player.Instance[GameState.MyIndex].Emote);
            }
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

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }
    }
    
    #endregion
}