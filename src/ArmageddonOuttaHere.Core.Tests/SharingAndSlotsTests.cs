namespace ArmageddonOuttaHere.Core.Tests;

public sealed class SharingAndSlotsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "aoh-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Scrap_is_split_evenly_and_leftovers_carry_over()
    {
        var save = new SaveGame();
        var players = save.Characters.Take(3).ToList();
        var pot = new ScrapPot();

        pot.Share(10, players);

        Assert.All(players, p => Assert.Equal(3, p.Scrap));

        pot.Share(2, players);

        Assert.All(players, p => Assert.Equal(4, p.Scrap));
        Assert.Equal(0, save.Characters[3].Scrap);
    }

    [Fact]
    public void Empty_slot_starts_a_new_save()
    {
        var slots = new SaveSlots(_folder);

        Assert.True(slots.Describe(2).IsEmpty);
        Assert.Equal(4, slots.Load(2).Characters.Count);
    }

    [Fact]
    public void Slots_save_separately_and_remember_the_last_one_used()
    {
        var slots = new SaveSlots(_folder);
        var save = new SaveGame();
        save.CreateCharacter("Dusty");
        save.LevelBeaten(1);

        slots.Save(3, save);
        slots.LastUsed = 3;

        var reopened = new SaveSlots(_folder);
        Assert.Equal(3, reopened.LastUsed);
        Assert.Equal(new SlotSummary(3, false, 5, 2), reopened.Describe(3));
        Assert.True(reopened.Describe(1).IsEmpty);
    }

    [Fact]
    public void Last_used_defaults_to_slot_one()
    {
        Assert.Equal(1, new SaveSlots(_folder).LastUsed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Only_slots_one_to_five_exist(int slot)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SaveSlots(_folder).Load(slot));
    }
}
