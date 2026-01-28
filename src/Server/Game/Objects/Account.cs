using Core.Globals;
using Core.Interfaces;
using Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Server.Globals.Commands;
using static Server.Database;

namespace Server
{
    public class Account : IData, IAsyncData
    {
        public string Login;
        public string Password;
        public bool Banned;
        public Player[] Player;
        public Bank[] Bank;

        public static List<Account> Instance { get; set; } = new List<Account>();

        public static void EnsureSize(int size)
        {
            if (size <= 0)
            {
                return;
            }

            if (Instance.Count >= size)
            {
                return;
            }

            lock (Instance)
            {
                while (Instance.Count < size)
                {
                    Instance.Add(new Account());
                }
            }
        }

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
                Data.TempPlayer = new Core.Globals.Type.TempPlayer[Core.Globals.Variables.MaxPlayers];

            Data.TempPlayer[index].SkillCd = new int[Core.Globals.Variables.MaxPlayerSkills];
            Data.TempPlayer[index].TradeOffer = new Core.Globals.Type.Item[Core.Globals.Variables.MaxInventory];
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
            if (index < 0 || index >= Instance.Count)
                return;
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

        public static void OnClear()
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
            JObject data;
            data = SelectRowByColumn("id", GetStringHash(GetAccountLogin(index)), "account", "data");

            if (data is null)
            {
                Account.OnClear(index);
                return;
            }

            var accountData = JObject.FromObject(data).ToObject<Account>();
            if (accountData is null)
            {
                Account.OnClear(index);
                return;
            }

            Account.Instance[index] = accountData;
        }

        public static Task OnLoadAllAsync()
        {
            EnsureSize(Core.Globals.Variables.MaxPlayers);
            return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxPlayers), OnLoadAsync);
        }

        static void IData.OnSave(int index)
        {
            throw new NotImplementedException();
        }
    }
}