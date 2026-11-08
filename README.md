# Armageddon Outta Here

A goofy, post-apocalyptic local multiplayer defense game. Friends on one couch, one screen, holding the line
against the end of the world in mech suits, from junk held together with duct tape and hope to high-tech
military and alien prototypes.

## The idea

- **Local co-op**: 1-4 players on the same screen.
- **Defend the base**: hold off waves of wasteland threats.
- **Unlockable mech suits**: find suits and weapons in supply crates, and spend scrap to upgrade them. Each suit
  plays differently.

See [docs/design.md](docs/design.md) for the full design.

## Suit ideas

| Suit | Role |
|---|---|
| The Fridge | Slow tank with a freezer beam |
| Lawnmower Larry | Spins its blades through crowds |
| Mailbox Mk II | Fires packages as artillery |
| Grill Sergeant | Flamethrower that buffs the team |
| The Shopping Cart | Fast and fragile, rams enemies |

## Running the game

Needs Godot 4.7 (.NET version) and the .NET 8 SDK.

1. Open Godot, click **Import**, choose `game/project.godot`, then open it.
2. Press **▶** (F5).
3. Press **Start** on a controller to join (up to 4 players). On the keyboard: Enter joins, WASD moves, J fires,
   Space dodge-rolls.

## Tests

The game rules (characters, scrap, the team's gear, loadouts and upgrades) live in `src/ArmageddonOuttaHere.Core`
and have unit tests:

```
dotnet test src/ArmageddonOuttaHere.Core.Tests
```

## Status

Early prototype: a test arena where up to 4 players join, move, dodge-roll and shoot, with placeholder art.
