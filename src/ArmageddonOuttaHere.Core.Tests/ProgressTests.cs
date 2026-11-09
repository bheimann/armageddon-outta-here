namespace ArmageddonOuttaHere.Core.Tests;

public class ProgressTests
{
    [Fact]
    public void Waves_grow_and_more_players_bring_more_enemies()
    {
        var level = LevelCatalog.Get(1);

        var solo = LevelCatalog.WavesFor(level, 1);
        var four = LevelCatalog.WavesFor(level, 4);

        Assert.Equal(level.Waves, solo.Count);
        Assert.True(solo[1].Total > solo[0].Total);
        Assert.True(four[0].Total > solo[0].Total);
    }

    [Fact]
    public void Later_levels_mix_in_earlier_enemies()
    {
        var wave = LevelCatalog.WavesFor(LevelCatalog.Get(3), 1)[0];

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

    [Fact]
    public void Save_keeps_an_unequipped_pistol()
    {
        var save = new SaveGame();
        var first = save.Characters[0];
        save.Equip(first, save.Armory.Find(GearCatalog.Minigun)!);
        save.Unequip(first, first.Starter);

        var loaded = SaveFile.FromJson(SaveFile.ToJson(save)).Characters[0];

        Assert.Equal(new[] { "minigun" }, loaded.Loadout.Items.Select(i => i.Type.Id));
        Assert.Equal(first.Starter.Id, loaded.Starter.Id);
        Assert.Equal(GearType.Pistol, loaded.Starter.Type);
    }
}
