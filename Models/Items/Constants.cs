namespace pacman3.Utils
{
    public static class Constants
    {
        public const int TileSize = 32;
        public const int GameWidthTiles = 28;
        public const int GameHeightTiles = 31;
        public const double PlayerSpeed = 4.0;
        public const double GhostSpeed = 2.0;
        public const double GhostVulnerableSpeed = 1.5;
        public const double GhostDeadSpeed = 4.0;

        // Время действия энерджайзера в секундах
        public const int EnergizerDuration = 10;

        // Очки
        public const int DotPoints = 10;
        public const int EnergizerPoints = 50;
        public const int GhostPoints = 200;
    }
}