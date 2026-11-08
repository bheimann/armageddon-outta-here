namespace ArmageddonOuttaHere.Core.Tests;

public class RulesTests
{
    private static readonly GearType NailGun = new("nail-gun", "Nail Gun", GearKind.Gun);
    private static readonly GearType Minigun = new("minigun", "Minigun", GearKind.HeavyGun);
    private static readonly GearType Fridge = new("fridge", "The Fridge", GearKind.Suit);

    private readonly SaveGame _save = new();

    private Character First => _save.Characters[0];

    [Fact]
    public void New_save_has_four_characters_with_no_scrap_and_a_pistol()
    {
        Assert.Equal(4, _save.Characters.Count);
        Assert.All(_save.Characters, c => Assert.Equal(0, c.Scrap));
        Assert.All(_save.Characters, c => Assert.Equal(GearType.Pistol, Assert.Single(c.Loadout.Items).Type));
    }

    [Fact]
    public void New_character_starts_with_a_pistol()
    {
        var character = _save.CreateCharacter("Dusty");

        Assert.Equal(GearType.Pistol, Assert.Single(character.Loadout.Items).Type);
    }

    [Fact]
    public void New_character_needs_a_name_and_starts_with_no_scrap()
    {
        Assert.Throws<ArgumentException>(() => _save.CreateCharacter("  "));

        var character = _save.CreateCharacter(" Dusty ");

        Assert.Equal("Dusty", character.Name);
        Assert.Equal(0, character.Scrap);
        Assert.Equal(5, _save.Characters.Count);
    }

    [Fact]
    public void Team_can_find_up_to_four_copies_of_a_suit()
    {
        for (var i = 0; i < 4; i++)
        {
            Assert.NotNull(_save.Armory.Find(Fridge));
        }

        Assert.Null(_save.Armory.Find(Fridge));
        Assert.Equal(4, _save.Armory.CopiesOf(Fridge).Count());
    }

    [Fact]
    public void A_copy_can_only_be_in_one_loadout()
    {
        var fridge = _save.Armory.Find(Fridge)!;

        Assert.True(_save.Equip(First, fridge));
        Assert.False(_save.Equip(_save.Characters[1], fridge));
    }

    [Fact]
    public void Heavy_guns_take_two_slots_and_the_loadout_holds_five()
    {
        Assert.True(_save.Equip(First, _save.Armory.Find(Minigun)!));
        Assert.True(_save.Equip(First, _save.Armory.Find(Minigun)!));

        Assert.Equal(5, First.Loadout.SlotsUsed);
        Assert.False(_save.Equip(First, _save.Armory.Find(NailGun)!));
    }

    [Fact]
    public void Each_suit_level_takes_a_slot_button_with_its_own_attack()
    {
        var fridge = _save.Armory.Find(Fridge)!;
        _save.Equip(First, fridge);
        First.AddScrap(1000);
        Workbench.Upgrade(First, fridge);
        Workbench.Upgrade(First, fridge);

        var buttons = First.Loadout.Buttons();

        Assert.Equal(
            new[] { (SlotButton.B, 0), (SlotButton.LB, 0), (SlotButton.RB, 1), (SlotButton.LT, 2) },
            buttons.Select(b => (b.Button, b.Attack)));
        Assert.All(buttons.Skip(1), b => Assert.Same(fridge, b.Item));
    }

    [Fact]
    public void Pistol_can_be_unequipped_to_make_room_and_equipped_again()
    {
        var pistol = First.Starter;
        _save.Equip(First, _save.Armory.Find(Fridge)!);

        Assert.True(_save.Unequip(First, pistol));
        Assert.Contains(pistol, _save.Equippable(First));
        Assert.DoesNotContain(pistol, _save.Equippable(_save.Characters[1]));

        Assert.True(_save.Equip(First, pistol));
        Assert.False(_save.Equip(_save.Characters[1], pistol));
    }

    [Fact]
    public void Last_piece_of_gear_cannot_be_unequipped()
    {
        Assert.False(_save.Unequip(First, First.Loadout.Items[0]));
    }

    [Fact]
    public void Upgrading_spends_the_characters_own_scrap()
    {
        var fridge = _save.Armory.Find(Fridge)!;
        _save.Equip(First, fridge);
        First.AddScrap(60);

        Assert.Equal(UpgradeResult.Upgraded, Workbench.Upgrade(First, fridge));

        Assert.Equal(2, fridge.Level);
        Assert.Equal(10, First.Scrap);
        Assert.Equal(0, _save.Characters[1].Scrap);
    }

    [Fact]
    public void Only_equipped_gear_can_be_upgraded()
    {
        var fridge = _save.Armory.Find(Fridge)!;
        First.AddScrap(1000);

        Assert.Equal(UpgradeResult.NotEquipped, Workbench.Upgrade(First, fridge));
    }

    [Fact]
    public void Upgrade_fails_without_enough_scrap()
    {
        var fridge = _save.Armory.Find(Fridge)!;
        _save.Equip(First, fridge);

        Assert.Equal(UpgradeResult.NotEnoughScrap, Workbench.Upgrade(First, fridge));
        Assert.Equal(1, fridge.Level);
    }

    [Fact]
    public void Suit_upgrade_needs_a_free_slot()
    {
        _save.Equip(First, _save.Armory.Find(NailGun)!);
        _save.Equip(First, _save.Armory.Find(NailGun)!);
        _save.Equip(First, _save.Armory.Find(NailGun)!);
        var fridge = _save.Armory.Find(Fridge)!;
        _save.Equip(First, fridge);
        First.AddScrap(1000);

        Assert.Equal(UpgradeResult.NoRoom, Workbench.Upgrade(First, fridge));
    }

    [Fact]
    public void Each_copy_keeps_its_own_upgrades()
    {
        var mine = _save.Armory.Find(Fridge)!;
        var theirs = _save.Armory.Find(Fridge)!;
        _save.Equip(First, mine);
        First.AddScrap(1000);

        Workbench.Upgrade(First, mine);

        Assert.Equal(2, mine.Level);
        Assert.Equal(1, theirs.Level);
    }

    [Fact]
    public void Beating_a_level_unlocks_the_next()
    {
        _save.LevelBeaten(1);

        Assert.Equal(2, _save.UnlockedLevel);
    }
}
