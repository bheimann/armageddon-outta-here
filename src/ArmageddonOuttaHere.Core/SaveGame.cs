namespace ArmageddonOuttaHere.Core;

/// <summary>One save: the team's gear and every character. A new save starts with 4 characters, each with 0 scrap.</summary>
public sealed class SaveGame
{
    public const int StartingCharacters = 4;

    private readonly List<Character> _characters = new();

    public SaveGame()
        : this(withStartingCharacters: true)
    {
    }

    private SaveGame(bool withStartingCharacters)
    {
        if (!withStartingCharacters)
        {
            return;
        }

        for (var i = 1; i <= StartingCharacters; i++)
        {
            CreateCharacter($"Player {i}");
        }
    }

    /// <summary>An empty save for <see cref="SaveFile"/> to fill in.</summary>
    internal static SaveGame Empty() => new(withStartingCharacters: false);

    internal void AddLoaded(Character character) => _characters.Add(character);

    /// <summary>The gun every character starts with: a pistol. Free, and not limited to 4 copies.</summary>
    public GearType StarterGun => GearType.Pistol;

    public Armory Armory { get; } = new();

    public IReadOnlyList<Character> Characters => _characters;

    /// <summary>The highest level the team can play. Beating a level unlocks the next.</summary>
    public int UnlockedLevel { get; internal set; } = 1;

    /// <summary>The character using a team copy, or null when nobody has it equipped.</summary>
    public Character? EquippedBy(GearItem item) => _characters.FirstOrDefault(c => c.Loadout.Contains(item));

    public Character CreateCharacter(string name)
    {
        var character = new Character(CheckName(name), new GearItem(StarterGun, isStarter: true));
        _characters.Add(character);
        return character;
    }

    public void LevelBeaten(int level) => UnlockedLevel = Math.Max(UnlockedLevel, level + 1);

    /// <summary>Puts a team copy into a character's loadout. Fails if it doesn't fit or another character has it.</summary>
    public bool Equip(Character character, GearItem item)
    {
        var owned = Armory.Items.Contains(item) || item == character.Starter;
        if (!owned || EquippedBy(item) is not null || !character.Loadout.CanFit(item.Slots))
        {
            return false;
        }

        character.Loadout.Add(item);
        return true;
    }

    /// <summary>What a character could put in their loadout: their own pistol and the team's free copies.</summary>
    public IEnumerable<GearItem> Equippable(Character character) =>
        Armory.Items.Prepend(character.Starter).Where(i => EquippedBy(i) is null);

    /// <summary>Takes gear out of a loadout. Fails if it's the last thing equipped.</summary>
    public bool Unequip(Character character, GearItem item) => character.Loadout.Remove(item);

    internal static string CheckName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A character needs a name.", nameof(name));
        }

        return name.Trim();
    }
}
