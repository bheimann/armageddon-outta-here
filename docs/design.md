# Armageddon Outta Here: Design

A goofy, post-apocalyptic local multiplayer defense game in the spirit of Deadtime Defenders, with mech suits
that are unlocked through play, from junkyard contraptions to high-tech prototypes.

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
| B, LB, RB, LT, RT | The 5 loadout slots. Each selects what's in that slot |

Players aim in whichever direction they're facing. There's no right-stick aiming. To attack or use an ability, a
player selects it with its slot button, then fires it with Y. See [Loadout](#loadout).

**In menus**

| Button | Action |
|---|---|
| A | Accept, OK, yes |
| B | Cancel, no |

## Running

- **Players:** 1 to 4, mainly on controllers. One or two can play on keyboard and mouse.
- **Joining:** players join only in the garage, never during a run. See [Hub: the garage](#hub-the-garage).
- **Platform:** Windows PC first. Linux and Steam Deck later.
- **Builds:** published from GitHub release tags.
- **Online:** if it reaches Steam, Remote Play Together lets friends play over the internet without any
  networking code.
- **Performance:** 60 fps target.
- **Engine:** Godot 4 (C#).

## Game structure

- **Not a roguelike.** Levels are fixed, and everything players earn (scrap, suits, weapons, upgrades) is kept
  for good. A "run" here just means one play of a level.
- **Levels:** each level has its own spot to defend, such as a gas station, a radio tower or a school bus full of
  survivors. Waves of enemies attack it, with a short break between waves for repairs.
- **Winning:** survive the waves, then beat the boss that shows up at the end.
- **Level order:** beating a level unlocks the next one. Players can replay any level they've beaten.
- **Run length:** about 5 minutes at the starting levels, up to 10 minutes for harder ones.
- **Enemies:** raccoons, zombies and aliens.
- **Finding suits and weapons:** supply crates appear on the map during waves, and players have to go grab them.
- **Downed players:** at 0 health a player is knocked down and stays down until a teammate revives them. There's
  no time limit. If every player is down, the run is lost.
- **Losing a run:** players keep everything they collected: scrap, suits and weapons.

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

Suits are found during runs, not built, and each one plays differently. They range from junk held together with
duct tape to high-tech suits: military prototypes left behind in the chaos, and salvaged alien tech.

- **Suits belong to the team.** Any player can use any suit the team has found.
- **Up to 4 of each suit**, one per player, even when fewer than 4 people are playing. Finding a suit the team
  already has adds another copy, up to 4.
- **Every player is always in a basic junk mech.** Suits are equipped on top of it, in the loadout like weapons
  (see [Loadout](#loadout)), and add attacks.
- **Upgrades belong to each suit.** Each copy of a suit keeps its own upgrades.
- **Weapons work the same way:** found during runs, shared by the team, up to 4 copies of each, and each copy
  keeps its own upgrades.
- **Only equipped gear can be upgraded.** At the workbench, a player can only upgrade the suit and weapons they
  have equipped, using their own character's scrap.
- **Starter gun (assumption, to confirm):** every character always has a free basic gun that doesn't count
  against the team's 4 copies, so nobody is ever left with an empty loadout.

| Suit | Tech | Role |
|---|---|---|
| The Fridge | Junk | Slow tank with a freezer beam |
| Lawnmower Larry | Junk | Spins its blades through crowds |
| Mailbox Mk II | Junk | Fires packages as artillery |
| Grill Sergeant | Junk | Flamethrower that buffs the team |
| The Shopping Cart | Junk | Fast and fragile, rams enemies |
| Prototype X-9 "Sparky" | High tech | A military prototype that chains lightning between enemies |
| The Hover Commander | High tech | Hovers over obstacles and calls in drone strikes |
| Probe Reverser | Alien tech | A salvaged alien suit with a tractor beam that throws enemies around |

## Loadout

Each player has **5 slots**, one per slot button (B, LB, RB, LT, RT). Guns and suits are equipped into them.

| Gear | Slots it takes |
|---|---|
| Gun | 1 |
| Heavy gun: minigun, RPG, grenade launcher, flamethrower | 2 |
| Suit | 1 per suit level: level 1 takes 1 slot, up to level 4 taking 4 slots |

- Each slot a suit takes holds one of its attacks, so a level 3 suit has 3 attacks on 3 slot buttons.
- A player always has at least one gun or suit equipped.
- Examples: 4 guns and a level 1 suit. A level 3 suit and a heavy gun. Two heavy guns and a gun.

## Bosses

Draft, to be edited. One boss per enemy type, one for each of the first 3 levels.

| Level | Enemies | Boss |
|---|---|---|
| 1 | Raccoons | **The Trash Panda King**: a giant raccoon riding a stolen garbage truck. Throws trash bags and calls in raccoon swarms. |
| 2 | Zombies | **Big Mall Cop**: a huge zombie security guard on a Segway, with a whistle that makes every zombie charge. |
| 3 | Aliens | **The Probe-ssor**: an alien scientist in a flying saucer that beams up junk, including players' mechs, and drops it back on them. |

## Story

Draft, to be edited.

Everything went wrong at once. A meteor hit, the zombies came up, and the aliens arrived to see what all the
noise was about. Meanwhile the raccoons, who'd been waiting for this their whole lives, took over the
landfill.

What's left of the neighborhood is a garage full of junk, and a crew of neighbors who decide the only sensible
move is to bolt lawnmowers, fridges and shopping carts into mech suits and defend whatever's still standing.
Along the way they scavenge better: the army left prototype suits behind when it fled, and the aliens drop
their tech when they're beaten.
They're not leaving. Armageddon can get outta *here*.

## First playable version

- 3 levels, one per enemy type, each ending in its boss.
- Enough suits, guns and heavy guns to test the loadout and upgrades.
- The garage with the workbench, mission board, wardrobe and side door.
