namespace ArmageddonOuttaHere.Core;

public enum UpgradeResult
{
    Upgraded,
    NotEquipped,
    MaxLevel,
    NotEnoughScrap,

    /// <summary>The suit's next level needs another loadout slot and none is free.</summary>
    NoRoom,
}

/// <summary>Upgrades gear with the character's own scrap. Only gear the character has equipped can be upgraded.</summary>
public static class Workbench
{
    /// <summary>Scrap to go from <paramref name="level"/> to the next. Placeholder numbers until balancing.</summary>
    public static int Cost(int level) => level * 50;

    public static UpgradeResult Upgrade(Character character, GearItem item)
    {
        if (!character.Loadout.Contains(item))
        {
            return UpgradeResult.NotEquipped;
        }

        if (item.Level >= GearType.MaxLevel)
        {
            return UpgradeResult.MaxLevel;
        }

        var cost = Cost(item.Level);
        if (character.Scrap < cost)
        {
            return UpgradeResult.NotEnoughScrap;
        }

        if (!character.Loadout.CanFit(item.SlotsAt(item.Level + 1) - item.Slots))
        {
            return UpgradeResult.NoRoom;
        }

        character.Scrap -= cost;
        item.Level++;
        return UpgradeResult.Upgraded;
    }
}
