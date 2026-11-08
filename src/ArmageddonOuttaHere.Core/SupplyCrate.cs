namespace ArmageddonOuttaHere.Core;

/// <summary>What came out of a supply crate: a new copy of a gun or suit, or scrap when the team has 4 of everything.</summary>
public sealed record CrateLoot(GearItem? Item, int Scrap);

public static class SupplyCrate
{
    /// <summary>Scrap given instead when the team already has 4 copies of everything.</summary>
    public const int ScrapWhenFull = 40;

    /// <summary>Opens a crate: adds a random gun or suit the team doesn't have 4 copies of yet.</summary>
    public static CrateLoot Open(Armory armory, Random random)
    {
        var candidates = GearCatalog.Findable
            .Where(t => armory.CopiesOf(t).Count() < Armory.MaxCopies)
            .ToList();
        if (candidates.Count == 0)
        {
            return new CrateLoot(null, ScrapWhenFull);
        }

        var type = candidates[random.Next(candidates.Count)];
        return new CrateLoot(armory.Find(type), 0);
    }
}
