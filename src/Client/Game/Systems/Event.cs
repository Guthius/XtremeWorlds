using System;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Microsoft.VisualBasic.CompilerServices;
using static Core.Globals.Type;
using EventCommand = Core.Globals.EventCommand;
using Type = Core.Globals.Type;

namespace Client
{
    public class Event
    {
        #region Globals

        // Temp event storage
        public static Type.Event TmpEvent;

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
                    if (Information.UBound(Data.MyMap.Event) > 2)
                    {
                        Data.MyMap.Event[i] = Data.MyMap.Event[i + 1];
                    }
                }

                for (i = lowIndex; i < Data.MyMap.EventCount; i++)
                {
                    if (Information.UBound(Data.MapEvents) > 2)
                    {
                        if (Data.MapEvents == null)
                            break;
                        Data.MapEvents[i] = Data.MapEvents[i + 1];
                    }
                }

                TmpEvent = default;
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
            ref var withBlock = ref Data.MyMap.Event[eventNum];
            withBlock.Name = "";
            withBlock.PageCount = 1;
            withBlock.Pages = new Type.EventPage[1];
            Array.Resize(ref withBlock.Pages[0].CommandList, 1);
            Array.Resize(ref withBlock.Pages[0].CommandList[0].Commands, 1);
            withBlock.Pages[0].CommandList[0].Commands[0].Index = -1;
            withBlock.Globals = 0;
            withBlock.X = 0;
            withBlock.Y = 0;
        }

        public static void EventEditorInit()
        {
            int EventNum = GameState.EventNum;
            EditorId = EventNum;
            TmpEvent = Data.MyMap.Event[EventNum];
            if (TmpEvent.Pages[0].CommandListCount == 0)
            {
                Array.Resize(ref TmpEvent.Pages[0].CommandList, 1);
                TmpEvent.Pages[0].CommandListCount = 0;
                TmpEvent.Pages[0].CommandList[0].CommandCount = 0;
                Array.Resize(ref TmpEvent.Pages[0].CommandList[0].Commands, TmpEvent.Pages[0].CommandList[0].CommandCount);
            }
        }

        public static void EventEditorLoadPage(int pageNum)
        {
            if (Event.TmpEvent.Pages == null)
                return;

            if (pageNum < 0 || pageNum >= TmpEvent.Pages.Length || TmpEvent.Pages == null)
            {
                // Invalid page number, return or throw an exception
                return;
            }

            // Guard UI updates to avoid firing change handlers
            EditorEvent.Instance.BeginPageSync();
            try
            {
            ref var withBlock = ref TmpEvent.Pages[pageNum];
            GraphicSelX = withBlock.GraphicX;
            GraphicSelY = withBlock.GraphicY;
            GraphicSelX2 = withBlock.GraphicX2;
            GraphicSelY2 = withBlock.GraphicY2;
            EditorEvent.Instance.cmbGraphic.SelectedIndex = withBlock.GraphicType;
            EditorEvent.Instance.cmbHasItem.SelectedIndex = withBlock.HasItemIndex;
            if (withBlock.HasItemAmount == 0)
            {
                EditorEvent.Instance.nudCondition_HasItem.Value = 1;
            }
            else
            {
                EditorEvent.Instance.nudCondition_HasItem.Value = withBlock.HasItemAmount;
            }

            EditorEvent.Instance.cmbMoveFreq.SelectedIndex = withBlock.MoveFreq;
            EditorEvent.Instance.cmbMoveSpeed.SelectedIndex = withBlock.MoveSpeed;
            EditorEvent.Instance.cmbMoveType.SelectedIndex = withBlock.MoveType;
            EditorEvent.Instance.cmbPlayerVar.SelectedIndex = withBlock.VariableIndex;
            EditorEvent.Instance.cmbPlayerSwitch.SelectedIndex = withBlock.SwitchIndex;
            EditorEvent.Instance.cmbSelfSwitchCompare.SelectedIndex = withBlock.SelfSwitchCompare;
            EditorEvent.Instance.cmbSelfSwitch.SelectedIndex = withBlock.SelfSwitchIndex;
            EditorEvent.Instance.cmbPlayerSwitchCompare.SelectedIndex = withBlock.SwitchCompare;
            EditorEvent.Instance.cmbPlayerVarCompare.SelectedIndex = withBlock.VariableCompare;
            EditorEvent.Instance.chkGlobal.Checked = Conversions.ToBoolean(TmpEvent.Globals);
            EditorEvent.Instance.cmbTrigger.SelectedIndex = withBlock.Trigger;
            EditorEvent.Instance.chkDirFix.Checked = Conversions.ToBoolean(withBlock.DirFix);
            EditorEvent.Instance.chkHasItem.Checked = Conversions.ToBoolean(withBlock.ChkHasItem);
            EditorEvent.Instance.chkPlayerVar.Checked = Conversions.ToBoolean(withBlock.ChkVariable);
            EditorEvent.Instance.chkPlayerSwitch.Checked = Conversions.ToBoolean(withBlock.ChkSwitch);
            EditorEvent.Instance.chkSelfSwitch.Checked = Conversions.ToBoolean(withBlock.ChkSelfSwitch);
            EditorEvent.Instance.chkWalkAnim.Checked = Conversions.ToBoolean(withBlock.WalkAnim);
            EditorEvent.Instance.chkWalkThrough.Checked = Conversions.ToBoolean(withBlock.WalkThrough);
            EditorEvent.Instance.chkShowName.Checked = Conversions.ToBoolean(withBlock.ShowName);
            EditorEvent.Instance.nudPlayerVariable.Value = withBlock.VariableCondition;
            EditorEvent.Instance.nudGraphic.Value = withBlock.Graphic;
            // Event-level fields
            EditorEvent.Instance.txtName.Text = TmpEvent.Name ?? string.Empty;

            if (withBlock.ChkSelfSwitch == 0)
            {
                EditorEvent.Instance.cmbSelfSwitch.Enabled = false;
                EditorEvent.Instance.cmbSelfSwitchCompare.Enabled = false;
            }
            else
            {
                EditorEvent.Instance.cmbSelfSwitch.Enabled = true;
                EditorEvent.Instance.cmbSelfSwitchCompare.Enabled = true;
            }

            if (withBlock.ChkSwitch == 0)
            {
                EditorEvent.Instance.cmbPlayerSwitch.Enabled = false;
                EditorEvent.Instance.cmbPlayerSwitchCompare.Enabled = false;
            }
            else
            {
                EditorEvent.Instance.cmbPlayerSwitch.Enabled = true;
                EditorEvent.Instance.cmbPlayerSwitchCompare.Enabled = true;
            }

            if (withBlock.ChkVariable == 0)
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

            EditorEvent.Instance.cmbPositioning.SelectedIndex = int.Parse(withBlock.Position.ToString());
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
            Data.MyMap.Event[EditorId] = TmpEvent;
            TmpEvent = default;

            // unload the form
            EditorEvent.Instance.Dispose();
        }

        public static void EventListCommands()
        {
            if (TmpEvent.Pages == null)
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

            if (TmpEvent.Pages[CurPageNum].CommandListCount > 0)
            {
                listleftoff = new int[TmpEvent.Pages[CurPageNum].CommandListCount];
                conditionalstage = new int[TmpEvent.Pages[CurPageNum].CommandListCount];
                curlist = 0;
                X = 0;
                Array.Resize(ref EventList, X + 1);
                newlist:
                var loopTo = TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount;
                for (i = 0; i < loopTo; i++)
                {
                    if (listleftoff[curlist] > 0)
                    {
                        if ((TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[listleftoff[curlist]].Index == (int) EventCommand.ConditionalBranch | TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[listleftoff[curlist]].Index == (int) EventCommand.ShowChoices) & conditionalstage[curlist] != 0)
                        {
                            i = listleftoff[curlist];
                        }
                        else if (listleftoff[curlist] >= i)
                        {
                            i = listleftoff[curlist] + 1;
                        }
                    }

                    if (i < TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount)
                    {
                        if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Index == (int) EventCommand.ConditionalBranch)
                        {
                            X = X + 1;
                            Array.Resize(ref EventList, X + 1);
                            switch (conditionalstage[curlist])
                            {
                                case 0:
                                {
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = i;
                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Condition)
                                    {
                                        case 0:
                                        {
                                            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2)
                                            {
                                                case 0:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] == " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 1:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] >= " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 2:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] <= " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 3:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] > " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 4:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] < " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                                case 5:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Variable [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1] + 1 + "] != " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                        case 1:
                                        {
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Switch [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Switches[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + 1] + "] == " + "True");
                                            }
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Switch [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + ". " + Switches[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + 1] + "] == " + "False");
                                            }

                                            break;
                                        }
                                        case 2:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Has Item [" + Data.Item[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1].Name + "] x" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2);
                                            break;
                                        }
                                        case 3:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Job Is [" + Strings.Trim(Data.Job[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1].Name) + "]");
                                            break;
                                        }
                                        case 4:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player Knows Skill [" + Strings.Trim(Data.Skill[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1].Name) + "]");
                                            break;
                                        }
                                        case 5:
                                        {
                                            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2)
                                            {
                                                case 0:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is == " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 1:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is >= " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 2:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is <= " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 3:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is > " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 4:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is < " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                                case 5:
                                                {
                                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Player's Level is NOT " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1);
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                        case 6:
                                        {
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 0)
                                            {
                                                switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
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
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 1)
                                            {
                                                switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
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
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 0)
                                            {
                                                switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3)
                                                {
                                                    case 0:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] not started.");
                                                        break;
                                                    }
                                                    case 1:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] is started.");
                                                        break;
                                                    }
                                                    case 2:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] is completed.");
                                                        break;
                                                    }
                                                    case 3:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] can be started.");
                                                        break;
                                                    }
                                                    case 4:
                                                    {
                                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] can be ended. (All tasks complete)");
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Conditional Branch: Quest [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1 + "] in progress and on task #" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data3);
                                            }

                                            break;
                                        }
                                        case 8:
                                        {
                                            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
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
                                            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.Data1)
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
                                    curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.CommandList;
                                    goto newlist;
                                }
                                case 1:
                                {
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = 0;
                                    EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "Else");
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 2;
                                    curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].ConditionalBranch.ElseCommandList;
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
                        else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Index == (int) EventCommand.ShowChoices)
                        {
                            X = X + 1;
                            switch (conditionalstage[curlist])
                            {
                                case 0:
                                {
                                    Array.Resize(ref EventList, X + 1);
                                    EventList[X].CommandList = curlist;
                                    EventList[X].CommandNum = i;
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Choices - Prompt: " + Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20));
                                    indent = indent + "       ";
                                    listleftoff[curlist] = i;
                                    conditionalstage[curlist] = 1;
                                    goto newlist;
                                }
                                case 1:
                                {
                                        if (!string.IsNullOrEmpty(Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text2)))
                                        {
                                            Array.Resize(ref EventList, X + 1);
                                            EventList[X].CommandList = 7;
                                            EventList[X].CommandNum = 0;
                                            EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text2) + "]");
                                            listleftoff[curlist] = i;
                                            conditionalstage[curlist] = 2;
                                            curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1;
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
                                    if (!string.IsNullOrEmpty(Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text3)))
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                        EventList[X].CommandList = curlist;
                                        EventList[X].CommandNum = 0;
                                        EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text3) + "]");
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 3;
                                        curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2;
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
                                    if (!string.IsNullOrEmpty(Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text4)))
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                        EventList[X].CommandList = curlist;
                                        EventList[X].CommandNum = 0;
                                        EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text4) + "]");
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 4;
                                        curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3;
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
                                    if (!string.IsNullOrEmpty(Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text5)))
                                    {
                                        Array.Resize(ref EventList, X + 1);
                                        EventList[X].CommandList = curlist;
                                        EventList[X].CommandNum = 0;
                                        EditorEvent.Instance.lstCommands.Items.Add(Strings.Mid(indent, 1, Strings.Len(indent) - 4) + " : " + "When [" + Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text5) + "]");
                                        listleftoff[curlist] = i;
                                        conditionalstage[curlist] = 5;
                                        curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4;
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
                            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Index)
                            {
                                case (byte) EventCommand.AddText:
                                {
                                    // Build the preview text safely as a string (avoid VB Operators.ConcatenateObject which returns object)
                                    string textPreview = Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20);
                                    string colorStr = Convert.ToString(GetColorString(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1));
                                    string chatType;
                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2)
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
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Text - " + Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20));
                                    break;
                                }
                                case (byte) EventCommand.ModifyVariable:
                                {
                                    string variableValue = Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1];
                                    if (variableValue == "")
                                        variableValue = ": None";
                                    else
                                        variableValue = ": " + variableValue;

                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2)
                                    {
                                        case 0:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] == " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                            break;
                                        }
                                        case 1:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] + " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                            break;
                                        }
                                        case 2:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] - " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                            break;
                                        }
                                        case 3:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Variable [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + variableValue + "] Random Between " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " and " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4);
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ModifySwitch:
                                {
                                    string switchValue = Variables[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1];
                                    if (switchValue == "")
                                        switchValue = ": None";
                                    else
                                        switchValue = ": " + switchValue;

                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Switch [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + switchValue + "] == False");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Switch [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + switchValue + "] == True");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ModifySelfSwitch:
                                {
                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1)
                                    {
                                        case 0:
                                        {
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [A] to Off");
                                            }
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [A] to On");
                                            }

                                            break;
                                        }
                                        case 1:
                                        {
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [B] to Off");
                                            }
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [B] to On");
                                            }

                                            break;
                                        }
                                        case 2:
                                        {
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [C] to Off");
                                            }
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [C] to On");
                                            }

                                            break;
                                        }
                                        case 3:
                                        {
                                            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Self Switch [D] to Off");
                                            }
                                            else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
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
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Item Amount of [" + Data.Item[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "] to " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3);
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Give Player " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " " + Data.Item[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "(s)");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 2)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Take " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " " + Data.Item[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "(s) from Player.");
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
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Level to " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1);
                                    break;
                                }
                                case (byte) EventCommand.ChangeSkills:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Teach Player Skill [" + Strings.Trim(Data.Skill[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name) + "]");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Remove Player Skill [" + Strings.Trim(Data.Skill[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name) + "]");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.ChangeJob:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Job to " + Strings.Trim(Data.Job[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name));
                                    break;
                                }
                                case (byte) EventCommand.ChangeSprite:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Sprite to " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1);
                                    break;
                                }
                                case (byte) EventCommand.ChangeSex:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Sex to Male.");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Sex to Female.");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.SetPlayerKillable:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player PK to No.");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player PK to Yes.");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.WarpPlayer:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") while retaining direction.");
                                    }
                                    else
                                    {
                                        switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4 - 1)
                                        {
                                            case (int) Direction.Up:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing upward.");
                                                break;
                                            }
                                            case (int) Direction.Down:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing downward.");
                                                break;
                                            }
                                            case (int) Direction.Left:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing left.");
                                                break;
                                            }
                                            case (int) Direction.Right:
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Warp Player To Map: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Tile(" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + ") facing right.");
                                                break;
                                            }
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.SetMoveRoute:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 <= Data.MyMap.EventCount)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Move Route for Event #" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " [" + Data.MyMap.Event[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]");
                                    }
                                    else
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Move Route for COULD NOT FIND EVENT!");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.PlayAnimation:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Animation " + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + " [" + Data.Animation[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]" + " On Player");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 1)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Animation " + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + " [" + Data.Animation[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]" + " On Event " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + " [" + Strings.Trim(Data.MyMap.Event[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3].Name) + "]");
                                    }
                                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 == 2)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Animation " + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + 1) + " [" + Data.Animation[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]" + " On Tile (" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3 + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4 + ")");
                                    }

                                    break;
                                }
                                case (byte) EventCommand.PlayBgm:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play BGM [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1 + "]");
                                    break;
                                }
                                case (byte) EventCommand.FadeOutBgm:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Fadeout BGM");
                                    break;
                                }
                                case (byte) EventCommand.PlaySound:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Play Sound [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1 + "]");
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
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Open Shop [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ". " + Data.Shop[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name + "]");
                                    break;
                                }
                                case (byte) EventCommand.SetAccessLevel:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Player Access [" + EditorEvent.Instance.cmbSetAccess.Items[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 - 1]);
                                    break;
                                }
                                case (byte) EventCommand.GiveExperience:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Give Player " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " Experience.");
                                    break;
                                }
                                case (byte) EventCommand.ShowChatBubble:
                                {
                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1)
                                    {
                                        case (int) TargetType.Player:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Player");
                                            break;
                                        }
                                        case (int) TargetType.Npc:
                                        {
                                            if (Data.MyMap.Npc[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2] <= 0)
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Npc [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + 1).ToString() + ". ]");
                                            }
                                            else
                                            {
                                                EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Npc [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + 1).ToString() + ". " + Data.Npc[Data.MyMap.Npc[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2]].Name + "]");
                                            }

                                            break;
                                        }
                                        case (int) TargetType.Event:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Chat Bubble - " + Strings.Mid(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1, 1, 20) + "... - On Event [" + (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2 + 1).ToString() + ". " + Data.MyMap.Event[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2].Name + "]");
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.Label:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Label: [" + Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1) + "]");
                                    break;
                                }
                                case (byte) EventCommand.GoToLabel:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Jump to Label: [" + Strings.Trim(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Text1) + "]");
                                    break;
                                }
                                case (byte) EventCommand.SpawnNpc:
                                {
                                    if (Data.MyMap.Npc[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1] <= 0)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Spawn Npc: [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ". " + "]");
                                    }
                                    else
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Spawn Npc: [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ". " + Data.Npc[Data.MyMap.Npc[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1]].Name + "]");
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
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Fog [Fog: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + " Speed: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + " Opacity: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3.ToString() + "]");
                                    break;
                                }
                                case (byte) EventCommand.SetWeather:
                                {
                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1)
                                    {
                                        case (int) WeatherType.None:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [None]");
                                            break;
                                        }
                                        case (int) WeatherType.Rain:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Rain - Intensity: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                        case (int) WeatherType.Snow:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Snow - Intensity: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                        case (int) WeatherType.Sandstorm:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Sand Storm - Intensity: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                        case (int) WeatherType.Storm:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Weather [Storm - Intensity: " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "]");
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.SetScreenTint:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Set Map Tint RGBA [" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2.ToString() + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data3.ToString() + "," + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4.ToString() + "]");
                                    break;
                                }
                                case (byte) EventCommand.Wait:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Wait " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + " Ms");
                                    break;
                                }
                                case (byte) EventCommand.ShowPicture:
                                {
                                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2)
                                    {
                                        case 0:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " Top Left, X: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                        case 1:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " Center Screen, X: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                        case 2:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " On Event, X: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                        case 3:
                                        {
                                            EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Show Picture " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString() + ": Pic=" + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data2) + " On Player, X: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data4) + " Y: " + Conversion.Str(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data5));
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case (byte) EventCommand.HidePicture:
                                {
                                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Hide Picture " + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1.ToString());
                                    break;
                                }
                                case (byte) EventCommand.WaitMovementCompletion:
                                {
                                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 <= Data.MyMap.EventCount)
                                    {
                                        EditorEvent.Instance.lstCommands.Items.Add(indent + "@>" + "Wait for Event #" + TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1 + " [" + Strings.Trim(Data.MyMap.Event[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i].Data1].Name) + "] to complete move route.");
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
                    EventList[X].CommandNum = TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount;
                    EditorEvent.Instance.lstCommands.Items.Add(indent + "@> ");
                    curlist = TmpEvent.Pages[CurPageNum].CommandList[curlist].ParentList;
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

            TmpEvent.Pages[CurPageNum].CommandListCount += 1;
            Array.Resize(ref TmpEvent.Pages[CurPageNum].CommandList, TmpEvent.Pages[CurPageNum].CommandListCount);
            TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount += 1;
            p = TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount;
            Array.Resize(ref TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands, p);

            if (EditorEvent.Instance.lstCommands.SelectedIndex + 1 == EditorEvent.Instance.lstCommands.Items.Count)
            {
                curslot = TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount - 1;
            }
            else
            {
                oldCommandList = TmpEvent.Pages[CurPageNum].CommandList[curlist];
                TmpEvent.Pages[CurPageNum].CommandList[curlist].ParentList = oldCommandList.ParentList;

                // copy old commands into resized array
                for (int j = 0; j < oldCommandList.CommandCount; j++)
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[j] = oldCommandList.Commands[j];

                // Determine insert index; clamp to [0, p - 1]
                var sel = EditorEvent.Instance.lstCommands.SelectedIndex;
                int selectedCommandNum = (EventList != null && sel >= 0 && sel < EventList.Length)
                    ? EventList[sel].CommandNum
                    : p - 1;
                int insertIndex = Math.Clamp(selectedCommandNum, 0, p - 1);

                // Shift right to make room
                for (int j = p - 1; j > insertIndex; j--)
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[j] =
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[j - 1];

                curslot = insertIndex;
            }

            switch (Index)
            {
                case (int) EventCommand.AddText:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtAddText_Text.Text;
                    if (EditorEvent.Instance.optAddText_Player.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optAddText_Map.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optAddText_Global.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    break;
                }
                case (int) EventCommand.ConditionalBranch:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandListCount += 1;
                    Array.Resize(ref TmpEvent.Pages[CurPageNum].CommandList, TmpEvent.Pages[CurPageNum].CommandListCount);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.CommandList = TmpEvent.Pages[CurPageNum].CommandListCount;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.ElseCommandList = TmpEvent.Pages[CurPageNum].CommandListCount;
                    TmpEvent.Pages[CurPageNum].CommandList[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.CommandList].ParentList = curlist;
                    TmpEvent.Pages[CurPageNum].CommandList[TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.ElseCommandList].ParentList = curlist;

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
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 0;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerVarIndex.SelectedIndex;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_PlayerVarCompare.SelectedIndex;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data3 = (int) Math.Round(EditorEvent.Instance.nudCondition_PlayerVarCondition.Value);
                            break;
                        }
                        case 1: // Player Switch
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 1;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerSwitch.SelectedIndex;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.SelectedIndex;
                            break;
                        }
                        case 2: // Has Item
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 2;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_HasItem.SelectedIndex;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = (int) Math.Round(EditorEvent.Instance.nudCondition_HasItem.Value);
                            break;
                        }
                        case 3: // Job Is
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 3;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_JobIs.SelectedIndex;
                            break;
                        }
                        case 4: // Learnt Skill
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 4;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_LearntSkill.SelectedIndex;
                            break;
                        }
                        case 5: // Level Is
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 5;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = (int) Math.Round(EditorEvent.Instance.nudCondition_LevelAmount.Value);
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_LevelCompare.SelectedIndex;
                            break;
                        }
                        case 6: // Self Switch
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 6;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_SelfSwitch.SelectedIndex;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_SelfSwitchCondition.SelectedIndex;
                            break;
                        }
                        case 7:
                        {
                            break;
                        }

                        case 8: // Gender
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 8;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Gender.SelectedIndex;
                            break;
                        }
                        case 9: // Time
                        {
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 9;
                            TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Time.SelectedIndex;
                            break;
                        }
                    }

                    break;
                }

                case (int) EventCommand.ShowText:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    string tmptxt = "";
                    // TextArea has no Lines property; split Text manually to mimic previous behavior
                    var rawText = EditorEvent.Instance.txtShowText.Text ?? string.Empty;
                    var splitLines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (i = 0; i < splitLines.Length; i++)
                    {
                        tmptxt += splitLines[i];
                    }
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = tmptxt;
                    break;
                }

                case (int) EventCommand.ShowChoices:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChoicePrompt.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text2 = EditorEvent.Instance.txtChoices1.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text3 = EditorEvent.Instance.txtChoices2.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text4 = EditorEvent.Instance.txtChoices3.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text5 = EditorEvent.Instance.txtChoices4.Text;
                    TmpEvent.Pages[CurPageNum].CommandListCount += 3;
                    Array.Resize(ref TmpEvent.Pages[CurPageNum].CommandList, TmpEvent.Pages[CurPageNum].CommandListCount + 1);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = TmpEvent.Pages[CurPageNum].CommandListCount - 3;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = TmpEvent.Pages[CurPageNum].CommandListCount - 2;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = TmpEvent.Pages[CurPageNum].CommandListCount - 1;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = TmpEvent.Pages[CurPageNum].CommandListCount;
                    TmpEvent.Pages[CurPageNum].CommandList[TmpEvent.Pages[CurPageNum].CommandListCount - 3].ParentList = curlist;
                    TmpEvent.Pages[CurPageNum].CommandList[TmpEvent.Pages[CurPageNum].CommandListCount - 2].ParentList = curlist;
                    TmpEvent.Pages[CurPageNum].CommandList[TmpEvent.Pages[CurPageNum].CommandListCount - 1].ParentList = curlist;
                    TmpEvent.Pages[CurPageNum].CommandList[TmpEvent.Pages[CurPageNum].CommandListCount].ParentList = curlist;
                    break;
                }

                case (int) EventCommand.ModifyVariable:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbVariable.SelectedIndex;

                    if (EditorEvent.Instance.optVariableAction0.Checked == true)
                        i = 0;
                    if (EditorEvent.Instance.optVariableAction1.Checked == true)
                        i = 1;
                    if (EditorEvent.Instance.optVariableAction2.Checked == true)
                        i = 2;
                    if (EditorEvent.Instance.optVariableAction3.Checked == true)
                        i = 3;

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = i;
                    if (i == 3)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData3.Value);
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudVariableData4.Value);
                    }
                    else if (i == 0)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData0.Value);
                    }
                    else if (i == 1)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData1.Value);
                    }
                    else if (i == 2)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData2.Value);
                    }

                    break;
                }

                case (int) EventCommand.ModifySwitch:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSwitch.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPlayerSwitchSet.SelectedIndex;
                    break;
                }

                case (int) EventCommand.ModifySelfSwitch:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetSelfSwitch.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbSetSelfSwitchTo.SelectedIndex;
                    break;
                }

                case (int) EventCommand.ExitEventProcess:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.ChangeItems:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeItemIndex.SelectedIndex;
                    if (EditorEvent.Instance.optChangeItemSet.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeItemAdd.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optChangeItemRemove.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudChangeItemsAmount.Value);
                    break;
                }

                case (int) EventCommand.RestoreHealth:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.RestoreMana:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.RestoreStamina:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.LevelUp:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.ChangeLevel:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeLevel.Value);
                    break;
                }

                case (int) EventCommand.ChangeSkills:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeSkills.SelectedIndex;
                    if (EditorEvent.Instance.optChangeSkillsAdd.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeSkillsRemove.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }

                    break;
                }

                case (int) EventCommand.ChangeJob:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeJob.SelectedIndex;
                    break;
                }

                case (int) EventCommand.ChangeSprite:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeSprite.Value);
                    break;
                }

                case (int) EventCommand.ChangeSex:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    if (EditorEvent.Instance.optChangeSexMale.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Sex.Male;
                    }
                    else if (EditorEvent.Instance.optChangeSexFemale.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Sex.Female;
                    }

                    break;
                }

                case (int) EventCommand.SetPlayerKillable:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetPK.SelectedIndex;
                    break;
                }

                case (int) EventCommand.WarpPlayer:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWPMap.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWPX.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudWPY.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = EditorEvent.Instance.cmbWarpPlayerDir.SelectedIndex;
                    break;
                }

                case (int) EventCommand.SetMoveRoute:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    if (ListOfEvents != null)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbEvent.SelectedIndex];
                    }

                    if (EditorEvent.Instance.chkIgnoreMove.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }

                    if (EditorEvent.Instance.chkRepeatRoute.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 1;
                    }
                    else
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 0;
                    }

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRouteCount = TempMoveRouteCount;
                    if (TempMoveRoute != null)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRoute = TempMoveRoute;
                    }
                    break;
                }

                case (int) EventCommand.PlayAnimation:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbPlayAnim.SelectedIndex;
                    if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 0)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 1)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 2 == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileX.Value);
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileY.Value);
                    }

                    break;
                }

                case (int) EventCommand.PlayBgm:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Sound.MusicCache[EditorEvent.Instance.cmbPlayBGM.SelectedIndex];
                    break;
                }

                case (int) EventCommand.FadeOutBgm:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.PlaySound:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Sound.SoundCache[EditorEvent.Instance.cmbPlaySound.SelectedIndex];
                    break;
                }

                case (int) EventCommand.StopSound:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.OpenBank:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    break;
                }

                case (int) EventCommand.OpenShop:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbOpenShop.SelectedIndex;
                    break;
                }

                case (int) EventCommand.SetAccessLevel:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetAccess.SelectedIndex + 1;
                    break;
                }

                case (int) EventCommand.GiveExperience:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudGiveExp.Value);
                    break;
                }

                case (int) EventCommand.ShowChatBubble:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChatbubbleText.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChatBubbleTargetType.SelectedIndex + 1;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbChatBubbleTarget.SelectedIndex;
                    break;
                }

                case (int) EventCommand.Label:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtLabelName.Text;
                    break;
                }

                case (int) EventCommand.GoToLabel:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtGoToLabel.Text;
                    break;
                }

                case (int) EventCommand.SpawnNpc:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSpawnNpc.SelectedIndex;
                    break;
                }

                case (int) EventCommand.FadeIn:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.FadeOut:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.FlashScreen:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.SetFog:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudFogData0.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudFogData1.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudFogData2.Value);
                    break;
                }

                case (int) EventCommand.SetWeather:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.CmbWeather.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWeatherIntensity.Value);
                    break;
                }

                case (int) EventCommand.SetScreenTint:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudMapTintData0.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudMapTintData1.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudMapTintData2.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudMapTintData3.Value);
                    break;
                }

                case (int) EventCommand.Wait:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWaitAmount.Value);
                    break;
                }

                case (int) EventCommand.ShowPicture:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudShowPicture.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPicLoc.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetX.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetY.Value);
                    break;
                }

                case (int) EventCommand.HidePicture:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.WaitMovementCompletion:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    if (ListOfEvents != null)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbMoveWait.SelectedIndex];
                    }
                    break;
                }

                case (int) EventCommand.HoldPlayer:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
                    break;
                }

                case (int) EventCommand.ReleasePlayer:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index = (byte) Index;
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

            if (curlist > TmpEvent.Pages[CurPageNum].CommandListCount)
                return;

            if (TmpEvent.Pages[CurPageNum].CommandList == null)
                return;

            if (curslot > TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount)
                return;

            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index)
            {
                case (byte) EventCommand.AddText:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtAddText_Text.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    // EditorEvent.Instance.scrlAddText_Color.Value = tmpEvent.Pages(curPageNum).CommandList(curlist).Commands(curslot).Data1
                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2)
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

                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition)
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

                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition)
                    {
                        case 0:
                        {
                            EditorEvent.Instance.cmbCondition_PlayerVarIndex.Enabled = true;
                            EditorEvent.Instance.cmbCondition_PlayerVarCompare.Enabled = true;
                            EditorEvent.Instance.nudCondition_PlayerVarCondition.Enabled = true;
                            EditorEvent.Instance.cmbCondition_PlayerVarIndex.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondition_PlayerVarCompare.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            EditorEvent.Instance.nudCondition_PlayerVarCondition.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data3;
                            break;
                        }
                        case 1:
                        {
                            EditorEvent.Instance.cmbCondition_PlayerSwitch.Enabled = true;
                            EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.Enabled = true;
                            EditorEvent.Instance.cmbCondition_PlayerSwitch.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 2:
                        {
                            EditorEvent.Instance.cmbCondition_HasItem.Enabled = true;
                            EditorEvent.Instance.nudCondition_HasItem.Enabled = true;
                            EditorEvent.Instance.cmbCondition_HasItem.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.nudCondition_HasItem.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 3:
                        {
                            EditorEvent.Instance.cmbCondition_JobIs.Enabled = true;
                            EditorEvent.Instance.cmbCondition_JobIs.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                        case 4:
                        {
                            EditorEvent.Instance.cmbCondition_LearntSkill.Enabled = true;
                            EditorEvent.Instance.cmbCondition_LearntSkill.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                        case 5:
                        {
                            EditorEvent.Instance.cmbCondition_LevelCompare.Enabled = true;
                            EditorEvent.Instance.nudCondition_LevelAmount.Enabled = true;
                            EditorEvent.Instance.nudCondition_LevelAmount.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondition_LevelCompare.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 6:
                        {
                            EditorEvent.Instance.cmbCondition_SelfSwitch.Enabled = true;
                            EditorEvent.Instance.cmbCondition_SelfSwitchCondition.Enabled = true;
                            EditorEvent.Instance.cmbCondition_SelfSwitch.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            EditorEvent.Instance.cmbCondition_SelfSwitchCondition.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2;
                            break;
                        }
                        case 7:
                        {
                            break;
                        }

                        case 8:
                        {
                            EditorEvent.Instance.cmbCondition_Gender.Enabled = true;
                            EditorEvent.Instance.cmbCondition_Gender.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                        case 9:
                        {
                            EditorEvent.Instance.cmbCondition_Time.Enabled = true;
                            EditorEvent.Instance.cmbCondition_Time.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1;
                            break;
                        }
                    }

                    break;
                }
                case (byte) EventCommand.ShowText:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtShowText.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowText.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ShowChoices:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtChoicePrompt.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.txtChoices1.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text2;
                    EditorEvent.Instance.txtChoices2.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text3;
                    EditorEvent.Instance.txtChoices3.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text4;
                    EditorEvent.Instance.txtChoices4.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text5;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowChoices.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ModifyVariable:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbVariable.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2)
                    {
                        case 0:
                        {
                            EditorEvent.Instance.optVariableAction0.Checked = true;
                            EditorEvent.Instance.nudVariableData0.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            break;
                        }
                        case 1:
                        {
                            EditorEvent.Instance.optVariableAction1.Checked = true;
                            EditorEvent.Instance.nudVariableData1.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            break;
                        }
                        case 2:
                        {
                            EditorEvent.Instance.optVariableAction2.Checked = true;
                            EditorEvent.Instance.nudVariableData2.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            break;
                        }
                        case 3:
                        {
                            EditorEvent.Instance.optVariableAction3.Checked = true;
                            EditorEvent.Instance.nudVariableData3.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                            EditorEvent.Instance.nudVariableData4.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
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
                    EditorEvent.Instance.cmbSwitch.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.cmbPlayerSwitchSet.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayerSwitch.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ModifySelfSwitch:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSetSelfSwitch.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.cmbSetSelfSwitchTo.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetSelfSwitch.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeItems:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbChangeItemIndex.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 0)
                    {
                        EditorEvent.Instance.optChangeItemSet.Checked = true;
                    }
                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 1)
                    {
                        EditorEvent.Instance.optChangeItemAdd.Checked = true;
                    }
                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 2)
                    {
                        EditorEvent.Instance.optChangeItemRemove.Checked = true;
                    }

                    EditorEvent.Instance.nudChangeItemsAmount.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeItems.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeLevel:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudChangeLevel.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeLevel.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeSkills:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbChangeSkills.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 0)
                    {
                        EditorEvent.Instance.optChangeSkillsAdd.Checked = true;
                    }
                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 1)
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
                    EditorEvent.Instance.cmbChangeJob.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeJob.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeSprite:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudChangeSprite.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangeSprite.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ChangeSex:
                {
                    IsEdit = true;
                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 == 0)
                    {
                        EditorEvent.Instance.optChangeSexMale.Checked = true;
                    }
                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 == 1)
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

                    EditorEvent.Instance.cmbSetPK.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraChangePK.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.WarpPlayer:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudWPMap.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudWPX.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.nudWPY.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.cmbWarpPlayerDir.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
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
                            if (i == TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1)
                                EditorEvent.Instance.cmbEvent.SelectedIndex = X;
                        }
                    }

                    IsMoveRouteCommand = true;
                    EditorEvent.Instance.chkIgnoreMove.Checked = Conversions.ToBoolean(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2);
                    EditorEvent.Instance.chkRepeatRoute.Checked = Conversions.ToBoolean(TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3);
                    TempMoveRouteCount = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRouteCount;
                    TempMoveRoute = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRoute;
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
                    EditorEvent.Instance.cmbPlayAnim.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.cmbPlayAnimEvent.Items.Clear();
                    var loopTo2 = Data.MyMap.EventCount;
                    for (i = 0; i < loopTo2; i++)
                        EditorEvent.Instance.cmbPlayAnimEvent.Items.Add(i + 1 + ". " + Data.MyMap.Event[i].Name);
                    EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex = 0;
                    if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 0)
                    {
                        EditorEvent.Instance.cmbAnimTargetType.SelectedIndex = 0;
                    }
                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 1)
                    {
                        EditorEvent.Instance.cmbAnimTargetType.SelectedIndex = 1;
                        EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    }
                    else if (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 == 2)
                    {
                        EditorEvent.Instance.cmbAnimTargetType.SelectedIndex = 2;
                        EditorEvent.Instance.nudPlayAnimTileX.MaxValue = Data.MyMap.MaxX;
                        EditorEvent.Instance.nudPlayAnimTileY.MaxValue = Data.MyMap.MaxY;
                        EditorEvent.Instance.nudPlayAnimTileX.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                        EditorEvent.Instance.nudPlayAnimTileY.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                    }

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraPlayAnimation.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }

                case (byte) EventCommand.PlayBgm:
                {
                    IsEdit = true;
                    var loopTo3 = Information.UBound(Sound.MusicCache);
                    for (i = 0; i < loopTo3; i++)
                    {
                        if ((Sound.MusicCache[i] ?? "") == (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 ?? ""))
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
                    var loopTo4 = Information.UBound(Sound.SoundCache);
                    for (i = 0; i < loopTo4; i++)
                    {
                        if ((Sound.SoundCache[i] ?? "") == (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 ?? ""))
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
                    EditorEvent.Instance.cmbOpenShop.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraOpenShop.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetAccessLevel:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSetAccess.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 - 1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetAccess.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.GiveExperience:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudGiveExp.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraGiveExp.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ShowChatBubble:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtChatbubbleText.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.cmbChatBubbleTargetType.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 - 1;
                    if (EditorEvent.Instance.cmbChatBubbleTarget.Items.Count > -1)
                        EditorEvent.Instance.cmbChatBubbleTarget.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;

                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraShowChatBubble.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.Label:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtLabelName.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraCreateLabel.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.GoToLabel:
                {
                    IsEdit = true;
                    EditorEvent.Instance.txtGoToLabel.Text = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraGoToLabel.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SpawnNpc:
                {
                    IsEdit = true;
                    EditorEvent.Instance.cmbSpawnNpc.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSpawnNpc.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetFog:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudFogData0.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudFogData1.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.nudFogData2.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetFog.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetWeather:
                {
                    IsEdit = true;
                    EditorEvent.Instance.CmbWeather.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudWeatherIntensity.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetWeather.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.SetScreenTint:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudMapTintData0.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.nudMapTintData1.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;
                    EditorEvent.Instance.nudMapTintData2.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.nudMapTintData3.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraMapTint.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.Wait:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudWaitAmount.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;
                    EditorEvent.Instance.fraDialogue.Visible = true;
                    EditorEvent.Instance.fraSetWait.Visible = true;
                    EditorEvent.Instance.fraCommands.Visible = false;
                    break;
                }
                case (byte) EventCommand.ShowPicture:
                {
                    IsEdit = true;
                    EditorEvent.Instance.nudShowPicture.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1;

                    EditorEvent.Instance.cmbPicLoc.SelectedIndex = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2;

                    EditorEvent.Instance.nudPicOffsetX.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3;
                    EditorEvent.Instance.nudPicOffsetY.Value = TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4;
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
                            if (i == TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1)
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

            if (curlist > TmpEvent.Pages[CurPageNum].CommandListCount)
                return;

            if (TmpEvent.Pages[CurPageNum].CommandList == null)
                return;

            if (curslot >= TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount)
                return;

            if (TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount != i + 1)
            {
                TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount--;
                p = TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount;
                oldCommandList = TmpEvent.Pages[CurPageNum].CommandList[curlist];

                if (p <= 0)
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands = new Type.EventCommand[1];
                }
                else
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands = new Type.EventCommand[p];
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].ParentList = oldCommandList.ParentList;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount = p;

                    // Move all commands down by 1  
                    for (i = EditorEvent.Instance.lstCommands.SelectedIndex + 1; i <= p; i++)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[i - 1] = oldCommandList.Commands[i];
                    }
                }
            }
            else
            {
                // If we are deleting the last command in the list, set only the last command  
                TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount--;
                Array.Resize(ref TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands, TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount);
            }

            EventListCommands();
        }

        public static void ClearEventCommands()
        {
            TmpEvent.Pages[CurPageNum].CommandList = new Type.CommandList[1];
            TmpEvent.Pages[CurPageNum].CommandListCount = 0;
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

            if (curlist > TmpEvent.Pages[CurPageNum].CommandListCount)
                return;

            if (curslot > TmpEvent.Pages[CurPageNum].CommandList[curlist].CommandCount)
                return;

            switch (TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Index)
            {
                case (byte) EventCommand.AddText:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtAddText_Text.Text;
                    // tmpEvent.Pages(curPageNum).CommandList(curlist).Commands(curslot).Data1 = EditorEvent.Instance.scrlAddText_Color.Value
                    if (EditorEvent.Instance.optAddText_Player.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optAddText_Map.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optAddText_Global.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    break;
                }
                case (byte) EventCommand.ConditionalBranch:
                {
                    if (EditorEvent.Instance.optCondition0.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 0;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerVarIndex.SelectedIndex;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_PlayerVarCompare.SelectedIndex;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data3 = (int) Math.Round(EditorEvent.Instance.nudCondition_PlayerVarCondition.Value);
                    }
                    else if (EditorEvent.Instance.optCondition1.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 1;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_PlayerSwitch.SelectedIndex;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondtion_PlayerSwitchCondition.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition2.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 2;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_HasItem.SelectedIndex;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = (int) Math.Round(EditorEvent.Instance.nudCondition_HasItem.Value);
                    }
                    else if (EditorEvent.Instance.optCondition3.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 3;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_JobIs.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition4.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 4;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_LearntSkill.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition5.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 5;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = (int) Math.Round(EditorEvent.Instance.nudCondition_LevelAmount.Value);
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_LevelCompare.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition6.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 6;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_SelfSwitch.SelectedIndex;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data2 = EditorEvent.Instance.cmbCondition_SelfSwitchCondition.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition8.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 8;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Gender.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.optCondition9.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Condition = 9;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].ConditionalBranch.Data1 = EditorEvent.Instance.cmbCondition_Time.SelectedIndex;
                    }

                    break;
                }
                case (byte) EventCommand.ShowText:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtShowText.Text;
                    break;
                }
                case (byte) EventCommand.ShowChoices:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChoicePrompt.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text2 = EditorEvent.Instance.txtChoices1.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text3 = EditorEvent.Instance.txtChoices2.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text4 = EditorEvent.Instance.txtChoices3.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text5 = EditorEvent.Instance.txtChoices4.Text;
                    break;
                }
                case (byte) EventCommand.ModifyVariable:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbVariable.SelectedIndex;
                    if (EditorEvent.Instance.optVariableAction0.Checked == true)
                        i = 0;
                    if (EditorEvent.Instance.optVariableAction1.Checked == true)
                        i = 1;
                    if (EditorEvent.Instance.optVariableAction2.Checked == true)
                        i = 2;
                    if (EditorEvent.Instance.optVariableAction3.Checked == true)
                        i = 3;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = i;
                    if (i == 0)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData0.Value);
                    }
                    else if (i == 1)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData1.Value);
                    }
                    else if (i == 2)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData2.Value);
                    }
                    else if (i == 3)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudVariableData3.Value);
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudVariableData4.Value);
                    }

                    break;
                }
                case (byte) EventCommand.ModifySwitch:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSwitch.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPlayerSwitchSet.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.ModifySelfSwitch:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetSelfSwitch.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbSetSelfSwitchTo.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.ChangeItems:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeItemIndex.SelectedIndex;
                    if (EditorEvent.Instance.optChangeItemSet.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeItemAdd.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else if (EditorEvent.Instance.optChangeItemRemove.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                    }

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudChangeItemsAmount.Value);
                    break;
                }
                case (byte) EventCommand.ChangeLevel:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeLevel.Value);
                    break;
                }
                case (byte) EventCommand.ChangeSkills:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeSkills.SelectedIndex;
                    if (EditorEvent.Instance.optChangeSkillsAdd.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeSkillsRemove.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }

                    break;
                }
                case (byte) EventCommand.ChangeJob:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChangeJob.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.ChangeSprite:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudChangeSprite.Value);
                    break;
                }
                case (byte) EventCommand.ChangeSex:
                {
                    if (EditorEvent.Instance.optChangeSexMale.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = 0;
                    }
                    else if (EditorEvent.Instance.optChangeSexFemale.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = 1;
                    }

                    break;
                }
                case (byte) EventCommand.SetPlayerKillable:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetPK.SelectedIndex;
                    break;
                }

                case (byte) EventCommand.WarpPlayer:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWPMap.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWPX.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudWPY.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = EditorEvent.Instance.cmbWarpPlayerDir.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.SetMoveRoute:
                {
                    if (ListOfEvents != null)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbEvent.SelectedIndex];
                    }
                    if (EditorEvent.Instance.chkIgnoreMove.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                    }
                    else
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }

                    if (EditorEvent.Instance.chkRepeatRoute.Checked == true)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 1;
                    }
                    else
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = 0;
                    }

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRouteCount = TempMoveRouteCount;
                    if (TempMoveRoute != null)
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].MoveRoute = TempMoveRoute;
                    break;
                }
                case (byte) EventCommand.PlayAnimation:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbPlayAnim.SelectedIndex;
                    if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 0)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 0;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 1)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 1;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = EditorEvent.Instance.cmbPlayAnimEvent.SelectedIndex;
                    }
                    else if (EditorEvent.Instance.cmbAnimTargetType.SelectedIndex == 2)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = 2;
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileX.Value);
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPlayAnimTileY.Value);
                    }

                    break;
                }
                case (byte) EventCommand.PlayBgm:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Sound.MusicCache[EditorEvent.Instance.cmbPlayBGM.SelectedIndex];
                    break;
                }
                case (byte) EventCommand.PlaySound:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = Sound.SoundCache[EditorEvent.Instance.cmbPlaySound.SelectedIndex];
                    break;
                }
                case (byte) EventCommand.OpenShop:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbOpenShop.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.SetAccessLevel:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSetAccess.SelectedIndex + 1;
                    break;
                }
                case (byte) EventCommand.GiveExperience:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudGiveExp.Value);
                    break;
                }
                case (byte) EventCommand.ShowChatBubble:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtChatbubbleText.Text;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbChatBubbleTargetType.SelectedIndex + 1;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbChatBubbleTarget.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.Label:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtLabelName.Text;
                    break;
                }
                case (byte) EventCommand.GoToLabel:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Text1 = EditorEvent.Instance.txtGoToLabel.Text;
                    break;
                }
                case (byte) EventCommand.SpawnNpc:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.cmbSpawnNpc.SelectedIndex;
                    break;
                }
                case (byte) EventCommand.SetFog:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudFogData0.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudFogData1.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudFogData2.Value);
                    break;
                }
                case (byte) EventCommand.SetWeather:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = EditorEvent.Instance.CmbWeather.SelectedIndex;
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudWeatherIntensity.Value);
                    break;
                }
                case (byte) EventCommand.SetScreenTint:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudMapTintData0.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = (int) Math.Round(EditorEvent.Instance.nudMapTintData1.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudMapTintData2.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudMapTintData3.Value);
                    break;
                }
                case (byte) EventCommand.Wait:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudWaitAmount.Value);
                    break;
                }
                case (byte) EventCommand.ShowPicture:
                {
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = (int) Math.Round(EditorEvent.Instance.nudShowPicture.Value);

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data2 = EditorEvent.Instance.cmbPicLoc.SelectedIndex;

                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data3 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetX.Value);
                    TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data4 = (int) Math.Round(EditorEvent.Instance.nudPicOffsetY.Value);
                    break;
                }
                case (byte) EventCommand.WaitMovementCompletion:
                {
                    if (ListOfEvents != null)
                    {
                        TmpEvent.Pages[CurPageNum].CommandList[curlist].Commands[curslot].Data1 = ListOfEvents[EditorEvent.Instance.cmbMoveWait.SelectedIndex];
                    }
                    break;
                }
            }

            EventListCommands();
        }

        #endregion

        #region Incoming Packets

        public static void Packet_SpawnEvent(ReadOnlyMemory<byte> data)
        {
            int id;
            var buffer = new PacketReader(data);

            GameState.CurrentEvents = buffer.ReadInt32();
            Array.Resize(ref Data.MapEvents, GameState.CurrentEvents);

            for (int i = 0; i < GameState.CurrentEvents; i++)
            {
                id = buffer.ReadInt32();

                if (id >= GameState.CurrentEvents)
                    break;

                ref var withBlock = ref Data.MapEvents[id];
                withBlock.Name = buffer.ReadString();
                withBlock.Dir = buffer.ReadInt32();
                withBlock.ShowDir = withBlock.Dir;
                withBlock.GraphicType = buffer.ReadByte();
                withBlock.Graphic = buffer.ReadInt32();
                withBlock.GraphicX = buffer.ReadInt32();
                withBlock.GraphicX2 = buffer.ReadInt32();
                withBlock.GraphicY = buffer.ReadInt32();
                withBlock.GraphicY2 = buffer.ReadInt32();
                withBlock.MovementSpeed = buffer.ReadInt32();
                withBlock.Moving = 0;
                withBlock.X = buffer.ReadInt32();
                withBlock.Y = buffer.ReadInt32();
                withBlock.Position = buffer.ReadByte();
                withBlock.Visible = buffer.ReadBoolean();
                withBlock.WalkAnim = buffer.ReadInt32();
                withBlock.DirFix = buffer.ReadInt32();
                withBlock.WalkThrough = buffer.ReadInt32();
                withBlock.ShowName = buffer.ReadInt32();
            }
        }

        public static void Packet_EventMove(ReadOnlyMemory<byte> data)
        {
            int id;
            int x;
            int y;
            int dir;
            int showDir;
            int movementSpeed;
            var buffer = new PacketReader(data);

            id = buffer.ReadInt32();
            x = buffer.ReadInt32();
            y = buffer.ReadInt32();
            dir = buffer.ReadInt32();
            showDir = buffer.ReadInt32();
            movementSpeed = buffer.ReadInt32();

            if (id > GameState.CurrentEvents)
                return;

            {
                if (Data.MapEvents == null)
                    return;
                ref var withBlock = ref Data.MapEvents[id];
                withBlock.X = x;
                withBlock.Y = y;
                withBlock.Dir = dir;
                withBlock.Moving = 1;
                withBlock.ShowDir = showDir;
                withBlock.MovementSpeed = movementSpeed;
            }
        }

        public static void Packet_EventDir(ReadOnlyMemory<byte> data)
        {
            int i;
            byte dir;
            var buffer = new PacketReader(data);
            i = buffer.ReadInt32();
            dir = (byte) buffer.ReadInt32();

            if (i > GameState.CurrentEvents)
                return;

            {
                if (Data.MapEvents == null)
                    return;
                ref var withBlock = ref Data.MapEvents[i];
                withBlock.Dir = dir;
                withBlock.ShowDir = dir;
                withBlock.Moving = 0;
            }
        }

        public static void Packet_SwitchesAndVariables(ReadOnlyMemory<byte> data)
        {
            int i;
            var buffer = new PacketReader(data);

            for (i = 0; i < Core.Globals.Variables.MaxSwitches; i++)
                Switches[i] = buffer.ReadString();

            for (i = 0; i < Core.Globals.Variables.MaxVariables; i++)
                Variables[i] = buffer.ReadString();
        }

        public static void Packet_MapEventData(ReadOnlyMemory<byte> data)
        {
            int i;
            int x;
            int y;
            int z;
            int w;
            var buffer = new PacketReader(data);

            Data.MyMap.EventCount = buffer.ReadInt32();

            if (Data.MyMap.EventCount > 0)
            {
                Data.MyMap.Event = new Type.Event[Data.MyMap.EventCount];
                var loopTo = Data.MyMap.EventCount;
                for (i = 0; i < loopTo; i++)
                {
                    {
                        ref var withBlock = ref Data.MyMap.Event[i];
                        withBlock.Name = buffer.ReadString();
                        withBlock.Globals = buffer.ReadByte();
                        withBlock.X = buffer.ReadInt32();
                        withBlock.Y = buffer.ReadInt32();
                        withBlock.PageCount = buffer.ReadInt32();
                    }

                    if (Data.MyMap.Event[i].PageCount > 0)
                    {
                        Data.MyMap.Event[i].Pages = new Type.EventPage[Data.MyMap.Event[i].PageCount];
                        var loopTo1 = Data.MyMap.Event[i].PageCount;
                        for (x = 0; x < loopTo1; x++)
                        {
                            {
                                ref var withBlock1 = ref Data.MyMap.Event[i].Pages[x];
                                withBlock1.ChkVariable = buffer.ReadInt32();
                                withBlock1.VariableIndex = buffer.ReadInt32();
                                withBlock1.VariableCondition = buffer.ReadInt32();
                                withBlock1.VariableCompare = buffer.ReadInt32();
                                withBlock1.ChkSwitch = buffer.ReadInt32();
                                withBlock1.SwitchIndex = buffer.ReadInt32();
                                withBlock1.SwitchCompare = buffer.ReadInt32();
                                withBlock1.ChkHasItem = buffer.ReadInt32();
                                withBlock1.HasItemIndex = buffer.ReadInt32();
                                withBlock1.HasItemAmount = buffer.ReadInt32();
                                withBlock1.ChkSelfSwitch = buffer.ReadInt32();
                                withBlock1.SelfSwitchIndex = buffer.ReadInt32();
                                withBlock1.SelfSwitchCompare = buffer.ReadInt32();
                                withBlock1.GraphicType = buffer.ReadByte();
                                withBlock1.Graphic = buffer.ReadInt32();
                                withBlock1.GraphicX = buffer.ReadInt32();
                                withBlock1.GraphicY = buffer.ReadInt32();
                                withBlock1.GraphicX2 = buffer.ReadInt32();
                                withBlock1.GraphicY2 = buffer.ReadInt32();

                                withBlock1.MoveType = buffer.ReadByte();
                                withBlock1.MoveSpeed = buffer.ReadByte();
                                withBlock1.MoveFreq = buffer.ReadByte();
                                withBlock1.MoveRouteCount = buffer.ReadInt32();
                                withBlock1.IgnoreMoveRoute = buffer.ReadInt32();
                                withBlock1.RepeatMoveRoute = buffer.ReadInt32();

                                if (withBlock1.MoveRouteCount > 0)
                                {
                                    Data.MyMap.Event[i].Pages[x].MoveRoute = new Type.MoveRoute[withBlock1.MoveRouteCount];
                                    var loopTo2 = withBlock1.MoveRouteCount;
                                    for (y = 0; y < loopTo2; y++)
                                    {
                                        withBlock1.MoveRoute[y].Index = buffer.ReadInt32();
                                        withBlock1.MoveRoute[y].Data1 = buffer.ReadInt32();
                                        withBlock1.MoveRoute[y].Data2 = buffer.ReadInt32();
                                        withBlock1.MoveRoute[y].Data3 = buffer.ReadInt32();
                                        withBlock1.MoveRoute[y].Data4 = buffer.ReadInt32();
                                        withBlock1.MoveRoute[y].Data5 = buffer.ReadInt32();
                                        withBlock1.MoveRoute[y].Data6 = buffer.ReadInt32();
                                    }
                                }

                                withBlock1.WalkAnim = buffer.ReadInt32();
                                withBlock1.DirFix = buffer.ReadInt32();
                                withBlock1.WalkThrough = buffer.ReadInt32();
                                withBlock1.ShowName = buffer.ReadInt32();
                                withBlock1.Trigger = buffer.ReadByte();
                                withBlock1.CommandListCount = buffer.ReadInt32();
                                withBlock1.Position = buffer.ReadByte();
                            }

                            if (Data.MyMap.Event[i].Pages[x].CommandListCount > 0)
                            {
                                Data.MyMap.Event[i].Pages[x].CommandList = new Type.CommandList[Data.MyMap.Event[i].Pages[x].CommandListCount];
                                var loopTo3 = Data.MyMap.Event[i].Pages[x].CommandListCount;
                                for (y = 0; y < loopTo3; y++)
                                {
                                    Data.MyMap.Event[i].Pages[x].CommandList[y].CommandCount = buffer.ReadInt32();
                                    Data.MyMap.Event[i].Pages[x].CommandList[y].ParentList = buffer.ReadInt32();
                                    if (Data.MyMap.Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                    {
                                        Data.MyMap.Event[i].Pages[x].CommandList[y].Commands = new Type.EventCommand[Data.MyMap.Event[i].Pages[x].CommandList[y].CommandCount];
                                        var loopTo4 = Data.MyMap.Event[i].Pages[x].CommandList[y].CommandCount;
                                        for (z = 0; z < loopTo4; z++)
                                        {
                                            {
                                                ref var withBlock2 = ref Data.MyMap.Event[i].Pages[x].CommandList[y].Commands[z];
                                                withBlock2.Index = buffer.ReadInt32();
                                                withBlock2.Text1 = buffer.ReadString();
                                                withBlock2.Text2 = buffer.ReadString();
                                                withBlock2.Text3 = buffer.ReadString();
                                                withBlock2.Text4 = buffer.ReadString();
                                                withBlock2.Text5 = buffer.ReadString();
                                                withBlock2.Data1 = buffer.ReadInt32();
                                                withBlock2.Data2 = buffer.ReadInt32();
                                                withBlock2.Data3 = buffer.ReadInt32();
                                                withBlock2.Data4 = buffer.ReadInt32();
                                                withBlock2.Data5 = buffer.ReadInt32();
                                                withBlock2.Data6 = buffer.ReadInt32();
                                                withBlock2.ConditionalBranch.CommandList = buffer.ReadInt32();
                                                withBlock2.ConditionalBranch.Condition = buffer.ReadInt32();
                                                withBlock2.ConditionalBranch.Data1 = buffer.ReadInt32();
                                                withBlock2.ConditionalBranch.Data2 = buffer.ReadInt32();
                                                withBlock2.ConditionalBranch.Data3 = buffer.ReadInt32();
                                                withBlock2.ConditionalBranch.ElseCommandList = buffer.ReadInt32();
                                                withBlock2.MoveRouteCount = buffer.ReadInt32();

                                                if (withBlock2.MoveRouteCount > 0)
                                                {
                                                    withBlock2.MoveRoute = new Type.MoveRoute[withBlock2.MoveRouteCount];
                                                    var loopTo5 = withBlock2.MoveRouteCount;
                                                    for (w = 0; w < loopTo5; w++)
                                                    {
                                                        withBlock2.MoveRoute[w].Index = buffer.ReadInt32();
                                                        withBlock2.MoveRoute[w].Data1 = buffer.ReadInt32();
                                                        withBlock2.MoveRoute[w].Data2 = buffer.ReadInt32();
                                                        withBlock2.MoveRoute[w].Data3 = buffer.ReadInt32();
                                                        withBlock2.MoveRoute[w].Data4 = buffer.ReadInt32();
                                                        withBlock2.MoveRoute[w].Data5 = buffer.ReadInt32();
                                                        withBlock2.MoveRoute[w].Data6 = buffer.ReadInt32();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void Packet_EventChat(ReadOnlyMemory<byte> data)
        {
            int i;
            int choices;
            var buffer = new PacketReader(data);
            EventReplyId = buffer.ReadInt32();
            EventReplyPage = buffer.ReadInt32();
            EventChatFace = buffer.ReadInt32();
            EventText = buffer.ReadString();
            if (string.IsNullOrEmpty(EventText))
                EventText = " ";
            EventChat = true;
            ShowEventLbl = true;
            choices = buffer.ReadInt32();
            
            for (i = 0; i < Core.Globals.Variables.MaxEventChoices; i++)
            {
                EventChoices[i] = "";
                EventChoiceVisible[i] = false;
            }

            EventChatType = 0;
            if (choices == 0)
            {
            }
            else
            {
                EventChatType = 1;
                var loopTo = choices;
                for (i = 0; i < loopTo; i++)
                {
                    EventChoices[i] = buffer.ReadString();
                    EventChoiceVisible[i] = true;
                }
            }

            AnotherChat = buffer.ReadInt32();
        }

        public static void Packet_EventStart(ReadOnlyMemory<byte> data)
        {
            InEvent = true;
        }

        public static void Packet_EventEnd(ReadOnlyMemory<byte> data)
        {
            InEvent = false;
        }

        public static void Packet_Picture(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);
            int picIndex;
            int spriteType;
            int xOffset;
            int yOffset;
            int eventid;

            eventid = buffer.ReadInt32();
            picIndex = buffer.ReadByte();

            if (picIndex == 0)
            {
                Picture.Index = 0;
                Picture.EventId = 0;
                Picture.SpriteType = 0;
                Picture.XOffset = 0;
                Picture.YOffset = 0;
                return;
            }

            spriteType = buffer.ReadByte();
            xOffset = buffer.ReadByte();
            yOffset = buffer.ReadByte();

            Picture.Index = (byte) picIndex;
            Picture.EventId = eventid;
            Picture.SpriteType = (byte) spriteType;
            Picture.XOffset = (byte) xOffset;
            Picture.YOffset = (byte) yOffset;
        }

        public static void Packet_HidePicture(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);

            Picture = default;
        }

        public static void Packet_HoldPlayer(ReadOnlyMemory<byte> data)
        {
            var buffer = new PacketReader(data);
            if (buffer.ReadInt32() == 0)
            {
                HoldPlayer = true;
            }
            else
            {
                HoldPlayer = false;
            }
        }

        public static void Packet_PlayBGM(ReadOnlyMemory<byte> data)
        {
            string music;
            var buffer = new PacketReader(data);

            music = buffer.ReadString();
            Data.MyMap.Music = music;
        }

        public static void Packet_FadeOutBGM(ReadOnlyMemory<byte> data)
        {
            Sound.CurrentMusic = "";
            Sound.FadeOutSwitch = true;
        }

        public static void Packet_PlaySound(ReadOnlyMemory<byte> data)
        {
            string sound;
            var buffer = new PacketReader(data);
            int x;
            int y;

            sound = buffer.ReadString();
            x = buffer.ReadInt32();
            y = buffer.ReadInt32();

            Sound.PlaySound(sound, x, y);
        }

        public static void Packet_StopSound(ReadOnlyMemory<byte> data)
        {
            Sound.StopSound();
        }

        public static void Packet_SpecialEffect(ReadOnlyMemory<byte> data)
        {
            int effectType;
            var buffer = new PacketReader(data);
            effectType = buffer.ReadInt32();

            switch (effectType)
            {
                case GameState.EffectTypeFadein:
                {
                    GameState.UseFade = true;
                    GameState.FadeType = 1;
                    GameState.FadeAmount = 0;
                    break;
                }
                case GameState.EffectTypeFadeout:
                {
                    GameState.UseFade = true;
                    GameState.FadeType = 0;
                    GameState.FadeAmount = 255;
                    break;
                }
                case GameState.EffectTypeFlash:
                {
                    GameState.FlashTimer = General.GetTickCount() + 150;
                    break;
                }
                case GameState.EffectTypeFog:
                {
                    GameState.CurrentFog = buffer.ReadInt32();
                    GameState.CurrentFogSpeed = buffer.ReadInt32();
                    GameState.CurrentFogOpacity = buffer.ReadInt32();
                    break;
                }
                case GameState.EffectTypeWeather:
                {
                    GameState.CurrentWeather = buffer.ReadInt32();
                    GameState.CurrentWeatherIntensity = buffer.ReadInt32();
                    break;
                }
                case GameState.EffectTypeTint:
                {
                    Data.MyMap.MapTint = true;
                    GameState.CurrentTintR = buffer.ReadInt32();
                    GameState.CurrentTintG = buffer.ReadInt32();
                    GameState.CurrentTintB = buffer.ReadInt32();
                    GameState.CurrentTintA = buffer.ReadInt32();
                    break;
                }
            }
        }

        #endregion

        #region Outgoing Packets

        public static void RequestSwitchesAndVariables()
        {
            var packetWriter = new PacketWriter(4);

            packetWriter.WriteEnum(Packets.ClientPackets.CRequestSwitchesAndVariables);

            Network.Send(packetWriter);
        }

        public static void SendSwitchesAndVariables()
        {
            var packetWriter = new PacketWriter(4);

            packetWriter.WriteEnum(Packets.ClientPackets.CSwitchesAndVariables);

            for (var i = 0; i < Core.Globals.Variables.MaxSwitches; i++)
            {
                packetWriter.WriteString(Switches[i]);
            }

            for (var i = 0; i < Core.Globals.Variables.MaxVariables; i++)
            {
                packetWriter.WriteString(Variables[i]);
            }

            Network.Send(packetWriter);
        }

        #endregion

        #region Misc

        public static void ProcessEventMovement(int id)
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
                            Data.MapEvents[id].Y = (byte)Math.Max(0, Data.MapEvents[id].Y - 1);
                            break;
                        case (int)Direction.Down:
                            Data.MapEvents[id].Y = (byte)Math.Min(byte.MaxValue, Data.MapEvents[id].Y + 1);
                            break;
                        case (int)Direction.Left:
                            Data.MapEvents[id].X = (byte)Math.Max(0, Data.MapEvents[id].X - 1);
                            break;
                        case (int)Direction.Right:
                            Data.MapEvents[id].X = (byte)Math.Min(byte.MaxValue, Data.MapEvents[id].X + 1);
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
    }
}