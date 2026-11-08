using System.Text.Json;

namespace ArmageddonOuttaHere.Core;

/// <summary>Turns a <see cref="SaveGame"/> into JSON and back.</summary>
public static class SaveFile
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string ToJson(SaveGame save)
    {
        var data = new SaveData(
            save.UnlockedLevel,
            save.Armory.Items.Select(ToData).ToList(),
            save.Characters.Select(c => new CharacterData(
                c.Id,
                c.Name,
                c.Scrap,
                c.Appearance.Color,
                ToData(c.Starter),
                c.Loadout.Items.Select(i => i.Id).ToList()))
            .ToList());
        return JsonSerializer.Serialize(data, Options);
    }

    public static SaveGame FromJson(string json)
    {
        var data = JsonSerializer.Deserialize<SaveData>(json, Options)
            ?? throw new InvalidDataException("The save file is empty.");
        var save = SaveGame.Empty();
        save.UnlockedLevel = Math.Max(1, data.UnlockedLevel);

        var armory = new Dictionary<Guid, GearItem>();
        foreach (var itemData in data.Armory)
        {
            if (ToItem(itemData) is { } item)
            {
                armory[item.Id] = item;
                save.Armory.AddLoaded(item);
            }
        }

        foreach (var c in data.Characters)
        {
            // Each character's own pistol is stored with them; the loadout lists item ids, pointing at the pistol
            // or at team copies.
            var starter = ToItem(c.Starter with { Starter = true }) ?? new GearItem(GearType.Pistol, isStarter: true);
            var character = new Character(SaveGame.CheckName(c.Name), starter)
            {
                Id = c.Id,
                Scrap = Math.Max(0, c.Scrap),
                Appearance = new Appearance(c.Color),
            };

            var loadout = new List<GearItem>();
            foreach (var id in c.Loadout)
            {
                var item = id == starter.Id ? starter : armory.GetValueOrDefault(id);
                if (item is not null && !loadout.Contains(item) && save.EquippedBy(item) is null
                    && loadout.Sum(i => i.Slots) + item.Slots <= Loadout.SlotCount)
                {
                    loadout.Add(item);
                }
            }

            character.Loadout.Replace(loadout.Count > 0 ? loadout : new List<GearItem> { starter });
            save.AddLoaded(character);
        }

        return save;
    }

    private static ItemData ToData(GearItem item) => new(item.Id, item.Type.Id, item.Level, item.IsStarter);

    private static GearItem? ToItem(ItemData data)
    {
        if (data.Type is null || GearCatalog.ById(data.Type) is not { } type)
        {
            return null;
        }

        return new GearItem(type, data.Starter)
        {
            Id = data.Id,
            Level = Math.Clamp(data.Level, 1, GearType.MaxLevel),
        };
    }

    private sealed record SaveData(int UnlockedLevel, List<ItemData> Armory, List<CharacterData> Characters);

    private sealed record CharacterData(Guid Id, string Name, int Scrap, int Color, ItemData Starter, List<Guid> Loadout);

    private sealed record ItemData(Guid Id, string? Type, int Level, bool Starter);
}
