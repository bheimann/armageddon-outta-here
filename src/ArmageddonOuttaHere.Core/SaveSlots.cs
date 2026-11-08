namespace ArmageddonOuttaHere.Core;

/// <summary>A short description of a save slot for the filing cabinet menu.</summary>
public sealed record SlotSummary(int Slot, bool IsEmpty, int Characters, int UnlockedLevel);

/// <summary>
/// The 5 save games, stored as JSON files in one folder. The game continues the last slot used.
/// </summary>
public sealed class SaveSlots
{
    public const int Count = 5;

    private readonly string _folder;

    public SaveSlots(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(folder);
    }

    /// <summary>The slot the game continues (1 to <see cref="Count"/>).</summary>
    public int LastUsed
    {
        get
        {
            var path = Path.Combine(_folder, "last-slot.txt");
            return File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out var slot) && IsValid(slot)
                ? slot
                : 1;
        }

        set
        {
            Check(value);
            File.WriteAllText(Path.Combine(_folder, "last-slot.txt"), value.ToString());
        }
    }

    /// <summary>Loads a slot, or starts a new save game when the slot is empty.</summary>
    public SaveGame Load(int slot)
    {
        Check(slot);
        var path = PathFor(slot);
        return File.Exists(path) ? SaveFile.FromJson(File.ReadAllText(path)) : new SaveGame();
    }

    public void Save(int slot, SaveGame save)
    {
        Check(slot);
        var path = PathFor(slot);
        var temp = path + ".tmp";
        File.WriteAllText(temp, SaveFile.ToJson(save));
        File.Move(temp, path, overwrite: true);
    }

    public SlotSummary Describe(int slot)
    {
        Check(slot);
        if (!File.Exists(PathFor(slot)))
        {
            return new SlotSummary(slot, true, SaveGame.StartingCharacters, 1);
        }

        var save = Load(slot);
        return new SlotSummary(slot, false, save.Characters.Count, save.UnlockedLevel);
    }

    private string PathFor(int slot) => Path.Combine(_folder, $"save-{slot}.json");

    private static bool IsValid(int slot) => slot is >= 1 and <= Count;

    private static void Check(int slot)
    {
        if (!IsValid(slot))
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Save slots are 1 to {Count}.");
        }
    }
}
