namespace Server.Globals
{
    public static class Commands
    {
        public static void SetPlayerLogin(int index, string login)
        {
            Account.Instance[index].Login = login;
        }

        public static string GetPlayerPassword(int index)
        {
            return Account.Instance[index].Password;
        }

        public static void SetPlayerPassword(int index, string password)
        {
            Account.Instance[index].Password = password;
        }

        public static string GetAccountLogin(int index)
        {
            return Account.Instance[index].Login;
        }
    }
}