# Armageddon Outta Here: Design

A goofy, post-apocalyptic local multiplayer defense game in the spirit of Deadtime Defenders, with mech suits
that are unlocked through play, from junkyard contraptions to high-tech prototypes.

## Display

- **View:** slightly angled 2D view, like Deadtime Defenders.
- **Art style:** cartoon. Smooth, bright, exaggerated art with squash and stretch to sell the goofy tone. For
  references, see *Overcooked*, *Brawlhalla*, *Cult of the Lamb* and *Death Squared*.
- **Camera:** one shared screen during runs, no split screen. The camera follows the group and zooms out as
  players spread apart. The garage uses split screen (see [Hub: the garage](#hub-the-garage)).
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

**Aim lock:** while Y is held, the aim stays locked in the direction the player started firing, so they can move
and keep shooting the same way. Letting go of Y unlocks it. A dodge roll goes the way the stick is pushed.

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
  survivors. Waves of enemies attack it, with a short break between waves.
- **Repairs:** the base slowly repairs itself during the breaks between waves. Players standing next to it repair
  it faster, at any time.
- **Winning:** survive the waves, then beat the boss that shows up at the end.
- **Level order:** beating a level unlocks the next one. Players can replay any level they've beaten.
- **Run length:** about 5 minutes at the starting levels, up to 10 minutes for harder ones.
- **Enemies:** raccoons, zombies and aliens. They go after players first: each one chases the nearest player it
  can see, and heads for the base when no player is near.
- **Friendly fire:** players' shots hurt teammates. A bullet damages a teammate and keeps flying through them.
- **The defended spot is solid:** shots can't pass through it, and players can't damage it. Only enemies can.
- **Scrap drops:** scrap from enemies is split evenly between every player in the level.
- **Finding suits and weapons:** supply crates appear on the map during waves, and players have to go grab them.
- **Downed players:** at 0 health a player is knocked down and stays down until a teammate revives them by
  standing next to them for about 2 seconds. There's no time limit. If every player is down, the run is lost.
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
- **Filing cabinet:** switch to a different save game.

**Split screen:** the garage is split screen, one view per player, so each player can use the workbench, the
wardrobe or pick a character without waiting for the others. Levels go back to one shared screen.

## Save games and characters

- **5 save games.** The game continues the last save used. Players switch saves at the filing cabinet in the
  garage.
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
- **One of each item per player.** A player can't equip two copies of the same suit or gun, so one person can't
  take all 4 Grill Sergeants.
- **Only equipped gear can be upgraded.** At the workbench, a player can only upgrade the suit and weapons they
  have equipped, using their own character's scrap.
- **Every character starts with a pistol.** It's free and doesn't count against the team's 4 copies, so nobody
  is ever left with an empty loadout. The pistol stays theirs when it's unequipped, so it can go back in later.
- **Upgrade costs:** 50 scrap times the current level (50, then 100, then 150). A first guess until playtesting.
- **Gun upgrades** make a gun hit harder (25% more damage per level). **Suit upgrades** add an attack and take
  another slot, so a suit can only be upgraded when there's a free slot.

**Placeholder suits.** These are in the game so the loadout and upgrades can be tested, but they'll probably be
replaced with the final suits later.

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

### Suit attacks

Each suit level unlocks the next attack, on its own slot button: level 1 has attack 1, level 4 has all four.

| Suit | Level 1 | Level 2 | Level 3 | Level 4 |
|---|---|---|---|---|
| The Fridge | **Freezer Beam:** a steady icy stream that slows enemies | **Ice Cube Toss:** a lobbed ice block that bursts and slows | **Door Slam:** a blast around the suit that knocks enemies back | **Deep Freeze:** a huge blast that nearly freezes everything nearby |
| Lawnmower Larry | **Blade Spin:** spinning blades hit everything close | **Grass Clippings:** a spray of 5 clippings | **Mow Down:** charges forward through enemies | **Turbo Mulch:** a big spinning blast |
| Mailbox Mk II | **Package Toss:** a lobbed package that explodes | **Junk Mail:** 3 letters fired in a spread | **Express Delivery:** explosions dropped on the 3 nearest enemies | **Return to Sender:** a blast that throws enemies far away |
| Grill Sergeant | **Grill Flames:** a short-range flamethrower | **Hot Dog Launcher:** fires hot dogs | **Cookout:** heals every teammate nearby | **Charcoal Bomb:** a lobbed bomb with a big blast |
| The Shopping Cart | **Ram:** a quick charge that knocks enemies away | **Bag Toss:** throws grocery bags | **Runaway Cart:** a long, hard-hitting charge | **Clean Up Aisle 5:** a big blast around the cart |
| Prototype X-9 "Sparky" | **Zap:** lightning that jumps between 3 enemies | **Static Field:** a blast that slows enemies nearby | **Overcharge:** lightning that jumps between 7 enemies | **Thunder Strike:** lightning bolts on the 4 nearest enemies |
| The Hover Commander | **Hover Blaster:** fast laser shots | **Drone Strike:** explosions on the 2 nearest enemies | **Hover Boost:** a quick charge forward | **Air Raid:** explosions on the 6 nearest enemies |
| Probe Reverser | **Tractor Beam:** pulls enemies in toward the suit | **Probe Laser:** a laser that goes through enemies | **Abduct:** a strong blast up close | **Saucer Slam:** a lobbed saucer with a huge blast |

### Guns

Upgrading a gun makes it hit harder (25% more damage per level).

| Gun | Slots | What it does |
|---|---|---|
| Pistol | 1 | Single shots. Every character starts with one |
| Nail Gun | 1 | Fast, slightly scattered nails |
| Shotgun | 1 | 6 pellets in a short-range spread |
| Ray Gun | 1 | Alien shots that go through enemies |
| Minigun | 2 | A very fast stream of bullets |
| RPG | 2 | A rocket that explodes on the first enemy it hits |
| Grenade Launcher | 2 | A lobbed grenade that explodes where it lands |
| Flamethrower | 2 | A short-range stream of fire that goes through enemies |

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
