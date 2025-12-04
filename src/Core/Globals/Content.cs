namespace Core.Globals
{
    public interface IContent
    {
        // Backing data from your Data.* arrays
        struct Data { get; set; }

        // Move logic
        void OnMove(int index);

        // Initialization logic
        void OnLoad(int index);

        // Clear logic
        void OnClear(int index);

        // Stream logic
        void OnStream(int index);

        // Reset logic
        void OnReset();

        // Per-frame logic
        void OnUpdate(float deltaTime);

        // Per-frame rendering
        void OnDraw(int index);
    }
}