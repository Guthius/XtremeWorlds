using Core;
using System;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using static Core.Globals.Command;
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
                    Sound.PlayMusic(Data.MyMap.Music);
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
                        byte mapMaxX = Data.MyMap.MaxX;
                        for (byte x = 0; x < mapMaxX; x++)
                        {
                            byte mapMaxY = Data.MyMap.MaxY;
                            for (byte y = 0; y < mapMaxY; y++)
                            {
                                if (GameLogic.IsValidMapPoint(x, y))
                                {
                                    if (Data.MyMap.Tile[x, y].Type == TileType.Animation)
                                    {                                      
                                        _animationTmr[layer] = _tick + Animation.PlayAnimation(Data.Animation[Data.MyMap.Tile[x, y].Data1].Sprite[layer], layer, Data.MyMap.Tile[x, y].Data1, x, y);
                                    }

                                    if (Data.MyMap.Tile[x, y].Type2 == TileType.Animation)
                                    {
                                        _animationTmr[layer] = _tick + Animation.PlayAnimation(Data.Animation[Data.MyMap.Tile[x, y].Data1_2].Sprite[layer], layer, Data.MyMap.Tile[x, y].Data1_2, x, y);
                                    }
                                }
                            }
                        }
                        ;
                    }
                }

                for (_i = 0; _i < byte.MaxValue; _i++)
                {
                    Animation.CheckAnimInstance(_i);
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
                        if (Data.Player[GameState.MyIndex].Skill[_i].Num >= 0)
                        {
                            if (Data.Player[GameState.MyIndex].Skill[_i].Cd > 0)
                            {
                                if (Data.Player[GameState.MyIndex].Skill[_i].Cd + Data.Skill[(int)Data.Player[GameState.MyIndex].Skill[_i].Num].CdTime * 1000 < _tick)
                                {
                                    Data.Player[GameState.MyIndex].Skill[_i].Cd = 0;
                                }
                            }
                        }
                    }
                }

                // check if we need to unlock the player's skill casting restriction
                if (GameState.SkillBuffer >= 0)
                {
                    if (GameState.SkillBufferTimer + Data.Skill[(int)Data.Player[GameState.MyIndex].Skill[GameState.SkillBuffer].Num].CastTime * 1000 < _tick)
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
                        Player.CheckMovement(); // Check if player is trying to move
                        Player.CheckAttack();   // Keyboard attack
                        // Mouse attack support:
                        // 1. On fresh press, face cursor & attempt attack.
                        // 2. While held, keep facing cursor and attempt attack when cooldown ready.
                        var leftPressedNow = GameClient.CurrentMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                        var leftPressedPrev = GameClient.PreviousMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                        if (leftPressedNow && !leftPressedPrev && !WindowManager.IsWindowActive)
                        {
                            Player.UpdateFacingFromMouse(GameClient.CurrentMouseState.X, GameClient.CurrentMouseState.Y);
                            Player.CheckAttack(mouse: true);
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
                            // Attempt attack each tick; internal cooldown logic in CheckAttack prevents spam.
                            Player.CheckAttack(mouse: true);
                        }
                    }
                    // Process player movements
                    for (_i = 0; _i < Variables.MaxPlayers; _i++)
                    {
                        if (IsPlaying(_i))
                        {
                            Player.ProcessMovement(_i);                            
                        }
                    }

                    // Process npc movements
                    for (_i = 0; _i < Variables.MaxMapNpcs; _i++)
                    {
                        Npc.ProcessMovement(_i);
                        
                    }

                    var loopTo2 = GameState.CurrentEvents;
                    for (_i = 0; _i < loopTo2; _i++)
                    {
                        Event.ProcessMovement(_i);
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
                        if (Data.MyMapNpc[_i].Num >= 0)
                        {
                            GameLogic.SetBarWidth(ref GameState.BarWidthPlayerHPMax[_i], ref GameState.BarWidthPlayerHP[_i]);
                        }
                    }

                    for (_i = 0; _i < Variables.MaxPlayers; _i++)
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
                    for (int i = 0; i < Variables.MaxPlayers; i++)
                    {
                        if (!IsPlaying(i)) continue;
                        if (GetPlayerMap(i) != GetPlayerMap(GameState.MyIndex)) continue;
                        // Always advance Steps (used modulo by idle/run frame counts in draw)
                        unchecked { Data.Player[i].Steps++; } // byte wraps automatically
                    }

                    for (int i = 0; i < Variables.MaxMapNpcs; i++)
                    {
                        if (Data.MyMapNpc[i].Num >= 0)
                        {
                            unchecked { Data.MyMapNpc[i].Steps++; }
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

                if (Sound.FadeInSwitch == true)
                {
                    Sound.FadeIn();
                }

                if (Sound.FadeOutSwitch == true)
                {
                    Sound.FadeOut();
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
                    Sound.PlayMusic(SettingsManager.Instance.MenuMusic);
                    _tmr25 = _tick + 25;
                }
            }

            if (_tmrWeather < _tick)
            {
                Weather.ProcessWeather();
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
                        if (adminWindow.GetChild("txtAdminName") is TextBox txtName)
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

                WindowManager.ComboBox_RemoveItems(admin, WindowManager.GetControlIndex("winAdmin", "cmbMaps"));
                for (int i = 0, loopTo = GameState.MapNames.Length; i < loopTo; i++)
                {
                    var raw = GameState.MapNames[i] ?? string.Empty;
                    var name = string.IsNullOrWhiteSpace(raw) ? "None" : raw.Trim();
                    WindowManager.Combobox_AddItem(
                        admin,
                        WindowManager.GetControlIndex("winAdmin", "cmbMaps"),
                        (i + 1) + ": " + name
                    );
                }

                GameState.InitMapReport = false;
            }

            if (GameState.InitMapEditor)
            {
                GameState.MyEditorType = EditorType.Map;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winMapEditor");
                GameState.CameraZoom = 1.0f;
                GameState.InitMapEditor = false;
            }

            if (GameState.InitEventEditor)
            {
                new EditorEvent().Show();
                GameState.InitEventEditor = false;
            }

            if (GameState.InitAnimationEditor)
            {
                GameState.MyEditorType = EditorType.Animation;
                GameState.EditorIndex = 0;
                new EditorAnimation().Show();
                GameState.InitAnimationEditor = false;
            }

            if (GameState.InitItemEditor)
            {
                GameState.MyEditorType = EditorType.Item;
                GameState.EditorIndex = 0;
                new EditorItem().Show();
                GameState.InitItemEditor = false;
            }

            if (GameState.InitJobEditor)
            {
                GameState.MyEditorType = EditorType.Job;
                GameState.EditorIndex = 0;
                new EditorJob().Show();
                GameState.InitJobEditor = false;
            }

            if (GameState.InitMoralEditor)
            {
                GameState.MyEditorType = EditorType.Moral;
                GameState.EditorIndex = 0;
                new EditorMoral().Show();
                GameState.InitMoralEditor = false;
            }

            if (GameState.InitResourceEditor)
            {
                GameState.MyEditorType = EditorType.Resource;
                GameState.EditorIndex = 0;
                new EditorResource().Show();
                GameState.InitResourceEditor = false;
            }

            if (GameState.InitNpcEditor)
            {
                GameState.MyEditorType = EditorType.Npc;
                GameState.EditorIndex = 0;
                WindowManager.ShowWindow("winNpcEditor");
                GameState.InitNpcEditor = false;
            }

            if (GameState.InitSkillEditor)
            {
                GameState.MyEditorType = EditorType.Skill;
                GameState.EditorIndex = 0;
                new EditorSkill().Show();
                GameState.InitSkillEditor = false;
            }

            if (GameState.InitShopEditor)
            {
                GameState.MyEditorType = EditorType.Shop;
                GameState.EditorIndex = 0;
                new EditorShop().Show();
                GameState.InitShopEditor = false;
            }

            if (GameState.InitProjectileEditor)
            {
                GameState.MyEditorType = EditorType.Projectile;
                GameState.EditorIndex = 0;
                new EditorProjectile().Show();
                GameState.InitProjectileEditor = false;
            }

            if (GameState.InitScriptEditor)
            {
                GameState.MyEditorType = EditorType.Script;
                GameState.EditorIndex = 0;
                new EditorScript().Show();
                GameState.InitScriptEditor = false;
            }
        }
    }
}