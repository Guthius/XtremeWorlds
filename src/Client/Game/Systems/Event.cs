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

            count = Data.MyMap.EventCount;
            if (count == 0)
                return;

            var loopTo = count;
            for (i = 0; i < loopTo; i++)
            {
                if (Data.MyMap.Event[i].X == X & Data.MyMap.Event[i].Y == Y)
                {
                    CopyEvent = Data.MyMap.Event[i];
                    return;
                }
            }
        }

        public static void PasteEvent_Map(int x, int y)
        {
            int count;
            int i;
            int EventNum = -1;

            count = Data.MyMap.EventCount;

            if (count > 0)
            {
                var loopTo = count;
                for (i = 0; i < loopTo; i++)
                {
                    if (Data.MyMap.Event[i].X == x & Data.MyMap.Event[i].Y == y)
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
                EventNum = Data.MyMap.EventCount - 1;
            }

            // copy it
            Data.MyMap.Event[EventNum] = CopyEvent;

            // set position
            Data.MyMap.Event[EventNum].X = x;
            Data.MyMap.Event[EventNum].Y = y;
        }

        public static void DeleteEvent(int X, int Y)
        {
            int i;
            int lowIndex = -1;

            if (GameState.MyEditorType != EditorType.Map)
                return;

            // First pass: find all events to delete and shift others down
            var loopTo = Data.MyMap.EventCount;
            for (i = 0; i < loopTo; i++)
            {
                if (Data.MyMap.Event.Length <= i)
                    break;

                if (Data.MyMap.Event[i].X == X & Data.MyMap.Event[i].Y == Y)
                {
                    // Clear the event
                    ClearEvent(i);
                    lowIndex = i;
                    break;
                }
            }

            if (lowIndex != -1)
            {
                for (i = lowIndex; i < Data.MyMap.EventCount; i++)
                {
                    if (Information.UBound(Data.MyMap.Event) > i)
                    {
                        Data.MyMap.Event[i] = Data.MyMap.Event[i + 1];
                    }
                }

                for (i = lowIndex; i < Data.MyMap.EventCount; i++)
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

            count = Data.MyMap.EventCount;

            // make sure there's not already an event
            if (count > 0)
            {
                var loopTo = count;
                for (i = 0; i < loopTo; i++)
                {
                    if (Data.MyMap.Event[i].X == X & Data.MyMap.Event[i].Y == Y)
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

            Data.MyMap.EventCount = count;
            Array.Resize(ref Data.MyMap.Event, count + 1);
            Array.Resize(ref Data.MapEvents, count + 1);
            // Initialize the newly added event slot (0-based index is count - 1)
            ClearEvent(count - 1);
            // set the new event
            Data.MyMap.Event[count - 1].X = X;
            Data.MyMap.Event[count - 1].Y = Y;
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
            ref var instance = ref Data.MyMap.Event[eventNum];
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
            if (Data.MyMap.Event == null || EventNum < 0 || EventNum >= Data.MyMap.Event.Length)
            {
                // Initialize with a default empty event
                Instance = new Type.Event();
                return;
            }
            
            Instance = Data.MyMap.Event[EventNum];
        }

        public static void EventEditorLoadPage(int pageNum)
        {
            if (Event.Instance.Pages == null)
                return;

            if (pageNum < 0 || pageNum >= Instance.Pages.Length || Instance.Pages == null)
            {
                // Invalid page number, return or throw an exception
                return;
            }

            // Guard UI updates to avoid firing change handlers
            EditorEvent.Instance.BeginPageSync();
            try
            {
            ref var instance = ref Instance.Pages[pageNum];
            GraphicSelX = instance.GraphicX;
            GraphicSelY = instance.GraphicY;
            GraphicSelX2 = instance.GraphicX2;
            GraphicSelY2 = instance.GraphicY2;
            EditorEvent.Instance.cmbGraphic.SelectedIndex = instance.GraphicType;
            EditorEvent.Instance.cmbHasItem.SelectedIndex = instance.HasItemIndex;
            if (instance.HasItemAmount == 0)
            {
                EditorEvent.Instance.nudCondition_HasItem.Value = 1;
            }
            else
            {
                EditorEvent.Instance.nudCondition_HasItem.Value = instance.HasItemAmount;
            }

            EditorEvent.Instance.cmbMoveFreq.SelectedIndex = instance.MoveFreq;
            EditorEvent.Instance.cmbMoveSpeed.SelectedIndex = instance.MoveSpeed;
            EditorEvent.Instance.cmbMoveType.SelectedIndex = instance.MoveType;
            EditorEvent.Instance.cmbPlayerVar.SelectedIndex = instance.VariableIndex;
            EditorEvent.Instance.cmbPlayerSwitch.SelectedIndex = instance.SwitchIndex;
            EditorEvent.Instance.cmbSelfSwitchCompare.SelectedIndex = instance.SelfSwitchCompare;
            EditorEvent.Instance.cmbSelfSwitch.SelectedIndex = instance.SelfSwitchIndex;
            EditorEvent.Instance.cmbPlayerSwitchCompare.SelectedIndex = instance.SwitchCompare;
            EditorEvent.Instance.cmbPlayerVarCompare.SelectedIndex = instance.VariableCompare;
            EditorEvent.Instance.chkGlobal.Checked = Conversions.ToBoolean(Instance.Globals);
            EditorEvent.Instance.cmbTrigger.SelectedIndex = instance.Trigger;
            EditorEvent.Instance.chkDirFix.Checked = Conversions.ToBoolean(instance.DirFix);
            EditorEvent.Instance.chkHasItem.Checked = Conversions.ToBoolean(instance.ChkHasItem);
            EditorEvent.Instance.chkPlayerVar.Checked = Conversions.ToBoolean(instance.ChkVariable);
            EditorEvent.Instance.chkPlayerSwitch.Checked = Conversions.ToBoolean(instance.ChkSwitch);
            EditorEvent.Instance.chkSelfSwitch.Checked = Conversions.ToBoolean(instance.ChkSelfSwitch);
            EditorEvent.Instance.chkWalkAnim.Checked = Conversions.ToBoolean(instance.IdleAnim);
            EditorEvent.Instance.chkWalkThrough.Checked = Conversions.ToBoolean(instance.WalkThrough);
            EditorEvent.Instance.chkShowName.Checked = Conversions.ToBoolean(instance.ShowName);
            EditorEvent.Instance.nudPlayerVariable.Value = instance.VariableCondition;
            EditorEvent.Instance.nudGraphic.Value = instance.Graphic;
            // Event-level fields
            EditorEvent.Instance.txtName.Text = Instance.Name ?? string.Empty;

            if (instance.ChkSelfSwitch == 0)
            {
                EditorEvent.Instance.cmbSelfSwitch.Enabled = false;
                EditorEvent.Instance.cmbSelfSwitchCompare.Enabled = false;
            }
            else
            {
                EditorEvent.Instance.cmbSelfSwitch.Enabled = true;
                EditorEvent.Instance.cmbSelfSwitchCompare.Enabled = true;
            }

            if (instance.ChkSwitch == 0)
            {
                EditorEvent.Instance.cmbPlayerSwitch.Enabled = false;
                EditorEvent.Instance.cmbPlayerSwitchCompare.Enabled = false;
            }
            else
            {
                EditorEvent.Instance.cmbPlayerSwitch.Enabled = true;
                EditorEvent.Instance.cmbPlayerSwitchCompare.Enabled = true;
            }

            if (instance.ChkVariable == 0)
            {
                EditorEvent.Instance.cmbPlayerVar.Enabled = false;
                EditorEvent.Instance.nudPlayerVariable.Enabled = false;
                EditorEvent.Instance.cmbPlayerVarCompare.Enabled = false;
            }
            else
            {
                EditorEvent.Instance.cmbPlayerVar.Enabled = true;
                EditorEvent.Instance.nudPlayerVariable.Enabled = true;
                EditorEvent.Instance.cmbPlayerVarCompare.Enabled = true;
            }

            if (EditorEvent.Instance.cmbMoveType.SelectedIndex == 2)
            {
                EditorEvent.Instance.btnMoveRoute.Enabled = true;
            }
            else
            {
                EditorEvent.Instance.btnMoveRoute.Enabled = false;
            }

            EditorEvent.Instance.cmbPositioning.SelectedIndex = int.Parse(instance.Position.ToString());
            // Refresh the UI list on the UI thread
            try
            {
                Eto.Forms.Application.Instance?.Invoke(() => EventListCommands());
            }
            catch
            {
                EventListCommands();
            }
            }
            finally
            {
                EditorEvent.Instance.EndPageSync();
            }
        }

        public static void EventEditorOK()
        {
            // copy the event data from the temp event
            Data.MyMap.Event[EditorId] = Instance;
        }

        public static void EventListCommands()
        {
            if (Instance.Pages == null)
                return;

            // Marshal the entire list build onto the UI thread to avoid cross-thread access
            if (Eto.Forms.Application.Instance != null)
            {
                try
                {
                    Eto.Forms.Application.Instance.Invoke(EventListCommandsCore);
                    return;
                }
                catch
                {
                    // fall through to direct call if Invoke fails
                }
            }

            EventListCommandsCore();
        }

        // Core implementation that assumes it's running on the UI thread
        private static void EventListCommandsCore()
        {
            int i;
            int curlist;
            int X;
            string indent = "";
            int[] listleftoff;
            int[] conditionalstage;

            EditorEvent.Instance.lstCommands.Items.Clear();

            if (Instance.Pages[CurPageNum].CommandListCount > 0)
            {
                listleftoff = new int[Instance.Pages[CurPageNum].CommandListCount];
                conditionalstage = new int[Instance.Pages[CurPageNum].CommandListCount];
                curlist = 0;
                X = 0;
                Array.Resize(ref EventList, X + 1);
                newlist:
                var loopTo = Instance.Pages[CurPageNum].CommandList[curlist].CommandCount;
                for (i = 0; i < loopTo; i++)
                {
                    if (listleftoff[curlist] > 0)
                    {
                        if ((Instance.Pages[CurPageNum].CommandList[curlist].Commands[listleftoff[curlist]].Index == (int) EventCommand.ConditionalBranch | Instance.Pages[CurPageNum].CommandList[curlist].Commands[listleftoff[curlist]].Index == (int) EventCommand.ShowChoices) & conditionalstage[curlist] != 0)
                        {
                            i = listleftoff[curlist];
                        }
                        else if (listleftoff[curlist] >= i)
                        {
                            i = listleftoff[curlist] + 1;
                        }
                    }

                    if (i < Instance.Pages[CurPageNum].CommandList[curlist].CommandCount)
                    {
                        if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Index == (int) EventCommand.ConditionalBranch)
                        {
                            X = X + 1;
                            Array.Resize(ref EventList, X + 1);
                            switch (conditionalstage[curlist])
                            {
                                case 0:
                                {
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = i;
                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Condition)
                                    {
                                        case 0:
                                        {
                                            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2)
                                            {
                                                case 0:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] == " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 1:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] >= " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 2:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] <= " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 3:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] > " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 4:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] < " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 5:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] != " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                        case 1:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Switch [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Switches[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + 1] + "] == " + "True");
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Switch [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Switches[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + 1] + "] == " + "False");
                                            }

                                            break;
                                        }
                                        case 2:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Has Item [" + Item.Instance[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1].Name + "] x" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2);
                                            break;
                                        }
                                        case 3:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Job Is [" + Strings.Trim(Data.Job[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1].Name) + "]");
                                            break;
                                        }
                                        case 4:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Knows Skill [" + Strings.Trim(Data.Skill[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1].Name) + "]");
                                            break;
                                        }
                                        case 5:
                                        {
                                            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2)
                                            {
                                                case 0:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is == " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 1:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is >= " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 2:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is <= " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 3:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is > " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 4:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is < " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 5:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is NOT " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                        case 6:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 0)
                                            {
                                                switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
                                                {
                                                    case 0:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [A] == " + "True");
                                                        break;
                                                    }
                                                    case 1:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [B] == " + "True");
                                                        break;
                                                    }
                                                    case 2:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [C] == " + "True");
                                                        break;
                                                    }
                                                    case 3:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [D] == " + "True");
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 1)
                                            {
                                                switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
                                                {
                                                    case 0:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [A] == " + "False");
                                                        break;
                                                    }
                                                    case 1:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [B] == " + "False");
                                                        break;
                                                    }
                                                    case 2:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [C] == " + "False");
                                                        break;
                                                    }
                                                    case 3:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Self Switch [D] == " + "False");
                                                        break;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                        case 7:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 0)
                                            {
                                                switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3)
                                                {
                                                    case 0:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] not started.");
                                                        break;
                                                    }
                                                    case 1:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] is started.");
                                                        break;
                                                    }
                                                    case 2:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] is completed.");
                                                        break;
                                                    }
                                                    case 3:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] can be started.");
                                                        break;
                                                    }
                                                    case 4:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] can be ended. (All tasks complete)");
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] in progress and on task #" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                            }

                                            break;
                                        }
                                        case 8:
                                        {
                                            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
                                            {
                                                case (int) Sex.Male:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Gender is Male");
                                                    break;
                                                }
                                                case (int) Sex.Female:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's  Gender is Female");
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                        case 9:
                                        {
                                            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
                                            {
                                                case (int) TimeOfDay.Day:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Time of Day is Day");
                                                    break;
                                                }
                                                case (int) TimeOfDay.Night:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Time of Day is Night");
                                                    break;
                                                }
                                                case (int) TimeOfDay.Dawn:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Time of Day is Dawn");
                                                    break;
                                                }
                                                case (int) TimeOfDay.Dusk:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Time of Day is Dusk");
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                    }

                                    indent = indent + "       ";
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 1;
                                    curlist = Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.CommandList;
                                    goto newlist;
                                }
                                case 1:
                                {
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = 0;
                                    EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "Else");
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 2;
                                    curlist = Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.ElseCommandList;
                                    goto newlist;
                                }
                                case 2:
                                {
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = 0;
                                    EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "End Branch");
                                    indent = Strings.Mid(indent, 1, Strings.Len(indent) - 7);
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 0;
                                    break;
                                }
                            }
                        }
                        else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Index == (int) EventCommand.ShowChoices)
                        {
                            X = X + 1;
                            switch (conditionalstage[curlist])
                            {
                                case 0:
                                {
                                    Array.Resize(ref EventList, X + 1);
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = i;
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Choices - Prompt: " + Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20));
                                    indent = indent + "       ";
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 1;
                                    goto newlist;
                                }
                                case 1:
                                {
                                        if (!string.IsNullOrEmpty(Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text2)))
                                        {
                                            Array.Resize(ref EventList, X + 1);
                                            EventList[X].CommandList = 7;
                                            EventList[X].CommandNum = 0;
                                            EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text2) + "]");
                                            listleftoff[curlist] = i;
                                            conditionalstage[curlist] = 2;
                                            curlist = Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1;
                                            goto newlist;
                                        }
                                        else
                                        {
                                            X = X - 1;
                                            Array.Resize(ref EventList, X + 1);
                                            listleftoff[curlist] = i;
                                            conditionalstage[curlist] = 2;
                                            goto newlist;
                                        }
                                }
                                case 2:
                                {
                                    if (!string.IsNullOrEmpty(Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text3)))
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                        EventList[X].CommandList = curlist;
                                        EventList[X].CommandNum = 0;
                                        EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text3) + "]");
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 3;
                                        curlist = Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2;
                                        goto newlist;
                                    }
                                    else
                                    {
                                        X = X - 1;
                                        Array.Resize(ref EventList, X + 1);
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 3;
                                        goto newlist;
                                    }
                                }
                                case 3:
                                {
                                    if (!string.IsNullOrEmpty(Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text4)))
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                        EventList[X].CommandList = curlist;
                                        EventList[X].CommandNum = 0;
                                        EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text4) + "]");
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 4;
                                        curlist = Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3;
                                        goto newlist;
                                    }
                                    else
                                    {
                                        X = X - 1;
                                        Array.Resize(ref EventList, X + 1);
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 4;
                                        goto newlist;
                                    }
                                }
                                case 4:
                                {
                                    if (!string.IsNullOrEmpty(Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text5)))
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                        EventList[X].CommandList = curlist;
                                        EventList[X].CommandNum = 0;
                                        EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text5) + "]");
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 5;
                                        curlist = Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4;
                                        goto newlist;
                                    }
                                    else
                                    {
                                        X = X - 1;
                                        Array.Resize(ref EventList, X + 1);
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 5;
                                        goto newlist;
                                    }
                                }
                                case 5:
                                {
                                    Array.Resize(ref EventList, X + 1);
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = 0;
                                    EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "Branch End");
                                    indent = Strings.Mid(indent, 1, Strings.Len(indent) - 7);
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 0;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            X = X + 1;
                            Array.Resize(ref EventList, X + 1);
                            EventList[X].CommandList = curlist;
                            EventList[X].CommandNum = i;
                            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Index)
                            {
                                case (byte) EventCommand.AddText:
                                {
                                    // Build the preview text safely as a string (avoid VB Operators.ConcatenateObject which returns object)
                                    string textPreview = Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20);
                                    string colorStr = Convert.ToString(GetColorString(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1));
                                    string chatType;
                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2)
                                    {
                                        case 0:
                                            chatType = "Player";
                                            break;
                                        case 1:
                                            chatType = "Map";
                                            break;
                                        case 2:
                                            chatType = "Global";
                                            break;
                                        default:
                                            chatType = "Unknown";
                                            break;
                                    }
                                    EditorEvent.Instance.lstCommands.Items.Add($"{indent}@>Add Text - {textPreview}... - Color: {colorStr} - Chat Type: {chatType}");
                                    break;
                                }
                                case (byte) EventCommand.ShowText:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Text - " + Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20));
                                    break;
                                }
                                case (byte) EventCommand.ModifyVariable:
                                {
                                    string variableValue = Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1];
                                    if (variableValue == "")
                                        variableValue = ": None";
                                    else
                                        variableValue = ": " + variableValue;

                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2)
                                    {
                                        case 0:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] == " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                            break;
                                        }
                                        case 1:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] + " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                            break;
                                        }
                                        case 2:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] - " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                            break;
                                        }
                                        case 3:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] Random Between " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " and " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4);
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ModifySwitch:
                                {
                                    string switchValue = Variables[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1];
                                    if (switchValue == "")
                                        switchValue = ": None";
                                    else
                                        switchValue = ": " + switchValue;

                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Switch [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + switchValue + "] == False");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Switch [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + switchValue + "] == True");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ModifySelfSwitch:
                                {
                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1)
                                    {
                                        case 0:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [A] to Off");
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [A] to On");
                                            }

                                            break;
                                        }
                                        case 1:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [B] to Off");
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [B] to On");
                                            }

                                            break;
                                        }
                                        case 2:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [C] to Off");
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [C] to On");
                                            }

                                            break;
                                        }
                                        case 3:
                                        {
                                            if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [D] to Off");
                                            }
                                            else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [D] to On");
                                            }

                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ExitEventProcess:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Exit Event Processing");
                                    break;
                                }
                                case (byte) EventCommand.ChangeItems:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Item Amount of [" + Item.Instance[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "] to " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Give Player " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " " + Item.Instance[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "(s)");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 2)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Take " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " " + Item.Instance[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "(s) from Player.");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.RestoreHealth:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Restore Player HP");
                                    break;
                                }
                                case (byte) EventCommand.RestoreMana:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Restore Player MP");
                                    break;
                                }
                                case (byte) EventCommand.RestoreStamina:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Restore Player SP");
                                    break;
                                }
                                case (byte) EventCommand.LevelUp:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Level Up Player");
                                    break;
                                }
                                case (byte) EventCommand.ChangeLevel:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Level to " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1);
                                    break;
                                }
                                case (byte) EventCommand.ChangeSkills:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Teach Player Skill [" + Strings.Trim(Data.Skill[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name) + "]");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Remove Player Skill [" + Strings.Trim(Data.Skill[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name) + "]");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ChangeJob:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Job to " + Strings.Trim(Data.Job[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name));
                                    break;
                                }
                                case (byte) EventCommand.ChangeSprite:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Sprite to " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1);
                                    break;
                                }
                                case (byte) EventCommand.ChangeSex:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Sex to Male.");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Sex to Female.");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.SetPlayerKillable:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player PK to No.");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player PK to Yes.");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.WarpPlayer:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") while retaining direction.");
                                    }
                                    else
                                    {
                                        switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4 - 1)
                                        {
                                            case (int) Direction.Up:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing upward.");
                                                break;
                                            }
                                            case (int) Direction.Down:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing downward.");
                                                break;
                                            }
                                            case (int) Direction.Left:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing left.");
                                                break;
                                            }
                                            case (int) Direction.Right:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing right.");
                                                break;
                                            }
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.SetMoveRoute:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 <= Data.MyMap.EventCount)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Move Route for Event #" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " [" + Data.MyMap.Event[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]");
                                    }
                                    else
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Move Route for COULD NOT FIND EVENT!");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.PlayAnimation:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Animation " + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + " [" + Data.Animation[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]" + " On Player");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Animation " + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + " [" + Data.Animation[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]" + " On Event " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " [" + Strings.Trim(Data.MyMap.Event[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3].Name) + "]");
                                    }
                                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 2)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Animation " + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + " [" + Data.Animation[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]" + " On Tile (" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4 + ")");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.PlayBgm:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play BGM [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1 + "]");
                                    break;
                                }
                                case (byte) EventCommand.FadeOutBgm:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Fadeout BGM");
                                    break;
                                }
                                case (byte) EventCommand.PlaySound:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Sound [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1 + "]");
                                    break;
                                }
                                case (byte) EventCommand.StopSound:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Stop Sound");
                                    break;
                                }
                                case (byte) EventCommand.OpenBank:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Open Bank");
                                    break;
                                }
                                case (byte) EventCommand.OpenShop:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Open Shop [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ". " + Data.Shop[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]");
                                    break;
                                }
                                case (byte) EventCommand.SetAccessLevel:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Access [" + EditorEvent.Instance.cmbSetAccess.Items[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 - 1]);
                                    break;
                                }
                                case (byte) EventCommand.GiveExperience:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Give Player " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Experience.");
                                    break;
                                }
                                case (byte) EventCommand.ShowChatBubble:
                                {
                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1)
                                    {
                                        case (int) TargetType.Player:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Player");
                                            break;
                                        }
                                        case (int) TargetType.Npc:
                                        {
                                            if (Data.MyMap.Npc[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2] <= 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Npc [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + 1).ToString() + ". ]");
                                            }
                                            else
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Npc [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + 1).ToString() + ". " + Data.Npc[Data.MyMap.Npc[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2]].Name + "]");
                                            }

                                            break;
                                        }
                                        case (int) TargetType.Event:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Event [" + (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + 1).ToString() + ". " + Data.MyMap.Event[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2].Name + "]");
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.Label:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Label: [" + Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1) + "]");
                                    break;
                                }
                                case (byte) EventCommand.GoToLabel:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Jump to Label: [" + Strings.Trim(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1) + "]");
                                    break;
                                }
                                case (byte) EventCommand.SpawnNpc:
                                {
                                    if (Data.MyMap.Npc[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1] <= 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Spawn Npc: [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ". " + "]");
                                    }
                                    else
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Spawn Npc: [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ". " + Data.Npc[Data.MyMap.Npc[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1]].Name + "]");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.FadeIn:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Fade In");
                                    break;
                                }
                                case (byte) EventCommand.FadeOut:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Fade Out");
                                    break;
                                }
                                case (byte) EventCommand.FlashScreen:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Flash White");
                                    break;
                                }
                                case (byte) EventCommand.SetFog:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Fog [Fog: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + " Speed: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + " Opacity: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3.ToString() + "]");
                                    break;
                                }
                                case (byte) EventCommand.SetWeather:
                                {
                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1)
                                    {
                                        case (int) WeatherType.None:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [None]");
                                            break;
                                        }
                                        case (int) WeatherType.Rain:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Rain - Intensity: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                        case (int) WeatherType.Snow:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Snow - Intensity: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                        case (int) WeatherType.Sandstorm:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Sand Storm - Intensity: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                        case (int) WeatherType.Storm:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Storm - Intensity: " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.SetScreenTint:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Map Tint RGBA [" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3.ToString() + "," + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4.ToString() + "]");
                                    break;
                                }
                                case (byte) EventCommand.Wait:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Wait " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + " Ms");
                                    break;
                                }
                                case (byte) EventCommand.ShowPicture:
                                {
                                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2)
                                    {
                                        case 0:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " Top Left, X: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                        case 1:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " Center Screen, X: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                        case 2:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " On Event, X: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                        case 3:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " On Player, X: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.HidePicture:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Hide Picture " + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString());
                                    break;
                                }
                                case (byte) EventCommand.WaitMovementCompletion:
                                {
                                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 <= Data.MyMap.EventCount)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Wait for Event #" + Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " [" + Strings.Trim(Data.MyMap.Event[Instance.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name) + "] to complete move route.");
                                    }
                                    else
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Wait for COULD NOT FIND EVENT to complete move route.");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.HoldPlayer:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Hold Player [Do not allow player to move.]");
                                    break;
                                }
                                case (byte) EventCommand.ReleasePlayer:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Release Player [Allow player to turn and move again.]");
                                    break;
                                }

                                default:
                                {
                                    // Ghost
                                    X = X - 1;
                                    if (X == -1)
                                    {
                                        EventList = new Type.EventList[1];
                                    }
                                    else
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                if (curlist > 1)
                {
                    X = X + 1;
                    Array.Resize(ref EventList, X + 1);
                    EventList[X].CommandList = curlist;
                    EventList[X].CommandNum = Instance.Pages[CurPageNum].CommandList[curlist].CommandCount;
                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@> ");
                    curlist = Instance.Pages[CurPageNum].CommandList[curlist].ParentList;
                    goto newlist;
                }
            }

            EditorEvent.Instance.lstCommands.Items.Add(indent + "@> ");

            var z = default(int);
            X = 0;
            var loopTo1 = EditorEvent.Instance.lstCommands.Items.Count;
            for (i = 0; i < loopTo1; i++)
            {
                if (X > z)
                    z = X;
            }
        }

        public static void AddCommand(int Index)
        {
            int curlist;
            var i = default(int);
            var X = default(int);
            int curslot;
            int p;
            Type.CommandList oldCommandList;

            // Determine the current list index safely
            var selIndex = EditorEvent.Instance.lstCommands.SelectedIndex;
            if (selIndex == -1 || EventList == null || selIndex < 0 || selIndex >= EventList.Length)
            {
                curlist = 0;
            }
            else
            {
                curlist = EventList[selIndex].CommandList;
            }

            Instance.Pages[CurPageNum].CommandListCount += 1;
            Array.Resize(ref Instance.Pages[CurPageNum].CommandList, Instance.Pages[CurPageNum].CommandListCount);
            Instance.Pages[CurPageNum].CommandList[curlist].CommandCount += 1;
            p = Instance.Pages[CurPageNum].CommandList[curlist].CommandCount;
            Array.Resize(ref Instance.Pages[CurPageNum].CommandList[curlist].Commands, p);

            if (EditorEvent.Instance.lstCommands.SelectedIndex + 1 == EditorEvent.Instance.lstCommands.Items.Count)
            {
                curslot = Instance.Pages[CurPageNum].CommandList[curlist].CommandCount - 1;
            }
            else
            {
                oldCommandList = Instance.Pages[CurPageNum].CommandList[curlist];
                Instance.Pages[CurPageNum].CommandList[curlist].ParentList = oldCommandList.ParentList;

                // copy old commands into resized array
                for (int j = 0; j < oldCommandList.CommandCount; j++)
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[j] = oldCommandList.Commands[j];

                // Determine insert index; clamp to [0, p - 1]
                var sel = EditorEvent.Instance.lstCommands.SelectedIndex;
                int selectedCommandNum = (EventList != null && sel >= 0 && sel < EventList.Length)
                    ? EventList[sel].CommandNum
                    : p - 1;
                int insertIndex = Math.Clamp(selectedCommandNum, 0, p - 1);

                // Shift right to make room
                for (int j = p - 1; j > insertIndex; j--)
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[j] =
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[j - 1];

                curslot = insertIndex;
            }

            switch (Index)
            {
                case (int) EventCommand.AddText:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtAddText_Text.Text;
                    if (EditorEvent.Instance.optAddText_Player.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optAddText_Map.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optAddText_Global.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    break;
                }
                case (int) EventCommand.ConditionalBranch:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandListCount += 1;
                    Array.Resize(ref Instance.Pages[CurPageNum].CommandList, Instance.Pages[CurPageNum].CommandListCount);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.CommandList = Instance.Pages[CurPageNum].CommandListCount;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.ElseCommandList = Instance.Pages[CurPageNum].CommandListCount;
                    Instance.Pages[CurPageNum].CommandList[Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.CommandList].ParentList = curlist;
                    Instance.Pages[CurPageNum].CommandList[Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.ElseCommandList].ParentList = curlist;

                    if (EditorEvent.Instance.optCondition0.Checked == true)
                        X = 0;
                    if (EditorEvent.Instance.optCondition1.Checked == true)
                        X = 1;
                    if (EditorEvent.Instance.optCondition2.Checked == true)
                        X = 2;
                    if (EditorEvent.Instance.optCondition3.Checked == true)
                        X = 3;
                    if (EditorEvent.Instance.optCondition4.Checked == true)
                        X = 4;
                    if (EditorEvent.Instance.optCondition5.Checked == true)
                        X = 5;
                    if (EditorEvent.Instance.optCondition6.Checked == true)
                        X = 6;
                    if (EditorEvent.Instance.optCondition8.Checked == true)
                        X = 8;
                    if (EditorEvent.Instance.optCondition9.Checked == true)
                        X = 9;

                    switch (X)
                    {
                        case 0: // Player Var
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 0;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerVarIndex.SelectedIndex;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_PlayerVarCompare.SelectedIndex;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data3 = (int) Math.Round(EditorEvent.Instance.nudCondition_PlayerVarCondition.Value);
                            break;
                        }
                        case 1: // Player Switch
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 1;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerSwitch.SelectedIndex;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.SelectedIndex;
                            break;
                        }
                        case 2: // Has Item
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 2;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_HasItem.SelectedIndex;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = (int) Math.Round(EditorEvent.Instance.nudCondition_HasItem.Value);
                            break;
                        }
                        case 3: // Job Is
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 3;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_JobIs.SelectedIndex;
                            break;
                        }
                        case 4: // Learnt Skill
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 4;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_LearntSkill.SelectedIndex;
                            break;
                        }
                        case 5: // Level Is
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 5;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = (int) Math.Round(EditorEvent.Instance.nudCondition_LevelAmount.Value);
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_LevelCompare.SelectedIndex;
                            break;
                        }
                        case 6: // Self Switch
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 6;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_SelfSwitch.SelectedIndex;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_SelfSwitchCondition.SelectedIndex;
                            break;
                        }
                        case 7:
                        {
                            break;
                        }

                        case 8: // Gender
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 8;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Gender.SelectedIndex;
                            break;
                        }
                        case 9: // Time
                        {
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 9;
                            Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Time.SelectedIndex;
                            break;
                        }
                    }

                    break;
                }

                case (int) EventCommand.ShowText:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    string tmptxt = "";
                    // TextArea has no Lines property; split Text manually to mimic previous behavior
                    var rawText = EditorEvent.Instance.txtShowText.Text ?? string.Empty;
                    var splitLines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (i = 0; i < splitLines.Length; i++)
                    {
                        tmptxt += splitLines[i];
                    }
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = tmptxt;
                    break;
                }

                case (int) EventCommand.ShowChoices:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChoicePrompt.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text2 = EditorEvent.Instance.txtChoices1.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text3 = EditorEvent.Instance.txtChoices2.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text4 = EditorEvent.Instance.txtChoices3.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text5 = EditorEvent.Instance.txtChoices4.Text;
                    Instance.Pages[CurPageNum].CommandListCount += 3;
                    Array.Resize(ref Instance.Pages[CurPageNum].CommandList, Instance.Pages[CurPageNum].CommandListCount + 1);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = Instance.Pages[CurPageNum].CommandListCount - 3;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = Instance.Pages[CurPageNum].CommandListCount - 2;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = Instance.Pages[CurPageNum].CommandListCount - 1;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = Instance.Pages[CurPageNum].CommandListCount;
                    Instance.Pages[CurPageNum].CommandList[Instance.Pages[CurPageNum].CommandListCount - 3].ParentList = curlist;
                    Instance.Pages[CurPageNum].CommandList[Instance.Pages[CurPageNum].CommandListCount - 2].ParentList = curlist;
                    Instance.Pages[CurPageNum].CommandList[Instance.Pages[CurPageNum].CommandListCount - 1].ParentList = curlist;
                    Instance.Pages[CurPageNum].CommandList[Instance.Pages[CurPageNum].CommandListCount].ParentList = curlist;
                    break;
                }

                case (int) EventCommand.ModifyVariable:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbVariable.SelectedIndex;

                    if (EditorEvent.Instance.optVariableAction0.Checked == true)
                        i = 0;
                    if (EditorEvent.Instance.optVariableAction1.Checked == true)
                        i = 1;
                    if (EditorEvent.Instance.optVariableAction2.Checked == true)
                        i = 2;
                    if (EditorEvent.Instance.optVariableAction3.Checked == true)
                        i = 3;

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = i;
                    if (i == 3)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData3.Value);
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudVariableData4.Value);
                    }
                    else if (i == 0)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData0.Value);
                    }
                    else if (i == 1)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData1.Value);
                    }
                    else if (i == 2)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData2.Value);
                    }

                    break;
                }

                case (int) EventCommand.ModifySwitch:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSwitch.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPlayerSwitchSet.SelectedIndex;
                    break;
                }

                case (int) EventCommand.ModifySelfSwitch:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetSelfSwitch.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbSetSelfSwitchTo.SelectedIndex;
                    break;
                }

                case (int) EventCommand.ExitEventProcess:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.ChangeItems:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeItemIndex.SelectedIndex;
                    if (EditorEvent.Instance.optChangeItemSet.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeItemAdd.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optChangeItemRemove.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudChangeItemsAmount.Value);
                    break;
                }

                case (int) EventCommand.RestoreHealth:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.RestoreMana:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.RestoreStamina:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.LevelUp:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.ChangeLevel:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeLevel.Value);
                    break;
                }

                case (int) EventCommand.ChangeSkills:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeSkills.SelectedIndex;
                    if (EditorEvent.Instance.optChangeSkillsAdd.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeSkillsRemove.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }

                    break;
                }

                case (int) EventCommand.ChangeJob:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeJob.SelectedIndex;
                    break;
                }

                case (int) EventCommand.ChangeSprite:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeSprite.Value);
                    break;
                }

                case (int) EventCommand.ChangeSex:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    if (EditorEvent.Instance.optChangeSexMale.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Sex.Male;
                    }
                    else if (EditorEvent.Instance.optChangeSexFemale.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Sex.Female;
                    }

                    break;
                }

                case (int) EventCommand.SetPlayerKillable:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetPK.SelectedIndex;
                    break;
                }

                case (int) EventCommand.WarpPlayer:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWPMap.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWPX.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudWPY.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = EditorEvent.Instance.cmbWarpPlayerDir.SelectedIndex;
                    break;
                }

                case (int) EventCommand.SetMoveRoute:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    if (ListOfEvents != null)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbEvent.SelectedIndex];
                    }

                    if (EditorEvent.Instance.chkIgnoreMove.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }

                    if (EditorEvent.Instance.chkRepeatRoute.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 1;
                    }
                    else
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 0;
                    }

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRouteCount = TempMoveRouteCount;
                    if (TempMoveRoute != null)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRoute = TempMoveRoute;
                    }
                    break;
                }

                case (int) EventCommand.PlayAnimation:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbPlayAnim.SelectedIndex;
                    if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 0)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 1)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 2 == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileX.Value);
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileY.Value);
                    }

                    break;
                }

                case (int) EventCommand.PlayBgm:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Audio.MusicCache[EditorEvent.Instance.cmbPlayBGM.SelectedIndex];
                    break;
                }

                case (int) EventCommand.FadeOutBgm:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.PlaySound:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Audio.SoundCache[EditorEvent.Instance.cmbPlaySound.SelectedIndex];
                    break;
                }

                case (int) EventCommand.StopSound:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.OpenBank:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.OpenShop:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbOpenShop.SelectedIndex;
                    break;
                }

                case (int) EventCommand.SetAccessLevel:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetAccess.SelectedIndex + 1;
                    break;
                }

                case (int) EventCommand.GiveExperience:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudGiveExp.Value);
                    break;
                }

                case (int) EventCommand.ShowChatBubble:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChatbubbleText.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChatBubbleTargetType.SelectedIndex + 1;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbChatBubbleTarget.SelectedIndex;
                    break;
                }

                case (int) EventCommand.Label:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtLabelName.Text;
                    break;
                }

                case (int) EventCommand.GoToLabel:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtGoToLabel.Text;
                    break;
                }

                case (int) EventCommand.SpawnNpc:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSpawnNpc.SelectedIndex;
                    break;
                }

                case (int) EventCommand.FadeIn:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.FadeOut:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.FlashScreen:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.SetFog:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudFogData0.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudFogData1.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudFogData2.Value);
                    break;
                }

                case (int) EventCommand.SetWeather:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.CmbWeather.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWeatherIntensity.Value);
                    break;
                }

                case (int) EventCommand.SetScreenTint:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudMapTintData0.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudMapTintData1.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudMapTintData2.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudMapTintData3.Value);
                    break;
                }

                case (int) EventCommand.Wait:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWaitAmount.Value);
                    break;
                }

                case (int) EventCommand.ShowPicture:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudShowPicture.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPicLoc.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetX.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetY.Value);
                    break;
                }

                case (int) EventCommand.HidePicture:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.WaitMovementCompletion:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    if (ListOfEvents != null)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbMoveWait.SelectedIndex];
                    }
                    break;
                }

                case (int) EventCommand.HoldPlayer:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.ReleasePlayer:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }
            }

            EventListCommands();
        }

        public static void EditEventCommand()
        {
            int i;
            var X = default(int);
            int curlist;
            int curslot;

            i = EditorEvent.Instance.lstCommands.SelectedIndex + 1;
            if (i == -1)
                return;

            if (i > Information.UBound(EventList))
                return;

            EditorEvent.Instance.fraConditionalBranch.Visible = false;

            if (EventList == null)
                return;
            curlist = EventList[i].CommandList;
            curslot = EventList[i].CommandNum;

            if (curlist > Instance.Pages[CurPageNum].CommandListCount)
                return;

            if (Instance.Pages[CurPageNum].CommandList == null)
                return;

            if (curslot > Instance.Pages[CurPageNum].CommandList[curlist].CommandCount)
                return;

            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index)
            {
                case (byte) EventCommand.AddText:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtAddText_Text.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    // EditorEvent.Instance.scrlAddText_Color.Value = Instance.Pages(curPageNum).CommandList(curlist).Commands(curslot).Data1
                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2)
                    {
                        case 0:
                        {
                            EditorEvent.Instance.optAddText_Player.Checked = true;
                            break;
                        }
                        case 1:
                        {
                            EditorEvent.Instance.optAddText_Map.Checked = true;
                            break;
                        }
                        case 2:
                        {
                            EditorEvent.Instance.optAddText_Global.Checked = true;
                            break;
                        }
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraAddText.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ConditionalBranch:
                {
                    IsEdit = true;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraConditionalBranch.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    EditorEvent.Instance.ClearConditionFrame();

                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition)
                    {
                        case 0:
                        {
                            EditorEvent.Instance.optCondition0.Checked = true;
                            break;
                        }
                        case 1:
                        {
                            EditorEvent.Instance.optCondition1.Checked = true;
                            break;
                        }
                        case 2:
                        {
                            EditorEvent.Instance.optCondition2.Checked = true;
                            break;
                        }
                        case 3:
                        {
                            EditorEvent.Instance.optCondition3.Checked = true;
                            break;
                        }
                        case 4:
                        {
                            EditorEvent.Instance.optCondition4.Checked = true;
                            break;
                        }
                        case 5:
                        {
                            EditorEvent.Instance.optCondition5.Checked = true;
                            break;
                        }
                        case 6:
                        {
                            EditorEvent.Instance.optCondition6.Checked = true;
                            break;
                        }
                        case 7:
                        {
                            break;
                        }

                        case 8:
                        {
                            EditorEvent.Instance.optCondition8.Checked = true;
                            break;
                        }
                        case 9:
                        {
                            EditorEvent.Instance.optCondition9.Checked = true;
                            break;
                        }
                    }

                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition)
                    {
                        case 0:
                        {
                            EditorEvent.Instance.cmbCondition_PlayerVarIndex.Enabled = true;
                            EditorEvent.Instance.cmbCondition_PlayerVarCompare.Enabled = true;
                            EditorEvent.Instance.nudCondition_PlayerVarCondition.Enabled = true;
                            EditorEvent.Instance.cmbCondition_PlayerVarIndex.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondition_PlayerVarCompare.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            EditorEvent.Instance.nudCondition_PlayerVarCondition.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data3;
                            break;
                        }
                        case 1:
                        {
                            EditorEvent.Instance.cmbCondition_PlayerSwitch.Enabled = true;
                            EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.Enabled = true;
                            EditorEvent.Instance.cmbCondition_PlayerSwitch.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 2:
                        {
                            EditorEvent.Instance.cmbCondition_HasItem.Enabled = true;
                            EditorEvent.Instance.nudCondition_HasItem.Enabled = true;
                            EditorEvent.Instance.cmbCondition_HasItem.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.nudCondition_HasItem.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 3:
                        {
                            EditorEvent.Instance.cmbCondition_JobIs.Enabled = true;
                            EditorEvent.Instance.cmbCondition_JobIs.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                        case 4:
                        {
                            EditorEvent.Instance.cmbCondition_LearntSkill.Enabled = true;
                            EditorEvent.Instance.cmbCondition_LearntSkill.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                        case 5:
                        {
                            EditorEvent.Instance.cmbCondition_LevelCompare.Enabled = true;
                            EditorEvent.Instance.nudCondition_LevelAmount.Enabled = true;
                            EditorEvent.Instance.nudCondition_LevelAmount.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondition_LevelCompare.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 6:
                        {
                            EditorEvent.Instance.cmbCondition_SelfSwitch.Enabled = true;
                            EditorEvent.Instance.cmbCondition_SelfSwitchCondition.Enabled = true;
                            EditorEvent.Instance.cmbCondition_SelfSwitch.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondition_SelfSwitchCondition.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 7:
                        {
                            break;
                        }

                        case 8:
                        {
                            EditorEvent.Instance.cmbCondition_Gender.Enabled = true;
                            EditorEvent.Instance.cmbCondition_Gender.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                        case 9:
                        {
                            EditorEvent.Instance.cmbCondition_Time.Enabled = true;
                            EditorEvent.Instance.cmbCondition_Time.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                    }

                    break;
                }
                case (byte) EventCommand.ShowText:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtShowText.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowText.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ShowChoices:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtChoicePrompt.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.txtChoices1.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text2;
                    EditorEvent.Instance.txtChoices2.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text3;
                    EditorEvent.Instance.txtChoices3.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text4;
                    EditorEvent.Instance.txtChoices4.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text5;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowChoices.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ModifyVariable:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbVariable.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2)
                    {
                        case 0:
                        {
                            EditorEvent.Instance.optVariableAction0.Checked = true;
                            EditorEvent.Instance.nudVariableData0.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            break;
                        }
                        case 1:
                        {
                            EditorEvent.Instance.optVariableAction1.Checked = true;
                            EditorEvent.Instance.nudVariableData1.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            break;
                        }
                        case 2:
                        {
                            EditorEvent.Instance.optVariableAction2.Checked = true;
                            EditorEvent.Instance.nudVariableData2.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            break;
                        }
                        case 3:
                        {
                            EditorEvent.Instance.optVariableAction3.Checked = true;
                            EditorEvent.Instance.nudVariableData3.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            EditorEvent.Instance.nudVariableData4.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                            break;
                        }
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayerVariable.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ModifySwitch:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSwitch.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.cmbPlayerSwitchSet.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayerSwitch.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ModifySelfSwitch:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSetSelfSwitch.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.cmbSetSelfSwitchTo.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetSelfSwitch.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeItems:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbChangeItemIndex.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 0)
                    {
                        EditorEvent.Instance.optChangeItemSet.Checked = true;
                    }
                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 1)
                    {
                        EditorEvent.Instance.optChangeItemAdd.Checked = true;
                    }
                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 2)
                    {
                        EditorEvent.Instance.optChangeItemRemove.Checked = true;
                    }

                    EditorEvent.Instance.nudChangeItemsAmount.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeItems.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeLevel:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudChangeLevel.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeLevel.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeSkills:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbChangeSkills.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 0)
                    {
                        EditorEvent.Instance.optChangeSkillsAdd.Checked = true;
                    }
                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 1)
                    {
                        EditorEvent.Instance.optChangeSkillsRemove.Checked = true;
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeSkills.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeJob:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbChangeJob.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeJob.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeSprite:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudChangeSprite.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeSprite.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeSex:
                {
                    IsEdit = true;
                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 == 0)
                    {
                        EditorEvent.Instance.optChangeSexMale.Checked = true;
                    }
                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 == 1)
                    {
                        EditorEvent.Instance.optChangeSexFemale.Checked = true;
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeGender.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetPlayerKillable:
                {
                    IsEdit = true;

                    EditorEvent.Instance.cmbSetPK.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangePK.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.WarpPlayer:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudWPMap.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudWPX.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.nudWPY.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.cmbWarpPlayerDir.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayerWarp.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetMoveRoute:
                {
                    IsEdit = true;
                    EditorEvent.Instance.fraMoveRoute.Visible = true;
                    EditorEvent.Instance.lstMoveRoute.Items.Clear();
                    ListOfEvents = new int[Data.MyMap.EventCount];
                    ListOfEvents[0] = EditorId;
                    var loopTo = Data.MyMap.EventCount;
                    for (i = 0; i < loopTo; i++)
                    {
                        if (i != EditorId)
                        {
                            EditorEvent.Instance.cmbEvent.Items.Add(Strings.Trim(Data.MyMap.Event[i].Name));
                            X = X + 1;
                            ListOfEvents[X] = i;
                            if (i == Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1)
                                EditorEvent.Instance.cmbEvent.SelectedIndex = X;
                        }
                    }

                    IsMoveRouteCommand = true;
                    EditorEvent.Instance.chkIgnoreMove.Checked = Conversions.ToBoolean(Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2);
                    EditorEvent.Instance.chkRepeatRoute.Checked = Conversions.ToBoolean(Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3);
                    TempMoveRouteCount = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRouteCount;
                    TempMoveRoute = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRoute;
                    var loopTo1 = TempMoveRouteCount;
                    for (i = 0; i < loopTo1; i++)
                    {
                        switch (TempMoveRoute[i].Index)
                        {
                            case 1:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Up");
                                break;
                            }
                            case 2:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Down");
                                break;
                            }
                            case 3:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Left");
                                break;
                            }
                            case 4:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Right");
                                break;
                            }
                            case 5:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Randomly");
                                break;
                            }
                            case 6:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Towards Player");
                                break;
                            }
                            case 7:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Move Away From Player");
                                break;
                            }
                            case 8:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Step Forward");
                                break;
                            }
                            case 9:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Step Back");
                                break;
                            }
                            case 10:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Wait 100ms");
                                break;
                            }
                            case 11:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Wait 500ms");
                                break;
                            }
                            case 12:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Wait 1000ms");
                                break;
                            }
                            case 13:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Up");
                                break;
                            }
                            case 14:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Down");
                                break;
                            }
                            case 15:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Left");
                                break;
                            }
                            case 16:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Right");
                                break;
                            }
                            case 17:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn 90 Degrees To the Right");
                                break;
                            }
                            case 18:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn 90 Degrees To the Left");
                                break;
                            }
                            case 19:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Around 180 Degrees");
                                break;
                            }
                            case 20:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Randomly");
                                break;
                            }
                            case 21:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Towards Player");
                                break;
                            }
                            case 22:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Away from Player");
                                break;
                            }
                            case 23:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Speed 8x Slower");
                                break;
                            }
                            case 24:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Speed 4x Slower");
                                break;
                            }
                            case 25:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Speed 2x Slower");
                                break;
                            }
                            case 26:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Speed to Normal");
                                break;
                            }
                            case 27:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Speed 2x Faster");
                                break;
                            }
                            case 28:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Speed 4x Faster");
                                break;
                            }
                            case 29:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Frequency Lowest");
                                break;
                            }
                            case 30:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Frequency Lower");
                                break;
                            }
                            case 31:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Frequency Normal");
                                break;
                            }
                            case 32:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Frequency Higher");
                                break;
                            }
                            case 33:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Frequency Highest");
                                break;
                            }
                            case 34:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn On Walking Animation");
                                break;
                            }
                            case 35:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Off Walking Animation");
                                break;
                            }
                            case 36:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn On Fixed Direction");
                                break;
                            }
                            case 37:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Off Fixed Direction");
                                break;
                            }
                            case 38:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn On Walk Through");
                                break;
                            }
                            case 39:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Turn Off Walk Through");
                                break;
                            }
                            case 40:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Position Below Characters");
                                break;
                            }
                            case 41:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Position Same as Characters");
                                break;
                            }
                            case 42:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Position Above Characters");
                                break;
                            }
                            case 43:
                            {
                                EditorEvent.Instance.lstMoveRoute.Items.Add("Set Graphic");
                                break;
                            }
                        }
                    }

                    EditorEvent.Instance.fraMoveRoute.Visible = true;
                    EditorEvent.Instance.fraDialogue.Visible = false;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.PlayAnimation:
                {
                    IsEdit = true;
                    EditorEvent.Instance.lblPlayAnimX.Visible = false;
                    EditorEvent.Instance.lblPlayAnimY.Visible = false;
                    EditorEvent.Instance.nudPlayAnimTileX.Visible = false;
                    EditorEvent.Instance.nudPlayAnimTileY.Visible = false;
                    EditorEvent.Instance.cmbPlayAnimEvent.Visible = false;
                    EditorEvent.Instance.cmbPlayAnim.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.cmbPlayAnimEvent.Items.Clear();
                    var loopTo2 = Data.MyMap.EventCount;
                    for (i = 0; i < loopTo2; i++)
                        EditorEvent.Instance.cmbPlayAnimEvent.Items.Add(i + 1 + ". " + Data.MyMap.Event[i].Name);
                    EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex = 0;
                    if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 0)
                    {
                        EditorEvent.Instance.cmbAnimTargetType.SelectedIndex = 0;
                    }
                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 1)
                    {
                        EditorEvent.Instance.cmbAnimTargetType.SelectedIndex = 1;
                        EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    }
                    else if (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 2)
                    {
                        EditorEvent.Instance.cmbAnimTargetType.SelectedIndex = 2;
                        EditorEvent.Instance.nudPlayAnimTileX.MaxValue = Data.MyMap.MaxX;
                        EditorEvent.Instance.nudPlayAnimTileY.MaxValue = Data.MyMap.MaxY;
                        EditorEvent.Instance.nudPlayAnimTileX.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                        EditorEvent.Instance.nudPlayAnimTileY.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayAnimation.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }

                case (byte) EventCommand.PlayBgm:
                {
                    IsEdit = true;
                    var loopTo3 = Information.UBound(Audio.MusicCache);
                    for (i = 0; i < loopTo3; i++)
                    {
                        if ((Audio.MusicCache[i] ?? "") == (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 ?? ""))
                        {
                            EditorEvent.Instance.cmbPlayBGM.SelectedIndex = i;
                        }
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayBGM.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.PlaySound:
                {
                    IsEdit = true;
                    var loopTo4 = Information.UBound(Audio.SoundCache);
                    for (i = 0; i < loopTo4; i++)
                    {
                        if ((Audio.SoundCache[i] ?? "") == (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 ?? ""))
                        {
                            EditorEvent.Instance.cmbPlaySound.SelectedIndex = i;
                        }
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlaySound.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.OpenShop:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbOpenShop.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraOpenShop.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetAccessLevel:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSetAccess.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 - 1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetAccess.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.GiveExperience:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudGiveExp.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraGiveExp.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ShowChatBubble:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtChatbubbleText.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.cmbChatBubbleTargetType.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 - 1;
                    if (EditorEvent.Instance.cmbChatBubbleTarget.Items.Count > -1)
                        EditorEvent.Instance.cmbChatBubbleTarget.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowChatBubble.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.Label:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtLabelName.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraCreateLabel.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.GoToLabel:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtGoToLabel.Text = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraGoToLabel.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SpawnNpc:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSpawnNpc.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSpawnNpc.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetFog:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudFogData0.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudFogData1.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.nudFogData2.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetFog.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetWeather:
                {
                    IsEdit = true;
                    EditorEvent.Instance.CmbWeather.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudWeatherIntensity.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetWeather.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetScreenTint:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudMapTintData0.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudMapTintData1.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.nudMapTintData2.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.nudMapTintData3.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraMapTint.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.Wait:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudWaitAmount.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetWait.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ShowPicture:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudShowPicture.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;

                    EditorEvent.Instance.cmbPicLoc.SelectedIndex = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;

                    EditorEvent.Instance.nudPicOffsetX.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.nudPicOffsetY.Value = Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowPic.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    Map.DrawPicture();
                    break;
                }
                case (byte) EventCommand.WaitMovementCompletion:
                {
                    IsEdit = true;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraMoveRouteWait.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    EditorEvent.Instance.cmbMoveWait.Items.Clear();
                    ListOfEvents = new int[Data.MyMap.EventCount];
                    ListOfEvents[0] = EditorId;
                    EditorEvent.Instance.cmbMoveWait.Items.Add("This Event");
                    EditorEvent.Instance.cmbMoveWait.SelectedIndex = 0;
                    var loopTo5 = Data.MyMap.EventCount;
                    for (i = 0; i < loopTo5; i++)
                    {
                        if (i != EditorId)
                        {
                            EditorEvent.Instance.cmbMoveWait.Items.Add(Strings.Trim(Data.MyMap.Event[i].Name));
                            X = X + 1;
                            ListOfEvents[X] = i;
                            if (i == Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1)
                                EditorEvent.Instance.cmbMoveWait.SelectedIndex = X;
                        }
                    }

                    break;
                }
            }
        }

        public static void DeleteEventCommand()
        {
            int i;
            int curlist;
            int curslot;
            int p;
            Type.CommandList oldCommandList;

            i = EditorEvent.Instance.lstCommands.SelectedIndex;
            if (i == -1)
                return;

            if (i > Information.UBound(EventList))
                return;

            if (EventList == null)
                return;
            curlist = EventList[i].CommandList;
            curslot = EventList[i].CommandNum;

            if (curlist > Instance.Pages[CurPageNum].CommandListCount)
                return;

            if (Instance.Pages[CurPageNum].CommandList == null)
                return;

            if (curslot >= Instance.Pages[CurPageNum].CommandList[curlist].CommandCount)
                return;

            if (Instance.Pages[CurPageNum].CommandList[curlist].CommandCount != i + 1)
            {
                Instance.Pages[CurPageNum].CommandList[curlist].CommandCount--;
                p = Instance.Pages[CurPageNum].CommandList[curlist].CommandCount;
                oldCommandList = Instance.Pages[CurPageNum].CommandList[curlist];

                if (p <= 0)
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands = new Type.EventCommand[1];
                }
                else
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands = new Type.EventCommand[p];
                    Instance.Pages[CurPageNum].CommandList[curlist].ParentList = oldCommandList.ParentList;
                    Instance.Pages[CurPageNum].CommandList[curlist].CommandCount = p;

                    // Move all commands down by 1  
                    for (i = EditorEvent.Instance.lstCommands.SelectedIndex + 1; i <= p; i++)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[i - 1] = oldCommandList.Commands[i];
                    }
                }
            }
            else
            {
                // If we are deleting the last command in the list, set only the last command  
                Instance.Pages[CurPageNum].CommandList[curlist].CommandCount--;
                Array.Resize(ref Instance.Pages[CurPageNum].CommandList[curlist].Commands, Instance.Pages[CurPageNum].CommandList[curlist].CommandCount);
            }

            EventListCommands();
        }

        public static void ClearEventCommands()
        {
            Instance.Pages[CurPageNum].CommandList = new Type.CommandList[1];
            Instance.Pages[CurPageNum].CommandListCount = 0;
            EventListCommands();
        }

        public static void EditCommand()
        {
            int i;
            int curlist;
            int curslot;

            i = EditorEvent.Instance.lstCommands.SelectedIndex;
            if (i == -1)
                return;

            if (i > Information.UBound(EventList))
                return;

            if (EventList == null)
                return;
            curlist = EventList[i].CommandList;
            curslot = EventList[i].CommandNum;

            if (curlist > Instance.Pages[CurPageNum].CommandListCount)
                return;

            if (curslot > Instance.Pages[CurPageNum].CommandList[curlist].CommandCount)
                return;

            switch (Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index)
            {
                case (byte) EventCommand.AddText:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtAddText_Text.Text;
                    // Instance.Pages(curPageNum).CommandList(curlist).Commands(curslot).Data1 = EditorEvent.Instance.scrlAddText_Color.Value
                    if (EditorEvent.Instance.optAddText_Player.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optAddText_Map.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optAddText_Global.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    break;
                }
                case (byte) EventCommand.ConditionalBranch:
                {
                    if (EditorEvent.Instance.optCondition0.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 0;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerVarIndex.SelectedIndex;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_PlayerVarCompare.SelectedIndex;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data3 = (int) Math.Round(EditorEvent.Instance.nudCondition_PlayerVarCondition.Value);
                    }
                    else if (EditorEvent.Instance.optCondition1.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 1;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerSwitch.SelectedIndex;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition2.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 2;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_HasItem.SelectedIndex;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = (int) Math.Round(EditorEvent.Instance.nudCondition_HasItem.Value);
                    }
                    else if (EditorEvent.Instance.optCondition3.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 3;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_JobIs.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition4.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 4;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_LearntSkill.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition5.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 5;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = (int) Math.Round(EditorEvent.Instance.nudCondition_LevelAmount.Value);
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_LevelCompare.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition6.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 6;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_SelfSwitch.SelectedIndex;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_SelfSwitchCondition.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition8.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 8;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Gender.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition9.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 9;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Time.SelectedIndex;
                    }

                    break;
                }
                case (byte) EventCommand.ShowText:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtShowText.Text;
                    break;
                }
                case (byte) EventCommand.ShowChoices:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChoicePrompt.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text2 = EditorEvent.Instance.txtChoices1.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text3 = EditorEvent.Instance.txtChoices2.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text4 = EditorEvent.Instance.txtChoices3.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text5 = EditorEvent.Instance.txtChoices4.Text;
                    break;
                }
                case (byte) EventCommand.ModifyVariable:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbVariable.SelectedIndex;
                    if (EditorEvent.Instance.optVariableAction0.Checked == true)
                        i = 0;
                    if (EditorEvent.Instance.optVariableAction1.Checked == true)
                        i = 1;
                    if (EditorEvent.Instance.optVariableAction2.Checked == true)
                        i = 2;
                    if (EditorEvent.Instance.optVariableAction3.Checked == true)
                        i = 3;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = i;
                    if (i == 0)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData0.Value);
                    }
                    else if (i == 1)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData1.Value);
                    }
                    else if (i == 2)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData2.Value);
                    }
                    else if (i == 3)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData3.Value);
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudVariableData4.Value);
                    }

                    break;
                }
                case (byte) EventCommand.ModifySwitch:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSwitch.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPlayerSwitchSet.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.ModifySelfSwitch:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetSelfSwitch.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbSetSelfSwitchTo.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.ChangeItems:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeItemIndex.SelectedIndex;
                    if (EditorEvent.Instance.optChangeItemSet.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeItemAdd.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optChangeItemRemove.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudChangeItemsAmount.Value);
                    break;
                }
                case (byte) EventCommand.ChangeLevel:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeLevel.Value);
                    break;
                }
                case (byte) EventCommand.ChangeSkills:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeSkills.SelectedIndex;
                    if (EditorEvent.Instance.optChangeSkillsAdd.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeSkillsRemove.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }

                    break;
                }
                case (byte) EventCommand.ChangeJob:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeJob.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.ChangeSprite:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeSprite.Value);
                    break;
                }
                case (byte) EventCommand.ChangeSex:
                {
                    if (EditorEvent.Instance.optChangeSexMale.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeSexFemale.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = 1;
                    }

                    break;
                }
                case (byte) EventCommand.SetPlayerKillable:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetPK.SelectedIndex;
                    break;
                }

                case (byte) EventCommand.WarpPlayer:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWPMap.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWPX.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudWPY.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = EditorEvent.Instance.cmbWarpPlayerDir.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.SetMoveRoute:
                {
                    if (ListOfEvents != null)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbEvent.SelectedIndex];
                    }
                    if (EditorEvent.Instance.chkIgnoreMove.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }

                    if (EditorEvent.Instance.chkRepeatRoute.Checked == true)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 1;
                    }
                    else
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 0;
                    }

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRouteCount = TempMoveRouteCount;
                    if (TempMoveRoute != null)
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRoute = TempMoveRoute;
                    break;
                }
                case (byte) EventCommand.PlayAnimation:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbPlayAnim.SelectedIndex;
                    if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 0)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 1)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 2)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileX.Value);
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileY.Value);
                    }

                    break;
                }
                case (byte) EventCommand.PlayBgm:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Audio.MusicCache[EditorEvent.Instance.cmbPlayBGM.SelectedIndex];
                    break;
                }
                case (byte) EventCommand.PlaySound:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Audio.SoundCache[EditorEvent.Instance.cmbPlaySound.SelectedIndex];
                    break;
                }
                case (byte) EventCommand.OpenShop:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbOpenShop.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.SetAccessLevel:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetAccess.SelectedIndex + 1;
                    break;
                }
                case (byte) EventCommand.GiveExperience:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudGiveExp.Value);
                    break;
                }
                case (byte) EventCommand.ShowChatBubble:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChatbubbleText.Text;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChatBubbleTargetType.SelectedIndex + 1;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbChatBubbleTarget.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.Label:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtLabelName.Text;
                    break;
                }
                case (byte) EventCommand.GoToLabel:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtGoToLabel.Text;
                    break;
                }
                case (byte) EventCommand.SpawnNpc:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSpawnNpc.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.SetFog:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudFogData0.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudFogData1.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudFogData2.Value);
                    break;
                }
                case (byte) EventCommand.SetWeather:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.CmbWeather.SelectedIndex;
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWeatherIntensity.Value);
                    break;
                }
                case (byte) EventCommand.SetScreenTint:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudMapTintData0.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudMapTintData1.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudMapTintData2.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudMapTintData3.Value);
                    break;
                }
                case (byte) EventCommand.Wait:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWaitAmount.Value);
                    break;
                }
                case (byte) EventCommand.ShowPicture:
                {
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudShowPicture.Value);

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPicLoc.SelectedIndex;

                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetX.Value);
                    Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetY.Value);
                    break;
                }
                case (byte) EventCommand.WaitMovementCompletion:
                {
                    if (ListOfEvents != null)
                    {
                        Instance.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbMoveWait.SelectedIndex];
                    }
                    break;
                }
            }

            EventListCommands();
        }

        #endregion

        #region Misc

        public static void OnMove(int id)
        {
            // Guard: ensure event system and target index are valid
            if (id < 0) return;

            if (Data.MapEvents == null) return;
            if (id >= Data.MyMap.EventCount) return;
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
            if (Data.MyMap.Event == null)
                return;

            // Iterate only actual events to avoid drawing the trailing empty slot
            int count = Math.Max(0, Data.MyMap.EventCount);
            for (int i = 0; i < count; i++)
            {
                if (i >= Data.MyMap.Event.Length)
                    break;
                    
                // Treat MyMap.Event.X/Y as tile coordinates; compute world pixel coordinates
                int worldX = Data.MyMap.Event[i].X * Constants.TileSize;
                int worldY = Data.MyMap.Event[i].Y * Constants.TileSize;

                // Skip event if there are no pages
                if (Data.MyMap.Event[i].PageCount <= 0)
                {
                    GameClient.DrawOutlineRectangle(GameLogic.ConvertMapX(worldX), GameLogic.ConvertMapY(worldY), Constants.TileSize, Constants.TileSize, Color.Blue, 0.6f);
                    continue;
                }

                // Precompute screen coordinates once
                int screenX = GameLogic.ConvertMapX(worldX);
                int screenY = GameLogic.ConvertMapY(worldY);

                // Render event based on its graphic type
                switch (Data.MyMap.Event[i].Pages[0].GraphicType)
                {
                    case 0: // Text Event (draw simple 'E' at the tile origin like other 32x32 textures)
                    {
                        TextRenderer.OnDraw("E", screenX, screenY, Color.Green, Color.Black);
                        break;
                    }

                    case 1: // Character Graphic
                    {
                        GameClient.RenderCharacterGraphic(Data.MyMap.Event[i], screenX, screenY);
                        break;
                    }

                    case 2: // Tileset Graphic
                    {
                        GameClient.RenderTilesetGraphic(Data.MyMap.Event[i], screenX, screenY);
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