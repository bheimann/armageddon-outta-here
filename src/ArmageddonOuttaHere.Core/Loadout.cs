namespace ArmageddonOuttaHere.Core;

/// <summary>The slot buttons, in the order gear fills them.</summary>
public enum SlotButton
{
    B,
    LB,
    RB,
    LT,
    RT,
}

/// <summary>
/// A character's 5 loadout slots. Gear fills them in order (B, LB, RB, LT, RT). A heavy gun, or a suit above
/// level 1, takes several. There's always at least one gun or suit equipped.
/// </summary>
public sealed class Loadout
{
    public const int SlotCount = 5;

    private readonly List<GearItem> _items = new();

    internal Loadout(GearItem first)
    {
        _items.Add(first);
    }

    public IReadOnlyList<GearItem> Items => _items;

    public int SlotsUsed => _items.Sum(i => i.Slots);

    public int FreeSlots => SlotCount - SlotsUsed;

    public bool Contains(GearItem item) => _items.Contains(item);

    /// <summary>
    /// What each slot button selects: the gear, and which of its attacks (always 0 for guns; 0 to level - 1 for a
    /// suit, one attack per slot).
    /// </summary>
    public IReadOnlyList<(SlotButton Button, GearItem Item, int Attack)> Buttons()
    {
        var result = new List<(SlotButton, GearItem, int)>();
        foreach (var item in _items)
        {
            for (var slot = 0; slot < item.Slots; slot++)
            {
                var attack = item.Type.Kind == GearKind.Suit ? slot : 0;
                result.Add(((SlotButton)result.Count, item, attack));
            }
        }

        return result;
    }

    internal bool CanFit(int slots) => slots <= FreeSlots;

    internal void Add(GearItem item) => _items.Add(item);

    /// <summary>Sets the whole loadout at once (loading a save).</summary>
    internal void Replace(IReadOnlyList<GearItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    internal bool Remove(GearItem item) => _items.Count > 1 && _items.Remove(item);
}
