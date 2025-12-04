namespace Core.Interfaces
{
    public interface IData
    {
        static abstract void OnDraw(int index);
        static abstract void OnClear(int index);
        static abstract void OnStream(int index);
        static abstract void OnReset();
        static abstract void OnLoad(int index);
        static abstract void OnSave(int index);
    }

    public interface IAsyncData
    {
        static abstract Task OnLoadAllAsync();
        static abstract ValueTask OnLoadAsync(int index, CancellationToken cancellationToken);
    }
}