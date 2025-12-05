namespace Core.Interfaces
{
    public interface IAsyncData
    {
        static abstract Task OnLoadAllAsync();
        static abstract ValueTask OnLoadAsync(int index, CancellationToken cancellationToken);
    }
}