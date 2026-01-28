using Core;
using System;
using System.Threading;
using Client.Game.UI;
using Client.Game.UI.Controls;
using Client.Game.UI.Windows;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;
using Core.Common;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public class Loop
    {
        private static int _watchdogStarted;
        private static int _lastHeartbeat;
        private static int _stageStarted;
        private static volatile string _stage = "init";

        // Perf/debug logging (throttled to avoid log spam)
        private const int WatchdogStallThresholdMs = 10000;
        private const int SlowStageThresholdMs = 250;
        private const int SlowLogThrottleMs = 5000;
        private const string PerfLogFile = "perf.log";

        private static readonly object _stageHistoryLock = new();
        private const int StageHistorySize = 32;
        private static readonly StageHistoryEntry[] _stageHistory = new StageHistoryEntry[StageHistorySize];
        private static int _stageHistoryNext;
        private static readonly Dictionary<string, int> _lastSlowStageLogAt = new(StringComparer.Ordinal);
        private static int _lastSlowFrameLogAt;
        private static int _stageHistoryCount;

        // Progress markers help pinpoint where we got stuck even if stage transitions are empty.
        private static int _progressMarker;
        private static int _progressStarted;

        private struct StageHistoryEntry
        {
            public string From;
            public string To;
            public int Started;
            public int Ended;
            public int Duration;
        }

        private static uint ElapsedTime(int now, int then)
        {
            return unchecked((uint)(now - then));
        }

        private static int ElapsedTimeInt(int now, int then)
        {
            var delta = ElapsedTime(now, then);
            return delta > int.MaxValue ? int.MaxValue : (int)delta;
        }

        private static void RecordStageTransition(string from, string to, int started, int ended)
        {
            var duration = ElapsedTimeInt(ended, started);

            lock (_stageHistoryLock)
            {
                _stageHistory[_stageHistoryNext] = new StageHistoryEntry
                {
                    From = from,
                    To = to,
                    Started = started,
                    Ended = ended,
                    Duration = duration,
                };
                _stageHistoryNext = (_stageHistoryNext + 1) % StageHistorySize;
            }

            Interlocked.Increment(ref _stageHistoryCount);

            if (duration >= SlowStageThresholdMs)
            {
                if (!_lastSlowStageLogAt.TryGetValue(from, out var lastAt) || ElapsedTime(ended, lastAt) >= (uint)SlowLogThrottleMs)
                {
                    _lastSlowStageLogAt[from] = ended;
                    Log.Add($"Slow stage: {from} took {duration}ms. NextStage={to} Tick={ended}.", PerfLogFile);
                }
            }
        }

        private static string GetRecentStageHistoryString()
        {
            var sb = new StringBuilder(256);
            lock (_stageHistoryLock)
            {
                for (var i = 0; i < StageHistorySize; i++)
                {
                    var idx = _stageHistoryNext - 1 - i;
                    if (idx < 0) idx += StageHistorySize;
                    var e = _stageHistory[idx];
                    if (string.IsNullOrEmpty(e.From) || string.IsNullOrEmpty(e.To))
                    {
                        continue;
                    }

                    if (sb.Length > 0) sb.Append(" | ");
                    sb.Append(e.From).Append("->").Append(e.To).Append(' ').Append(e.Duration).Append("ms");
                }
            }
            return sb.ToString();
        }

        private static void MaybeLogSlowFrame(int frameMs, int now)
        {
            if (frameMs < SlowStageThresholdMs * 4)
            {
                return;
            }

            if (ElapsedTime(now, _lastSlowFrameLogAt) < (uint)SlowLogThrottleMs)
            {
                return;
            }

            _lastSlowFrameLogAt = now;
            Log.Add($"Slow frame: {frameMs}ms. Stage={_stage} RecentTransitions=[{GetRecentStageHistoryString()}]", PerfLogFile);
        }

        private static void SetProgress(int marker, int now)
        {
            if (Volatile.Read(ref _progressMarker) != marker)
            {
                Volatile.Write(ref _progressMarker, marker);
                Volatile.Write(ref _progressStarted, now);
            }
        }

        // Wrap-safe time check for TickCount-style integers.
        // Returns true when now is equal or after the scheduled time, even if TickCount has overflowed.
        private static bool TimePassed(int now, int scheduled)
        {
            // Many timers are initialized to 0 to mean "run immediately".
            // Without this, TickCount wrapping negative would prevent the timer from ever firing until TickCount becomes positive again.
            if (scheduled == 0)
            {
                return true;
            }

            return unchecked(now - scheduled) >= 0;
        }

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

        private static void TryPlayCurrentMapMusic()
        {
            if (!GameState.InGame)
                return;

            if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
                return;

            var mapId = GetMap(GameState.MyIndex);
            if (mapId < 0 || mapId >= Client.Map.Instance.Count)
                return;

            Audio.PlayMusic(Client.Map.Instance[mapId].Music);
        }

        private static void StartWatchdog()
        {
            if (Interlocked.Exchange(ref _watchdogStarted, 1) == 1)
            {
                return;
            }

            var thread = new Thread(() =>
            {
                // If the client update blocks (deadlock, infinite loop, runaway content scan), this thread can still log.
                while (true)
                {
                    Thread.Sleep(5000);

                    var now = General.GetTickCount();
                    var hb = Volatile.Read(ref _lastHeartbeat);
                    if (hb == 0)
                    {
                        continue;
                    }

                    var stalled = ElapsedTimeInt(now, hb);
                    if (stalled < WatchdogStallThresholdMs)
                    {
                        continue;
                    }

                    var stage = _stage;
                    var stageFor = ElapsedTimeInt(now, Volatile.Read(ref _stageStarted));

                    var recent = GetRecentStageHistoryString();
                    var progress = Volatile.Read(ref _progressMarker);
                    var progressFor = ElapsedTimeInt(now, Volatile.Read(ref _progressStarted));
                    var historyCount = Volatile.Read(ref _stageHistoryCount);

                    var msg = $"Client loop heartbeat stalled for {stalled}ms. Stage={stage} (for {stageFor}ms). Progress={progress} (for {progressFor}ms). StageHistoryCount={historyCount}. RecentTransitions=[{recent}]";
                    Log.Add(msg, "errors.log");
                    Console.WriteLine(msg);
                }
            })
            {
                IsBackground = true,
                Name = "ClientLoopWatchdog"
            };

            thread.Start();
        }

        public static void WatchdogPulse(string stage, int progressMarker)
        {
            var nowMs = General.GetTickCount();
            StartWatchdog();
            Volatile.Write(ref _lastHeartbeat, nowMs);
            SetProgress(progressMarker, nowMs);
            SetStage(stage, nowMs);
        }

        private static void SetStage(string stage, int now)
        {
            var prevStage = _stage;
            var prevStarted = Volatile.Read(ref _stageStarted);

            // Always refresh the stage timestamp so watchdog reports time spent in the *current frame* stage,
            // not how long ago we first entered the stage name.
            _stage = stage;
            Volatile.Write(ref _stageStarted, now);

            if (!string.Equals(prevStage, stage, StringComparison.Ordinal))
            {
                RecordStageTransition(prevStage, stage, prevStarted, now);
            }
        }

        public static void OnUpdate()
        {
            StartWatchdog();
            _tick = General.GetTickCount();
            var frameStart = _tick;
            SetProgress(1, _tick); // entered OnUpdate
            Volatile.Write(ref _lastHeartbeat, _tick);
            SetStage("Update", _tick);
            GameState.ElapsedTime = _tick - _frameTime; // Set the time difference for time-based movement

            _frameTime = _tick;

            try
            {
                if (GameLogic.GameStarted())
                {
                    SetProgress(2, _tick); // in-game branch
                    var mapId = GetMap(GameState.MyIndex);

                    if (TimePassed(_tick, _tmr1000))
                    {
                        SetProgress(10, _tick);
                        SetStage("Ping", _tick);
                        Sender.GetPing();
                        _tmr1000 = _tick + 1000;
                    }

                    if (TimePassed(_tick, _tmr25))
                    {
                        SetProgress(20, _tick);
                        SetStage("Editors", _tick);
                        TryPlayCurrentMapMusic();
                        UpdateEditors();
                        _tmr25 = _tick + 25;
                    }

                    if (TimePassed(_tick, GameState.ShowAnimTimer))
                    {
                        GameState.ShowAnimLayers = !GameState.ShowAnimLayers;
                        GameState.ShowAnimTimer = _tick + 500;
                    }

                    // Tile animations are expensive: scan the map at most when a layer timer expires.
                    // Also avoid per-tile GetMap/IsValidMapPoint calls by caching the map and using bounds.
                    if (TimePassed(_tick, _animationTmr[0]) || TimePassed(_tick, _animationTmr[1]))
                    {
                        SetProgress(30, _tick);
                        SetStage("TileAnimations", _tick);

                        if (mapId >= 0 && mapId < Client.Map.Instance.Count)
                        {
                            var map = Client.Map.Instance[mapId];
                            int mapMaxX = map.MaxX;
                            int mapMaxY = map.MaxY;

                            var min0 = int.MaxValue;
                            var min1 = int.MaxValue;

                            for (int x = 0; x < mapMaxX; x++)
                            {
                                for (int y = 0; y < mapMaxY; y++)
                                {
                                    var tile = map.Tile[x, y];

                                    if (tile.Type == TileType.Animation)
                                    {
                                        var animId = tile.Data1;
                                        if (animId >= 0 && animId < Animation.Instance.Count)
                                        {
                                            MapAnimation.OnCreate(animId, (byte)x, (byte)y);
                                            var d0 = MapAnimation.GetDuration(animId, 0);
                                            var d1 = MapAnimation.GetDuration(animId, 1);
                                            if (d0 > 0 && d0 < min0) min0 = d0;
                                            if (d1 > 0 && d1 < min1) min1 = d1;
                                        }
                                    }

                                    if (tile.Type2 == TileType.Animation)
                                    {
                                        var animId = tile.Data1_2;
                                        if (animId >= 0 && animId < Animation.Instance.Count)
                                        {
                                            MapAnimation.OnCreate(animId, (byte)x, (byte)y);
                                            var d0 = MapAnimation.GetDuration(animId, 0);
                                            var d1 = MapAnimation.GetDuration(animId, 1);
                                            if (d0 > 0 && d0 < min0) min0 = d0;
                                            if (d1 > 0 && d1 < min1) min1 = d1;
                                        }
                                    }
                                }
                            }

                            // If no animations were found, back off so we don't rescan every frame.
                            _animationTmr[0] = _tick + (min0 == int.MaxValue ? 500 : min0);
                            _animationTmr[1] = _tick + (min1 == int.MaxValue ? 500 : min1);
                        }
                    }

                    SetProgress(40, _tick); // entering map animation updates

                    for (_i = 0; _i < byte.MaxValue; _i++)
                    {
                        // If we stall inside a specific animation update, watchdog will report Progress=4000+index.
                        SetProgress(4000 + _i, _tick);
                        MapAnimation.OnUpdate(_i);
                    }

                    SetProgress(4099, _tick); // finished map animation updates

                    SetProgress(4100, _tick); // post-map-animations: event chat timer check

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

                    SetProgress(4110, _tick); // post-map-animations: screenshake

                    // screenshake
                    if (GameState.ShakeTimerEnabled)
                    {
                        if (TimePassed(_tick, GameState.ShakeTimer))
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

                    SetProgress(4120, _tick); // post-map-animations: skill cooldown icon expiry

                    // check if we need to end the CD icon
                    if (GameState.NumSkills > 0)
                    {
                        for (_i = 0; _i < Core.Globals.Variables.MaxPlayerSkills; _i++)
                        {
                            SetProgress(4200 + _i, _tick);
                            if (Player.Instance.Count <= GameState.MyIndex) break;
                            if (Skill.Instance.Count <= Player.Instance[GameState.MyIndex].Skill[_i].Num) break;
                            if (Player.Instance[GameState.MyIndex].Skill[_i].Num >= 0)
                            {
                                if (Player.Instance[GameState.MyIndex].Skill[_i].Cd > 0)
                                {
                                    if (TimePassed(_tick, Player.Instance[GameState.MyIndex].Skill[_i].Cd + Skill.Instance[Player.Instance[GameState.MyIndex].Skill[_i].Num].CdTime * 1000))
                                    {
                                        Player.Instance[GameState.MyIndex].Skill[_i].Cd = 0;
                                    }
                                }
                            }
                        }
                    }

                    SetProgress(4300, _tick); // post-map-animations: skill buffer unlock

                    // check if we need to unlock the player's skill casting restriction
                    if (GameState.SkillBuffer >= 0)
                    {
                        if (Skill.Instance.Count > Player.Instance[GameState.MyIndex].Skill[GameState.SkillBuffer].Num)
                        {
                            if (TimePassed(_tick, GameState.SkillBufferTimer + Skill.Instance[Player.Instance[GameState.MyIndex].Skill[GameState.SkillBuffer].Num].CastTime * 1000))
                            {
                                GameState.SkillBuffer = -1;
                                GameState.SkillBufferTimer = 0;
                            }
                        }
                    }

                    SetProgress(4400, _tick); // post-map-animations: before movement/input timer gate
                    
                    // Process input before rendering, otherwise input will be behind by 1 frame
                    if (TimePassed(_tick, _walkTimer))
                    {
                        SetProgress(50, _tick);
                        SetStage("Movement", _tick);

                        // If we hang inside movement, advance progress markers so watchdog can pinpoint exactly where.
                        // 50 = entered movement, 51+ = sub-steps, 1000+/2000+/3000+ = per-entity indices.
                        SetProgress(51, _tick);
                        if (GameState.CanMoveNow)
                        {
                            SetProgress(52, _tick);
                            Player.OnMove(); // Check if player is trying to move
                            SetProgress(53, _tick);
                            Player.OnAttack(); // Keyboard attack
                            // Mouse attack support:
                            // 1. On fresh press, face cursor & attempt attack.
                            // 2. While held, keep facing cursor and attempt attack when cooldown ready.
                            var leftPressedNow = GameClient.CurrentMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                            var leftPressedPrev = GameClient.PreviousMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                            if (leftPressedNow && !leftPressedPrev && !WindowManager.IsWindowActive)
                            {
                                SetProgress(54, _tick);
                                Player.UpdateFacingFromMouse(GameClient.CurrentMouseState.X, GameClient.CurrentMouseState.Y);
                                Player.OnAttack(mouse: true);
                            }
                            else if (leftPressedNow && !WindowManager.IsWindowActive)
                            {
                                SetProgress(55, _tick);
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
                                // Attempt attack each tick; internal cooldown logic in attack prevents spam.
                                Player.OnAttack(mouse: true);
                            }
                        }

                        // Process player movements
                        SetProgress(56, _tick);
                        var playerCount = Math.Min(Player.Instance.Count, Core.Globals.Variables.MaxPlayers);
                        for (_i = 0; _i < playerCount; _i++)
                        {
                            SetProgress(1000 + _i, _tick);
                            if (IsPlaying(_i))
                            {
                                Player.OnMove(_i);                            
                            }
                        }

                        // Process npc movements
                        SetProgress(57, _tick);
                        for (_i = 0; _i < Core.Globals.Variables.MaxMapNpcs; _i++)
                        {
                            SetProgress(2000 + _i, _tick);
                            Npc.OnMove(_i);
                            
                        }

                        SetProgress(58, _tick);
                        var count = Data.MapEvents == null ? 0 : Math.Min(GameState.CurrentEvents, Data.MapEvents.Length);
                        if (mapId >= 0 && mapId < Client.Map.Instance.Count)
                        {
                            count = Math.Min(count, Client.Map.Instance[mapId].EventCount);
                        }
                        for (_i = 0; _i < count; _i++)
                        {
                            SetProgress(3000 + _i, _tick);
                            Event.OnMove(_i);
                        }

                        SetProgress(59, _tick);
                        _walkTimer = _tick + 5;
                    }

                    // chat timer
                    if (TimePassed(_tick, _chatTmr))
                    {
                        SetProgress(60, _tick);
                        SetStage("Chat", _tick);
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
                    if (TimePassed(_tick, _fogTmr))
                    {
                        SetProgress(70, _tick);
                        SetStage("Fog", _tick);
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

                    if (TimePassed(_tick, _tmr500))
                    {
                        SetProgress(80, _tick);
                        SetStage("Anim500ms", _tick);
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
                    if (TimePassed(_tick, _barTmr))
                    {
                        SetProgress(90, _tick);
                        SetStage("Bars", _tick);
                        GameLogic.SetBarWidth(ref GameState.BarWidthGuiHPMax, ref GameState.BarWidthGuiHP);
                        GameLogic.SetBarWidth(ref GameState.BarWidthGuiMPMax, ref GameState.BarWidthGuiMP);
                        GameLogic.SetBarWidth(ref GameState.BarWidthGuiExpMax, ref GameState.BarWidthGuiExp);
                        for (_i = 0; _i < Core.Globals.Variables.MaxMapNpcs; _i++)
                        {
                            if (MapNpc.Instance[_i].Num >= 0)
                            {
                                GameLogic.SetBarWidth(ref GameState.BarWidthNpcHPMax[_i], ref GameState.BarWidthNpcHP[_i]);
                            }
                        }

                        for (_i = 0; _i < Player.Instance.Count; _i++)
                        {
                            if (IsPlaying(_i) & GetMap(_i) == GetMap(GameState.MyIndex))
                            {
                                GameLogic.SetBarWidth(ref GameState.BarWidthPlayerHPMax[_i], ref GameState.BarWidthPlayerHP[_i]);
                                GameLogic.SetBarWidth(ref GameState.BarWidthPlayerMPMax[_i], ref GameState.BarWidthPlayerMP[_i]);
                            }
                        }

                        // reset timer
                        _barTmr = _tick + 10;
                    }

                    // Change map animation
                    if (TimePassed(_tick, _tmr250))
                    {
                        SetProgress(100, _tick);
                        SetStage("Steps250ms", _tick);
                        for (int i = 0; i < Player.Instance.Count; i++)
                        {
                            if (!IsPlaying(i)) continue;
                            if (GetMap(i) != GetMap(GameState.MyIndex)) continue;
                            // Always advance Steps (used modulo by idle/run frame counts in draw)
                            unchecked { Player.Instance[i].Steps++; } // byte wraps automatically
                        }

                        for (int i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
                        {
                            if (MapNpc.Instance[i].Num >= 0)
                            {
                                unchecked { MapNpc.Instance[i].Steps++; }
                            }
                        }

                        var mapEvents = Core.Globals.Data.MapEvents;
                        if (mapEvents != null)
                        {
                            var count = Math.Min(GameState.CurrentEvents, mapEvents.Length);
                            for (_i = 0; _i < count; _i++)
                            {
                                unchecked { mapEvents[_i].Steps++; }
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
                    SetProgress(3, _tick); // menu branch
                    if (TimePassed(_tick, _tmr500))
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

                    if (TimePassed(_tick, _tmr25))
                    {
                        Audio.PlayMusic(SettingsManager.Instance.MenuMusic);
                        _tmr25 = _tick + 25;
                    }
                }

                if (TimePassed(_tick, _tmrWeather))
                {
                    SetProgress(110, _tick);
                    SetStage("Weather", _tick);
                    Weather.OnUpdate();
                    _tmrWeather = _tick + 50;
                }

                if (TimePassed(_tick, _fadeTmr))
                {
                    SetProgress(120, _tick);
                    SetStage("Fade", _tick);
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
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message, "errors.log");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                var end = General.GetTickCount();
                var frameMs = ElapsedTimeInt(end, frameStart);
                MaybeLogSlowFrame(frameMs, end);
            }
        }

        private static void UpdateEditors()
        {
            // Map editor: map resize is queued and applied on save.

            if (GameState.InitAdminForm)
            {
                Sender.RequestMapReport();
                WindowManager.ShowWindow("winAdmin");
                GameState.AdminPanel = true;

                // Ensure admin panel shows the current player's name when it opens
                try
                {
                    var playerName = GetName(GameState.MyIndex);
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
                var admin = WindowManager.GetWindow("winAdmin");

                if (WindowManager.TryGetControl("winAdmin", "lstMaps", out var lstCtrl) && lstCtrl is ListBox lst)
                {
                    lst.Clear();
                    for (int i = 0, count = GameState.MapNames.Length; i < count; i++)
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
