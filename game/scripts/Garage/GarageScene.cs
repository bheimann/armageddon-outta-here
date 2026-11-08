using System.Collections.Generic;
using System.Linq;
using ArmageddonOuttaHere.Actors;
using ArmageddonOuttaHere.Combat;
using ArmageddonOuttaHere.Core;
using ArmageddonOuttaHere.UI;
using Godot;

namespace ArmageddonOuttaHere.Garage;

/// <summary>
/// The garage between levels. Split screen: each player gets their own view and menus, so nobody waits.
/// Press Start on an unused controller to join (pick or create a character, then walk in the side door);
/// walk back out the door to leave. Walk up to a station and press A to use it.
/// </summary>
public partial class GarageScene : Node2D
{
    private const float StationRange = 130f;
    private const float DoorRange = 60f;

    private readonly Dictionary<int, PadInput> _waiting = new();
    private readonly List<Pane> _panes = new();
    private Node2D _world = null!;
    private Control _screen = null!;

    private static Session Session => Session.Instance;

    public override void _Ready()
    {
        _world = GetNode<Node2D>("World");
        _world.AddToGroup(Hittables.WorldGroup);
        _screen = GetNode<Control>("Screen/Panes");
        GetNode<GarageHud>("Screen/Hud").Garage = this;

        AddWaiting(PadInput.Keyboard);
        foreach (var pad in Input.GetConnectedJoypads())
        {
            AddWaiting(pad);
        }

        Input.JoyConnectionChanged += OnJoyConnectionChanged;

        Session.AddDevPlayers();
        foreach (var player in Session.Players)
        {
            _waiting.Remove(player.Input.Device);
            var pane = AddPane(player.Input);
            SpawnMech(pane, player, GarageRoom.Entrance + new Vector2(-200 - 90 * _panes.Count, 0));
        }

        Layout();
        OpenDevMenu();
        Session.Dev.ScreenshotIfAsked(this);
    }

    public override void _ExitTree() => Input.JoyConnectionChanged -= OnJoyConnectionChanged;

    public int PaneCount => _panes.Count;

    public override void _PhysicsProcess(double delta)
    {
        foreach (var input in _waiting.Values.ToList())
        {
            input.Poll();
            if (input.StartPressed && _panes.Count < Session.MaxPlayers)
            {
                BeginJoin(input);
            }
        }

        foreach (var pane in _panes.ToList())
        {
            pane.Input.Poll();
            UpdatePane(pane);
        }
    }

    private void UpdatePane(Pane pane)
    {
        if (pane.Top is { } panel)
        {
            if (pane.Mech is not null)
            {
                pane.Mech.Frozen = true;
            }

            panel.Handle(pane.Input);
            return;
        }

        if (pane.Mech is not { } mech)
        {
            return;
        }

        mech.Frozen = false;
        var station = GarageRoom.Stations
            .Where(s => s.At.DistanceTo(mech.Position) <= StationRange)
            .OrderBy(s => s.At.DistanceTo(mech.Position))
            .FirstOrDefault();
        var atDoor = mech.Position.DistanceTo(GarageRoom.Door) <= DoorRange;
        if (mech.Position.DistanceTo(GarageRoom.Door) > 140)
        {
            pane.CanLeave = true;
        }

        pane.Hint = station is not null ? $"A: Use the {station.Name.ToLower()}" : string.Empty;
        if (station is not null && pane.Input.AcceptPressed)
        {
            mech.Frozen = true;
            OpenStation(pane, station.Kind);
        }
        else if (atDoor && pane.CanLeave)
        {
            Leave(pane);
        }
    }

    // ---- Joining and leaving ----

    private void BeginJoin(PadInput input)
    {
        _waiting.Remove(input.Device);
        var pane = AddPane(input);
        Layout();
        pane.Open(CharacterSelect(pane));
    }

    private Menu CharacterSelect(Pane pane) => new(
        "Who's playing?",
        () => Session.FreeCharacters
            .Select(c => new MenuItem($"{c.Name}  ({c.Scrap} scrap)", () => Pick(pane, c)))
            .Append(new MenuItem("+ New character", () => pane.Open(new NameEntry(
                "Name your character",
                string.Empty,
                name => Pick(pane, Session.Save.CreateCharacter(name)),
                pane.Close))))
            .ToList(),
        () => CancelJoin(pane))
    {
        Subtitle = () => "Pick a character, or make a new one",
    };

    private void Pick(Pane pane, Character character)
    {
        if (Session.Players.Any(p => p.Character == character))
        {
            return;
        }

        pane.CloseAll();
        var player = Session.Join(pane.Input, character);
        SpawnMech(pane, player, GarageRoom.Entrance);
        pane.CanLeave = false;
        Session.SaveNow();
    }

    private void CancelJoin(Pane pane)
    {
        RemovePane(pane);
        AddWaiting(pane.Input.Device);
    }

    private void Leave(Pane pane)
    {
        if (pane.Player is { } player)
        {
            Session.Leave(player);
        }

        pane.Mech?.QueueFree();
        RemovePane(pane);
        AddWaiting(pane.Input.Device);
        Session.SaveNow();
    }

    private void SpawnMech(Pane pane, Player player, Vector2 at)
    {
        pane.Player = player;
        pane.Mech = new Mech
        {
            Player = player,
            Position = at,
            CanFight = false,
            Constrain = p => p.Clamp(
                GarageRoom.Floor.Position + new Vector2(30, 40),
                GarageRoom.Floor.End - new Vector2(10, 20)),
        };
        _world.AddChild(pane.Mech);
    }

    private Pane AddPane(PadInput input)
    {
        var pane = new Pane { Input = input };
        _screen.AddChild(pane);
        pane.Build(GetViewport().World2D);
        _panes.Add(pane);
        return pane;
    }

    private void RemovePane(Pane pane)
    {
        _panes.Remove(pane);
        pane.QueueFree();
        Layout();
    }

    /// <summary>1 player: full screen. 2: side by side. 3 or 4: one quarter each.</summary>
    private void Layout()
    {
        var size = GetViewportRect().Size;
        for (var i = 0; i < _panes.Count; i++)
        {
            var rect = _panes.Count switch
            {
                1 => new Rect2(Vector2.Zero, size),
                2 => new Rect2(new Vector2(i * size.X / 2, 0), new Vector2(size.X / 2, size.Y)),
                _ => new Rect2(new Vector2(i % 2 * size.X / 2, i / 2 * size.Y / 2), size / 2),
            };
            _panes[i].Place(rect);
        }
    }

    private void AddWaiting(int device)
    {
        if (!_waiting.ContainsKey(device) && !Session.IsPlaying(device) && _panes.All(p => p.Input.Device != device))
        {
            _waiting[device] = new PadInput(device);
        }
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        if (connected)
        {
            AddWaiting((int)device);
        }
        else
        {
            _waiting.Remove((int)device);
        }
    }

    // ---- Stations ----

    private void OpenStation(Pane pane, StationKind kind)
    {
        var character = pane.Player!.Character;
        pane.Open(kind switch
        {
            StationKind.Workbench => WorkbenchMenu(pane, character),
            StationKind.MissionBoard => MissionBoard(pane),
            StationKind.Wardrobe => Wardrobe(pane, character),
            _ => FilingCabinet(pane),
        });
    }

    private Menu WorkbenchMenu(Pane pane, Character character) => new(
        "Workbench",
        () => character.Loadout.Items
            .Select(item => new MenuItem(Describe(item), () => pane.Open(ItemMenu(pane, character, item))))
            .Append(new MenuItem("Equip gear...", () => pane.Open(EquipMenu(pane, character))))
            .Append(new MenuItem("Done", pane.Close))
            .ToList(),
        pane.Close)
    {
        Subtitle = () => $"{character.Scrap} scrap    {character.Loadout.SlotsUsed} of {Loadout.SlotCount} slots used",
    };

    private Menu ItemMenu(Pane pane, Character character, GearItem item) => new(
        item.Type.Name,
        () =>
        {
            var items = new List<MenuItem> { UpgradeItem(pane, character, item) };
            if (character.Loadout.Items.Count > 1)
            {
                items.Add(new MenuItem("Unequip", () =>
                {
                    Session.Save.Unequip(character, item);
                    pane.Close();
                }));
            }

            items.Add(new MenuItem("Back", pane.Close));
            return items;
        },
        pane.Close)
    {
        Subtitle = () => AttackList(item),
    };

    private MenuItem UpgradeItem(Pane pane, Character character, GearItem item)
    {
        if (item.Level >= GearType.MaxLevel)
        {
            return new MenuItem($"Level {item.Level} (max)", Enabled: false);
        }

        var cost = Workbench.Cost(item.Level);
        var needsSlot = item.Type.Kind == GearKind.Suit && character.Loadout.FreeSlots < 1;
        var text = $"Upgrade to level {item.Level + 1}: {cost} scrap" + (needsSlot ? " (needs a free slot)" : string.Empty);
        return new MenuItem(text, () =>
        {
            var result = Workbench.Upgrade(character, item);
            pane.Say(result switch
            {
                UpgradeResult.Upgraded => $"{item.Type.Name} is level {item.Level}!",
                UpgradeResult.NotEnoughScrap => $"Need {cost} scrap",
                UpgradeResult.NoRoom => "Free up a slot first",
                _ => "Can't upgrade that",
            });
            Session.SaveNow();
        }, Enabled: character.Scrap >= cost && !needsSlot);
    }

    private Menu EquipMenu(Pane pane, Character character) => new(
        "Equip gear",
        () =>
        {
            var gear = Session.Save.Equippable(character).ToList();
            var items = gear
                .OrderBy(i => i.Type.Kind)
                .ThenBy(i => i.Type.Name)
                .Select(item => new MenuItem(Describe(item), () =>
                {
                    if (Session.Save.Equip(character, item))
                    {
                        pane.Say($"Equipped {item.Type.Name}");
                        Session.SaveNow();
                        pane.Close();
                    }
                }, Enabled: item.Slots <= character.Loadout.FreeSlots))
                .ToList();
            if (items.Count == 0)
            {
                items.Add(new MenuItem("Nothing free. Find gear in supply crates!", Enabled: false));
            }

            items.Add(new MenuItem("Back", pane.Close));
            return items;
        },
        pane.Close)
    {
        Subtitle = () => $"{character.Loadout.FreeSlots} free slots",
    };

    private Menu MissionBoard(Pane pane) => new(
        "Mission board",
        () => LevelCatalog.All
            .Select(level => level.Number <= Session.Save.UnlockedLevel
                ? new MenuItem($"{level.Number}. {level.Name}  ({level.Spot.ToLower()}, {level.Enemy.ToString().ToLower()}s)",
                    () => pane.Open(new Menu(
                        $"Level {level.Number}: {level.Name}",
                        () => new[]
                        {
                            new MenuItem($"Go! ({Session.Players.Count} {(Session.Players.Count == 1 ? "player" : "players")})", () => Session.StartLevel(level.Number)),
                            new MenuItem("Back", pane.Close),
                        },
                        pane.Close)
                    {
                        Subtitle = () => $"Defend the {level.Spot.ToLower()} and beat {level.Boss}",
                    }))
                : new MenuItem($"{level.Number}. ???  (beat level {level.Number - 1} first)", Enabled: false))
            .Append(new MenuItem("Done", pane.Close))
            .ToList(),
        pane.Close);

    private Menu Wardrobe(Pane pane, Character character) => new(
        "Wardrobe",
        () => new[]
        {
            new MenuItem(
                $"Color:  <  {Palette.PlayerName(character.Appearance.Color)}  >",
                () => ChangeColor(character, 1),
                Side: step => ChangeColor(character, step)),
            new MenuItem("Rename", () => pane.Open(new NameEntry(
                "New name",
                character.Name,
                name =>
                {
                    character.Rename(name);
                    Session.SaveNow();
                    pane.Close();
                },
                pane.Close))),
            new MenuItem("Done", pane.Close),
        },
        pane.Close)
    {
        Subtitle = () => "Left / right changes your color",
    };

    private static void ChangeColor(Character character, int step)
    {
        character.Appearance = character.Appearance with { Color = Palette.Wrap(character.Appearance.Color + step) };
        Session.SaveNow();
    }

    private Menu FilingCabinet(Pane pane) => new(
        "Filing cabinet",
        () => Enumerable.Range(1, SaveSlots.Count)
            .Select(slot =>
            {
                var summary = Session.DescribeSlot(slot);
                var current = slot == Session.Slot;
                var text = $"Save {slot}: " + (summary.IsEmpty ? "empty" : $"{summary.Characters} characters, level {summary.UnlockedLevel}")
                    + (current ? "  (playing)" : string.Empty);
                return new MenuItem(text, current ? null : () => pane.Open(new Menu(
                    $"Switch to save {slot}?",
                    () => new[]
                    {
                        new MenuItem("Switch", () => Session.SwitchSlot(slot)),
                        new MenuItem("Back", pane.Close),
                    },
                    pane.Close)
                {
                    Subtitle = () => "This save is kept. Everyone rejoins with the new save's characters.",
                }));
            })
            .Append(new MenuItem("Done", pane.Close))
            .ToList(),
        pane.Close);

    private static string Describe(GearItem item)
    {
        var slots = item.Slots == 1 ? "1 slot" : $"{item.Slots} slots";
        return $"{item.Type.Name}  level {item.Level}  ({slots})";
    }

    private static string AttackList(GearItem item)
    {
        var count = item.Type.Kind == GearKind.Suit ? item.Level : 1;
        return string.Join(", ", Enumerable.Range(0, count).Select(i => Attacks.For(item, i).Name));
    }

    /// <summary>For automatic checks: <c>--open=workbench|board|wardrobe|cabinet|join</c>.</summary>
    private void OpenDevMenu()
    {
        if (Session.Dev.Open is not { } open)
        {
            return;
        }

        if (open == "join")
        {
            BeginJoin(new PadInput(150));
            return;
        }

        if (_panes.FirstOrDefault(p => p.Player is not null) is not { } pane)
        {
            return;
        }

        var kind = open switch
        {
            "board" => StationKind.MissionBoard,
            "wardrobe" => StationKind.Wardrobe,
            "cabinet" => StationKind.FilingCabinet,
            _ => StationKind.Workbench,
        };
        OpenStation(pane, kind);
    }
}
