using System.Data;
using Core;
using System.Net.Security;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core.Globals;
using Core.Net;
using static Core.Globals.Command;
using Type = Core.Globals.Type;

namespace Client
{
    public class Player
    {
        #region Database

        public static void ClearPlayers()
        {
            Data.Account = new Type.Account[Constant.MaxPlayers];
            Data.Player = new Type.Player[Constant.MaxPlayers];
            Data.TempPlayer = new Type.TempPlayer[Constant.MaxPlayers];

            for (int i = 0; i < Constant.MaxPlayers; i++)
            {
                ClearPlayer(i);
            }
        }

        public static void ClearAccount(int index)
        {
            Data.Account[index].Login = "";
            Data.Account[index].Password = "";
        }

        public static void ClearPlayer(int index)
        {
            ClearAccount(index);

            Data.Player[index].Name = "";
            Data.Player[index].Attacking = 0;
            Data.Player[index].AttackTimer = 0;
            Data.Player[index].Job = 0;
            Data.Player[index].Dir = 0;
            Data.Player[index].Access = (byte) AccessLevel.Player;

            Data.Player[index].Equipment = new Type.PlayerEq[Enum.GetValues(typeof(Equipment)).Length];
            for (int y = 0; y < Data.Player[index].Equipment.Length; y++)
                Data.Player[index].Equipment[y] = new Type.PlayerEq();

            Data.Player[index].Exp = 0;
            Data.Player[index].Level = 0;
            Data.Player[index].Map = 0;
            Data.Player[index].Moving = 0;
            Data.Player[index].Pk = false;
            Data.Player[index].Points = 0;
            Data.Player[index].Sprite = 0;

            Data.Player[index].Inv = new Type.PlayerInv[Constant.MaxInv];
            for (int x = 0; x < Constant.MaxInv; x++)
            {
                Data.Player[index].Inv[x].Num = -1;
                Data.Player[index].Inv[x].Value = 0;
                Data.TradeTheirOffer[x].Num = -1;
                Data.TradeYourOffer[x].Num = -1;
            }

            Data.Player[index].Skill = new Type.PlayerSkill[Constant.MaxPlayerSkills];
            for (int x = 0; x < Constant.MaxPlayerSkills; x++)
            {
                Data.Player[index].Skill[x].Num = -1;
                Data.Player[index].Skill[x].Cd = 0;
            }

            Data.Player[index].Stat = new byte[Enum.GetValues(typeof(Stat)).Length];
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                Data.Player[index].Stat[(int) stat] = 0;

            Data.Player[index].Steps = 0;

            int vitalCount = Enum.GetValues(typeof(Vital)).Length;
            Data.Player[index].Vital = new int[vitalCount];
            Data.Player[index].MaxVital = new int[vitalCount];
            foreach (Vital vital in Enum.GetValues(typeof(Vital)))
            {
                Data.Player[index].Vital[(int)vital] = 0;
                Data.Player[index].MaxVital[(int)vital] = 0;
            }
            Data.Player[index].X = 0;
            Data.Player[index].Y = 0;

            Data.Player[index].Hotbar = new Type.Hotbar[Constant.MaxHotbar];
            Data.Player[index].GatherSkills = new Type.ResourceType[Enum.GetValues(typeof(ResourceSkill)).Length];

            Trade.InTrade = -1;
        }

        #endregion

        #region Movement

        public static void CheckMovement()
        {
            // Guard against invalid player or map state
            if (GameState.MyIndex < 0 || GameState.MyIndex >= Constant.MaxPlayers)
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
                    Data.Player[GameState.MyIndex].Moving = (byte)(GameState.VbKeyShift ? MovementState.Walking : MovementState.Running);
                    Sender.SendPlayerMove();
                }
                else if (Data.Player[GameState.MyIndex].IsMoving)
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
                if (Data.Player[GameState.MyIndex].Dir != newDir)
                {
                    Data.Player[GameState.MyIndex].Dir = (byte)newDir;
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
                if (Data.Player[GameState.MyIndex].IsMoving)
                {
                    Sender.SendStopPlayerMove();
                    Data.Player[GameState.MyIndex].IsMoving = false;
                }
                // Always ensure numeric Moving flag (animation driver) is cleared whenever no movement keys are down
                Data.Player[GameState.MyIndex].Moving = 0; // 0 = idle
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

            var remaining = (int) (Data.Player[GameState.MyIndex].DeathTimer - General.GetTickCount()) / 1000;
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
                Trade.SendDeclineTrade();
            }

            if (GameState.InShop >= 0)
            {
                Shop.CloseShop();
            }

            if (GameState.InBank)
            {
                Bank.CloseBank();
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
                    if (CheckPlayerDir((byte) Direction.Up))
                    {
                        if (d != (int) Direction.Up)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Up > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            if (GameState.DirDown && !GameState.DirUp && !GameState.DirLeft && !GameState.DirRight)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Down);
                if (GetPlayerY(GameState.MyIndex) < Data.MyMap.MaxY - 1)
                {
                    if (CheckPlayerDir((byte) Direction.Down))
                    {
                        if (d != (int) Direction.Down)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Down > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            if (GameState.DirLeft && !GameState.DirUp && !GameState.DirDown && !GameState.DirRight)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Left);
                if (GetPlayerX(GameState.MyIndex) > 0)
                {
                    if (CheckPlayerDir((byte) Direction.Left))
                    {
                        if (d != (int) Direction.Left)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Left > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            if (GameState.DirRight && !GameState.DirUp && !GameState.DirDown && !GameState.DirLeft)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.Right);
                if (GetPlayerX(GameState.MyIndex) < Data.MyMap.MaxX)
                {
                    if (CheckPlayerDir((byte) Direction.Right))
                    {
                        if (d != (int) Direction.Right)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Right > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            // Check for diagonal movements first
            if (GameState.DirUp && GameState.DirRight && !GameState.DirLeft && !GameState.DirDown)
            {
                if (GetPlayerY(GameState.MyIndex) > 0 & GetPlayerX(GameState.MyIndex) < Data.MyMap.MaxX)
                {
                    SetPlayerDir(GameState.MyIndex, (int) Direction.UpRight);
                    if (CheckPlayerDir((byte)Direction.UpRight))
                    {
                        if (d != (int)Direction.UpRight)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Up > 0 & Data.MyMap.Right > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }
            else if (GameState.DirUp && GameState.DirLeft && !GameState.DirRight && !GameState.DirDown)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.UpLeft);
                if (GetPlayerY(GameState.MyIndex) > 0 & GetPlayerX(GameState.MyIndex) > 0)
                {
                    if (CheckPlayerDir((byte) Direction.UpLeft))
                    {
                        if (d != (int) Direction.UpLeft)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Up > 0 & Data.MyMap.Left > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }
            else if (GameState.DirDown && GameState.DirRight && !GameState.DirLeft && !GameState.DirUp)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.DownRight);
                if (GetPlayerY(GameState.MyIndex) < Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) < Data.MyMap.MaxX)
                {
                    if (CheckPlayerDir((byte) Direction.DownRight))
                    {
                        if (d != (int) Direction.DownRight)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Down > 0 & Data.MyMap.Right > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }
            else if (GameState.DirDown && GameState.DirLeft && !GameState.DirRight && !GameState.DirUp)
            {
                SetPlayerDir(GameState.MyIndex, (int) Direction.DownLeft);
                if (GetPlayerY(GameState.MyIndex) < Data.MyMap.MaxY & GetPlayerX(GameState.MyIndex) > 0)
                {
                    if (CheckPlayerDir((byte) Direction.DownLeft))
                    {
                        if (d != (int) Direction.DownLeft)
                        {
                            Sender.SendPlayerDir();
                        }
                    }
                }
                else if (Data.MyMap.Down > 0 & Data.MyMap.Left > 0)
                {
                    Map.SendPlayerRequestNewMap();
                    return canMove;
                }
            }

            canMove = true;
            return canMove;
        }

        public static bool CheckPlayerDir(byte direction)
        {
            bool checkPlayerDir = default;
            var x = default(int);
            var y = default(int);
            int i;

            if (GetPlayerX(GameState.MyIndex) >= Data.Map[GetPlayerMap(GameState.MyIndex)].MaxX || GetPlayerY(GameState.MyIndex) >= Data.Map[GetPlayerMap(GameState.MyIndex)].MaxY)
            {
                checkPlayerDir = true;
                return checkPlayerDir;
            }

            // check directional blocking
            if (GameLogic.IsDirBlocked(ref Data.MyMap.Tile[GetPlayerX(GameState.MyIndex), GetPlayerY(GameState.MyIndex)].DirBlock, ref direction))
            {
                checkPlayerDir = true;
                return checkPlayerDir;
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
                checkPlayerDir = true;
                return checkPlayerDir;
            }

            // Check to see if the map tile is blocked or not
            if (Data.MyMap.Tile[x, y].Type == TileType.Blocked | Data.MyMap.Tile[x, y].Type2 == TileType.Blocked)
            {
                checkPlayerDir = true;
                return checkPlayerDir;
            }

            // Check to see if the map tile is tree or not
            if (Data.MyMap.Tile[x, y].Type == TileType.Resource | Data.MyMap.Tile[x, y].Type2 == TileType.Resource)
            {
                checkPlayerDir = true;
                return checkPlayerDir;
            }

            // Check to see if a player is already on that tile
            if (Data.MyMap.Moral > 0)
            {
                if (Data.Moral[Data.MyMap.Moral].PlayerBlock)
                {
                    for (i = 0; i < Constant.MaxPlayers; i++)
                    {
                        if (IsPlaying(i))
                        {
                            if (Data.Player[i].X == x & Data.Player[i].Y == y)
                            {
                                checkPlayerDir = true;
                                return checkPlayerDir;
                            }
                        }
                    }
                }

                // Check to see if a Npc is already on that tile
                if (Data.Moral[Data.MyMap.Moral].NpcBlock)
                {
                    for (i = 0; i < Constant.MaxMapNpcs; i++)
                    {
                        if (Data.MyMapNpc[i].Num >= 0 & Data.MyMapNpc[i].X == x & Data.MyMapNpc[i].Y == y)
                        {
                            checkPlayerDir = true;
                            return checkPlayerDir;
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
                            checkPlayerDir = true;
                            return checkPlayerDir;
                        }
                    }
                }
            }

            return checkPlayerDir;
        }

        /// <summary>
        /// Update player facing based on mouse cursor screen position. Converts current player world position
        /// to screen center and sets an 8-direction facing, then sends direction packet.
        /// </summary>
        public static void UpdateFacingFromMouse(int mouseScreenX, int mouseScreenY)
        {
            if (GameState.MyIndex < 0 | GameState.MyIndex > Constant.MaxPlayers) return;
            int playerScreenX = GameLogic.ConvertMapX(GetPlayerRawX(GameState.MyIndex)) + GameState.SizeX / 2;
            int playerScreenY = GameLogic.ConvertMapY(GetPlayerRawY(GameState.MyIndex)) + GameState.SizeY / 2;
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
            if (Data.Player[GameState.MyIndex].Dir != (byte)dir)
            {
                Data.Player[GameState.MyIndex].Dir = (byte)dir;
                Sender.SendPlayerDir();
            }
        }

        public static void ProcessMovement(int index)
        {
            // BUGFIX: Previously gated all players' pixel movement on the LOCAL player's IsMoving flag,
            // causing remote players to slide only while you moved (and freeze otherwise), producing
            // desynced positions and camera jitter when targeting them. Now use each player's own flag.
            if (!IsPlaying(index)) return;
            if (!Data.Player[index].IsMoving) return;

            // Update per‑pixel offsets based on direction.
            // NOTE: This assumes 1px per tick step. If variable speed is desired later, introduce a per-player speed.
            switch (GetPlayerDir(index))
            {
                case (int)Direction.Up:
                    Data.Player[index].Y -= 1;
                    break;
                case (int)Direction.Down:
                    Data.Player[index].Y += 1;
                    break;
                case (int)Direction.Left:
                    Data.Player[index].X -= 1;
                    break;
                case (int)Direction.Right:
                    Data.Player[index].X += 1;
                    break;
                case (int)Direction.UpRight:
                    Data.Player[index].X += 1;
                    Data.Player[index].Y -= 1;
                    break;
                case (int)Direction.UpLeft:
                    Data.Player[index].X -= 1;
                    Data.Player[index].Y -= 1;
                    break;
                case (int)Direction.DownRight:
                    Data.Player[index].X += 1;
                    Data.Player[index].Y += 1;
                    break;
                case (int)Direction.DownLeft:
                    Data.Player[index].X -= 1;
                    Data.Player[index].Y += 1;
                    break;
            }
        }

        public static void CheckAttack(bool mouse = false)
        {
            int attackSpeed;
            var x = default(int);
            var y = default(int);

            if (GameState.VbKeyControl || mouse)
            {
                if (GameState.MyIndex < 0 | GameState.MyIndex > Constant.MaxPlayers)
                    return;

                if (Event.InEvent)
                    return;

                // If server is holding the player (e.g., on death), block attacks and show a quick local message
                if (Event.HoldPlayer)
                {
                    return;
                }

                var remaining = (int) (Data.Player[GameState.MyIndex].DeathTimer - General.GetTickCount()) / 1000;
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
                if (GetPlayerEquipment(GameState.MyIndex, Equipment.Weapon) >= 0)
                {
                    attackSpeed = Data.Item[GetPlayerEquipment(GameState.MyIndex, Equipment.Weapon)].Speed * 1000;
                }
                else
                {
                    attackSpeed = 1000;
                }

                if (Data.Player[GameState.MyIndex].AttackTimer + attackSpeed < General.GetTickCount())
                {
                    if (Data.Player[GameState.MyIndex].Attacking == 0)
                    {
                        {
                            ref var withBlock = ref Data.Player[GameState.MyIndex];
                            withBlock.Attacking = 1;
                            withBlock.AttackTimer = General.GetTickCount();
                        }

                        // If weapon has a projectile, send mouse-aimed attack with world pixel coords
                        int weapon = GetPlayerEquipment(GameState.MyIndex, Equipment.Weapon);
                        if (mouse && weapon >= 0 && Data.Item[weapon].Projectile >= 0)
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

                switch (Data.Player[GameState.MyIndex].Dir)
                {
                    case (byte) Direction.Up:
                    {
                        x = GetPlayerRawX(GameState.MyIndex);
                        y = GetPlayerRawY(GameState.MyIndex) - GameState.SizeY;
                        break;
                    }

                    case (byte) Direction.Down:
                    {
                        x = GetPlayerRawX(GameState.MyIndex);
                        y = GetPlayerRawY(GameState.MyIndex) + GameState.SizeY;
                        break;
                    }

                    case (byte) Direction.Left:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) - GameState.SizeX;
                        y = GetPlayerRawY(GameState.MyIndex);
                        break;
                    }
                    case (byte) Direction.Right:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) + GameState.SizeX;
                        y = GetPlayerRawY(GameState.MyIndex);
                        break;
                    }

                    case (byte) Direction.UpRight:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) + GameState.SizeX;
                        y = GetPlayerRawY(GameState.MyIndex) - GameState.SizeY;
                        break;
                    }

                    case (byte) Direction.UpLeft:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) - GameState.SizeX;
                        y = GetPlayerRawY(GameState.MyIndex) - GameState.SizeY;
                        break;
                    }

                    case (byte) Direction.DownRight:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) + GameState.SizeX;
                        y = GetPlayerRawY(GameState.MyIndex) + GameState.SizeY;
                        break;
                    }

                    case (byte) Direction.DownLeft:
                    {
                        x = GetPlayerRawX(GameState.MyIndex) - GameState.SizeX;
                        y = GetPlayerRawY(GameState.MyIndex) + GameState.SizeY;
                        break;
                    }
                }

                if (General.GetTickCount() > Data.Player[GameState.MyIndex].EventTimer)
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
                            if (Math.Abs(px - eventX) <= GameState.SizeX && Math.Abs(py - eventY) <= GameState.SizeY)
                            {
                                var packetWriter = new PacketWriter(8);

                                packetWriter.WriteEnum(Packets.ClientPackets.CEvent);
                                packetWriter.WriteInt32(i);

                                Network.Send(packetWriter);

                                Data.Player[GameState.MyIndex].EventTimer = General.GetTickCount() + 200;
                            }
                        }
                    }
                }
            }
        }

        public static void CastSkill(int skillSlot)
        {
            // Check for subscript out of range
            if (skillSlot < 0 | skillSlot > Constant.MaxPlayerSkills)
                return;

            if (Data.Player[GameState.MyIndex].Skill[skillSlot].Cd > 0)
            {
                TextRenderer.AddText("Skill has not cooled down yet!", (int) ColorName.BrightRed);
                return;
            }

            if (Data.Player[GameState.MyIndex].Skill[skillSlot].Num < 0)
                return;

            // Check if player has enough MP
            if (GetPlayerVital(GameState.MyIndex, Vital.Mana) < Data.Skill[Data.Player[GameState.MyIndex].Skill[skillSlot].Num].MpCost)
            {
                TextRenderer.AddText("Not enough mana to cast " + Data.Skill[Data.Player[GameState.MyIndex].Skill[skillSlot].Num].Name + ".", (int) ColorName.BrightRed);
                return;
            }

            if (Data.Player[GameState.MyIndex].Skill[skillSlot].Num >= 0)
            {
                if (General.GetTickCount() > Data.Player[GameState.MyIndex].AttackTimer + 1000)
                {
                    if (Data.Player[GameState.MyIndex].Moving == 0)
                    {
                        if (Data.MyMap.Moral >= 0)
                        {
                            if (Data.Moral[Data.MyMap.Moral].CanCast)
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
            if (skillNum < 0 | skillNum > Constant.MaxSkills)
            {
                return findSkill;
            }

            for (i = 0; i < Constant.MaxPlayerSkills; i++)
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

        #endregion

        #region Outgoing Traffic

        #endregion

        #region Incoming Traffic

        public static void Packet_PlayerHP(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);

            SetPlayerVital(GameState.MyIndex, Vital.Health, buffer.ReadInt32());
            SetPlayerMaxVital(GameState.MyIndex, Vital.Health, buffer.ReadInt32());

            // set max width
            if (GetPlayerVital(GameState.MyIndex, Vital.Health) > 0)
            {
                GameState.BarWidthGuiHPMax = (int) Math.Round(GetPlayerVital(GameState.MyIndex, Vital.Health) / 209d / (GetPlayerMaxVital(GameState.MyIndex, Vital.Health) / 209d) * 209d);
            }
            else
            {
                GameState.BarWidthGuiHPMax = 0;
            }

            WinCharacter.Update();
        }

        public static void Packet_PlayerMP(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);

            SetPlayerVital(GameState.MyIndex, Vital.Mana, buffer.ReadInt32());
            SetPlayerMaxVital(GameState.MyIndex, Vital.Mana, buffer.ReadInt32());

            // set max width
            if (GetPlayerVital(GameState.MyIndex, Vital.Mana) > 0)
            {
                GameState.BarWidthGuiMPMax = (int) Math.Round(GetPlayerVital(GameState.MyIndex, Vital.Mana) / 209d / (GetPlayerMaxVital(GameState.MyIndex, Vital.Mana) / 209d) * 209d);
            }
            else
            {
                GameState.BarWidthGuiMPMax = 0;
            }

            WinCharacter.Update();
        }

        public static void Packet_PlayerSP(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);

            SetPlayerVital(GameState.MyIndex, Vital.Stamina, buffer.ReadInt32());
            SetPlayerMaxVital(GameState.MyIndex, Vital.Stamina, buffer.ReadInt32());

            // set max width
            if (GetPlayerVital(GameState.MyIndex, Vital.Stamina) > 0)
            {
                GameState.BarWidthGuiSPMax = (int) Math.Round(GetPlayerVital(GameState.MyIndex, Vital.Stamina) / 209d / (GetPlayerMaxVital(GameState.MyIndex, Vital.Stamina) / 209d) * 209d);
            }
            else
            {
                GameState.BarWidthGuiSPMax = 0;
            }

            WinCharacter.Update();
        }

        public static void Packet_PlayerStats(ReadOnlyMemory<byte> data)
        {
            int i;
            int index;
            var buffer = new PacketReader(data);

            index = buffer.ReadInt32();

            int statCount = Enum.GetValues(typeof(Stat)).Length;
            for (i = 0; i < statCount; i++)
                SetPlayerStat(index, (Stat) i, buffer.ReadInt32());
        }

        public static void Packet_PlayerData(ReadOnlyMemory<byte> data)
        {
            int i;
            int x;
            var buffer = new PacketReader(data);

            i = buffer.ReadInt32();
            SetPlayerName(i, buffer.ReadString());
            SetPlayerJob(i, buffer.ReadInt32());
            SetPlayerLevel(i, buffer.ReadInt32());
            SetPlayerPoints(i, buffer.ReadInt32());
            SetPlayerSprite(i, buffer.ReadInt32());
            SetPlayerMap(i, buffer.ReadInt32());
            SetPlayerAccess(i, buffer.ReadByte());
            SetPlayerPk(i, buffer.ReadBoolean());
            Data.Player[i].Moving = 0;

            int statCount = Enum.GetValues(typeof(Stat)).Length;
            for (x = 0; x < statCount; x++)
                SetPlayerStat(i, (Stat) x, buffer.ReadInt32());

            int resourceSkillCount = Enum.GetValues(typeof(ResourceSkill)).Length;
            for (x = 0; x < resourceSkillCount; x++)
            {
                Data.Player[i].GatherSkills[x].SkillLevel = buffer.ReadInt32();
                Data.Player[i].GatherSkills[x].SkillCurExp = buffer.ReadInt32();
                Data.Player[i].GatherSkills[x].SkillNextLevelExp = buffer.ReadInt32();
            }

            // Check if the player is the client player
            if (i == GameState.MyIndex)
            {
                // Reset directions
                GameState.DirUp = false;
                GameState.DirDown = false;
                GameState.DirLeft = false;
                GameState.DirRight = false;

                // set form
                {
                    var withBlock = WindowManager.Windows[WindowManager.GetWindowIndex("winCharacter")];
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblName")].Text = "Name";
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblJob")].Text = "Job";
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblLevel")].Text = "Level";
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblGuild")].Text = "Guild";
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblName2")].Text = GetPlayerName(GameState.MyIndex);
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblJob2")].Text = Data.Job[GetPlayerJob(GameState.MyIndex)].Name;
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblLevel2")].Text = GetPlayerLevel(GameState.MyIndex).ToString();
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblGuild2")].Text = "None";
                    WinCharacter.Update();

                    // stats
                    for (x = 0; x < statCount; x++)
                        withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblStat_" + (x + 1))].Text = GetPlayerStat(GameState.MyIndex, (Stat) x).ToString();

                    // points
                    withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "lblPoints")].Text = GetPlayerPoints(GameState.MyIndex).ToString();

                    // grey out buttons
                    if (GetPlayerPoints(GameState.MyIndex) == 0)
                    {
                        for (x = 0; x < statCount; x++)
                            withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "btnGreyStat_" + (x + 1))].Visible = true;
                    }
                    else
                    {
                        for (x = 0; x < statCount; x++)
                            withBlock.Controls[WindowManager.GetControlIndex("winCharacter", "btnGreyStat_" + (x + 1))].Visible = false;
                    }
                }
                GameState.PlayerData = true;
            }
        }

        public static void Packet_StopPlayerMove(ReadOnlyMemory<byte> data)
        {
            int i;
            var buffer = new PacketReader(data);

            i = buffer.ReadInt32();

            // Make sure the player is in range
            if (i < 0 || i >= Constant.MaxPlayers)
                return;

            // Stop the player from moving
            Data.Player[i].Moving = 0;
            Data.Player[i].IsMoving = false; // ensure per-pixel movement halts client-side
        }

        public static void Packet_PlayerDir(ReadOnlyMemory<byte> data)
        {
            int dir;
            int i;
            var buffer = new PacketReader(data);

            i = buffer.ReadInt32();
            dir = buffer.ReadByte();

            SetPlayerDir(i, dir);
            
            // Do not reset local player's movement state on our own echoed dir packets; this causes micro-stutters
            if (i != GameState.MyIndex)
            {
                ref var withBlock = ref Data.Player[i];
                withBlock.Moving = 0;
            }
        }

        public static void Packet_PlayerExp(ReadOnlyMemory<byte> data)
        {
            int index;
            int tnl;
            var buffer = new PacketReader(data);
            int maxLevel = 0;

            index = buffer.ReadInt32();
            maxLevel = buffer.ReadInt32();
            GameState.MaxLevel = maxLevel;
            SetPlayerExp(index, buffer.ReadInt32());

            tnl = buffer.ReadInt32();
            GameState.NextlevelExp = tnl;

            // set max width
            if (GetPlayerLevel(GameState.MyIndex) < GameState.MaxLevel)
            {
                if (GetPlayerExp(GameState.MyIndex) > 0)
                {
                    GameState.BarWidthGuiExpMax = (int) Math.Round(GetPlayerExp(GameState.MyIndex) / 209d / (tnl / 209d) * 209d);
                }
                else
                {
                    GameState.BarWidthGuiExpMax = 0;
                }
            }
            else
            {
                GameState.BarWidthGuiExpMax = 209;
            }

            // Update GUI
            WinCharacter.Update();
        }

        public static void Packet_PlayerXY(ReadOnlyMemory<byte> data)
        {
            int x;
            int y;
            int dir;
            int index;
            byte moving;
            var buffer = new PacketReader(data);

            index = buffer.ReadInt32();
            x = buffer.ReadInt32();
            y = buffer.ReadInt32();
            dir = buffer.ReadByte();
            moving = buffer.ReadByte();

            SetPlayerX(index, x);
            SetPlayerY(index, y);
            SetPlayerDir(index, dir);
            Data.Player[index].Moving = moving;
            Data.Player[index].IsMoving = buffer.ReadBoolean();
        }

        #endregion
    }
}