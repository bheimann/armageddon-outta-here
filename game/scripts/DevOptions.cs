using System.Linq;
using Godot;

namespace ArmageddonOuttaHere;

/// <summary>
/// Options for checking the game without controllers, passed after <c>--</c> on the command line:
/// <list type="bullet">
/// <item><c>--players=N</c>: N pretend players join straight away.</item>
/// <item><c>--screenshot=path.png</c> and <c>--frames=N</c>: save a screenshot after N frames, then quit.</item>
/// <item><c>--saves=folder</c>: keep saves there instead of the real save folder.</item>
/// <item><c>--wave=N</c>: start a level at wave N (N past the last wave starts the boss).</item>
/// <item><c>--open=workbench|board|wardrobe|cabinet|join</c>: open that garage menu for player 1.</item>
/// </list>
/// </summary>
public sealed class DevOptions
{
    public int Players { get; init; }

    public string? Screenshot { get; init; }

    public int Frames { get; init; } = 90;

    public string? SaveFolder { get; init; }

    public int Wave { get; init; } = 1;

    public string? Open { get; init; }

    /// <summary><c>--allgear</c>: the team finds one of every gun and suit (to see long lists).</summary>
    public bool AllGear { get; init; }

    /// <summary><c>--level=N</c>: which level the level scene plays when started directly.</summary>
    public int Level { get; init; }

    /// <summary><c>--speed=N</c>: run the game N times faster (to play a whole level quickly).</summary>
    public int Speed { get; init; } = 1;

    public static DevOptions FromCommandLine()
    {
        var args = OS.GetCmdlineUserArgs();

        string? Value(string name) =>
            args.FirstOrDefault(a => a.StartsWith($"--{name}="))?[(name.Length + 3)..];

        int IntValue(string name, int fallback) => int.TryParse(Value(name), out var v) ? v : fallback;

        return new DevOptions
        {
            Players = IntValue("players", 0),
            Screenshot = Value("screenshot"),
            Frames = IntValue("frames", 90),
            SaveFolder = Value("saves"),
            Wave = IntValue("wave", 1),
            Open = Value("open"),
            Level = IntValue("level", 0),
            AllGear = args.Contains("--allgear"),
            Speed = Mathf.Clamp(IntValue("speed", 1), 1, 8),
        };
    }

    /// <summary>Waits <see cref="Frames"/> frames, saves the screenshot and quits, if one was asked for.</summary>
    public async void ScreenshotIfAsked(Node node)
    {
        if (Screenshot is null)
        {
            return;
        }

        for (var i = 0; i < Frames; i++)
        {
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        node.GetViewport().GetTexture().GetImage().SavePng(Screenshot);
        node.GetTree().Quit();
    }
}
