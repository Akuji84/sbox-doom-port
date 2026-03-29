namespace ManagedDoom
{
    public interface IAnalyticsListener
    {
        void OnNewGame(int episode, int map, GameSkill skill);
        void OnSaveGame(int slotNumber);
        void OnLoadGame(int slotNumber);
    }
}
