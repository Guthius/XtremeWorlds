using Core;
using System;
using System.Data.Common;
using Client.Game.UI;
using Client.Game.UI.Windows;
using System.Linq;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using System.Threading;
using Core.Net;
using static Core.Globals.Commands;
using static Core.Globals.Type;
using Type = Core.Globals.Type;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Client
{
    public class GameLogic
    {
        public static bool IsInBounds()
        {
            bool isInBounds = false;

            if (Map.Instance.Count <= GetMap(GameState.MyIndex))
                return isInBounds;
                
            if (GameState.CurX >= 0 & GameState.CurX <= Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX)
            {
                if (GameState.CurY >= 0 & GameState.CurY <= Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY)
                {
                    isInBounds = true;
                }
            }

            return isInBounds;

        }

        public static bool GameStarted()
        {
            bool gameStarted = false;

            if (GameState.InGame == false || GameState.MapData == false || GameState.PlayerData == false)
                return gameStarted;

            gameStarted = true;
            return gameStarted;
        }

        public static void CreateActionMessage(string message, int color, byte msgType, int x, int y)
        {

            GameState.ActionMessageIndex = (byte)(GameState.ActionMessageIndex + 1);
            if (GameState.ActionMessageIndex >= byte.MaxValue)
                GameState.ActionMessageIndex = 1;
            
            ref var instance = ref Data.ActionMessage[GameState.ActionMessageIndex];
            instance.Message = message;
            instance.Color = color;
            instance.Type = msgType;
            instance.Created = General.GetTickCount();
            instance.Scroll = 0;
            instance.X = x;
            instance.Y = y;        

            if (Data.ActionMessage[GameState.ActionMessageIndex].Type == (int)ActionMessageType.Scroll)
            {
                Data.ActionMessage[GameState.ActionMessageIndex].Y = Data.ActionMessage[GameState.ActionMessageIndex].Y + Rand(-2, 6);
                Data.ActionMessage[GameState.ActionMessageIndex].X = Data.ActionMessage[GameState.ActionMessageIndex].X + Rand(-8, 8);
            }

        }

        public static int Rand(int maxNumber, int minNumber = 0)
        {
            if (minNumber > maxNumber)
            {
                int t = minNumber;
                minNumber = maxNumber;
                maxNumber = t;
            }

            return (int)Math.Round(General.Random.NextDouble(minNumber, maxNumber));
        }

        // BitWise Operators for directional blocking
        public static void SetDirBlock(ref byte blockvar, ref byte dir, bool block)
        {
            if (block)
            {
                blockvar = (byte)(blockvar | (long)Math.Round(Math.Pow(2d, dir)));
            }
            else
            {
                blockvar = (byte)(blockvar & ~(byte)Math.Pow(2d, dir));
            }
        }

        public static bool IsDirBlocked(ref byte blockvar, ref byte dir)
        {
            return (blockvar & (byte)Math.Round(Math.Pow(2d, dir))) != 0;
        }

        public static string ConvertCurrency(int amount)
        {
            string convertCurrency = string.Empty;

            if (Conversion.Int(amount) < 10000)
            {
                convertCurrency = amount.ToString();
            }
            else if (Conversion.Int(amount) < 999999)
            {
                convertCurrency = Conversion.Int(amount / 1000d) + "k";
            }
            else if (Conversion.Int(amount) < 999999999)
            {
                convertCurrency = Conversion.Int(amount / 1000000d) + "m";
            }
            else
            {
                convertCurrency = Conversion.Int(amount / 1000000000d) + "b";
            }

            return convertCurrency;

        }

        public static void HandlePressEnter()
        {
            string chatText = string.Empty;
            string name;
            int i;
            int n = 0;
            string[] command;

            if (GameState.InGame)
            {
                if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl))
                {
                    chatText = chatCtrl!.Text;
                }
            }

            if (chatText != null)
                chatText = chatText.Replace("\0", string.Empty);

            // hide/show chat window
            if (string.IsNullOrEmpty(chatText))
            {
                if (WindowManager.TryGetWindow("winChat", out var winChat) && winChat!.Visible)
                {
                    if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl2))
                    {
                        chatCtrl2!.Text = "";
                    }
                    WinChat.Hide();
                    return;
                }
            }

            // Admin message
            if (Strings.Left(chatText, 1) == "@")
            {
                chatText = Strings.Mid(chatText, 2, Strings.Len(chatText) - 1);

                if (Strings.Len(chatText) > 0)
                {
                    Sender.AdminMessage(chatText);
                }

                if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl3)) chatCtrl3!.Text = "";
                return;
            }

            // Broadcast message
            if (Strings.Left(chatText, 1) == "'")
            {
                chatText = Strings.Mid(chatText, 2, Strings.Len(chatText) - 1);

                if (Strings.Len(chatText) > 0)
                {
                    Sender.BroadcastMsg(chatText);
                }

                if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl4)) chatCtrl4!.Text = "";
                return;
            }

            // party message
            if (Strings.Left(chatText, 1) == "-")
            {
                chatText = Strings.Mid(chatText, 2, Strings.Len(chatText) - 1);

                if (Strings.Len(chatText) > 0)
                {
                    Sender.PartyChatMsg(chatText);
                }

                if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl5)) chatCtrl5!.Text = "";
                return;
            }

            // Player message
            if (Strings.Left(chatText, 1) == "!")
            {
                chatText = Strings.Mid(chatText, 2, Strings.Len(chatText) - 1);
                name = "";

                // Get the desired player from the user text
                var count = Strings.Len(chatText);
                for (i = 0; i < count; i++)
                {

                    if ((Strings.Mid(chatText, i, 1) ?? "") != (Strings.Space(1) ?? ""))
                    {
                        name = name + Strings.Mid(chatText, i, 1);
                    }
                    else
                    {
                        break;
                    }

                }

                chatText = Strings.Mid(chatText, i, Strings.Len(chatText) - 1);

                // Make sure they are actually sending something
                if (!string.IsNullOrEmpty(chatText) && Strings.Len(chatText) > 0)
                {
                    // Send the message to the player
                    Sender.PlayerMsg(chatText, name);
                }
                else
                {
                    TextRenderer.AddText(LocalesManager.Get("PlayerMsg"), (int)ColorName.Yellow);
                }

                goto Continue1;
            }

            if (Strings.Left(chatText, 1) == "/")
            {
                command = Strings.Split(chatText, Strings.Space(1));

                switch (command[0] ?? "")
                {
                    case "/emote":
                        {
                            // Checks to make sure we have more than one string in the array
                            if (command.Length < 2 || !int.TryParse(command[1], out var emote))
                            {
                                TextRenderer.AddText(LocalesManager.Get("Emote"), (int)ColorName.Yellow);
                                goto Continue1;
                            }

                            Sender.UseEmote(emote);
                            break;
                        }

                    case "/help":
                        {
                            TextRenderer.AddText(LocalesManager.Get("Help1"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("Help2"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("Help3"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("Help4"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("Help5"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("Help6"), (int)ColorName.Yellow);
                            break;
                        }

                    // Whos Online
                    case "/who":
                        {
                            Sender.WhosOnline();
                            break;
                        }

                    // Requets level up
                    case "/levelup":
                        {
                            Sender.RequestLevelUp();
                            break;
                        }

                    // Checking fps
                    case "/fps":
                        {
                            GameState.Bfps = !GameState.Bfps;
                            break;
                        }

                    case "/lps":
                        {
                            GameState.Blps = !GameState.Blps;
                            break;
                        }

                    case "/party":
                        {
                            if (GameState.MyTarget >= 0)
                            {
                                if (GameState.MyTargetType == (int)TargetType.Player)
                                {
                                    Sender.PartyRequest(GetPlayerName(GameState.MyTarget));
                                    goto Continue1;
                                }
                            }

                            // Make sure they are actually sending something
                            if (Information.UBound(command) < 1 || Information.IsNumeric(command[1]))
                            {
                                TextRenderer.AddText(LocalesManager.Get("Party"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.PartyRequest(command[1]);
                            break;
                        }

                    // Join party
                    case "/join":
                        {
                            Sender.AcceptParty();
                            break;
                        }

                    // Leave party
                    case "/leave":
                        {
                            Sender.LeaveParty();
                            break;
                        }

                    // Trade
                    case "/trade":
                        {
                            if (GameState.MyTarget >= 0)
                            {
                                if (GameState.MyTargetType == (int)TargetType.Player)
                                {
                                    Sender.TradeRequest(GetPlayerName(GameState.MyTarget));
                                    goto Continue1;
                                }
                            }

                            // Make sure they are actually sending something
                            if (Information.UBound(command) < 1 || Information.IsNumeric(command[1]))
                            {
                                TextRenderer.AddText(LocalesManager.Get("Trade"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.TradeRequest(command[1]);
                            break;
                        }

                    // // Moderator Admin Commands //
                    // Admin Help
                    case "/admin":
                        {
                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Moderator)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            TextRenderer.AddText(LocalesManager.Get("Admin1"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("Admin2"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("AdminGblMsg"), (int)ColorName.Yellow);
                            TextRenderer.AddText(LocalesManager.Get("AdminPvtMsg"), (int)ColorName.Yellow);
                            break;
                        }

                    case "/acp":
                        {
                            Sender.RequestAdmin();
                            break;
                        }

                    // Kicking a player
                    case "/kick":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Moderator)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessAlert"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (Information.UBound(command) < 1 || Information.IsNumeric(command[1]))
                            {
                                TextRenderer.AddText(LocalesManager.Get("Kick"), (int)ColorName.Yellow);
                                goto Continue1;
                            }

                            Sender.Kick(command[1]);
                            break;
                        }

                    // // Mapper Admin Commands //
                    // Location
                    case "/loc":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            GameState.BLoc = !GameState.BLoc;
                            break;
                        }

                    // Warping to a player
                    case "/warpmeto":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (Information.UBound(command) < 1 || Information.IsNumeric(command[1]))
                            {
                                TextRenderer.AddText(LocalesManager.Get("WarpMeTo"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.WarpMeTo(command[1]);
                            break;
                        }

                    // Warping a player to you
                    case "/warptome":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (command.Length < 2 || int.TryParse(command[1], out _))
                            {
                                TextRenderer.AddText(LocalesManager.Get("WarpToMe"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.WarpToMe(command[1]);
                            break;
                        }

                    // Warping to a map
                    case "/warpto":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (command.Length < 2 || !int.TryParse(command[1], out n))
                            {
                                TextRenderer.AddText(LocalesManager.Get("WarpTo"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            // Check to make sure its a valid map #
                            if (n >= 0 & n < Core.Globals.Variables.MaxMaps)
                            {
                                Sender.WarpTo(n);
                            }
                            else
                            {
                                TextRenderer.AddText(LocalesManager.Get("InvalidMap"), (int)ColorName.BrightRed);
                            }

                            break;
                        }

                    // Setting sprite
                    case "/sprite":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (command.Length < 2 || !int.TryParse(command[1], out var sprite))
                            {
                                TextRenderer.AddText(LocalesManager.Get("Sprite"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.SetSprite(sprite);
                            break;
                        }

                    // Map report
                    case "/mapreport":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestMapReport();
                            break;
                        }

                    // Respawn request
                    case "/respawn":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.MapRespawn();
                            break;
                        }

                    // Early respawn while waiting on death timer
                    case "/respawnnow":
                    case "/giveup":
                        {
                            var remaining = (int)(Player.Instance[GameState.MyIndex].DeathTimer - General.GetTickCount()) / 1000;
                            if (remaining < 0) remaining = 0;

                            if (remaining <= 0)
                            {
                                TextRenderer.AddText("You are not in the respawn window.", (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RespawnNow();
                            break;
                        }

                    case "/editmap":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Mapper)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditMap();
                            break;
                        }

                    case "/editscript":
                        {
                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Owner)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                            }

                            Sender.RequestEditScript(0);
                            break;
                        }

                    // // Moderator Commands //
                    // Welcome change
                    case "/welcome":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Moderator)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (Information.UBound(command) < 1)
                            {
                                TextRenderer.AddText(LocalesManager.Get("Welcome"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.MotdChange(Strings.Right(chatText, Strings.Len(chatText) - 5));
                            break;
                        }

                    // Check the ban list
                    case "/banlist":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Moderator)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.BanList();
                            break;
                        }

                    // Banning a player
                    case "/ban":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Moderator)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if (Information.UBound(command) < 1)
                            {
                                TextRenderer.AddText(LocalesManager.Get("Ban"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.Ban(command[1]);
                            break;
                        }

                    // // Owner Admin Commands //
                    // Giving another player access
                    case "/bandestroy":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Owner)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.BanDestroy();
                            break;
                        }

                    case "/access":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Owner)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            if ((command.Length < 3 || int.TryParse(command[1], out _)) || !int.TryParse(command[2], out var access))
                            {
                                TextRenderer.AddText(LocalesManager.Get("Access"), (int)ColorName.Yellow);
                                goto Continue1;
                            }

                            Sender.SetAccess(command[1], (byte)access);
                            break;
                        }

                    // // Developer Admin Commands //
                    case "/editresource":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditResource();
                            break;
                        }

                    case "/editanimation":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditAnimation();
                            break;
                        }

                    case "/edititem":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditItem();
                            break;
                        }

                    case "/editprojectile":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditProjectiles();
                            break;
                        }

                    case "/editnpc":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditNpc();
                            break;
                        }

                    case "/editjob":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditJob();
                            break;
                        }

                    case "/editskill":
                        {

                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditSkill();
                            break;
                        }

                    case "/editshop":
                        {
                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditShop();
                            break;
                        }

                    case "/editmoral":
                        {
                            if (GetPlayerAccess(GameState.MyIndex) < (int)AccessLevel.Developer)
                            {
                                TextRenderer.AddText(LocalesManager.Get("AccessDenied"), (int)ColorName.BrightRed);
                                goto Continue1;
                            }

                            Sender.RequestEditMoral();
                            break;
                        }

                    case "":
                        {
                            break;
                        }

                    default:
                        {
                            TextRenderer.AddText(LocalesManager.Get("InvalidCmd"), (int)ColorName.BrightRed);
                            break;
                        }
                }
            }

            else if (Strings.Len(chatText) > 0) // Say message
            {
                if (!string.IsNullOrEmpty(chatText))
                {
                    Client.Net.Sender.SayMessage(chatText);
                }
            }

        Continue1:
            ;

            if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl6)) chatCtrl6!.Text = "";
        }

        public static void CheckMapGetItem()
        {
            var packetWriter = new PacketWriter(4);
            
            packetWriter.WriteEnum(Packets.ClientPackets.CMapGetItem);
            
            Network.Send(packetWriter);
        }

        public static void ClearActionMessage(byte index)
        {
            Data.ActionMessage[index].Message = "";
            Data.ActionMessage[index].Created = 0;
            Data.ActionMessage[index].Type = 0;
            Data.ActionMessage[index].Color = 0;
            Data.ActionMessage[index].Scroll = 0;
            Data.ActionMessage[index].X = 0;
            Data.ActionMessage[index].Y = 0;
        }

        public static void UpdateDrawMapName()
        {
            if (Moral.Instance.Count <= Client.Map.Instance[GetMap(GameState.MyIndex)].Moral)
                return;

            if (Client.Map.Instance[GetMap(GameState.MyIndex)].Moral >= 0)
            {
                GameState.DrawMapNameColor = GameClient.QbColorToXnaColor(Moral.Instance[Client.Map.Instance[GetMap(GameState.MyIndex)].Moral].Color);
            }
        }

        public static void AddChatBubble(int target, byte targetType, string message, int color)
        {
            int i;
            int index;

            // Set the global index
            GameState.ChatBubbleindex = GameState.ChatBubbleindex + 1;
            if (GameState.ChatBubbleindex < 1 | GameState.ChatBubbleindex > byte.MaxValue)
                GameState.ChatBubbleindex = 1;

            // Default to new bubble
            index = GameState.ChatBubbleindex;

            // Loop through and see if that player/Npc already has a chat bubble
            for (i = 0; i < byte.MaxValue; i++)
            {
                if (Data.ChatBubble[i].TargetType == targetType)
                {
                    if (Data.ChatBubble[i].Target == target)
                    {
                        // Reset master index
                        if (GameState.ChatBubbleindex > 1)
                            GameState.ChatBubbleindex = GameState.ChatBubbleindex - 1;

                        // We use this one now, yes?
                        index = i;
                        break;
                    }
                }
            }

            // Set the bubble up
            {
                ref var instance = ref Data.ChatBubble[index];
                instance.Target = target;
                instance.TargetType = targetType;
                instance.Msg = message;
                instance.Color = color;
                instance.Timer = General.GetTickCount();
                instance.Active = true;
            }

        }

        public static void DialogueAlert(byte index)
        {
            var header = default(string);
            var body = default(string); 
            var body2 = default(string);

            // find the body/header
            switch (index)
            {
                case (byte)SystemMessage.Connection:
                    {
                        header = "Invalid Connection";
                        body = "You lost connection to the game server.";
                        body2 = "Please try again later.";
                        GameState.InGame = false;
                        break;
                    }

                case (byte)SystemMessage.Banned:
                    {
                        header = "Banned";
                        body = "You have been banned, have a nice day!";
                        body2 = "Please send all ban appeals to an administrator.";
                        GameState.InGame = false;
                        break;
                    }

                case (byte)SystemMessage.Kicked:
                    {
                        header = "Kicked";
                        body = "You have been kicked.";
                        body2 = "Please try and behave.";
                        GameState.InGame = false;
                        break;
                    }

                case (byte)SystemMessage.ClientOutdated:
                    {
                        header = "Wrong Version";
                        body = "Your game client is the wrong version.";
                        body2 = "Please try updating.";
                        break;
                    }

                case (byte)SystemMessage.ServerMaintenance:
                    {
                        header = "Connection Refused";
                        body = "The server is currently going under maintenance.";
                        body2 = "Please try again soon.";
                        break;
                    }

                case (byte)SystemMessage.NameTaken:
                    {
                        header = "Invalid Name";
                        body = "This name is already in use.";
                        body2 = "Please try another name.";
                        break;
                    }

                case (byte)SystemMessage.NameLengthInvalid:
                    {
                        header = "Invalid Name";
                        body = "This name is too short or too long.";
                        body2 = "Please try another name.";
                        break;
                    }

                case (byte)SystemMessage.NameContainsIllegalCharacters:
                    {
                        header = "Invalid Name";
                        body = "This name contains illegal characters.";
                        body2 = "Please try another name.";
                        break;
                    }

                case (byte)SystemMessage.DatabaseError:
                    {
                        header = "Invalid Connection";
                        body = "Cannot connect to database.";
                        body2 = "Please try again later.";
                        break;
                    }

                case (byte)SystemMessage.WrongPassword:
                    {
                        header = "Invalid Login";
                        body = "Invalid username or password.";
                        body2 = "Please try again.";
                        WinRegister.ClearPasswords();
                        break;
                    }

                case (byte)SystemMessage.AccountActivationRequired:
                    {
                        header = "Inactive Account";
                        body = "Your account is not activated.";
                        body2 = "Please activate your account then try again.";
                        break;
                    }

                case (byte)SystemMessage.MaxCharactersReached:
                    {
                        header = "Cannot Merge";
                        body = "You cannot merge a full account.";
                        body2 = "Please clear a character slot.";
                        break;
                    }

                case (byte)SystemMessage.ConfirmCharacterDeletion:
                    {
                        header = "Deleted Character";
                        body = "Your character was successfully deleted.";
                        body2 = "Please log on to continue playing.";
                        break;
                    }

                case (byte)SystemMessage.CreateAccount:
                    {
                        header = "Account Created";
                        body = "Your account was successfully created.";
                        body2 = "Now, you can play!";
                        break;
                    }

                case (byte)SystemMessage.MultipleAccountsNotAllowed:
                    {
                        header = "Multiple Accounts";
                        body = "Multiple accounts are not authorized.";
                        body2 = "Please logout and try again!";
                        break;
                    }

                case (byte)SystemMessage.Login:
                    {
                        header = "Cannot Login";
                        body = "This account does not exist.";
                        body2 = "Please try registering the account.";
                        break;
                    }

                case (byte)SystemMessage.Crashed:
                    {
                        header = "Error";
                        body = "There was a network error.";
                        body2 = "Check logs folder for details.";

                        WindowManager.HideWindows();
                        WindowManager.ShowWindow("winLogin");
                        break;
                    }
            }
         
            Dialogue(header ?? string.Empty, body ?? string.Empty, body2 ?? string.Empty, DialogueType.Alert);

            // Ensure the dialogue window is visible
            WindowManager.ShowWindow("winDialogue", true);
        }

        public static void CloseDialogue()
        {
            WindowManager.HideWindow("winDialogue");
        }

        public static void Dialogue(string header, string body, string body2, DialogueType index, DialogueStyle style = 0, long data1 = 0L, long data2 = 0L, long data3 = 0L, long data4 = 0L, long data5 = 0L)
        {
            // Ensure the window exists before proceeding
            if (!WindowManager.TryGetWindow("winDialogue", out var dlg) || dlg is null)
            {
                // UI not ready yet; bail out safely
                return;
            }

            if (dlg.Visible)
                return;

            // Set button/field visibility per style using safe control lookups
            switch (style)
            {
                case DialogueStyle.YesNo:
                    if (WindowManager.TryGetControl("winDialogue", "btnYes", out var c1)) c1!.Visible = true;
                    if (WindowManager.TryGetControl("winDialogue", "btnNo", out var c2)) c2!.Visible = true;
                    if (WindowManager.TryGetControl("winDialogue", "btnOkay", out var c3)) c3!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "txtInput", out var c4)) c4!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "lblBody_2", out var c5)) c5!.Visible = true;
                    break;
                case DialogueStyle.Okay:
                    if (WindowManager.TryGetControl("winDialogue", "btnYes", out var c6)) c6!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "btnNo", out var c7)) c7!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "btnOkay", out var c8)) c8!.Visible = true;
                    if (WindowManager.TryGetControl("winDialogue", "txtInput", out var c9)) c9!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "lblBody_2", out var c10)) c10!.Visible = true;
                    break;
                case DialogueStyle.Input:
                    if (WindowManager.TryGetControl("winDialogue", "btnYes", out var c11)) c11!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "btnNo", out var c12)) c12!.Visible = false;
                    if (WindowManager.TryGetControl("winDialogue", "btnOkay", out var c13)) c13!.Visible = true;
                    if (WindowManager.TryGetControl("winDialogue", "txtInput", out var c14)) c14!.Visible = true;
                    if (WindowManager.TryGetControl("winDialogue", "lblBody_2", out var c15)) c15!.Visible = false;
                    break;
            }

            // Set labels safely
            if (WindowManager.TryGetControl("winDialogue", "lblHeader", out var h)) h!.Text = header;
            if (WindowManager.TryGetControl("winDialogue", "lblBody_1", out var b1)) b1!.Text = body;
            if (WindowManager.TryGetControl("winDialogue", "lblBody_2", out var b2)) b2!.Text = body2;
            if (WindowManager.TryGetControl("winDialogue", "txtInput", out var inp)) inp!.Text = string.Empty;

            // Set state
            GameState.Dialogue = index;
            GameState.DiaData1 = data1;
            GameState.DiaData2 = data2;
            GameState.DiaData3 = data3;
            GameState.DiaData4 = data4;
            GameState.DiaData5 = data5;
            GameState.DiaStyle = style;

            // Show window
            WindowManager.ShowWindow("winDialogue", true);

            // Ensure text input prompts reliably focus the textbox.
            if (style == DialogueStyle.Input)
            {
                WindowManager.SetActiveControl(dlg, "txtInput");
            }
        }

        public static void DialogueHandler(long index)
        {
            long value;
            string diaInput = string.Empty;
            int x;
            int y;

            if (WindowManager.TryGetControl("winDialogue", "txtInput", out var diaInputCtrl)) diaInput = diaInputCtrl!.Text;

            // Find out which button
            if (index == 1L) // Okay button
            {
                // Dialogue index
                switch (GameState.Dialogue)
                {
                    case DialogueType.TradeAmount:
                        {
                            value = (long)Math.Round(Conversion.Val(diaInput));
                            Sender.TradeItem((int)GameState.DiaData1, (int)value);
                            break;
                        }

                    case DialogueType.DepositItem:
                        {
                            value = (long)Math.Round(Conversion.Val(diaInput));
                            Sender.DepositItem((int)GameState.DiaData1, (int)value);
                            break;
                        }

                    case DialogueType.WithdrawItem:
                        {
                            value = (long)Math.Round(Conversion.Val(diaInput));
                            Sender.WithdrawItem((byte)(int)GameState.DiaData1, (int)value);
                            break;
                        }

                    case DialogueType.DropItem:
                        {
                            value = (long)Math.Round(Conversion.Val(diaInput));
                            Sender.DropItem((int)GameState.DiaData1, (int)value);
                            break;
                        }

                    case DialogueType.Information:
                        {
                            GameState.Info = true;
                            break;
                        }

                    case DialogueType.RenameVarSwitch:
                        {
                            // DiaData1 = mode (0 switches / 1 variables), DiaData2 = id
                            var mode = (int)GameState.DiaData1;
                            var id = (int)GameState.DiaData2;
                            WinEventEditor.ApplyVarSwitchRenameFromDialogue(diaInput ?? string.Empty, mode, id);
                            break;
                        }
                }
            }

            else if (index == 2L) // Yes button
            {
                // Dialogue index
                switch (GameState.Dialogue)
                {
                    case DialogueType.Trade:
                        {
                            Sender.HandleTradeInvite(1);
                            break;
                        }

                    case DialogueType.ForgetSkill:
                        {
                            Sender.ForgetSkill((int)GameState.DiaData1);
                            break;
                        }

                    case DialogueType.PartyInvite:
                        {
                            Sender.AcceptParty();
                            break;
                        }

                    case DialogueType.LootConfirmation:
                        {
                            CheckMapGetItem();
                            break;
                        }

                    case DialogueType.DeleteCharacter:
                        {
                            Sender.DelChar((byte)GameState.DiaData1);
                            break;
                        }

                    case DialogueType.FillLayer:
                        {
                            if (GameState.DiaData2 > 0L)
                            {
                                var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
                                for (x = 0; x < count; x++)
                                {
                                    var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                                    for (y = 0; y < count2; y++)
                                    {
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].X = (int)GameState.DiaData3;
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].Y = (int)GameState.DiaData4;
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].Tileset = (int)GameState.DiaData5;
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].AutoTile = (byte)GameState.DiaData2;
                                        Autotile.CacheRenderState(x, y, (int)GameState.DiaData1);
                                    }
                                }

                                // do a re-init so we can see our changes
                                Autotile.InitAutotiles();
                            }
                            else
                            {
                                var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
                                for (x = 0; x < count; x++)
                                {
                                    var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                                    for (y = 0; y < count2; y++)
                                    {
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].X = (int)GameState.DiaData3;
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].Y = (int)GameState.DiaData4;
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].Tileset = (int)GameState.DiaData5;
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Layer[(int)GameState.DiaData1].AutoTile = 0;
                                        Autotile.CacheRenderState(x, y, (int)GameState.DiaData1);
                                    }
                                }
                            }

                            break;
                        }

                    case DialogueType.ClearLayer:
                        {
                            var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
                            for (x = 0; x < count; x++)
                            {
                                var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                                for (y = 0; y < count2; y++)
                                {
                                    {
                                        ref var instance = ref Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y];
                                        instance.Layer[(int)GameState.DiaData1].X = 0;
                                        instance.Layer[(int)GameState.DiaData1].Y = 0;
                                        instance.Layer[(int)GameState.DiaData1].Tileset = 0;
                                        instance.Layer[(int)GameState.DiaData1].AutoTile = 0;
                                        Autotile.CacheRenderState(x, y, (int)GameState.DiaData1);
                                    }
                                }
                            }

                            break;
                        }

                    case DialogueType.ClearAttributes:
                        {
                            var count = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
                            for (x = 0; x < count; x++)
                            {
                                var count2 = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                                for (y = 0; y < count2; y++)
                                {
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type = 0;
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type2 = 0;
                                }
                            }

                            break;
                        }

                    case DialogueType.FillAttributes:
                        {                           
                            TileType resolvedType = TileType.None;
                            if (GameState.OptBlocked) resolvedType = TileType.Blocked;
                            if (GameState.OptWarp) resolvedType = TileType.Warp;
                            if (GameState.OptItem) resolvedType = TileType.Item;
                            if (GameState.OptNpcAvoid) resolvedType = TileType.NpcAvoid;
                            if (GameState.OptResource) resolvedType = TileType.Resource;
                            if (GameState.OptNpcSpawn) resolvedType = TileType.NpcSpawn;
                            if (GameState.OptShop) resolvedType = TileType.Shop;
                            if (GameState.OptBank) resolvedType = TileType.Bank;
                            if (GameState.OptHeal) resolvedType = TileType.Heal;
                            if (GameState.OptTrap) resolvedType = TileType.Trap;
                            if (GameState.OptAnimation) resolvedType = TileType.Animation;
                            if (GameState.OptNoCrossing) resolvedType = TileType.NoCrossing;

                            int attrIndexCached = Math.Clamp((int)GameState.EditorAttribute, 1, 2) - 1; // 0 => primary, 1 => secondary

                            var maxX = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
                            var maxY = (int)Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                            for (x = 0; x < maxX; x++)
                            {
                                for (y = 0; y < maxY; y++)
                                {
                                    if (attrIndexCached == 0)
                                    {
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type = resolvedType;
                                    }
                                    else
                                    {
                                        Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type2 = resolvedType;
                                    }
                                }
                            }

                            break;
                        }

                    case DialogueType.DeleteMap:
                        Map.OnClear();
                        GameState.OnClear();
                        break;

                    case DialogueType.ClearDirBlocks:
                        {
                            int maxX = Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
                            int maxY = Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;
                            for (x = 0; x < maxX; x++)
                            {
                                for (y = 0; y < maxY; y++)
                                {
                                    Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].DirBlock = 0;
                                }
                            }
                            break;
                        }
                }
            }

            else if (index == 3L) // No button
            {
                // Dialogue index
                switch (GameState.Dialogue)
                {
                    case DialogueType.Trade:
                        {
                            Sender.HandleTradeInvite(0);
                            break;
                        }

                    case DialogueType.PartyInvite:
                        {
                            Sender.DeclineParty();
                            break;
                        }
                }
            }

            CloseDialogue();
            GameState.Dialogue = 0L;
            diaInput = "";
        }

        public static void ShowJobs()
        {
            WindowManager.HideWindows();
            GameState.NewCharJob = 0;
            GameState.NewCharactersprite = 1;
            GameState.NewCnarGender = Sex.Male;
            if (WindowManager.TryGetControl("winJobs", "lblJobName", out var jobNameLbl)) jobNameLbl!.Text = Job.Instance[(int)GameState.NewCharJob].Name;
            WindowManager.ShowWindow("winJobs");
        }

        public static void AddChar(string name, int sex, int job, int sprite)
        {
            if (Network.IsConnected == true)
            {
                Sender.AddChar(name, sex, job);
            }
            else
            {
                Dialogue("Invalid Connection", "Cannot connect to game server.", "Please try again.", DialogueType.Alert);
            }
        }

        public static void SetChatHeight(long height)
        {
            GameState.ActChatHeight = height;
        }

        public static void SetChatWidth(long width)
        {
            GameState.ActChatWidth = width;
        }

        public static void ScrollChatBox(byte direction)
        {
            if (direction == 0) // up
            {
                if (Strings.Len(Data.Chat[(int)(GameState.ChatScroll + 7L)].Text) > 0)
                {
                    if (GameState.ChatScroll < Variables.ChatLines)
                    {
                        GameState.ChatScroll = GameState.ChatScroll + 1L;
                    }
                }
            }
            else if (GameState.ChatScroll > 0L)
            {
                GameState.ChatScroll = GameState.ChatScroll - 1L;
            }
        }

        public static int IsHotbar(long startX, long startY)
        {
            for (var i = 0; i < Core.Globals.Variables.MaxHotbar; i++)
            {
                Rectangle rec;
                
                rec.Top = (int)(startY + GameState.HotbarTop);
                rec.Left = (int)(startX + i * GameState.HotbarOffsetX);
                rec.Right = rec.Left + Constants.TileSize;
                rec.Bottom = rec.Top + Constants.TileSize;

                if (Player.Instance[GameState.MyIndex].Hotbar[i].Slot < 0)
                {
                    continue;
                }
                
                if (GameState.CurMouseX >= rec.Left & GameState.CurMouseX <= rec.Right && 
                    GameState.CurMouseY >= rec.Top & GameState.CurMouseY <= rec.Bottom)
                {
                    return i;
                }
            }

            return -1;
        }

        public static void ShowInvDesc(int x, int y, int invSlot)
        {
            // reserved for future use

            if (invSlot < 0L | invSlot > Core.Globals.Variables.MaxInventory)
                return;

            // show
            if (GetPlayerInv(GameState.MyIndex, invSlot) >= 0)
            {
                ShowItemDesc(x, y, GetPlayerInv(GameState.MyIndex, invSlot), invSlot);
            }
        }

        public static void ShowItemDesc(int x, int y, int item, int inv = -1, int eq = -1)
        {
            var color = default(Microsoft.Xna.Framework.Color);
            string theName;
            string jobName;
            string levelTxt;

            // set globals
            GameState.DescType = (byte)DraggablePartType.Item; // inventory
            GameState.DescItem = item;

            // set position (guard if UI not ready)
            if (WindowManager.TryGetWindow("winDescription", out var winDescription))
            {
                winDescription!.X = x;
                winDescription!.Y = y;
            }

            // show the window
            WindowManager.ShowWindow("winDescription", resetPosition: false);

            // exit out early if last is same
            if (GameState.DescLastType == GameState.DescType & GameState.DescLastItem == GameState.DescItem)
                return;

            // set last to this
            GameState.DescLastType = GameState.DescType;
            GameState.DescLastItem = GameState.DescItem;

            // show req. labels
            if (WindowManager.TryGetControl("winDescription", "lblJob", out var lblJob)) lblJob!.Visible = true;
            if (WindowManager.TryGetControl("winDescription", "lblLevel", out var lblLevel)) lblLevel!.Visible = true;
            if (WindowManager.TryGetControl("winDescription", "picBar", out var picBar)) picBar!.Visible = false;

            // set variables
            {
                var instance = WindowManager.GetWindowByName("winDescription");
                if (inv >= 0)
                {
                    if (Player.Instance[GameState.MyIndex].Inventory[inv].Bound > 0)
                        theName = "(SB) " + Item.Instance[(int)item].Name;
                    else
                        theName = Item.Instance[(int)item].Name;


                    if (WindowManager.TryGetControl("winDescription", "lblName", out var lblName)) lblName!.Text = theName;
                }

                if (eq >= 0)
                {
                    if (Player.Instance[GameState.MyIndex].Paperdoll[eq].Bound > 0)
                        theName = "(SB) " + Item.Instance[(int)item].Name;
                    else
                        theName = Item.Instance[(int)item].Name;


                    if (WindowManager.TryGetControl("winDescription", "lblName", out var lblName)) lblName!.Text = theName;
                }

                switch (Item.Instance[(int)item].Rarity)
                {
                    case 0: // white
                        {
                            color = Microsoft.Xna.Framework.Color.White;
                            break;
                        }
                    case 1: // green
                        {
                            color = Microsoft.Xna.Framework.Color.Green;
                            break;
                        }
                    case 2: // blue
                        {
                            color = Microsoft.Xna.Framework.Color.Blue;
                            break;
                        }
                    case 3: // maroon
                        {
                            color = Microsoft.Xna.Framework.Color.Red;
                            break;
                        }
                    case 4: // purple
                        {
                            color = Microsoft.Xna.Framework.Color.Magenta;
                            break;
                        }
                    case 5: // cyan
                        {
                            color = Microsoft.Xna.Framework.Color.Cyan;
                            break;
                        }
                }
                if (WindowManager.TryGetControl("winDescription", "lblName", out var lblName0))
                    lblName0!.Color = color;

                // class req
                if (Item.Instance[(int)item].JobReq > 0)
                {
                    jobName = Job.Instance[Item.Instance[(int)item].JobReq].Name;
                    // do we match it?
                    if (GetJob(GameState.MyIndex) == Item.Instance[(int)item].JobReq)
                    {
                        color = Microsoft.Xna.Framework.Color.Green;
                    }
                    else
                    {
                        color = Microsoft.Xna.Framework.Color.Red;
                    }
                }
                else
                {
                    jobName = "No Job Req.";
                    color = Microsoft.Xna.Framework.Color.Green;
                }

                if (WindowManager.TryGetControl("winDescription", "lblJob", out var lblJob0))
                {
                    lblJob0!.Text = jobName;
                    lblJob0!.Color = color;
                }

                // level
                if (Item.Instance[(int)item].LevelReq > 0)
                {
                    levelTxt = "Level " + Item.Instance[(int)item].LevelReq;
                    // do we match it?
                    if (GetPlayerLevel(GameState.MyIndex) >= Item.Instance[(int)item].LevelReq)
                    {
                        color = Microsoft.Xna.Framework.Color.Green;
                    }
                    else
                    {
                        color = Microsoft.Xna.Framework.Color.Red;
                    }
                }
                else
                {
                    levelTxt = "No Level Req.";
                    color = Microsoft.Xna.Framework.Color.Green;
                }
                if (WindowManager.TryGetControl("winDescription", "lblLevel", out var lblLevel0))
                    lblLevel0!.Text = levelTxt;
                if (WindowManager.TryGetControl("winDescription", "lblLevel", out var lblLevel1))
                    lblLevel1!.Color = color;
            }

            // clear
            GameState.Description = new Type.Text[2];

            // go through the rest of the text
            switch (Item.Instance[(int)item].Type)
            {
                case (byte)ItemCategory.Equipment:
                    {
                        switch ((Equipment)Item.Instance[(int)item].SubType)
                        {
                            case Equipment.Weapon:
                                AddDescInfo("Weapon", Microsoft.Xna.Framework.Color.White);
                                break;
                            case Equipment.Armor:
                                AddDescInfo("Armor", Microsoft.Xna.Framework.Color.White);
                                break;
                            case Equipment.Helmet:
                                AddDescInfo("Helmet", Microsoft.Xna.Framework.Color.White);
                                break;
                            case Equipment.Shield:
                                AddDescInfo("Shield", Microsoft.Xna.Framework.Color.White);
                                break;
                            default:
                                AddDescInfo("Equipment", Microsoft.Xna.Framework.Color.White);
                                break;
                        }

                        break;
                    }
                case (byte)ItemCategory.Consumable:
                    {
                        AddDescInfo("Consumable", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)ItemCategory.Currency:
                    {
                        AddDescInfo("Currency", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)ItemCategory.Skill:
                    {
                        AddDescInfo("Skill", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)ItemCategory.Projectile:
                    {
                        AddDescInfo("Projectile", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
            }

            // more info
            switch (Item.Instance[(int)item].Type)
            {
                case (byte)ItemCategory.Currency:
                    {
                        // binding
                        if (Item.Instance[(int)item].BindType == 1)
                        {
                            AddDescInfo("Bind on Pickup", Microsoft.Xna.Framework.Color.White);
                        }
                        else if (Item.Instance[(int)item].BindType == 2)
                        {
                            AddDescInfo("Bind on Equip", Microsoft.Xna.Framework.Color.White);
                        }

                        AddDescInfo("Value: " + Item.Instance[(int)item].Price + " g", Microsoft.Xna.Framework.Color.Yellow);
                        break;
                    }
                case (byte)ItemCategory.Equipment:
                    {
                        // Damage/defense
                        if (Item.Instance[(int)item].SubType == (byte)Equipment.Weapon)
                        {
                            AddDescInfo("Damage: " + Item.Instance[(int)item].Data2, Microsoft.Xna.Framework.Color.White);
                            AddDescInfo("Speed: " + Item.Instance[(int)item].AttackSpeed / 1000d + "s", Microsoft.Xna.Framework.Color.White);
                        }
                        else if (Item.Instance[(int)item].Data2 > 0)
                        {
                            AddDescInfo("Defense: " + Item.Instance[(int)item].Data2, Microsoft.Xna.Framework.Color.White);
                        }

                        // binding
                        if (Item.Instance[(int)item].BindType == 1)
                        {
                            AddDescInfo("Bind on Pickup", Microsoft.Xna.Framework.Color.White);
                        }
                        else if (Item.Instance[(int)item].BindType == 2)
                        {
                            AddDescInfo("Bind on Equip", Microsoft.Xna.Framework.Color.White);
                        }

                        AddDescInfo("Value: " + Item.Instance[(int)item].Price + " G", Microsoft.Xna.Framework.Color.Yellow);

                        // stat bonuses
                        if (Item.Instance[(int)item].AddStat[(int)Stat.Strength] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Strength] + " Str", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Luck] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Luck] + " End", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Spirit] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Spirit] + " Spi", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Luck] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Luck] + " Luc", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Intelligence] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Intelligence] + " Int", Microsoft.Xna.Framework.Color.White);
                        }

                        break;
                    }
                case (byte)ItemCategory.Consumable:
                    {
                        if (Item.Instance[(int)item].AddStat[(int)Stat.Strength] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Strength] + " Str", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Luck] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Luck] + " End", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Spirit] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Spirit] + " Spi", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Luck] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Luck] + " Luc", Microsoft.Xna.Framework.Color.White);
                        }

                        if (Item.Instance[(int)item].AddStat[(int)Stat.Intelligence] > 0)
                        {
                            AddDescInfo("+" + Item.Instance[(int)item].AddStat[(int)Stat.Intelligence] + " Int", Microsoft.Xna.Framework.Color.White);
                        }

                        AddDescInfo("Value: " + Item.Instance[(int)item].Price + " G", Microsoft.Xna.Framework.Color.Yellow);
                        break;
                    }
                case (byte)ItemCategory.Skill:
                    {
                        AddDescInfo("Value: " + Item.Instance[(int)item].Price + " G", Microsoft.Xna.Framework.Color.Yellow);
                        break;
                    }
            }
        }

        public static void ShowSkillDesc(int x, int y, int skill, long skillSlot)
        {
            string sUse = string.Empty;
            int tmpWidth = 0;

            if (skill < 0 || skill > Core.Globals.Variables.MaxSkills)
                return;

            // set globals
            GameState.DescType = 2; // Skill
            GameState.DescItem = skill;

            // set position (guard if UI not ready)
            if (WindowManager.TryGetWindow("winDescription", out var winDescription))
            {
                winDescription!.X = x;
                winDescription!.Y = y;
            }

            // show the window
            WindowManager.ShowWindow("winDescription", resetPosition: false);

            // exit out early if last is same
            if (GameState.DescLastType == GameState.DescType & GameState.DescLastItem == GameState.DescItem)
                return;

            // clear
            GameState.Description = new Type.Text[2];

            // hide req. labels
            if (WindowManager.TryGetControl("winDescription", "lblLevel", out var lblLevel2)) lblLevel2!.Visible = false;
            if (WindowManager.TryGetControl("winDescription", "picBar", out var picBar2)) picBar2!.Visible = true;

            // set variables
            {
                var instance = WindowManager.GetWindowByName("winDescription");
                if (instance is null) return;
                // set name
                if (WindowManager.TryGetControl("winDescription", "lblName", out var lblName2))
                {
                    if (Skill.Instance.Count <= skill) return;
                    lblName2!.Text = Skill.Instance[(int)skill].Name;
                    lblName2!.Color = Microsoft.Xna.Framework.Color.White;
                }

                // find ranks
                if (skillSlot >= 0L)
                {
                    // draw the rank bar (fixed width for now)
                    // If Type.Skill(skillNum).rank > 0 Then
                    // tmpWidth = ((PlayerSkills(SkillSlot).Uses / barWidth) / (Type.Skill(skillNum).NextUses / barWidth)) * barWidth
                    // Else
                    tmpWidth = 66;
                    // End If
                    instance.Controls[WindowManager.GetControl("winDescription", "picBar")].Value = tmpWidth;
                    // does it rank up?
                    // If Type.Skill(skillNum).NextRank > 0 Then
                    // sUse = "Uses: " & PlayerSkills(SkillSlot).Uses & "/" & Type.Skill(skillNum).NextUses
                    // If PlayerSkills(SkillSlot).Uses = Type.Skill(skillNum).NextUses Then
                    // If Not GetPlayerLevel(GameState.MyIndex) >= Skill(Type.Skill(skillNum).NextRank).LevelReq Then
                    // Color = BrightRed
                    // sUse = "Lvl " & Skill(Type.Skill(skillNum).NextRank).LevelReq & " req."
                    // End If
                    // End If
                    // Else
                    sUse = "Max Rank";
                    // End If
                    // show controls
                    instance.Controls[WindowManager.GetControl("winDescription", "lblJob")].Visible = true;
                    instance.Controls[WindowManager.GetControl("winDescription", "picBar")].Visible = true;
                    // set vals
                    instance.Controls[WindowManager.GetControl("winDescription", "lblJob")].Text = sUse;
                    instance.Controls[WindowManager.GetControl("winDescription", "lblJob")].Color = Microsoft.Xna.Framework.Color.White;
                }
                else
                {
                    // hide some controls
                    instance.Controls[WindowManager.GetControl("winDescription", "lblJob")].Visible = false;
                    instance.Controls[WindowManager.GetControl("winDescription", "picBar")].Visible = false;
                }
            }

            switch (Skill.Instance[(int)skill].Type)
            {
                case (byte)SkillEffect.DamageHealth:
                    {
                        AddDescInfo("Damage HP", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)SkillEffect.DamageMana:
                    {
                        AddDescInfo("Damage SP", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)SkillEffect.HealHealth:
                    {
                        AddDescInfo("Heal HP", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)SkillEffect.HealMana:
                    {
                        AddDescInfo("Heal SP", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
                case (byte)SkillEffect.Warp:
                    {
                        AddDescInfo("Warp", Microsoft.Xna.Framework.Color.White);
                        break;
                    }
            }

            // more info
            switch (Skill.Instance[(int)skill].Type)
            {
                case (byte)SkillEffect.DamageHealth:
                case (byte)SkillEffect.DamageMana:
                case (byte)SkillEffect.HealHealth:
                case (byte)SkillEffect.HealMana:
                    {
                        // damage
                        AddDescInfo("Vital: " + Skill.Instance[(int)skill].Vital, Microsoft.Xna.Framework.Color.White);

                        // mp cost
                        AddDescInfo("Cost: " + Skill.Instance[(int)skill].MpCost + " SP", Microsoft.Xna.Framework.Color.White);

                        // cast time
                        AddDescInfo("Cast Time: " + Skill.Instance[(int)skill].CastTime + "s", Microsoft.Xna.Framework.Color.White);

                        // cd time
                        AddDescInfo("Cooldown: " + Skill.Instance[(int)skill].CdTime + "s", Microsoft.Xna.Framework.Color.White);

                        // aoe
                        if (Skill.Instance[(int)skill].AoE > 0)
                        {
                            AddDescInfo("AoE: " + Skill.Instance[(int)skill].AoE, Microsoft.Xna.Framework.Color.White);
                        }

                        // stun
                        if (Skill.Instance[(int)skill].StunDuration > 0)
                        {
                            AddDescInfo("Stun: " + Skill.Instance[(int)skill].StunDuration + "s", Microsoft.Xna.Framework.Color.White);
                        }

                        // dot
                        if (Skill.Instance[(int)skill].Duration > 0 & Skill.Instance[(int)skill].Interval > 0)
                        {
                            AddDescInfo("DoT: " + Skill.Instance[(int)skill].Duration / (double)Skill.Instance[(int)skill].Interval + " tick", Microsoft.Xna.Framework.Color.White);
                        }

                        break;
                    }
            }
        }

        public static void ShowShopDesc(int x, int y, int item)
        {
            if (item < 0L | item > Core.Globals.Variables.MaxItems)
                return;

            // show
            ShowItemDesc(x, y, item);
        }

        public static void ShowEqDesc(int x, int y, long eq)
        {
            var equipmentCount = System.Enum.GetValues(typeof(Equipment)).Length;

            if (eq < 0L || eq >= equipmentCount)
                return;

            if (Player.Instance[GameState.MyIndex].Paperdoll[(int)eq].Num < 0 || Player.Instance[GameState.MyIndex].Paperdoll[(int)eq].Num > Core.Globals.Variables.MaxItems)
                return;

            // show
            ShowItemDesc(x, y, Player.Instance[GameState.MyIndex].Paperdoll[(int)eq].Num, -1, (byte)eq);
            
        }

        public static void AddDescInfo(string text, Microsoft.Xna.Framework.Color color)
        {
            long count;
            if (GameState.Description == null || GameState.Description.Length == 0)
            {
                GameState.Description = new Type.Text[1];
                count = 0;
            }
            else
            {
                count = GameState.Description.Length;
                Array.Resize(ref GameState.Description, (int)(count + 1));
            }
            GameState.Description[(int)count].Caption = text;
            GameState.Description[(int)count].Color = GameClient.ToDrawingColor(color);
        }

        public static void LogoutGame()
        {
            GameState.InMenu = true;
            GameState.GettingMap = false;
            GameState.InGame = false;
            WindowManager.HideWindows();
            WindowManager.ShowWindow("winLogin");
            General.ClearGameData();
        }

        public static void SetOptionsScreen()
        {
            var windowIndex = WindowManager.GetWindow("winOptions");
            
            // Resolutions (add directly to ComboBox)
            if (WindowManager.TryGetControl("winOptions", "cmbRes", out var cmbCtrl) && cmbCtrl is Client.Game.UI.Controls.ComboBox cmbRes)
            {
                cmbRes.Items.Clear();
                cmbRes.Items.Add("1920x1080");
                cmbRes.Items.Add("1680x1050");
                cmbRes.Items.Add("1600x900");
                cmbRes.Items.Add("1440x900");
                cmbRes.Items.Add("1440x1050");
                cmbRes.Items.Add("1366x768");
                cmbRes.Items.Add("1360x1024");
                cmbRes.Items.Add("1360x768");
                cmbRes.Items.Add("1280x1024");
                cmbRes.Items.Add("1280x800");
                cmbRes.Items.Add("1280x768");
                cmbRes.Items.Add("1280x720");
                cmbRes.Items.Add("1120x864");
                // set selected value from settings (clamp to range)
                cmbRes.Value = Math.Clamp(SettingsManager.Instance.Resolution, 0, Math.Max(0, cmbRes.Items.Count - 1));
            }

            // fill the options screen
            {
                var instance = WindowManager.Windows[WindowManager.GetWindow("winOptions")];
                instance.Controls[WindowManager.GetControl("winOptions", "chkMusic")].Value = SettingsManager.Instance.Music ? 1 : 0;
                instance.Controls[WindowManager.GetControl("winOptions", "chkSound")].Value = SettingsManager.Instance.Sound ? 1 : 0;
                instance.Controls[WindowManager.GetControl("winOptions", "chkAutotile")].Value = SettingsManager.Instance.Autotile ? 1 : 0;
                instance.Controls[WindowManager.GetControl("winOptions", "chkFullscreen")].Value = SettingsManager.Instance.Fullscreen ? 1 : 0;
                instance.Controls[WindowManager.GetControl("winOptions", "chkVsync")].Value = SettingsManager.Instance.Vsync ? 1 : 0;
                instance.Controls[WindowManager.GetControl("winOptions", "cmbRes")].Value = SettingsManager.Instance.Resolution;
            }
        }

        public static void OpenShop(long shopNum)
        {
            var shop = (int)shopNum;
            if (shop < 0 || shop >= Shop.Instance.Count)
            {
                GameState.InShop = -1;
                return;
            }

            // set globals
            GameState.InShop = shop;
            GameState.ShopSelectedSlot = 0;
            GameState.ShopSelectedItem = Shop.Instance[GameState.InShop].TradeItem.Length > 0
                ? Shop.Instance[GameState.InShop].TradeItem[0].Item
                : -1;
            WindowManager.Windows[WindowManager.GetWindow("winShop")].Controls[WindowManager.GetControl("winShop", "CheckboxSelling")].Value = 0;
            WindowManager.Windows[WindowManager.GetWindow("winShop")].Controls[WindowManager.GetControl("winShop", "CheckboxBuying")].Value = 0;
            WindowManager.Windows[WindowManager.GetWindow("winShop")].Controls[WindowManager.GetControl("winShop", "btnSell")].Visible = false;
            WindowManager.Windows[WindowManager.GetWindow("winShop")].Controls[WindowManager.GetControl("winShop", "btnBuy")].Visible = true;
            GameState.ShopIsSelling = false;

            // set the current item
            WinShop.UpdateShop();

            // show the window
            WindowManager.ShowWindow("winShop");
        }

        public static void UpdatePartyBars()
        {
            long i;
            long pIndex;
            int barWidth;
            int width;

            // unload it if we're not in a party
            if (Data.MyParty.Leader == 0)
            {
                return;
            }

            // max bar width
            barWidth = 173;

            // make sure we're in a party
            {
                var instance = WindowManager.Windows[WindowManager.GetWindow("winParty")];
                for (i = 0L; i <= 3L; i++)
                {
                    // get the pIndex from the control
                    if (instance.Controls[WindowManager.GetControl("winParty", "picChar" + i)].Visible == true)
                    {
                        pIndex = instance.Controls[WindowManager.GetControl("winParty", "picChar" + i)].Value;
                        // make sure they exist
                        if (pIndex > 0L)
                        {
                            if (IsPlaying((int)pIndex))
                            {
                                // get their health
                                if (GetPlayerVital((int)pIndex, Core.Globals.Vital.Health) > 0 & GetPlayerMaxVital((int)pIndex, Core.Globals.Vital.Health) > 0)
                                {
                                    width = (int)Math.Round(GetPlayerVital((int)pIndex, Core.Globals.Vital.Health) / (double)barWidth / (GetPlayerMaxVital((int)pIndex, Core.Globals.Vital.Health) / (double)barWidth) * barWidth);
                                    instance.Controls[WindowManager.GetControl("winParty", "picBar_HP" + i)].Width = width;
                                }
                                else
                                {
                                    instance.Controls[WindowManager.GetControl("winParty", "picBar_HP" + i)].Width = 0;
                                }
                                // get their spirit
                                if (GetPlayerVital((int)pIndex, Core.Globals.Vital.Stamina) > 0 & GetPlayerMaxVital((int)pIndex, Core.Globals.Vital.Stamina) > 0)
                                {
                                    width = (int)Math.Round(GetPlayerVital((int)pIndex, Core.Globals.Vital.Stamina) / (double)barWidth / (GetPlayerMaxVital((int)pIndex, Core.Globals.Vital.Stamina) / (double)barWidth) * barWidth);
                                    instance.Controls[WindowManager.GetControl("winParty", "picBar_SP" + i)].Width = width;
                                }
                                else
                                {
                                    instance.Controls[WindowManager.GetControl("winParty", "picBar_SP" + i)].Width = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ShowTrade()
        {
            // show the window
            WindowManager.ShowWindow("winTrade");

            // set the controls up
            {
                var instance = WindowManager.Windows[WindowManager.GetWindow("winTrade")];
                instance.Text = "Trading with " + GetPlayerName(Trade.InTrade);
                instance.Controls[WindowManager.GetControl("winTrade", "lblYourTrade")].Text = GetPlayerName(GameState.MyIndex) + "'s Offer";
                instance.Controls[WindowManager.GetControl("winTrade", "lblTheirTrade")].Text = GetPlayerName(Trade.InTrade) + "'s Offer";
                instance.Controls[WindowManager.GetControl("winTrade", "lblYourValue")].Text = "0g";
                instance.Controls[WindowManager.GetControl("winTrade", "lblTheirValue")].Text = "0g";
                instance.Controls[WindowManager.GetControl("winTrade", "lblStatus")].Text = "Choose items to offer.";
            }
        }

        public static void ShowPlayerMenu(long index, int x, int y)
        {
            GameState.PlayerMenuIndex = index;
            if (GameState.PlayerMenuIndex == 0L | GameState.PlayerMenuIndex == GameState.MyIndex)
                return;
            WindowManager.Windows[WindowManager.GetWindow("winPlayerMenu")].X = x - 5;
            WindowManager.Windows[WindowManager.GetWindow("winPlayerMenu")].Y = y - 5;
            WindowManager.Windows[WindowManager.GetWindow("winPlayerMenu")].Controls[WindowManager.GetControl("winPlayerMenu", "btnName")].Text = GetPlayerName((int)GameState.PlayerMenuIndex);
            WindowManager.ShowWindow("winRightClickBG");
            WindowManager.ShowWindow("winPlayerMenu");
        }

        public static void SetBarWidth(ref int maxWidth, ref int width)
        {
            int barDifference;

            if (maxWidth <  width)
            {
                // find out the amount to increase per loop
                barDifference = (int)Math.Round((double)(width - maxWidth) / 100) * 10;

                // if it's less than 1 then default to 1
                if (barDifference < 0)
                    barDifference = 0;
                
                if (width != maxWidth && barDifference == 0L)
                {
                    barDifference = Math.Clamp(width - maxWidth, 1, width);
                }
                
                // set the width
                width -= barDifference;
            }
            else if (maxWidth > width)
            {
                // find out the amount to increase per loop
                barDifference = (int)Math.Round((double)(maxWidth - width) / 100) * 10;

                // if it's less than 1 then default to 1
                if (barDifference < 0)
                    barDifference = 0;

                if (maxWidth != width && barDifference == 0L)
                {
                    barDifference = Math.Clamp(maxWidth - width, 1, maxWidth);
                }

                // set the width
                width += barDifference;
            }
        }

        public static void SetGoldLabel()
        {
            long i;
            var amount = default(long);

            for (i = 0L; i < Core.Globals.Variables.MaxInventory; i++)
            {
                if (GetPlayerInv(GameState.MyIndex, (int)i) == 1)
                {
                    amount = GetPlayerInvValue(GameState.MyIndex, (int)i);
                }
            }
            WindowManager.Windows[WindowManager.GetWindow("winShop")].Controls[WindowManager.GetControl("winShop", "lblGold")].Text = Strings.Format(amount, "#,###,###,###") + "g";
            WindowManager.Windows[WindowManager.GetWindow("winInventory")].Controls[WindowManager.GetControl("winInventory", "lblGold")].Text = Strings.Format(amount, "#,###,###,###") + "g";
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int ConvertMapX(int x)
        {
            int convertMapX = default;
            convertMapX = (int)Math.Round(x - GameState.TileView.Left - GameState.Camera.Left);
            return convertMapX;
        }

        public static int ConvertMapY(int y)
        {
            int convertMapY = default;
            convertMapY = (int)Math.Round(y - GameState.TileView.Top - GameState.Camera.Top);
            return convertMapY;
        }

        public static bool IsValidMapPoint(int x, int y)
        {
            if (x < 0)
                return default;
                
            if (y < 0)
                return default;

            var map = Client.Map.Instance[GetMap(GameState.MyIndex)];
            int maxMapX = map.MaxX;
            int maxMapY = map.MaxY;

            if (x >= maxMapX)
                return default;

            if (y >= maxMapY)
                return default;

            return true;
        }

        public static List<Microsoft.Xna.Framework.Vector2> GetCellsInSquare(int xCenter, int yCenter, int distance)
        {
            int xMin = Math.Max(0, xCenter - distance);
            int xMax = Math.Min(Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX, xCenter + distance);
            int yMin = Math.Max(0, yCenter - distance);
            int yMax = Math.Min(Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY, yCenter + distance);

            var cells = new List<Microsoft.Xna.Framework.Vector2>();
            for (int y = yMin, count2 = yMax; y < count2; y++)
            {
                for (int x = xMin, count = xMax; x < count; x++)
                    cells.Add(new Microsoft.Xna.Framework.Vector2(x, y));
            }
            return cells;
        }

        public static List<Microsoft.Xna.Framework.Vector2> GetBorderCellsInSquare(int xCenter, int yCenter, int distance)
        {
            int xMin = Math.Max(0, xCenter - distance);
            int xMax = Math.Min(Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX, xCenter + distance);
            int yMin = Math.Max(0, yCenter - distance);
            int yMax = Math.Min(Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY, yCenter + distance);

            var borderCells = new List<Microsoft.Xna.Framework.Vector2>();

            // Top and bottom border
            for (int x = xMin, count = xMax; x < count; x++)
            {
                borderCells.Add(new Microsoft.Xna.Framework.Vector2(x, yMin));
                borderCells.Add(new Microsoft.Xna.Framework.Vector2(x, yMax));
            }

            // Left and right border
            for (int y = yMin + 1, count = yMax - 1; y < count; y++)
            {
                borderCells.Add(new Microsoft.Xna.Framework.Vector2(xMin, y));
                borderCells.Add(new Microsoft.Xna.Framework.Vector2(xMax, y));
            }

            borderCells.Remove(new Microsoft.Xna.Framework.Vector2(xCenter, yCenter));
            return borderCells;
        }

        private static List<Microsoft.Xna.Framework.Vector2> Line(int x, int y, int xDestination, int yDestination)
        {
            var discovered = new HashSet<Microsoft.Xna.Framework.Vector2>();
            var litTiles = new List<Microsoft.Xna.Framework.Vector2>();

            int dx = Math.Abs(xDestination - x);
            int dy = Math.Abs(yDestination - y);
            int sx = x < xDestination ? 1 : -1;
            int sy = y < yDestination ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                var pos = new Microsoft.Xna.Framework.Vector2(x, y);
                if (discovered.Add(pos))
                    litTiles.Add(pos);

                if (x == xDestination && y == yDestination)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return litTiles;
        }

        private static void PostProcessFovQuadrant(ref List<Microsoft.Xna.Framework.Vector2> inFov, int x, int y, Quadrant quadrant)
        {
            int x1 = x;
            int y1 = y;
            int x2 = x;
            int y2 = y;
            var pos = new Microsoft.Xna.Framework.Vector2(x, y); // Use Vector2i for integer-based coordinates

            // Adjust coordinates based on the quadrant
            switch (quadrant)
            {
                case Quadrant.Northeast:
                    {
                        y1 = y + 1;
                        x2 = x - 1;
                        break;
                    }
                case Quadrant.Southeast:
                    {
                        y1 = y - 1;
                        x2 = x - 1;
                        break;
                    }
                case Quadrant.Southwest:
                    {
                        y1 = y - 1;
                        x2 = x + 1;
                        break;
                    }
                case Quadrant.Northwest:
                    {
                        y1 = y + 1;
                        x2 = x + 1;
                        break;
                    }
            }

            // Check if the position is already in the field of view and is not transparent
            if (!inFov.Contains(pos) && !IsTransparent(x, y))
            {
                // Check neighboring cells to determine visibility
                if (IsTransparent(x1, y1) && inFov.Contains(new Microsoft.Xna.Framework.Vector2(x1, y1)) || IsTransparent(x2, y2) && inFov.Contains(new Microsoft.Xna.Framework.Vector2(x2, y2)) || IsTransparent(x2, y1) && inFov.Contains(new Microsoft.Xna.Framework.Vector2(x2, y1)))
                {
                    inFov.Add(pos);
                }
            }
        }

        public static List<Microsoft.Xna.Framework.Vector2> AppendFov(int xOrigin, int yOrigin, int radius, bool lightWalls)
        {
            var inFov = new List<Microsoft.Xna.Framework.Vector2>();

            // Get all the border cells in a square around the origin within the given radius
            foreach (Microsoft.Xna.Framework.Vector2 borderCell in GetBorderCellsInSquare(xOrigin, yOrigin, radius))
            {
                // Trace a line from the origin to the border cell
                foreach (Microsoft.Xna.Framework.Vector2 cell in Line(xOrigin, yOrigin, (int)Math.Round(borderCell.X), (int)Math.Round(borderCell.Y)))
                {
                    // Stop if the cell is outside the radius
                    if (Math.Abs(cell.X - xOrigin) + Math.Abs(cell.Y - yOrigin) > radius)
                        break;

                    // Add the cell to the FOV list if it's transparent or light walls is true
                    if (IsTransparent((int)Math.Round(cell.X), (int)Math.Round(cell.Y)))
                    {
                        inFov.Add(cell);
                    }
                    else
                    {
                        if (lightWalls)
                            inFov.Add(cell);
                        break;
                    } // Stop the line if a non-transparent wall is encountered
                }
            }

            // Optional: Post-process the FOV for specific quadrants
            if (lightWalls)
            {
                foreach (Microsoft.Xna.Framework.Vector2 cell in GetCellsInSquare(xOrigin, yOrigin, radius))
                {
                    // Check the relative position to the origin and post-process based on quadrant
                    if (cell.X > xOrigin)
                    {
                        if (cell.Y > yOrigin)
                        {
                            PostProcessFovQuadrant(ref inFov, (int)Math.Round(cell.X), (int)Math.Round(cell.Y), Quadrant.Southeast);
                        }
                        else if (cell.Y < yOrigin)
                        {
                            PostProcessFovQuadrant(ref inFov, (int)Math.Round(cell.X), (int)Math.Round(cell.Y), Quadrant.Northeast);
                        }
                    }
                    else if (cell.X < xOrigin)
                    {
                        if (cell.Y > yOrigin)
                        {
                            PostProcessFovQuadrant(ref inFov, (int)Math.Round(cell.X), (int)Math.Round(cell.Y), Quadrant.Southwest);
                        }
                        else if (cell.Y < yOrigin)
                        {
                            PostProcessFovQuadrant(ref inFov, (int)Math.Round(cell.X), (int)Math.Round(cell.Y), Quadrant.Northwest);
                        }
                    }
                }
            }

            return inFov;
        }

        private static bool IsTransparent(int x, int y)
        {
            if (Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type == TileType.Blocked | Client.Map.Instance[GetMap(GameState.MyIndex)].Tile[x, y].Type2 == TileType.Blocked)
            {
                return false;
            }

            return true;
        }
        
        public static void UpdateCamera()
        {
            int nativeWidth = GameState.ResolutionWidth;
            int nativeHeight = GameState.ResolutionHeight;

            var map = GetMap(GameState.MyIndex);
            if (map < 0 || map >= Client.Map.Instance.Count)
                return;

            // Find the center of the map
            float mapWidth = Client.Map.Instance[map].MaxX * Constants.TileSize;
            float mapHeight = Client.Map.Instance[map].MaxY * Constants.TileSize;
            float mapCenterX = mapWidth / 2f;
            float mapCenterY = mapHeight / 2f;

            // Get the target (or player) position
            float targetX, targetY;
            if (GameState.MyTarget >= 0)
            {
                if (GameState.MyTargetType == (int)TargetType.Player)
                {
                    int t = GameState.MyTarget;
                    if (IsPlaying(t) && Player.Instance[t].Map == Player.Instance[GameState.MyIndex].Map)
                    {
                        targetX = GetPlayerRawX(t);
                        targetY = GetPlayerRawY(t);
                    }
                    else
                    {
                        targetX = GetPlayerRawX(GameState.MyIndex);
                        targetY = GetPlayerRawY(GameState.MyIndex);
                    }
                }
                else if (GameState.MyTargetType == (int)TargetType.Npc)
                {
                    int npcIndex = GameState.MyTarget;
                    if (npcIndex >= 0 && npcIndex < MapNpc.Instance.Length && MapNpc.Instance[npcIndex].Num >= 0)
                    {
                        targetX = MapNpc.Instance[npcIndex].X;
                        targetY = MapNpc.Instance[npcIndex].Y;
                    }
                    else
                    {
                        targetX = GetPlayerRawX(GameState.MyIndex);
                        targetY = GetPlayerRawY(GameState.MyIndex);
                    }
                }
                else
                {
                    targetX = GetPlayerRawX(GameState.MyIndex);
                    targetY = GetPlayerRawY(GameState.MyIndex);
                }
            }
            else
            {
                targetX = GetPlayerRawX(GameState.MyIndex);
                targetY = GetPlayerRawY(GameState.MyIndex);
            }

            // Desired camera top-left so target/player is centered
            float desiredX = targetX - (nativeWidth / 2f);
            float desiredY = targetY - (nativeHeight / 2f);

            // Clamp desired to map bounds, with centering for small maps
            if (mapWidth <= nativeWidth)
                desiredX = -(nativeWidth - mapWidth) / 2f;
            else
                desiredX = Math.Max(0, Math.Min(desiredX, mapWidth - nativeWidth));

            if (mapHeight <= nativeHeight)
                desiredY = -(nativeHeight - mapHeight) / 2f;
            else
                desiredY = Math.Max(0, Math.Min(desiredY, mapHeight - nativeHeight));

            // Smoothly move current camera toward desired unless we're loading/warping
            // Initialize on first use or snap on big jumps/getting map
            bool needSnap = GameState.GettingMap
                             || double.IsNaN(GameState.CurrentCameraX)
                             || double.IsNaN(GameState.CurrentCameraY)
                             || (Math.Abs(GameState.CurrentCameraX - desiredX) > nativeWidth)
                             || (Math.Abs(GameState.CurrentCameraY - desiredY) > nativeHeight);

            if (needSnap)
            {
                GameState.CurrentCameraX = desiredX;
                GameState.CurrentCameraY = desiredY;
            }
            else
            {
                // Simple exponential smoothing toward the target
                const float smooth = 0.12f; // 0..1, higher = snappier
                GameState.CurrentCameraX += (desiredX - GameState.CurrentCameraX) * smooth;
                GameState.CurrentCameraY += (desiredY - GameState.CurrentCameraY) * smooth;
            }

            GameState.Camera.Left = (long)Math.Round(GameState.CurrentCameraX);
            GameState.Camera.Top = (long)Math.Round(GameState.CurrentCameraY);

            long StartX = Math.Max(0, Math.Min((long)Math.Floor(GameState.Camera.Left), Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX - 1) / Constants.TileSize);
            long StartY = Math.Max(0, Math.Min((long)Math.Floor(GameState.Camera.Top), Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY - 1) / Constants.TileSize);
            long EndX = Client.Map.Instance[GetMap(GameState.MyIndex)].MaxX;
            long EndY = Client.Map.Instance[GetMap(GameState.MyIndex)].MaxY;

            // Update the tile view  
            ref var instance = ref GameState.TileView;
            instance.Top = StartY;
            instance.Bottom = EndY;
            instance.Left = StartX;
            instance.Right = EndX;

            // Update the camera bounds  
            ref var instance1 = ref GameState.Camera;
            instance1.Right = instance1.Left;
            instance1.Bottom = instance1.Top;

            // Optional: Update the map name display  
            UpdateDrawMapName();
        }

    }
}