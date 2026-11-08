namespace ArmageddonOuttaHere.Core;

public enum GearKind
{
    /// <summary>Takes 1 loadout slot.</summary>
    Gun,

    /// <summary>Minigun, RPG, grenade launcher, flamethrower: takes 2 loadout slots.</summary>
    HeavyGun,

    /// <summary>Takes 1 loadout slot per level, and has 1 attack per level.</summary>
    Suit,
}

/// <summary>A kind of gun or suit, such as "The Fridge". The team can own up to <see cref="Armory.MaxCopies"/> copies of it.</summary>
public sealed record GearType(string Id, string Name, GearKind Kind)
{
    public const int MaxLevel = 4;

    /// <summary>Every character starts with one.</summary>
    public static readonly GearType Pistol = new("pistol", "Pistol", GearKind.Gun);
}

/// <summary>One copy of a gun or suit owned by the team. Each copy keeps its own upgrades.</summary>
public sealed class GearItem
{
    internal GearItem(GearType type, bool isStarter = false)
    {
        Type = type;
        IsStarter = isStarter;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    public GearType Type { get; }

    /// <summary>1 to <see cref="GearType.MaxLevel"/>.</summary>
    public int Level { get; internal set; } = 1;

    /// <summary>The free basic gun every character always has. It doesn't count against the team's copies.</summary>
    public bool IsStarter { get; }

    /// <summary>Loadout slots this takes at its current level.</summary>
    public int Slots => SlotsAt(Level);

    internal int SlotsAt(int level) => Type.Kind switch
    {
        GearKind.Gun => 1,
        GearKind.HeavyGun => 2,
        _ => level,
    };
}
