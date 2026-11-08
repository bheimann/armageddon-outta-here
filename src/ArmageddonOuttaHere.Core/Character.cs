namespace ArmageddonOuttaHere.Core;

/// <summary>A player's character. Scrap belongs to the character; gear belongs to the team.</summary>
public sealed class Character
{
    internal Character(string name, GearItem starterGun)
    {
        Name = name;
        Loadout = new Loadout(starterGun);
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; private set; }

    public int Scrap { get; internal set; }

    public Appearance Appearance { get; set; } = new();

    public Loadout Loadout { get; }

    public void Rename(string name) => Name = SaveGame.CheckName(name);

    public void AddScrap(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Scrap += amount;
    }
}

/// <summary>How a character looks. Filled in when the wardrobe is built.</summary>
public sealed record Appearance(int Color = 0);
