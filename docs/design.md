# Armageddon Outta Here: Design

A goofy, post-apocalyptic local multiplayer defense game in the spirit of Deadtime Defenders, with junkyard mech
suits that are unlocked through play.

## Display

- **View:** slightly angled 2D view, like Deadtime Defenders.
- **Art style:** cartoon. Smooth, bright, exaggerated art with squash and stretch to sell the goofy tone. For
  references, see *Overcooked*, *Brawlhalla*, *Cult of the Lamb* and *Death Squared*.
- **Camera:** one shared screen during runs, no split screen. The camera follows the group and zooms out as
  players spread apart. The garage may use split screen (see [Hub: the garage](#hub-the-garage)).
- **Screen:** fullscreen 16:9, with a windowed option for testing.
- **Player colors:** each player has their own color on their mech outline, health bar and cursor.

## Controls

The same layout as Deadtime Defenders, on an Xbox-style controller (we play on Logitech F310s).

**In a run**

| Button | Action |
|---|---|
| Left stick | Move |
| X | Punch |
| Y | Attack: fire the selected weapon |
| A | Dodge roll |
| B | Select the first weapon |
| LB, RB, LT, RT | Each selects a different weapon or mech ability |

Players aim in whichever direction they're facing. There's no right-stick aiming. To attack or use an ability, a
player selects it (B or a shoulder button), then fires it with Y.

**In menus**

| Button | Action |
|---|---|
| A | Accept, OK, yes |
| B | Cancel, no |

## Running

- **Players:** 1 to 4, mainly on controllers. One or two can play on keyboard and mouse.
- **Joining:** players join only in the garage, never during a run. See [Hub: the garage](#hub-the-garage).
- **Platform:** Windows PC first. Linux and Steam Deck later.
- **Builds:** published from GitHub release tags, as in `game-lab`.
- **Online:** if it reaches Steam, Remote Play Together lets friends play over the internet without any
  networking code.
- **Performance:** 60 fps target.
- **Engine:** Godot 4 (C#), the same as `game-lab`.

## Game structure

- **Runs:** waves of wasteland threats attack the base. A break between waves is for repairs and spending scrap.
- **Run length:** about 5 minutes at the starting levels, up to 10 minutes for harder ones.

## Hub: the garage

Between runs, the crew returns to a cluttered suburban garage, with the mechs parked next to the lawnmower.

- **Workbench:** upgrade weapons and mech suits with scrap.
- **Mission board:** select the next level.
- **Wardrobe:** change your character's appearance.
- **Side door:** where players join and leave.
  - **Joining:** press Start on an unused controller while in the garage. The player picks an existing character
    or creates a new one (see [Save games and characters](#save-games-and-characters)), and that character walks
    in through the side door. During a run, Start on an unused controller does nothing.
  - **Leaving:** walk back out through the side door.

Being considered: split screen in the garage, so each player can upgrade and change their appearance without
waiting for the others.

## Save games and characters

- Each save game starts with 4 characters, each with 0 scrap. They're customizable: players can change their name
  and appearance.
- When joining, a player either picks an existing character or creates a new one. A new character needs a name,
  and starts with 0 scrap.
- **Scrap belongs to the character.** Each character collects and spends their own.

## Mech suits

Suits are found during runs, not built. They're made from junk, and each one plays differently.

- **Suits belong to the team.** Any player can use any suit the team has found.
- **Up to 4 of each suit**, one per player, even when fewer than 4 people are playing. Finding a suit the team
  already has adds another copy, up to 4.
- **Upgrades belong to each suit.** Each copy of a suit keeps its own upgrades.
- **Weapons work the same way:** found during runs, shared by the team, up to 4 copies of each, and each copy
  keeps its own upgrades.
- **Only equipped gear can be upgraded.** At the workbench, a player can only upgrade the suit and weapons they
  have equipped, using their own character's scrap.

| Suit | Role |
|---|---|
| The Fridge | Slow tank with a freezer beam |
| Lawnmower Larry | Spins its blades through crowds |
| Mailbox Mk II | Fires packages as artillery |
| Grill Sergeant | Flamethrower that buffs the team |
| The Shopping Cart | Fast and fragile, rams enemies |
