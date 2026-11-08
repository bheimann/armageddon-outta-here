namespace ArmageddonOuttaHere.Core;

/// <summary>Every gun and suit in the game.</summary>
public static class GearCatalog
{
    public static readonly GearType NailGun = new("nail-gun", "Nail Gun", GearKind.Gun);
    public static readonly GearType Shotgun = new("shotgun", "Shotgun", GearKind.Gun);
    public static readonly GearType RayGun = new("ray-gun", "Ray Gun", GearKind.Gun);

    public static readonly GearType Minigun = new("minigun", "Minigun", GearKind.HeavyGun);
    public static readonly GearType Rpg = new("rpg", "RPG", GearKind.HeavyGun);
    public static readonly GearType GrenadeLauncher = new("grenade-launcher", "Grenade Launcher", GearKind.HeavyGun);
    public static readonly GearType Flamethrower = new("flamethrower", "Flamethrower", GearKind.HeavyGun);

    public static readonly GearType Fridge = new("fridge", "The Fridge", GearKind.Suit);
    public static readonly GearType LawnmowerLarry = new("lawnmower-larry", "Lawnmower Larry", GearKind.Suit);
    public static readonly GearType Mailbox = new("mailbox", "Mailbox Mk II", GearKind.Suit);
    public static readonly GearType GrillSergeant = new("grill-sergeant", "Grill Sergeant", GearKind.Suit);
    public static readonly GearType ShoppingCart = new("shopping-cart", "The Shopping Cart", GearKind.Suit);
    public static readonly GearType Sparky = new("sparky", "Prototype X-9 \"Sparky\"", GearKind.Suit);
    public static readonly GearType HoverCommander = new("hover-commander", "The Hover Commander", GearKind.Suit);
    public static readonly GearType ProbeReverser = new("probe-reverser", "Probe Reverser", GearKind.Suit);

    public static IReadOnlyList<GearType> All { get; } = new[]
    {
        GearType.Pistol, NailGun, Shotgun, RayGun,
        Minigun, Rpg, GrenadeLauncher, Flamethrower,
        Fridge, LawnmowerLarry, Mailbox, GrillSergeant, ShoppingCart, Sparky, HoverCommander, ProbeReverser,
    };

    /// <summary>What supply crates can hold: everything except the pistol, which every character already has.</summary>
    public static IEnumerable<GearType> Findable => All.Where(t => t != GearType.Pistol);

    public static GearType? ById(string id) => All.FirstOrDefault(t => t.Id == id);
}
