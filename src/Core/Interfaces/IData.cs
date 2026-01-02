namespace Core.Interfaces
{
    public interface IData
    {
        static abstract void OnDraw(int index);
        static abstract void OnClear(int index);
        static abstract void OnClear();
        static abstract void OnLoad(int index);
        static abstract void OnSave(int index);
        static abstract void OnUpdate(int index);
    }
}