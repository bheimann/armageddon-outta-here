# Armageddon Outta Here

A goofy, post-apocalyptic local multiplayer defense game. Friends on one couch, one screen, holding the line
against the end of the world in mech suits, from junk held together with duct tape and hope to high-tech
military and alien prototypes.

- **Local co-op**: 1 to 4 players on one screen during levels, split screen in the garage.
- **Defend the spot**: hold off waves of raccoons, zombies and aliens, then beat the boss.
- **Find and upgrade gear**: suits and guns come from supply crates; scrap upgrades them at the workbench.

See [docs/design.md](docs/design.md) for the full design.

## Playing

**Without Godot:** download the latest `ArmageddonOuttaHere-windows.zip` from the repo's **Releases** page, unzip
it anywhere and run `ArmageddonOuttaHere.exe`.

**From the code:** needs Godot 4.7 (.NET version) and the .NET 8 SDK.

1. Open Godot, click **Import**, choose `game/project.godot`, then **Import & Edit**.
2. Press **F5**.

The game opens in the garage. Press **Start** on each controller to join, pick or create a character, then walk
up to the **mission board** and press **A** to start a level.

## Controls

Xbox-style controllers (such as a Logitech F310 with its back switch set to **X**).

| Button | In a level | In menus |
|---|---|---|
| Left stick | Move | Move the cursor |
| Y | Fire the selected weapon or suit attack (hold to keep firing) | |
| X | Punch | |
| A | Dodge roll | Select |
| B, LB, RB, LT, RT | Select what's in that loadout slot | B: back |
| Start | Join (in the garage) | |

Aim is the way you're facing. Revive a downed teammate by standing next to them. Leave by walking out the
garage's side door.

One player can use the keyboard: Enter joins, WASD moves, J fires, K punches, Space rolls, 1 to 5 pick slots,
Escape goes back in menus.

## Garage

| Station | What it does |
|---|---|
| Workbench | Upgrade equipped gear with your scrap, and equip or unequip gear |
| Mission board | Pick the next level |
| Wardrobe | Change your color and name |
| Filing cabinet | Switch between the 5 save games |
| Side door | Walk out to leave |

## Code

| Folder | Holds |
|---|---|
| `game/` | The Godot project: scenes and scripts for the garage, levels, mechs, enemies and menus |
| `src/ArmageddonOuttaHere.Core` | The game rules: characters, scrap, the team's gear, loadouts, upgrades, waves, save files |
| `src/ArmageddonOuttaHere.Core.Tests` | Unit tests for the rules |

```
dotnet test src/ArmageddonOuttaHere.Core.Tests
```

Saves are JSON files in Godot's user folder (`%APPDATA%\Godot\app_userdata\Armageddon Outta Here\saves`).

### Checking without controllers

Options after `--` on the Godot command line add pretend players and take screenshots, for example:

```
godot --path game -- --players=3 --open=workbench --screenshot=shot.png
godot --headless --path game res://scenes/level.tscn -- --players=2 --speed=8
```

See `game/scripts/DevOptions.cs` for the full list. Pretend players in a level fight on their own.

`res://scenes/effects_gallery.tscn` replays every attack visual side by side.

## Windows builds

`.github/workflows/build.yml` builds the game on GitHub. Run it from the Actions tab for a test zip, or push a
version tag (`git tag v0.1.0`, `git push origin v0.1.0`) to publish it on the Releases page.

## Status

Early prototype with placeholder cartoon art: the garage, 3 levels with bosses, 16 guns and suits, and 5 save
games. Numbers (health, damage, scrap, upgrade costs) are first guesses until playtesting.
