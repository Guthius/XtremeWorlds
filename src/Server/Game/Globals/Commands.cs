namespace Server.Globals
{
    public static class Commands
    {
        public static void SetPlayerLogin(int index, string login)
        {
            if (Account.Instance == null || index < 0 || index >= Account.Instance.Count)
            {
                return;
            }

            Account.Instance[index].Login = login;
        }

        public static string GetPlayerPassword(int index)
        {
            if (Account.Instance == null || index < 0 || index >= Account.Instance.Count)
            {
                return string.Empty;
            }

            return Account.Instance[index].Password;
        }

        public static void SetPlayerPassword(int index, string password)
        {
            if (Account.Instance == null || index < 0 || index >= Account.Instance.Count)
            {
                return;
            }

            Account.Instance[index].Password = password;
        }

        public static string GetAccountLogin(int index)
        {
            if (Account.Instance == null || index < 0 || index >= Account.Instance.Count)
            {
                return string.Empty;
            }

            return Account.Instance[index].Login;
        }
    }
}