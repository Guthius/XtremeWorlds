using Core;
using System;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Game.UI.Windows;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;

namespace Client
{
    public class Loop
    {
        // Declare private fields
        private static int _i;
        private static int _tmr1000;
        private static int _tick;
        private static int _fogTmr;
        private static int _chatTmr;
        #pragma warning disable CS0169
        private static int _tmpFps;
        private static int _tmpLps;
        private static int _walkTimer;
        private static int _frameTime;
        private static int _tmrWeather;
        private static int _barTmr;
        private static int _tmr25;
        private static int _tmr500;
        private static int _tmr250;
        private static int _tmrConnect;
        private static int _tickFps;
        private static int _fadeTmr;
        private static int _renderTmr;
    #pragma warning restore CS0169
        private static int[] _animationTmr = new int[2];
        private static int _lastMouseAttackX = int.MinValue; // cache last facing update
        private static int _lastMouseAttackY = int.MinValue;

        public static void Game()
        {
            _tick = General.GetTickCount();
            GameState.ElapsedTime = _tick - _frameTime; // Set the time difference for time-based movement

            _frameTime = _tick;

            if (GameLogic.GameStarted())
            {
                if (_tmr1000 < _tick)
                {
                    Sender.GetPing();
                    _tmr1000 = _tick + 1000;
                }

                if (_tmr25 < _tick)
                {
                    Audio.PlayMusic(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Music);
                    UpdateEditors();
                    _tmr25 = _tick + 25;
                }

                if (GameState.ShowAnimTimer < _tick)
                {
                    GameState.ShowAnimLayers = !GameState.ShowAnimLayers;
                    GameState.ShowAnimTimer = _tick + 500;
                }

                for (int layer = 0; layer <= 1; layer++)
                {
                    if (_animationTmr[layer] < _tick)
                    {
                        if (Map.Instance.Count <= GetPlayerMap(GameState.MyIndex)) continue; // No maps loaded
                        byte mapMaxX = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX;
                        for (byte x = 0; x < mapMaxX; x++)
                        {
                            byte mapMaxY = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY;
                            for (byte y = 0; y < mapMaxY; y++)
                            {
                                if (GameLogic.IsValidMapPoint(x, y))
                                {
                                    if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type == TileType.Animation)
                                    {      
                                        if (Animation.Instance.Count <= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1) continue; // No animations loaded                           
                                        _animationTmr[layer] = _tick + MapAnimation.OnPlay(Animation.Instance[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1].Sprite[layer], layer, Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1, x, y);
                                    }

                                    if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type2 == TileType.Animation)
                                    {
                                        if (Animation.Instance.Count <= Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1_2) continue; // No animations loaded                           
                                        _animationTmr[layer] = _tick + MapAnimation.OnPlay(Animation.Instance[Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1_2].Sprite[layer], layer, Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1_2, x, y);
                                    }
                                }
                            }
                        }
                        ;
                    }
                }

                for (_i = 0; _i < byte.MaxValue; _i++)
                {
                    MapAnimation.OnUpdate(_i);
                }

                if (_tick > Event.EventChatTimer)
                {
                    if (string.IsNullOrEmpty(Event.EventText))
                    {
                        if (Event.EventChat)
                        {
                            Event.EventChat = false;
                        }
                    }
                }

                // screenshake
                if (GameState.ShakeTimerEnabled)
                {
                    if (GameState.ShakeTimer < _tick)
                    {
                        if (GameState.ShakeCount < 10)
                        {
                            if (GameState.LastDir == 0)
                            {
                                GameState.LastDir = 1;
                            }
                            else
                            {
                                GameState.LastDir = 0;
                            }
                        }
                        else
                        {
                            GameState.ShakeCount = 0;
                            GameState.ShakeTimerEnabled = false;
                        }

                        GameState.ShakeCount += 1;

                        GameState.ShakeTimer = _tick + 50;
                    }
                }

                // check if we need to end the CD icon
                if (GameState.NumSkills > 0)
                {
                    for (_i = 0; _i < Variables.MaxPlayerSkills; _i++)
                    {
                        if (Player.Instance[GameState.MyIndex].Skill[_i].Num >= 0)
                        {
                            if (Player.Instance[GameState.MyIndex].Skill[_i].Cd > 0)
                            {
                                if (Player.Instance[GameState.MyIndex].Skill[_i].Cd + Skill.Instance[(int)Player.Instance[GameState.MyIndex].Skill[_i].Num].CdTime * 1000 < _tick)
                                {
                                    Player.Instance[GameState.MyIndex].Skill[_i].Cd = 0;
                                }
                            }
                        }
                    }
                }

                // check if we need to unlock the player's skill casting restriction
                if (GameState.SkillBuffer >= 0)
                {
                    if (GameState.SkillBufferTimer + Skill.Instance[(int)Player.Instance[GameState.MyIndex].Skill[GameState.SkillBuffer].Num].CastTime * 1000 < _tick)
                    {
                        GameState.SkillBuffer = -1;
                        GameState.SkillBufferTimer = 0;
                    }
                }
                
                // Process input before rendering, otherwise input will be behind by 1 frame
                if (_walkTimer < _tick)
                {
                    if (GameState.CanMoveNow)
                    {
                        Player.OnMove(); // Check if player is trying to move
                        Player.OnAttack(); // Keyboard attack
                        // Mouse attack support:
                        // 1. On fresh press, face cursor & attempt attack.
                        // 2. While held, keep facing cursor and attempt attack when cooldown ready.
                        var leftPressedNow = GameClient.CurrentMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                        var leftPressedPrev = GameClient.PreviousMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                        if (leftPressedNow && !leftPressedPrev && !WindowManager.IsWindowActive)
                        {
                            Player.UpdateFacingFromMouse(GameClient.CurrentMouseState.X, GameClient.CurrentMouseState.Y);
                            Player.OnAttack(mouse: true);
                        }
                        else if (leftPressedNow && !WindowManager.IsWindowActive)
                        {
                            // While holding: only update facing if cursor moved at least 2px to reduce network spam
                            // (Simple heuristic: compare to last stored facing update position.)
                            // We'll store last position in GameState (add if not present) or use static fields in Loop.
                            // For minimal intrusion, static private cached fields:
                            if (_lastMouseAttackX != GameClient.CurrentMouseState.X || _lastMouseAttackY != GameClient.CurrentMouseState.Y)
                            {
                                // Handle initial case or check distance moved
                                if (_lastMouseAttackX == int.MinValue || _lastMouseAttackY == int.MinValue ||
                                    Math.Abs(_lastMouseAttackX - GameClient.CurrentMouseState.X) + Math.Abs(_lastMouseAttackY - GameClient.CurrentMouseState.Y) >= 2)
                                {
                                    Player.UpdateFacingFromMouse(GameClient.CurrentMouseState.X, GameClient.CurrentMouseState.Y);
                                    _lastMouseAttackX = GameClient.CurrentMouseState.X;
                                    _lastMouseAttackY = GameClient.CurrentMouseState.Y;
                                }
                            }   
                            // Attempt attack each tick; internal cooldown logic in OnCheckAttack prevents spam.
                            Player.OnAttack(mouse: true);
                        }
                    }
                    
                    // Process player movements
                    for (_i = 0; _i < Player.Instance.Count; _i++)
                    {
                        if (IsPlaying(_i))
                        {
                            Player.OnMove(_i);                            
                        }
                    }

                    // Process npc movements
                    for (_i = 0; _i < Variables.MaxMapNpcs; _i++)
                    {
                        Npc.OnMove(_i);
                        
                    }

                    var loopTo2 = GameState.CurrentEvents;
                    for (_i = 0; _i < loopTo2; _i++)
                    {
                        Event.OnMove(_i);
                    }

                    _walkTimer = _tick + 5;
                }

                // chat timer
                if (_chatTmr < _tick)
                {
                    // scrolling
                    if (GameState.ChatButtonUp)
                    {
                        GameLogic.ScrollChatBox(0);
                    }

                    if (GameState.ChatButtonDown)
                    {
                        GameLogic.ScrollChatBox(1);
                    }

                    _chatTmr = _tick + 50;
                }

                // fog scrolling
                if (_fogTmr < _tick)
                {
                    if (GameState.CurrentFogSpeed > 0)
                    {
                        // move
                        GameState.FogOffsetX = GameState.FogOffsetX - 1;
                        GameState.FogOffsetY = GameState.FogOffsetY - 1;

                        // reset
                        if (GameState.FogOffsetX < -255)
                            GameState.FogOffsetX = 1;

                        if (GameState.FogOffsetY < -255)
                            GameState.FogOffsetY = 1;

                        _fogTmr = _tick + 255 - GameState.CurrentFogSpeed;
                    }
                }

                if (_tmr500 < _tick)
                {
                    // animate waterfalls
                    switch (GameState.WaterfallFrame)
                    {
                        case 0:
                            {
                                GameState.WaterfallFrame = 1;
                                break;
                            }
                        case 1:
                            {
                                GameState.WaterfallFrame = 2;
                                break;
                            }
                        case 2:
                            {
                                GameState.WaterfallFrame = 0;
                                break;
                            }
                    }

                    // animate autotiles
                    switch (GameState.AutoTileFrame)
                    {
                        case 0:
                            {
                                GameState.AutoTileFrame = 1;
                                break;
                            }
                        case 1:
                            {
                                GameState.AutoTileFrame = 2;
                                break;
                            }
                        case 2:
                            {
                                GameState.AutoTileFrame = 0;
                                break;
                            }
                    }

                    // animate textbox
                    if (GameState.ChatShowLine == "|")
                    {
                        GameState.ChatShowLine = "";
                    }
                    else
                    {
                        GameState.ChatShowLine = "|";
                    }

                    _tmr500 = _tick + 500;
                }

                // elastic bars
                if (_barTmr < _tick)
                {
                    GameLogic.SetBarWidth(ref GameState.BarWidthGuiHPMax, ref GameState.BarWidthGuiHP);
                    GameLogic.SetBarWidth(ref GameState.BarWidthGuiMPMax, ref GameState.BarWidthGuiMP);
                    GameLogic.SetBarWidth(ref GameState.BarWidthGuiExpMax, ref GameState.BarWidthGuiExp);
                    for (_i = 0; _i < Variables.MaxMapNpcs; _i++)
                    {
                        if (MapNpc.Instance[_i].Num >= 0)
                        {
                            GameLogic.SetBarWidth(ref GameState.BarWidthPlayerHPMax[_i], ref GameState.BarWidthPlayerHP[_i]);
                        }
                    }

                    for (_i = 0; _i < Player.Instance.Count; _i++)
                    {
                        if (IsPlaying(_i) & GetPlayerMap(_i) == GetPlayerMap(GameState.MyIndex))
                        {
                            GameLogic.SetBarWidth(ref GameState.BarWidthPlayerHPMax[_i], ref GameState.BarWidthPlayerHP[_i]);
                            GameLogic.SetBarWidth(ref GameState.BarWidthPlayerMPMax[_i], ref GameState.BarWidthPlayerMP[_i]);
                        }
                    }

                    // reset timer
                    _barTmr = _tick + 10;
                }

                // Change map animation
                if (_tmr250 < _tick)
                {
                    for (int i = 0; i < Player.Instance.Count; i++)
                    {
                        if (!IsPlaying(i)) continue;
                        if (GetPlayerMap(i) != GetPlayerMap(GameState.MyIndex)) continue;
                        // Always advance Steps (used modulo by idle/run frame counts in draw)
                        unchecked { Player.Instance[i].Steps++; } // byte wraps automatically
                    }

                    for (int i = 0; i < Variables.MaxMapNpcs; i++)
                    {
                        if (MapNpc.Instance[i].Num >= 0)
                        {
                            unchecked { MapNpc.Instance[i].Steps++; }
                        }
                    }

                    var loopTo = GameState.CurrentEvents;
                    for (_i = 0; _i < loopTo; _i++)
                    {
                        if (Core.Globals.Data.MapEvents != null && _i < Core.Globals.Data.MapEvents.Length)
                        {
                            unchecked { Core.Globals.Data.MapEvents[_i].Steps++; }
                        }
                    }

                    GameState.MapAnim = !GameState.MapAnim;
                    _tmr250 = _tick + 250;
                }

                if (Audio.FadeInSwitch == true)
                {
                    Audio.FadeIn();
                }

                if (Audio.FadeOutSwitch == true)
                {
                    Audio.FadeOut();
                }
            }
            else
            {
                if (_tmr500 < _tick)
                {
                    // animate textbox
                    if (GameState.ChatShowLine == "|")
                    {
                        GameState.ChatShowLine = "";
                    }
                    else
                    {
                        GameState.ChatShowLine = "|";
                    }

                    _tmr500 = _tick + 500;
                }

                if (_tmr25 < _tick)
                {
                    Audio.PlayMusic(SettingsManager.Instance.MenuMusic);
                    _tmr25 = _tick + 25;
                }
            }

            if (_tmrWeather < _tick)
            {
                Weather.OnUpdate();
                _tmrWeather = _tick + 50;
            }

            if (_fadeTmr < _tick)
            {
                if (GameState.FadeType != 2)
                {
                    if (GameState.FadeType == 1)
                    {
                        if (GameState.FadeAmount == 255)
                        {

                        }
                        else
                        {
                            GameState.FadeAmount = GameState.FadeAmount + 5;
                        }
                    }
                    else if (GameState.FadeType == 0)
                    {
                        if (GameState.FadeAmount == 0)
                        {
                            GameState.UseFade = false;
                        }
                        else
                        {
                            GameState.FadeAmount = GameState.FadeAmount - 5;
                        }
                    }
                }
                _fadeTmr = _tick + 30;
            }

            WindowManager.ResizeGui();
        }

        private static void UpdateEditors()
        {
            // Map editor: apply debounced map resize from txtMaxX/txtMaxY on the main thread.
            if (GameState.MyEditorType == EditorType.Map && GameState.MapResizePending)
            {
                if (_tick - GameState.MapResizeLastEditTick >= 350)
                {
                    GameState.MapResizePending = false;
                    var nx = (byte)Math.Clamp(GameState.MapResizePendingX, 1, byte.MaxValue);
                    var ny = (byte)Math.Clamp(GameState.MapResizePendingY, 1, byte.MaxValue);
                    WinMapEditor.ResizeMap(nx, ny);
                }
            }

            if (GameState.InitAdminForm)
            {
                Sender.SendRequestMapReport();
                WindowManager.ShowWindow("winAdmin");
                GameState.AdminPanel = true;

                // Ensure admin panel shows the current player's name when it opens
                try
                {
                    var playerName = GetPlayerName(GameState.MyIndex);
                    var adminWindow = WindowManager.GetWindowByName("winAdmin");
                    if (adminWindow != null)
                    {
                        if (adminWindow.GetChild("txtName") is TextBox txtName)
                        {
                            txtName.Text = playerName;
                        }
                    }
                }
                catch
                {
                    // If anything goes wrong here, just leave the default caption/text.
                }

                GameState.InitAdminForm = false;
            }

            if (GameState.InitMapReport)
            {
                // Populate the Admin map list control in the skin window
                var admin = WindowManager.GetWindowIndex("winAdmin");

                if (WindowManager.TryGetControl("winAdmin", "lstMaps", out var lstCtrl) && lstCtrl is ListBox lst)
                {
                    lst.Clear();
                    for (int i = 0, loopTo = GameState.MapNames.Length; i < loopTo; i++)
                    {
                        var raw = GameState.MapNames[i] ?? string.Empty;
                        var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                        lst.AddItem((i + 1) + ": " + name);
                    }
                    // Ensure view starts at top
                    lst.Value = 0;
                }

                GameState.InitMapReport = false;
            }

            if (GameState.InitEventEditor)
            {
                WindowManager.ShowWindow("winEventEditor");
                Client.Game.UI.Windows.WinEventEditor.Init();
                GameState.InitEventEditor = false;
            }

            if (GameState.InitScriptEditor)
            {
                GameState.MyEditorType = EditorType.Script;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winScriptEditor");
                GameState.InitScriptEditor = false;
            }
        }
    }
}
