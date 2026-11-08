using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArmageddonOuttaHere;

/// <summary>
/// A test arena: press Start on a controller (or Enter on the keyboard) to join, up to 4 players.
/// One shared camera follows everyone and zooms out as players spread apart.
/// </summary>
public partial class Arena : Node2D
{
    private const int MaxPlayers = 4;
    private const float CameraMargin = 160f;
    private const float MinZoom = 0.6f;
    private const float MaxZoom = 1.2f;

    private static readonly Color[] PlayerColors =
    {
        new(0.95f, 0.35f, 0.3f),
        new(0.3f, 0.6f, 0.95f),
        new(0.4f, 0.85f, 0.35f),
        new(0.98f, 0.8f, 0.25f),
    };

    private readonly List<PadInput> _waiting = new();
    private readonly List<Mech> _players = new();
    private Camera2D _camera = null!;
    private Label _hint = null!;

    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("Camera");
        _hint = GetNode<Label>("Hud/Hint");
        _waiting.Add(new PadInput(PadInput.Keyboard));
        foreach (var pad in Input.GetConnectedJoypads())
        {
            _waiting.Add(new PadInput(pad));
        }

        Input.JoyConnectionChanged += OnJoyConnectionChanged;
        StartScreenshotIfAsked();
    }

    /// <summary>
    /// For checking the scene without a controller: run with <c>-- --screenshot=path.png</c> to join 3 players,
    /// spread them out, save a screenshot and quit.
    /// </summary>
    private async void StartScreenshotIfAsked()
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--screenshot="));
        if (arg is null)
        {
            return;
        }

        for (var i = 0; i < 3; i++)
        {
            Join(new PadInput(100 + i));
        }

        _players[0].Position = new Vector2(-300, -100);
        _players[2].Position = new Vector2(300, 150);
        for (var frame = 0; frame < 90; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        GetViewport().GetTexture().GetImage().SavePng(arg["--screenshot=".Length..]);
        GetTree().Quit();
    }

    public override void _ExitTree() => Input.JoyConnectionChanged -= OnJoyConnectionChanged;

    public override void _PhysicsProcess(double delta)
    {
        foreach (var player in _players)
        {
            player.Input.Poll();
        }

        foreach (var input in _waiting.ToList())
        {
            input.Poll();
            if (input.StartPressed && _players.Count < MaxPlayers)
            {
                Join(input);
            }
        }

        UpdateCamera(delta);
    }

    public override void _Draw()
    {
        // Placeholder ground: a dusty lot with cracked paving squares.
        var ground = new Rect2(-1200, -800, 2400, 1600);
        DrawRect(ground, new Color(0.8f, 0.68f, 0.48f));
        for (var x = -1200; x < 1200; x += 160)
        {
            DrawLine(new Vector2(x, -800), new Vector2(x, 800), new Color(0.7f, 0.58f, 0.4f), 2f);
        }

        for (var y = -800; y < 800; y += 160)
        {
            DrawLine(new Vector2(-1200, y), new Vector2(1200, y), new Color(0.7f, 0.58f, 0.4f), 2f);
        }
    }

    private void Join(PadInput input)
    {
        _waiting.Remove(input);
        var mech = new Mech
        {
            Input = input,
            PlayerColor = PlayerColors[_players.Count],
            Position = _camera.Position + new Vector2(-150 + _players.Count * 100, 0),
        };
        _players.Add(mech);
        GetNode("World").AddChild(mech);
        _hint.Text = _players.Count < MaxPlayers ? "Press Start to join" : string.Empty;
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        var id = (int)device;
        if (connected && _waiting.All(i => i.Device != id) && _players.All(p => p.Input.Device != id))
        {
            _waiting.Add(new PadInput(id));
        }
    }

    private void UpdateCamera(double delta)
    {
        if (_players.Count == 0)
        {
            return;
        }

        var box = new Rect2(_players[0].Position, Vector2.Zero);
        foreach (var p in _players.Skip(1))
        {
            box = box.Expand(p.Position);
        }

        var needed = box.Size + Vector2.One * CameraMargin * 2;
        var screen = GetViewportRect().Size;
        var zoom = Mathf.Clamp(Mathf.Min(screen.X / needed.X, screen.Y / needed.Y), MinZoom, MaxZoom);
        var t = 1f - Mathf.Exp(-4f * (float)delta);
        _camera.Position = _camera.Position.Lerp(box.GetCenter(), t);
        _camera.Zoom = Vector2.One * Mathf.Lerp(_camera.Zoom.X, zoom, t);
    }
}
