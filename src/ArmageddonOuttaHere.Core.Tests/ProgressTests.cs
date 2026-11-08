namespace ArmageddonOuttaHere.Core.Tests;

public class ProgressTests
{
    [Fact]
    public void Crate_gives_gear_the_team_has_fewer_than_four_of()
    {
        var armory = new Armory();
        var random = new Random(1);

        var loot = SupplyCrate.Open(armory, random);

        Assert.NotNull(loot.Item);
        Assert.NotEqual(GearType.Pistol, loot.Item!.Type);
        Assert.Contains(loot.Item, armory.Items);
    }

    [Fact]
    public void Crate_gives_scrap_when_the_team_has_four_of_everything()
    {
        var armory = new Armory();
        foreach (var type in GearCatalog.Findable)
        {
            for (var i = 0; i < Armory.MaxCopies; i++)
            {
                armory.Find(type);
            }
        }

        var loot = SupplyCrate.Open(armory, new Random(1));

        Assert.Null(loot.Item);
        Assert.Equal(SupplyCrate.ScrapWhenFull, loot.Scrap);
    }

    [Fact]
    public void Waves_grow_and_more_players_bring_more_enemies()
    {
        var level = Levels.Get(1);

        var solo = Levels.WavesFor(level, 1);
        var four = Levels.WavesFor(level, 4);

        Assert.Equal(level.Waves, solo.Count);
        Assert.True(solo[1].Total > solo[0].Total);
        Assert.True(four[0].Total > solo[0].Total);
    }

    [Fact]
    public void Later_levels_mix_in_earlier_enemies()
    {
        var wave = Levels.WavesFor(Levels.Get(3), 1)[0];

        Assert.Equal(
            new HashSet<EnemyKind> { EnemyKind.Raccoon, EnemyKind.Zombie, EnemyKind.Alien },
            wave.Enemies.Keys.ToHashSet());
        Assert.True(wave.Enemies[EnemyKind.Alien] > wave.Enemies[EnemyKind.Raccoon]);
    }

    [Fact]
    public void Save_round_trips_characters_gear_and_progress()
    {
        var save = new SaveGame();
        var dusty = save.CreateCharacter("Dusty");
        dusty.AddScrap(120);
        dusty.Appearance = new Appearance(3);
        var fridge = save.Armory.Find(GearCatalog.Fridge)!;
        save.Equip(dusty, fridge);
        Workbench.Upgrade(dusty, fridge);
        save.Armory.Find(GearCatalog.Minigun);
        save.LevelBeaten(1);

        var loaded = SaveFile.FromJson(SaveFile.ToJson(save));

        Assert.Equal(2, loaded.UnlockedLevel);
        Assert.Equal(5, loaded.Characters.Count);
        var loadedDusty = loaded.Characters.Single(c => c.Name == "Dusty");
        Assert.Equal(dusty.Id, loadedDusty.Id);
        Assert.Equal(70, loadedDusty.Scrap);
        Assert.Equal(3, loadedDusty.Appearance.Color);
        Assert.Equal(new[] { "pistol", "fridge" }, loadedDusty.Loadout.Items.Select(i => i.Type.Id));
        var loadedFridge = loadedDusty.Loadout.Items[1];
        Assert.Equal(2, loadedFridge.Level);
        Assert.Same(loadedFridge, loaded.Armory.CopiesOf(GearCatalog.Fridge).Single());
        Assert.Equal(2, loaded.Armory.Items.Count);
    }
}
