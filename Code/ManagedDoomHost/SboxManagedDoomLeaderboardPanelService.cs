namespace ManagedDoom
{
    public static class SboxManagedDoomLeaderboardPanelService
    {
        private static int version;

        public static bool IsOpen { get; private set; }
        public static bool IsLoading { get; set; }
        public static string Error { get; set; }
        public static string SelectedBoard { get; private set; } = "kills";
        public static PlayerProfile Profile { get; set; }
        public static PlayerStats Stats { get; set; }
        public static int Version => version;

        public static void Open()
        {
            IsOpen = true;
            version++;
        }

        public static void Close()
        {
            IsOpen = false;
            Error = null;
        }

        public static void SelectBoard(string board)
        {
            if (string.IsNullOrWhiteSpace(board))
            {
                return;
            }

            SelectedBoard = board;
            version++;
        }
    }
}
