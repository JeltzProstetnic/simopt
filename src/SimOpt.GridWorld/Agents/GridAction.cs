namespace SimOpt.GridWorld.Agents;

public static class Actions
{
    public const int Stay = 0;

    public static class Rect
    {
        public const int Stay = 0;
        public const int North = 1;
        public const int South = 2;
        public const int East = 3;
        public const int West = 4;
    }

    public static class Hex
    {
        public const int Stay = 0;
        public const int East = 1;
        public const int NorthEast = 2;
        public const int NorthWest = 3;
        public const int West = 4;
        public const int SouthWest = 5;
        public const int SouthEast = 6;
    }

    public static class Cubic
    {
        public const int Stay = 0;
        public const int North = 1;
        public const int South = 2;
        public const int East = 3;
        public const int West = 4;
        public const int Up = 5;
        public const int Down = 6;
    }
}
