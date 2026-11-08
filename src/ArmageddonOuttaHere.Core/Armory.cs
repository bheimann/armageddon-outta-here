namespace ArmageddonOuttaHere.Core;

/// <summary>The guns and suits the team has found. Shared by every character.</summary>
public sealed class Armory
{
    /// <summary>One copy per player, even when fewer than 4 are playing.</summary>
    public const int MaxCopies = 4;

    private readonly List<GearItem> _items = new();

    public IReadOnlyList<GearItem> Items => _items;

    public IEnumerable<GearItem> CopiesOf(GearType type) => _items.Where(i => i.Type == type);

    /// <summary>Adds a copy found in a supply crate. Returns null when the team already has <see cref="MaxCopies"/>.</summary>
    public GearItem? Find(GearType type)
    {
        if (CopiesOf(type).Count() >= MaxCopies)
        {
            return null;
        }

        var item = new GearItem(type);
        _items.Add(item);
        return item;
    }

    internal void AddLoaded(GearItem item) => _items.Add(item);
}
