using System;
using Client.Game.UI;
using Client.Net;
using Core;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Xna.Framework;
using static Core.Globals.Type;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;
using static Core.Globals.Commands;

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

        public static bool EventCopy;
        public static bool EventPaste;
        public static Type.EventList[]? EventList;
        public static Type.Event CopyEvent;
        public static Type.EventPage CopyEventPage;

        public static bool InEvent;
        public static bool HoldPlayer;

        public static Type.Picture Picture;

        #endregion

        #region EventEditor

        public static void CopyEvent_Map(int X, int Y)
        {
            int count;
            int i;

            count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;
            if (count == 0)
                return;

            var loopTo = count;
            for (i = 0; i < loopTo; i++)
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
                var loopTo = count;
                for (i = 0; i < loopTo; i++)
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
            int i;
            int lowIndex = -1;

            if (GameState.MyEditorType != EditorType.Map)
                return;

            // First pass: find all events to delete and shift others down
            var loopTo = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount;
            for (i = 0; i < loopTo; i++)
            {
                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event.Length <= i)
                    break;

                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].X == X & Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Y == Y)
                {
                    // Clear the event
                    ClearEvent(i);
                    lowIndex = i;
                    break;
                }
            }

            if (lowIndex != -1)
            {
                for (i = lowIndex; i < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount; i++)
                {
                    if (Information.UBound(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event) > i)
                    {
                        Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i] = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i + 1];
                    }
                }

                for (i = lowIndex; i < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount; i++)
                {
                    if (Information.UBound(Data.MapEvents) > i)
                    {
                        if (Data.MapEvents == null)
                            break;
                        Data.MapEvents[i] = Data.MapEvents[i + 1];
                    }
                }

                Instance = default;
            }
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
                var loopTo = count;
                for (i = 0; i < loopTo; i++)
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
            // Guard: ensure event system and target index are valid
            if (id < 0) return;

            if (Data.MapEvents == null) return;
            if (id >= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount) return;
            if (id >= Data.MapEvents.Length) return;

            // Some events may be uninitialized structs (default). We can skip if MovementSpeed == 0 and name null/empty and not moving.
            if (Data.MapEvents[id].Moving != 1) return;

            if (GameState.MyEditorType == EditorType.Map)
                return;
                
            // Only process if actually moving toward next tile
            if (Data.MapEvents[id].Moving > 0)
            {
                int dir = Data.MapEvents[id].Dir;
                // Adjust position when heading Right or Down first (mimicking original intent)
                if (dir == (int)Direction.Right || dir == (int)Direction.Down ||
                    dir == (int)Direction.Left || dir == (int)Direction.Up)
                {
                    switch (dir)
                    {
                        case (int)Direction.Up:
                            Data.MapEvents[id].Y -= 1;
                            break;
                        case (int)Direction.Down:
                            Data.MapEvents[id].Y += 1;
                            break;
                        case (int)Direction.Left:
                            Data.MapEvents[id].X -= 1;
                            break;
                        case (int)Direction.Right:
                            Data.MapEvents[id].X += 1;
                            break;
                    }
                }
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

            // X position: use same centering math as player names (sprite feet center)
            int baseWorldX = Data.MapEvents[index].X;
            int feetCenterX = GameLogic.ConvertMapX(baseWorldX) + Constants.TileSize / 2 - 4;
            var textX = feetCenterX - (int)(TextRenderer.Fonts[Font.Georgia].MeasureString(name).X / 2f);

            if (Data.MapEvents[index].GraphicType == 1)
            {
                int spriteNum = Data.MapEvents[index].Graphic;
                if (spriteNum <= 0 || spriteNum > GameState.NumCharacters)
                {
                    textY = GameLogic.ConvertMapY(Data.MapEvents[index].Y) - 16;
                }
                else
                {
                    var gfxInfo = GameClient.GetGfxInfo(System.IO.Path.Combine(Core.Globals.DataPath.Characters, spriteNum.ToString()));
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
                        int textPixelHeight = (int)Math.Ceiling(TextRenderer.Fonts[Font.Georgia].LineSpacing * TextRenderer.BaseScale);
                        int margin = 8;
                        textY = spriteTopScreenY - textPixelHeight + margin;
                    }
                }
            }
            else if (Data.MapEvents[index].GraphicType == 2)
            {
                if (Data.MapEvents[index].GraphicY2 > 0)
                {
                    textX = textX + Data.MapEvents[index].GraphicY2 * Constants.TileSize / 2 - 6;
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

            TextRenderer.OnDraw(name, textX, textY, color, backcolor);
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
                        TextRenderer.OnDraw("E", screenX, screenY, Color.Green, Color.Black);
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