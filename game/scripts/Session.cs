using System.Collections.Generic;
using System.Linq;
using ArmageddonOuttaHere.Core;
using Godot;

namespace ArmageddonOuttaHere;

/// <summary>A person playing: their controller and the character they picked.</summary>
public sealed class Player
{
    public Player(PadInput input, Character character)
    {
        Input = input;
        Character = character;
    }

    public PadInput Input { get; }

    public Character Character { get; }

    public Color Color => Palette.Player(Character.Appearance.Color);
}

/// <summary>
/// Always loaded (an autoload). Holds the save game being played, who is playing, and moves between the garage and
/// levels. Players stay joined when a level starts and when it ends.
/// </summary>
public partial class Session : Node
{
    public const int MaxPlayers = 4;

    private readonly List<Player> _players = new();
    private SaveSlots _slots = null!;

    public static Session Instance { get; private set; } = null!;

    public SaveGame Save { get; private set; } = null!;

    public int Slot { get; private set; }

    public IReadOnlyList<Player> Players => _players;

    /// <summary>The level being played (or about to be).</summary>
    public int CurrentLevel { get; private set; } = 1;

    /// <summary>Command-line options for automatic checks (see <see cref="DevOptions"/>).</summary>
    public DevOptions Dev { get; private set; } = new();

    public override void _EnterTree()
    {
        Instance = this;
        Dev = DevOptions.FromCommandLine();
        var folder = Dev.SaveFolder ?? ProjectSettings.GlobalizePath("user://saves");
        _slots = new SaveSlots(folder);
        Slot = _slots.LastUsed;
        Save = _slots.Load(Slot);
        if (Dev.Speed > 1)
        {
            Engine.TimeScale = Dev.Speed;
            Engine.PhysicsTicksPerSecond = 60 * Dev.Speed;
            Engine.MaxPhysicsStepsPerFrame = 8 * Dev.Speed;
        }

        if (Dev.Level > 0)
        {
            CurrentLevel = Dev.Level;
        }
    }

    public IEnumerable<Character> FreeCharacters => Save.Characters.Where(c => _players.All(p => p.Character != c));

    public bool IsPlaying(int device) => _players.Any(p => p.Input.Device == device);

    public Player Join(PadInput input, Character character)
    {
        var player = new Player(input, character);
        _players.Add(player);
        return player;
    }

    public void Leave(Player player) => _players.Remove(player);

    /// <summary>For automatic checks: pretend players join (see <see cref="DevOptions.Players"/>).</summary>
    public void AddDevPlayers()
    {
        for (var i = _players.Count; i < Mathf.Min(Dev.Players, MaxPlayers); i++)
        {
            if (FreeCharacters.FirstOrDefault() is { } character)
            {
                character.Appearance = new Appearance(i);
                Join(new PadInput(100 + i), character);
            }
        }
    }

    public void SaveNow() => _slots.Save(Slot, Save);

    public SlotSummary DescribeSlot(int slot) => _slots.Describe(slot);

    /// <summary>Saves, switches to another save game, and sends everyone back out the door (characters belong to a save).</summary>
    public void SwitchSlot(int slot)
    {
        SaveNow();
        Slot = slot;
        _slots.LastUsed = slot;
        Save = _slots.Load(slot);
        _players.Clear();
        GoToGarage();
    }

    public void StartLevel(int level)
    {
        CurrentLevel = level;
        SaveNow();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/level.tscn");
    }

    public void GoToGarage()
    {
        SaveNow();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/garage.tscn");
    }
}
