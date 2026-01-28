using Core.Interfaces;
using Core.Globals;

namespace Core.Objects
{
    public class Bank : IData, IAsyncData
    {
        public static List<Bank> Instance { get; set; } = new List<Bank>();
        public Core.Globals.Type.Item[] Item;

        public Bank()
        {
            Item = new  Core.Globals.Type.Item[Core.Globals.Variables.MaxBank];
            for (int i = 0; i < Core.Globals.Variables.MaxBank; i++)
            {
                Item[i] = new Core.Globals.Type.Item();
                Item[i].Num = -1;
            }
        }

        public static void OnClear(int index)
        {
            if (index < 0 || index >= Instance.Count)
                return;
            Instance[index] = new Bank();
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
            for (int i = 0; i < Core.Globals.Variables.MaxBank; i++)
                OnClear(i);
        }

        public static void OnSave(int index)
        {
            throw new NotImplementedException();
        }

        public static void OnUpdate(int index)
        {
            throw new NotImplementedException();
        }

        public static System.Threading.Tasks.Task OnLoadAllAsync()
        {
            throw new NotImplementedException();
        }

        public static ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}