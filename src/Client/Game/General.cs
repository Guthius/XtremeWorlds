using Core;
using System.Runtime.InteropServices;
using Client.Game.UI;
using Client.Net;
using Core.Common;
using Core.Configurations;
using Core.Globals;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;
using System.IO;
using System;
using System.Diagnostics;
using Core.Objects;

namespace Client
{
    public class General
    {
        public static GameClient Client = new GameClient();
        public static GameState State = new GameState();
        public static RandomUtility Random = new RandomUtility();
        public static WindowManager Gui = new WindowManager();

        public static byte[] AesKey = new byte[32];
        public static byte[] AesIV = new byte[16];

        public static int GetTickCount()
        {
            return Environment.TickCount;
        }

        public static void Startup()
        {
            GameState.InMenu = true;
            ClearGameData();
            LoadGame();
        }

        public static void LoadGame()
        {
            InitializeDirArrows();
            LocalesManager.Initialize();
            CheckAnimations();
            CheckCharacters();
            CheckEmotes();
            CheckTilesets();
            CheckFogs();
            CheckItems();
            CheckPanoramas();
            CheckPaperdolls();
            CheckParallax();
            CheckPictures();
            CheckProjectile();
            CheckResources();
            CheckSkills();
            CheckInterface();
            CheckGradients();
            CheckDesigns();
            Audio.Init();
            UIScript.Load();
            WindowManager.Init();
            GameState.Ping = -1;
        }

        public static void InitializeDirArrows()
        {
            // Set values for directional blocking arrows
            GameState.DirArrowX[0] = 12; // up
            GameState.DirArrowY[0] = 0;
            GameState.DirArrowX[1] = 12; // down
            GameState.DirArrowY[1] = 23;
            GameState.DirArrowX[2] = 0; // left
            GameState.DirArrowY[2] = 12;
            GameState.DirArrowX[3] = 23; // right
            GameState.DirArrowY[3] = 12;
        }

        public static void CheckAnimations()
        {
            GameState.NumAnimations = GetFileCount(DataPath.Animations);
        }

        public static void CheckCharacters()
        {
            GameState.NumCharacters = GetFileCount(DataPath.Characters);
        }

        public static void CheckEmotes()
        {
            GameState.NumEmotes = GetFileCount(DataPath.Emotes);
        }

        public static void CheckTilesets()
        {
            GameState.NumTileSets = GetFileCount(DataPath.Tilesets);
        }

        public static void CheckFogs()
        {
            GameState.NumFogs = GetFileCount(DataPath.Fogs);
        }

        public static void CheckItems()
        {
            GameState.NumItems = GetFileCount(DataPath.Items);
        }

        public static void CheckPanoramas()
        {
            GameState.NumPanoramas = GetFileCount(DataPath.Panoramas);
        }

        public static void CheckPaperdolls()
        {
            GameState.NumPaperdolls = GetFileCount(DataPath.Paperdolls);
        }

        public static void CheckParallax()
        {
            GameState.NumParallax = GetFileCount(DataPath.Parallax);
        }

        public static void CheckPictures()
        {
            GameState.NumPictures = GetFileCount(DataPath.Pictures);
        }

        public static void CheckProjectile()
        {
            GameState.NumProjectiles = GetFileCount(DataPath.Projectiles);
        }

        public static void CheckResources()
        {
            GameState.NumResources = GetFileCount(DataPath.Resources);
        }

        public static void CheckSkills()
        {
            GameState.NumSkills = GetFileCount(DataPath.Skills);
        }

        public static void CheckInterface()
        {
            GameState.NumInterface = GetFileCount(DataPath.Gui);
        }

        public static void CheckGradients()
        {
            GameState.NumGradients = GetFileCount(DataPath.Gradients);
        }

        public static void CheckDesigns()
        {
            GameState.NumDesigns = GetFileCount(DataPath.Designs);
        }

        public static (int Width, int Height) GetResolutionSize(byte resolution)
        {
            return resolution switch
            {
                1 => (1920, 1080),
                2 => (1680, 1050),
                3 => (1600, 900),
                4 => (1440, 900),
                5 => (1440, 1050),
                6 => (1366, 768),
                7 => (1360, 1024),
                8 => (1360, 768),
                9 => (1280, 1024),
                10 => (1280, 800),
                11 => (1280, 768),
                12 => (1280, 720),
                13 => (1120, 864),
                14 => (1024, 768),
                _ => (1280, 720)
            };
        }

        public static void ClearGameData()
        {
            Map.Instance.Add(new Map());
            Map.OnClear();
            Npc.OnClear();
            Resource.OnClear();
            Item.OnClear();
            Shop.OnClear();
            Skill.OnClear();
            Animation.OnClear();
            MapAnimation.OnClear();
            Projectile.OnClear();
            Job.OnClear();
            Moral.OnClear();
            Bank.OnClear();
            Party.OnClear();

            for (int i = 0; i < Player.Instance.Count; i++)
                Player.OnClear(i);

            Animation.OnClear();
            Autotile.OnClear();

            // clear chat
            for (int i = 0; i < Variables.ChatLines; i++)
                Data.Chat[i].Text = "";
        }

        public static int GetFileCount(string folderPath)
        {
            // folderPath is expected to be an absolute directory path (e.g., DataPath.Resources)
            if (Directory.Exists(folderPath))
            {
                return Directory.GetFiles(folderPath, "*.png").Length; // Adjust for other formats if needed
            }
            else
            {
                Console.WriteLine($"Folder not found: {folderPath}");
                return 0;
            }
        }

        public static void CacheMusic()
        {
            Audio.MusicCache = new string[Directory.GetFiles(DataPath.Music, "*" + SettingsManager.Instance.MusicExt).Count() + 1];
            string[] files = Directory.GetFiles(DataPath.Music, "*" + SettingsManager.Instance.MusicExt);
            string maxNum = Directory.GetFiles(DataPath.Music, "*" + SettingsManager.Instance.MusicExt).Count().ToString();
            int counter = 0;

            foreach (var fileName in files)
            {
                Audio.MusicCache[counter] = System.IO.Path.GetFileName(fileName);
                counter = counter + 1;
            }
        }

        public static void CacheSound()
        {
            Audio.SoundCache = new string[Directory.GetFiles(DataPath.Sounds, "*" + SettingsManager.Instance.SoundExt).Count() + 1];
            string[] files = Directory.GetFiles(DataPath.Sounds, "*" + SettingsManager.Instance.SoundExt);
            string maxNum = Directory.GetFiles(DataPath.Sounds, "*" + SettingsManager.Instance.SoundExt).Count().ToString();
            int counter = 0;

            foreach (var fileName in files)
            {
                Audio.SoundCache[counter] = System.IO.Path.GetFileName(fileName);
                counter = counter + 1;
            }
        }

        public static void GameInit()
        {
            // Send a request to the server to open the admin menu if the user wants it.
            if (SettingsManager.Instance.OpenAdminPanelOnLogin == true)
            {
                if (GetPlayerAccess(GameState.MyIndex) > 0)
                {
                    Sender.SendRequestAdmin();
                }
            }
        }

        public static void DestroyGame()
        {
            try
            {
                // Stop networking first so background I/O can shut down
                Network.Stop();
            }
            catch
            {
                // ignore shutdown errors
            }

            Cocca.OnExit();
        }

        // Get the shifted version of a digit key (for symbols)
        public static char GetShiftedDigit(char digit)
        {
            return digit switch
            {
                '1' => '!',
                '2' => '@',
                '3' => '#',
                '4' => '$',
                '5' => '%',
                '6' => '^',
                '7' => '&',
                '8' => '*',
                '9' => '(',
                '0' => ')',
                _ => digit
            };
        }

        public static int IsEq(long startX, long startY)
        {
            if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
                return -1;

            var player = Player.Instance[GameState.MyIndex];
            if (player.Paperdoll == null)
                return -1;

            var equipmentCount = Enum.GetValues<Equipment>().Length;
            var slotCount = Math.Min(equipmentCount, player.Paperdoll.Length);

            for (var i = 0; i < slotCount; i++)
            {
                if (player.Paperdoll[i].Num < 0)
                {
                    continue;
                }

                Type.Rect rec;

                rec.Top = startY + GameState.EqTop + (GameState.EqOffsetY + Constants.TileSize) * (i / GameState.EqColumns);
                rec.Bottom = rec.Top + Constants.TileSize;
                rec.Left = startX + GameState.EqLeft + (GameState.EqOffsetX + Constants.TileSize) * (i % GameState.EqColumns);
                rec.Right = rec.Left + Constants.TileSize;

                if (GameState.CurMouseX >= rec.Left && GameState.CurMouseX <= rec.Right &&
                    GameState.CurMouseY >= rec.Top && GameState.CurMouseY <= rec.Bottom)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IsInv(long startX, long startY)
        {
            if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
                return -1;

            var player = Player.Instance[GameState.MyIndex];
            if (player.Inventory == null)
                return -1;

            var slotCount = Math.Min((int)Variables.MaxInventory, player.Inventory.Length);
            for (var i = 0; i < slotCount; i++)
            {
                if (player.Inventory[i].Num < 0)
                    continue;

                Type.Rect rec;

                rec.Top = startY + GameState.InvTop + (GameState.InvOffsetY + Constants.TileSize) * (i / GameState.InvColumns);
                rec.Bottom = rec.Top + Constants.TileSize;
                rec.Left = startX + GameState.InvLeft + (GameState.InvOffsetX + Constants.TileSize) * (i % GameState.InvColumns);
                rec.Right = rec.Left + Constants.TileSize;

                if (GameState.CurMouseX >= rec.Left && GameState.CurMouseX <= rec.Right &&
                    GameState.CurMouseY >= rec.Top && GameState.CurMouseY <= rec.Bottom)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IsSkill(long startX, long startY)
        {
            int isSkill = default;
            Type.Rect tempRec;
            int i;

            for (i = 0; i < Variables.MaxPlayerSkills; i++)
            {
                if (Player.Instance[GameState.MyIndex].Skill[(int) i].Num >= 0)
                {
                    tempRec.Top = startY + GameState.SkillTop + (GameState.SkillOffsetY + Constants.TileSize) * (i / GameState.SkillColumns);
                    tempRec.Bottom = tempRec.Top + Constants.TileSize;
                    tempRec.Left = startX + GameState.SkillLeft + (GameState.SkillOffsetX + Constants.TileSize) * (i % GameState.SkillColumns);
                    tempRec.Right = tempRec.Left + Constants.TileSize;

                    if (GameState.CurMouseX >= tempRec.Left && GameState.CurMouseX <= tempRec.Right &&
                        GameState.CurMouseY >= tempRec.Top && GameState.CurMouseY <= tempRec.Bottom)
                    {
                        isSkill = i;
                        return isSkill;
                    }
                }
            }

            return -1;
        }

        public static int IsBank(long startX, long startY)
        {
            if (GameState.MyIndex < 0 || GameState.MyIndex >= Bank.Instance.Count)
                return -1;

            var bank = Bank.Instance[GameState.MyIndex];

            var slotCount = Math.Min((int)Variables.MaxBank, bank.Item.Length);
            for (var i = 0; i < slotCount; i++)
            {
                if (bank.Item[i].Num < 0)
                    continue;

                Type.Rect tempRec;
                tempRec.Top = startY + GameState.BankTop + (GameState.BankOffsetY + Constants.TileSize) * (i / GameState.BankColumns);
                tempRec.Bottom = tempRec.Top + Constants.TileSize;
                tempRec.Left = startX + GameState.BankLeft + (GameState.BankOffsetX + Constants.TileSize) * (i % GameState.BankColumns);
                tempRec.Right = tempRec.Left + Constants.TileSize;

                if (GameState.CurMouseX >= tempRec.Left && GameState.CurMouseX <= tempRec.Right &&
                    GameState.CurMouseY >= tempRec.Top && GameState.CurMouseY <= tempRec.Bottom)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IsShop(long startX, long startY)
        {
            // When selling, we will later use this slot as an inventory index.
            // Apply the same safety checks as IsInv so UI hover/click can't crash.
            if (GameState.ShopIsSelling)
            {
                if (GameState.MyIndex < 0 || GameState.MyIndex >= Player.Instance.Count)
                    return -1;

                var player = Player.Instance[GameState.MyIndex];
                if (player.Inventory == null)
                    return -1;
            }

            Type.Rect tempRec;

            for (var i = 0; i < Variables.MaxTrades; i++)
            {
                tempRec.Top = startY + GameState.ShopTop + (GameState.ShopOffsetY + Constants.TileSize) * (i / GameState.ShopColumns);
                tempRec.Bottom = tempRec.Top + Constants.TileSize;
                tempRec.Left = startX + GameState.ShopLeft + (GameState.ShopOffsetX + Constants.TileSize) * (i % GameState.ShopColumns);
                tempRec.Right = tempRec.Left + Constants.TileSize;

                if (GameState.CurMouseX >= tempRec.Left && GameState.CurMouseX <= tempRec.Right &&
                    GameState.CurMouseY >= tempRec.Top && GameState.CurMouseY <= tempRec.Bottom)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IsTrade(long startX, long startY)
        {
            Type.Rect tempRec;

            var slotCount = (int)Variables.MaxInventory;
            slotCount = Math.Min(slotCount, Data.TradeYourOffer.Length);
            slotCount = Math.Min(slotCount, Data.TradeTheirOffer.Length);

            for (var i = 0; i < slotCount; i++)
            {
                tempRec.Top = startY + GameState.TradeTop + (GameState.TradeOffsetY + Constants.TileSize) * (i / GameState.TradeColumns);
                tempRec.Bottom = tempRec.Top + Constants.TileSize;
                tempRec.Left = startX + GameState.TradeLeft + (GameState.TradeOffsetX + Constants.TileSize) * (i % GameState.TradeColumns);
                tempRec.Right = tempRec.Left + Constants.TileSize;

                if (GameState.CurMouseX >= tempRec.Left & GameState.CurMouseX <= tempRec.Right)
                {
                    if (GameState.CurMouseY >= tempRec.Top & GameState.CurMouseY <= tempRec.Bottom)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static bool IsDirBlocked(byte blockvar, Direction dir)
        {
            return dir switch
            {
                Direction.UpRight =>
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Up))) != 0 ||
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Right))) != 0,

                Direction.UpLeft =>
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Up))) != 0 ||
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Left))) != 0,

                Direction.DownRight =>
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Down))) != 0 ||
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Right))) != 0,

                Direction.DownLeft =>
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Down))) != 0 ||
                    (blockvar & (long)Math.Round(Math.Pow(2d, (double)Direction.Left))) != 0,

                _ => (blockvar & (long)Math.Round(Math.Pow(2d, (byte)dir))) != 0
            };
        }
    }
}