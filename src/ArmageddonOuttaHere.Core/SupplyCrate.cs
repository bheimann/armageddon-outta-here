namespace ArmageddonOuttaHere.Core;

/// <summary>
/// What's in a supply crate. Crates strongly favor gear the team has the fewest of:
/// <list type="number">
/// <item>An 80% chance of something the team has none of.</item>
/// <item>If not, an 80% chance of something it has at most 1 of.</item>
/// <item>Then at most 2, then at most 3, each with an 80% chance.</item>
/// <item>If none of those hit, anything the team has fewer than 4 of.</item>
/// </list>
/// A step with nothing in it is skipped. Once the team has 4 of everything, crates stop dropping.
/// </summary>
public static class SupplyCrate
{
    /// <summary>The chance of taking each step's pick before moving on to the next step.</summary>
    public const double TierChance = 0.8;

    /// <summary>False once the team has 4 copies of everything; then no more crates drop.</summary>
    public static bool HasAnythingLeft(Armory armory) => Available(armory).Any();

    /// <summary>Opens a crate: adds a gun or suit to the team. Null only when the team already has everything.</summary>
    public static GearItem? Open(Armory armory, Random random)
    {
        var available = Available(armory).ToList();
        if (available.Count == 0)
        {
            return null;
        }

        for (var most = 0; most < Armory.MaxCopies - 1; most++)
        {
            var tier = available.Where(t => t.Copies <= most).ToList();
            if (tier.Count > 0 && random.NextDouble() < TierChance)
            {
                return armory.Find(tier[random.Next(tier.Count)].Type);
            }
        }

        return armory.Find(available[random.Next(available.Count)].Type);
    }

    private static IEnumerable<(GearType Type, int Copies)> Available(Armory armory) =>
        GearCatalog.Findable
            .Select(t => (t, armory.CopiesOf(t).Count()))
            .Where(x => x.Item2 < Armory.MaxCopies);
}
