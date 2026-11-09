namespace ArmageddonOuttaHere.Core.Tests;

public class CrateTests
{
    private readonly Armory _armory = new();

    /// <summary>Plays back set chance rolls, and always picks the first item of a list.</summary>
    private sealed class ScriptedRandom : Random
    {
        private readonly Queue<double> _rolls;

        public ScriptedRandom(params double[] rolls) => _rolls = new Queue<double>(rolls);

        public override double NextDouble() => _rolls.Dequeue();

        public override int Next(int maxValue) => 0;
    }

    /// <summary>Gives the team <paramref name="copies"/> of every findable item except those listed.</summary>
    private void FillAllBut(int copies, params GearType[] except)
    {
        foreach (var type in GearCatalog.Findable.Except(except))
        {
            while (_armory.CopiesOf(type).Count() < copies)
            {
                _armory.Find(type);
            }
        }
    }

    [Fact]
    public void Picks_something_the_team_has_none_of_on_an_80_percent_roll()
    {
        FillAllBut(2, GearCatalog.Fridge);

        var item = SupplyCrate.Open(_armory, new ScriptedRandom(0.79));

        Assert.Equal(GearCatalog.Fridge, item!.Type);
    }

    [Fact]
    public void Missing_the_roll_moves_on_to_items_the_team_has_at_most_one_of()
    {
        FillAllBut(3, GearCatalog.Fridge, GearCatalog.Shotgun);
        _armory.Find(GearCatalog.Shotgun);

        // 0.9 misses "none of" (just the Fridge); 0.1 hits "at most 1" (the Fridge and the Shotgun).
        var item = SupplyCrate.Open(_armory, new ScriptedRandom(0.9, 0.1));

        Assert.Contains(item!.Type, new[] { GearCatalog.Fridge, GearCatalog.Shotgun });
    }

    [Fact]
    public void Empty_steps_are_skipped_without_a_roll()
    {
        // Nothing at 0 or 1 copies, so the first roll is for "at most 2".
        FillAllBut(3, GearCatalog.Rpg);
        _armory.Find(GearCatalog.Rpg);
        _armory.Find(GearCatalog.Rpg);

        var item = SupplyCrate.Open(_armory, new ScriptedRandom(0.5));

        Assert.Equal(GearCatalog.Rpg, item!.Type);
    }

    [Fact]
    public void When_every_roll_misses_it_picks_anything_with_fewer_than_four()
    {
        FillAllBut(3);

        var item = SupplyCrate.Open(_armory, new ScriptedRandom(0.99));

        Assert.NotNull(item);
        Assert.Equal(4, _armory.CopiesOf(item!.Type).Count());
    }

    [Fact]
    public void Never_gives_a_fifth_copy()
    {
        FillAllBut(4, GearCatalog.Minigun);
        while (_armory.CopiesOf(GearCatalog.Minigun).Count() < 3)
        {
            _armory.Find(GearCatalog.Minigun);
        }

        var item = SupplyCrate.Open(_armory, new ScriptedRandom(0.99, 0.99, 0.99, 0.99));

        Assert.Equal(GearCatalog.Minigun, item!.Type);
    }

    [Fact]
    public void No_crates_once_the_team_has_four_of_everything()
    {
        Assert.True(SupplyCrate.HasAnythingLeft(_armory));

        FillAllBut(4);

        Assert.False(SupplyCrate.HasAnythingLeft(_armory));
        Assert.Null(SupplyCrate.Open(_armory, new Random(1)));
    }

    [Fact]
    public void Crates_strongly_favor_missing_gear()
    {
        // Over many crates with half the gear missing, about 80% or more should be missing gear.
        var random = new Random(7);
        var missingPicks = 0;
        for (var i = 0; i < 1000; i++)
        {
            var armory = new Armory();
            foreach (var type in GearCatalog.Findable.Take(8))
            {
                armory.Find(type);
            }

            var item = SupplyCrate.Open(armory, random)!;
            if (armory.CopiesOf(item.Type).Count() == 1 && !GearCatalog.Findable.Take(8).Contains(item.Type))
            {
                missingPicks++;
            }
        }

        Assert.InRange(missingPicks, 780, 1000);
    }
}
