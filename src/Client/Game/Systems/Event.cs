using System;
using Client.Game.UI;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using Microsoft.Xna.Framework;
using static Core.Globals.Type;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;
using static Core.Globals.Commands;
using System.IO;

namespace Client
{
    public class Event
    {
        #region Globals

        // Temp event storage
        public static Type.Event Instance;

        public static bool IsEdit;

        public static int CurPageNum;
        public static int CurCommand;
        public static int GraphicSelX;
        public static int GraphicSelY;
        public static int GraphicSelX2;
        public static int GraphicSelY2;

        public static int EventTileX;
        public static int EventTileY;

        public static int EditorId;

        public static int GraphicSelType;
        public static int TempMoveRouteCount;
        public static Type.MoveRoute[]? TempMoveRoute;
        public static bool IsMoveRouteCommand;
        public static int[]? ListOfEvents;

        public static int EventReplyId;
        public static int EventReplyPage;
        public static int EventChatFace;

        public static int RenameType;
        public static int RenameIndex;
        public static int EventChatTimer;

        public static bool EventChat;
        public static string EventText = "";
        public static bool ShowEventLbl;
        public static string[] EventChoices = new string[Core.Globals.Variables.MaxEventChoices];
        public static bool[] EventChoiceVisible = new bool[Core.Globals.Variables.MaxEventChoices];
        public static int EventChatType;
        public static int AnotherChat;

        // constants
        public static string[] Switches = new string[Core.Globals.Variables.MaxSwitches];
        public static string[] Variables = new string[Core.Globals.Variables.MaxVariables];

        // Client-side event movement bookkeeping (NPC-style stepped movement)
        private static readonly int[] RemainingPixels = new int[Core.Globals.Variables.MaxEvents];
        private static readonly int[] DestX = new int[Core.Globals.Variables.MaxEvents];
        private static readonly int[] DestY = new int[Core.Globals.Variables.MaxEvents];

        public static void StartStep(int id, int startX, int startY, byte dir)
        {
            if (id < 0 || id >= Core.Globals.Variables.MaxEvents) return;
            RemainingPixels[id] = Constants.TileSize;
            var (dx, dy) = GetDirectionDelta(dir, Constants.TileSize);
            DestX[id] = startX + dx;
            DestY[id] = startY + dy;
        }

        public static void SnapToDest(int id)
        {
            if (id < 0) return;
            if (Data.MapEvents == null) return;
            if (id >= Data.MapEvents.Length) return;
            if (RemainingPixels[id] > 0)
            {
                Data.MapEvents[id].X = DestX[id];
                Data.MapEvents[id].Y = DestY[id];
                RemainingPixels[id] = 0;
            }
        }

        private static (int dx, int dy) GetDirectionDelta(byte dir, int amount)
        {
            return (Direction)dir switch
            {
                Direction.Up => (0, -amount),
                Direction.Down => (0, amount),
                Direction.Left => (-amount, 0),
                Direction.Right => (amount, 0),
                _ => (0, 0)
            };
        }

        public static bool EventCopy;
        public static bool EventPaste;
        public static Type.EventList[]? EventList;
        public static Type.Event CopyEvent;
        public static Type.EventPage CopyEventPage;

        public static bool InEvent;
        public static bool HoldPlayer;

        public static Type.Picture Picture;

        public static void OnDraw(int id) // draw on map, outside the editor
        {
            int x;
            int y;
            int width;
            int height;
            var sRect = default(Microsoft.Xna.Framework.Rectangle);
            var spritetop = default(int);

            if (Data.MapEvents?[id].Visible == false)
            {
                return;
            }

            if (EditorType.Map == GameState.MyEditorType)
                return;

            switch (Data.MapEvents?[id].GraphicType)
            {
                case 0:
                    return;
                case 1:
                    {
                        // Segmented character event (idle/run/attack) mirroring player/NPC logic.
                        if (Data.MapEvents[id].Graphic <= 0 || Data.MapEvents[id].Graphic > GameState.NumCharacters)
                            return;

                        var gfxInfo = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, Data.MapEvents[id].Graphic.ToString()));
                        if (gfxInfo == null) return;

                        int directionRows = GameClient.ComputeDirectionRows(gfxInfo.Height, Math.Max(1, SettingsManager.Instance.SpriteDirections));
                        spritetop = GameClient.MapDirectionToRow((Direction)Data.MapEvents[id].ShowDir, directionRows);

                        int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
                        int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
                        int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
                        int expectedTotalColumns = idleFrames + runFrames + attackFrames;
                        int frameRowHeight = gfxInfo.Height / Math.Max(1, directionRows);
                        if (frameRowHeight <= 0) frameRowHeight = gfxInfo.Height; // safety fallback
                        int autoColsBySquare = frameRowHeight > 0 ? gfxInfo.Width / frameRowHeight : 1;
                        if (autoColsBySquare <= 0) autoColsBySquare = 1;
                        bool widthDivisible = expectedTotalColumns > 0 && gfxInfo.Width % expectedTotalColumns == 0;
                        bool canSegment = widthDivisible; // same relaxed heuristic as NPCs
                        int frameColumnsForWidth = canSegment ? expectedTotalColumns : autoColsBySquare;

                        // Segment ordering
                        string orderCsv = SettingsManager.Instance.SpriteSegmentOrder ?? "idle,run,attack";
                        var tokens = orderCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length != 3) tokens = new[] { "idle", "run", "attack" };
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

                        bool isMoving = Data.MapEvents[id].Moving != 0 && Data.MapEvents[id].IdleAnim == 0;
                        bool isAttacking = false; // events currently have no attack cycle; placeholder if added later

                        byte frameWithinSegment;
                        if (canSegment)
                        {
                            if (isAttacking)
                                frameWithinSegment = (byte)(Data.MapEvents[id].Steps % Math.Max(1, attackFrames));
                            else if (isMoving)
                                frameWithinSegment = (byte)(Data.MapEvents[id].Steps % Math.Max(1, runFrames));
                            else
                                frameWithinSegment = (byte)(Data.MapEvents[id].Steps % Math.Max(1, idleFrames));
                        }
                        else
                        {
                            frameWithinSegment = (byte)(Data.MapEvents[id].Steps % frameColumnsForWidth);
                        }

                        int segmentOffset = 0;
                        if (canSegment)
                        {
                            if (isAttacking) segmentOffset = attackOffset;
                            else if (isMoving) segmentOffset = runOffset;
                            else segmentOffset = idleOffset;
                        }
                        int frameColumn = Math.Min(frameColumnsForWidth - 1, segmentOffset + frameWithinSegment);

                        double frameWidthD = gfxInfo.Width / (double)frameColumnsForWidth;
                        double frameHeightD = frameRowHeight;
                        sRect = new Microsoft.Xna.Framework.Rectangle(
                            (int)Math.Round(frameColumn * frameWidthD),
                            (int)Math.Round(spritetop * frameHeightD),
                            (int)Math.Round(frameWidthD),
                            (int)Math.Round(frameHeightD));

                        width = sRect.Width;
                        height = sRect.Height;

                        // Center consistent with NPC/Player logic
                        x = (int)Math.Round(Data.MapEvents[id].X - (frameWidthD - 32d) / 2d);
                        if (frameRowHeight > 32)
                            y = (int)Math.Round(Data.MapEvents[id].Y - (frameHeightD - 32d));
                        else
                            y = Data.MapEvents[id].Y;

                        GameClient.DrawCharacterSprite(Data.MapEvents[id].Graphic, x, y, sRect);
                        break;
                    }
                case 2:
                    {
                        if (Data.MapEvents[id].Graphic < 1 |
                            Data.MapEvents[id].Graphic > GameState.NumTileSets)
                            return;

                        if (Data.MapEvents[id].GraphicY2 > 0 | Data.MapEvents[id].GraphicX2 > 0)
                        {
                            sRect.X = Data.MapEvents[id].GraphicX * 32;
                            sRect.Y = Data.MapEvents[id].GraphicY * 32;
                            sRect.Width = Data.MapEvents[id].GraphicX2 * 32;
                            sRect.Height = Data.MapEvents[id].GraphicY2 * 32;
                        }
                        else
                        {
                            sRect.X = Data.MapEvents[id].GraphicY * 32;
                            sRect.Height = sRect.Top + 32;
                            sRect.Y = Data.MapEvents[id].GraphicX * 32;
                            sRect.Width = sRect.Left + 32;
                        }

                        x = Data.MapEvents[id].X * 32;
                        y = Data.MapEvents[id].Y * 32;
                        x = (int)Math.Round(x - (sRect.Right - sRect.Left) / 2d);
                        y = y - (sRect.Bottom - sRect.Top) + 32;

                        if (Data.MapEvents[id].GraphicY2 > 1)
                        {
                            string argPath = Path.Combine(DataPath.Tilesets,
                                Data.MapEvents[id].Graphic.ToString());
                            GameClient.RenderTexture(ref argPath,
                                GameLogic.ConvertMapX(Data.MapEvents[id].X),
                                GameLogic.ConvertMapY(Data.MapEvents[id].Y) - Constants.TileSize,
                                sRect.Left, sRect.Top, sRect.Width, sRect.Height);
                        }
                        else
                        {
                            string argPath1 = Path.Combine(DataPath.Tilesets,
                                Data.MapEvents[id].Graphic.ToString());
                            GameClient.RenderTexture(ref argPath1,
                                GameLogic.ConvertMapX(Data.MapEvents[id].X),
                                GameLogic.ConvertMapY(Data.MapEvents[id].Y), sRect.Left,
                                sRect.Top,
                                sRect.Width, sRect.Height);
                        }

                        break;
                    }
            }
        }

        #endregion

        #region EventEditor

        public static void CopyEvent_Map(int X, int Y)
        {
            int count;
            int i;

            count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;
            if (count == 0)
                return;

            for (i = 0; i < count; i++)
            {
                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].X == X & Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Y == Y)
                {
                    CopyEvent = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i];
                    return;
                }
            }
        }

        public static void PasteEvent_Map(int x, int y)
        {
            int count;
            int i;
            int EventNum = -1;

            count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;

            if (count > 0)
            {
                for (i = 0; i < count; i++)
                {
                    if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].X == x & Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Y == y)
                    {
                        EventNum = i;
                    }
                }
            }

            // couldn't find one - create one
            if (EventNum == -1)
            {
                AddEvent(x, y, true);
                // Index of the newly added event is the last valid slot (0-based)
                EventNum = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount - 1;
            }

            // copy it
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[EventNum] = CopyEvent;

            // set position
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[EventNum].X = x;
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[EventNum].Y = y;
        }

        public static void DeleteEvent(int X, int Y)
        {
            if (GameState.MyEditorType != EditorType.Map)
                return;

            int mapIndex = GetPlayerMap(GameState.MyIndex);
            var map = Client.Map.Instance[mapIndex];

            int count = map.EventCount;
            if (count <= 0 || map.Event == null)
                return;

            int removeIndex = -1;
            for (int i = 0; i < count && i < map.Event.Length; i++)
            {
                if (map.Event[i].X == X && map.Event[i].Y == Y)
                {
                    removeIndex = i;
                    break;
                }
            }

            if (removeIndex < 0)
                return;

            // Shift down within the active range [0, count)
            for (int i = removeIndex; i < count - 1 && (i + 1) < map.Event.Length; i++)
                map.Event[i] = map.Event[i + 1];

            if (Data.MapEvents != null)
            {
                for (int i = removeIndex; i < count - 1 && (i + 1) < Data.MapEvents.Length; i++)
                    Data.MapEvents[i] = Data.MapEvents[i + 1];
            }

            // Decrement the logical count and resize arrays to keep a trailing empty slot.
            count = Math.Max(0, count - 1);
            map.EventCount = count;
            Array.Resize(ref map.Event, count + 1);
            if (Data.MapEvents != null)
                Array.Resize(ref Data.MapEvents, count + 1);

            // Ensure the trailing slot is clean/default so it can't be rendered as an "empty event".
            if (map.Event.Length > count)
                map.Event[count] = default;

            Client.Map.Instance[mapIndex] = map;
            Instance = default;
        }


        public static void AddEvent(int X, int Y, bool cancelLoad = false)
        {
            int count;
            int i;

            if (Event.InEvent)
                return;

            count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;

            // make sure there's not already an event
            if (count > 0)
            {
                for (i = 0; i < count; i++)
                {
                    if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].X == X & Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Y == Y)
                    {
                        // already an event - edit it
                        if (!cancelLoad)
                        {
                            GameState.InitEventEditor = true;
                            GameState.EventNum = i;
                            InEvent = true;
                        }
                        return;
                    }
                }
            }

            // increment count
            if (count == 0)
            {
                count = 1;
            }
            else
            {
                count++;
            }

            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount = count;
            Array.Resize(ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event, count + 1);
            Array.Resize(ref Data.MapEvents, count + 1);
            // Initialize the newly added event slot (0-based index is count - 1)
            ClearEvent(count - 1);
            // set the new event
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[count - 1].X = X;
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[count - 1].Y = Y;
            // ClearEvent already initialized a single page (PageCount=1),
            // so do NOT add another page here. New events should start with exactly 1 page.
            // load the editor
            if (!cancelLoad)
            {
                GameState.InitEventEditor = true;
                GameState.EventNum = count - 1;
                InEvent = true;
            }
        }

        public static void ClearEvent(int eventNum)
        {
            ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[eventNum];
            instance.Name = "";
            instance.PageCount = 1;
            instance.Pages = new Type.EventPage[1];
            Array.Resize(ref instance.Pages[0].CommandList, 1);
            Array.Resize(ref instance.Pages[0].CommandList[0].Commands, 1);
            instance.Pages[0].CommandList[0].Commands[0].Index = -1;
            instance.Globals = 0;
            instance.X = 0;
            instance.Y = 0;
        }

        public static void EventEditorInit()
        {
            int EventNum = GameState.EventNum;
            EditorId = EventNum;
            
            // Check if Event array is null or EventNum is out of bounds
            if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event == null || EventNum < 0 || EventNum >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event.Length)
            {
                // Initialize with a default empty event
                Instance = new Type.Event();
                return;
            }
            
            Instance = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[EventNum];
        }


        public static void EventEditorOK()
        {
            // copy the event data from the temp event
            Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[EditorId] = Instance;
        }

        #endregion

        #region Misc

        public static void OnMove(int id)
        {
            if (id < 0) return;
            if (Data.MapEvents == null) return;
            if (id >= Data.MapEvents.Length) return;
            if (id >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount) return;

            if (GameState.MyEditorType == EditorType.Map) return;

            // Only process active walking state
            if (Data.MapEvents[id].Moving != 1)
            {
                RemainingPixels[id] = 0;
                return;
            }

            // Initialize step bookkeeping if needed
            if (RemainingPixels[id] <= 0)
            {
                RemainingPixels[id] = Constants.TileSize;
                var (fullDx, fullDy) = GetDirectionDelta((byte)Data.MapEvents[id].Dir, Constants.TileSize);
                DestX[id] = Data.MapEvents[id].X + fullDx;
                DestY[id] = Data.MapEvents[id].Y + fullDy;
            }

            // Move 1px per walk tick (matches NPC/event server tick cadence)
            var (dx, dy) = GetDirectionDelta((byte)Data.MapEvents[id].Dir, 1);
            Data.MapEvents[id].X += dx;
            Data.MapEvents[id].Y += dy;
            RemainingPixels[id]--;

            if (RemainingPixels[id] <= 0)
            {
                // Clamp to planned destination to avoid drift
                Data.MapEvents[id].X = DestX[id];
                Data.MapEvents[id].Y = DestY[id];
                RemainingPixels[id] = 0;

                // Defensive: stop locally in case SEventDir is delayed/dropped.
                Data.MapEvents[id].Moving = 0;
            }
        }

        public static object GetColorString(int color)
        {
            object getColorString = default;

            switch (color)
            {
                case 0:
                {
                    getColorString = "Black";
                    break;
                }
                case 1:
                {
                    getColorString = "Blue";
                    break;
                }
                case 2:
                {
                    getColorString = "Green";
                    break;
                }
                case 3:
                {
                    getColorString = "Cyan";
                    break;
                }
                case 4:
                {
                    getColorString = "Red";
                    break;
                }
                case 5:
                {
                    getColorString = "Magenta";
                    break;
                }
                case 6:
                {
                    getColorString = "Brown";
                    break;
                }
                case 7:
                {
                    getColorString = "Grey";
                    break;
                }
                case 8:
                {
                    getColorString = "Dark Grey";
                    break;
                }
                case 9:
                {
                    getColorString = "Bright Blue";
                    break;
                }
                case 10:
                {
                    getColorString = "Bright Green";
                    break;
                }
                case 11:
                {
                    getColorString = "Bright Cyan";
                    break;
                }
                case 12:
                {
                    getColorString = "Bright Red";
                    break;
                }
                case 13:
                {
                    getColorString = "Pink";
                    break;
                }
                case 14:
                {
                    getColorString = "Yellow";
                    break;
                }
                case 15:
                {
                    getColorString = "White";
                    break;
                }

                default:
                {
                    getColorString = "Black";
                    break;
                }
            }

            return getColorString;
        }

        public static void ClearEventChat()
        {
            int i;

            if (AnotherChat == 1)
            {
                for (i = 0; i < Core.Globals.Variables.MaxEventChoices; i++)
                    EventChoiceVisible[i] = false;
                EventText = "";
                EventChatType = 1;
                EventChatTimer = General.GetTickCount() + 100;
            }
            else if (AnotherChat == 2)
            {
                for (i = 0; i < Core.Globals.Variables.MaxEventChoices; i++)
                    EventChoiceVisible[i] = false;
                EventText = "";
                EventChatType = 1;
                EventChatTimer = General.GetTickCount() + 100;
            }
            else
            {
                EventChat = false;
            }
        }

    #endregion

        public static void OnDrawName(int index)
        {
            if (Data.MapEvents == null) return;
            if (index < 0 || index >= Data.MapEvents.Length) return;

            var textY = 0;
            var color = Color.Green;
            var backcolor = Color.Black;
            var name = Data.MapEvents[index].Name;

            var uiFont = TextRenderer.ConfiguredFont;

            // X position: use same centering math as player names (sprite feet center)
            int baseWorldX = GameLogic.ConvertMapX(Data.MapEvents[index].X);
            var textWidth = TextRenderer.GetTextWidth(name, uiFont);
            if (!SettingsManager.Instance.BitmapFont)
            {
                textWidth = (int)Math.Round(textWidth * TextRenderer.BaseScale);
            }
            var drawX = baseWorldX + (Constants.TileSize - textWidth) / 2;

            if (Data.MapEvents[index].GraphicType == 1)
            {
                int sprite = Data.MapEvents[index].Graphic;
                if (sprite <= 0 || sprite > GameState.NumCharacters)
                {
                    textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
                }
                else
                {
                    var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, sprite.ToString()));
                    if (gfxInfo == null || gfxInfo.Height <= 0)
                    {
                        textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
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

                        int baseWorldY = Data.MapEvents[index].Y;
                        int spriteTopWorldY = baseWorldY;
                        if (frameHeight > 32) spriteTopWorldY = baseWorldY - (frameHeight - 32);

                        int spriteTopScreenY = GameLogic.ConvertMapY(spriteTopWorldY);
                        var spriteFont = TextRenderer.Fonts.TryGetValue(uiFont, out var sf) ? sf : TextRenderer.Fonts.Values.FirstOrDefault();
                        int textPixelHeight = (int)Math.Ceiling(((spriteFont?.LineSpacing ?? 16) * TextRenderer.BaseScale));
                        int margin = 8;
                        textY = spriteTopScreenY - textPixelHeight + margin;
                    }
                }
            }
            else if (Data.MapEvents[index].GraphicType == 2)
            {
                if (Data.MapEvents[index].GraphicY2 > 0)
                {
                    textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - Data.MapEvents[index].GraphicY2 * Constants.TileSize + 16;
                }
                else
                {
                    textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 32 + 16;
                }
            }
            else
            {
                textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
            }

            TextRenderer.Render(name, drawX, textY, color, backcolor, uiFont);
        }

        public static void OnDraw()
        {
            if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event == null)
                return;

            // Iterate only actual events to avoid drawing the trailing empty slot
            int count = Math.Max(0, Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount);
            for (int i = 0; i < count; i++)
            {
                if (i >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event.Length)
                    break;
                    
                // Treat MyMap.Event.X/Y as tile coordinates; compute world pixel coordinates
                int worldX = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].X * Constants.TileSize;
                int worldY = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Y * Constants.TileSize;

                // Skip event if there are no pages
                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount <= 0)
                {
                    GameClient.DrawOutlineRectangle(GameLogic.ConvertMapX(worldX), GameLogic.ConvertMapY(worldY), Constants.TileSize, Constants.TileSize, Color.Blue, 0.6f);
                    continue;
                }

                // Precompute screen coordinates once
                int screenX = GameLogic.ConvertMapX(worldX);
                int screenY = GameLogic.ConvertMapY(worldY);

                // Render event based on its graphic type
                switch (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[0].GraphicType)
                {
                    case 0: // Text Event (draw simple 'E' at the tile origin like other 32x32 textures)
                    {
                        TextRenderer.Render("E", screenX, screenY, Color.Green, Color.Black);
                        break;
                    }

                    case 1: // Character Graphic
                    {
                        GameClient.RenderCharacterGraphic(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i], screenX, screenY);
                        break;
                    }

                    case 2: // Tileset Graphic
                    {
                        GameClient.RenderTilesetGraphic(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i], screenX, screenY);
                        break;
                    }

                    default:
                    {
                        // Draw fallback outline if the graphic type is unknown
                        GameClient.DrawOutlineRectangle(GameLogic.ConvertMapX(worldX), GameLogic.ConvertMapY(worldY), Constants.TileSize, Constants.TileSize, Color.Blue, 0.6f);
                        break;
                    }
                }
            }
        }
    }
}