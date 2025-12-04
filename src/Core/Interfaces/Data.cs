namespace Core.Interfaces
{
    public interface IData
    {
        static abstract void OnDraw(int index);
        static abstract void OnClear(int index);
        static abstract void OnStream(int index);
        static abstract void OnReset();
        static abstract void OnLoad(int index);
    }
}