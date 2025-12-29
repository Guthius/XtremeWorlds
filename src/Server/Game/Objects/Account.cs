using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using Newtonsoft.Json.Linq;
using Server.Game.Net;
using static Server.Globals.Commands;
using static Server.Database;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace Server
{
    public class Account : IAsyncData
    {
        public string Login;
        public string Password;
        public bool Banned;
        public Player[] Player;
        public Bank[] Bank;

        public static List<Account> Instance { get; set; } = new List<Account>();

        public Account()
        {
            Login = "";
            Password = "";
            Player = new Player[Core.Globals.Variables.MaxCharacters];
            for (int i = 0; i < Core.Globals.Variables.MaxCharacters; i++)
            {
                Player[i] = new Player();
            }
            Bank = new Bank[Core.Globals.Variables.MaxBank];
            for (int i = 0; i < Core.Globals.Variables.MaxBank; i++)
            {
                Bank[i] = new Bank();
            }

            int index = Instance.Count;
            if (Data.TempPlayer == null)
                Data.TempPlayer = new Core.Globals.Type.TempPlayer[Variables.MaxPlayers];

            Data.TempPlayer[index].SkillCd = new int[Variables.MaxPlayerSkills];
            Data.TempPlayer[index].TradeOffer = new Core.Globals.Type.Item[Variables.MaxInventory];
            Data.TempPlayer[index].Editor = EditorType.None;
            Data.TempPlayer[index].SkillBuffer = -1;
            Data.TempPlayer[index].InShop = -1;
            Data.TempPlayer[index].InTrade = 0;
            Data.TempPlayer[index].InParty = -1;

            Data.TempPlayer[index].MoveSpeedMultiplier = 1.0f;
            Data.TempPlayer[index].MoveSpeedMultiplierTimer = 0;
        }

        public static void OnClear(int index)
        {
            if (Instance.Count > index)
                Instance[index] = new Account();
        }

        public static void OnDraw(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnLoad(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnReset()
        {
            throw new NotImplementedException();
        }

        public static async Task OnSave(int index)
        {
            var json = JsonConvert.SerializeObject(Account.Instance[index]).ToString();
            var login = GetAccountLogin(index);
            var id = GetStringHash(login);

            if (await RowExistsAsync(id, "account"))
            {
                await UpdateRowByColumnAsync("id", id, "data", json, "account");
            }
            else
            {
                await InsertRowByColumnAsync(id, json, "account", "data", "id");
            }
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }

        public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
        {
            string login = GetAccountLogin(index);
            JObject data;
            data = SelectRowByColumn("id", GetStringHash(login), "account", "data");

            if (data is null)
            {
                Account.OnClear(index);
                return;
            }

            var accountData = JObject.FromObject(data).ToObject<Account>();
            Account.Instance[index] = accountData;
        }

        public static Task OnLoadAllAsync()
        {
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxPlayers), OnLoadAsync);
        }
    }
}