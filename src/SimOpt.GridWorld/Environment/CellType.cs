namespace SimOpt.GridWorld.Environment;

public enum CellType
{
    Empty,
    Wall,
    Hazard,
    Resource
}

public record CellInfo(
    CellType Type,
    string? HazardFamily = null,
    string? CausalMechanism = null)
{
    public static readonly CellInfo EmptyCell = new(CellType.Empty);
    public static readonly CellInfo WallCell = new(CellType.Wall);
    public static readonly CellInfo ResourceCell = new(CellType.Resource);

    public static CellInfo HazardCell(string family, string mechanism) =>
        new(CellType.Hazard, family, mechanism);
}

public static class HazardFamilies
{
    public const string Lava = "lava";
    public const string DeepWater = "deep_water";
    public const string Cliff = "cliff";
    public const string Predator = "predator";
}

public static class CausalMechanisms
{
    public const string Thermal = "thermal";
    public const string Submersion = "submersion";
    public const string Fall = "fall";
    public const string Predation = "predation";
}
