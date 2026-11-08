namespace ArmageddonOuttaHere.Core;

/// <summary>One save: the team's gear and every character. A new save starts with 4 characters, each with 0 scrap.</summary>
public sealed class SaveGame
{
    public const int StartingCharacters = 4;

    private readonly List<Character> _characters = new();

    public SaveGame(GearType starterGun)
    {
        StarterGun = starterGun;
        for (var i = 1; i <= StartingCharacters; i++)
        {
            CreateCharacter($"Player {i}");
        }
    }

    /// <summary>The basic gun every character has. Free, and not limited to 4 copies.</summary>
    public GearType StarterGun { get; }

    public Armory Armory { get; } = new();

    public IReadOnlyList<Character> Characters => _characters;

    /// <summary>The highest level the team can play. Beating a level unlocks the next.</summary>
    public int UnlockedLevel { get; private set; } = 1;

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
        if (!Armory.Items.Contains(item)
            || _characters.Any(c => c.Loadout.Contains(item))
            || !character.Loadout.CanFit(item.Slots))
        {
            return false;
        }

        character.Loadout.Add(item);
        return true;
    }

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
