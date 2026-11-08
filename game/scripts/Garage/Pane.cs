using System.Collections.Generic;
using ArmageddonOuttaHere.Actors;
using ArmageddonOuttaHere.UI;
using Godot;

namespace ArmageddonOuttaHere.Garage;

/// <summary>
/// One player's part of the split screen in the garage: their own camera on the shared garage, plus their menus.
/// A pane exists from the moment a controller presses Start (while picking a character) until the player leaves.
/// </summary>
public partial class Pane : Control
{
    private readonly Stack<IPanel> _panels = new();
    private SubViewport _viewport = null!;
    private Camera2D _camera = null!;
    private Control _overlay = null!;

    public PadInput Input { get; init; } = null!;

    /// <summary>Null while still picking a character.</summary>
    public Player? Player { get; set; }

    public Mech? Mech { get; set; }

    /// <summary>Becomes true once the mech has walked away from the door, so it doesn't leave the moment it arrives.</summary>
    public bool CanLeave { get; set; }

    /// <summary>What's shown along the bottom ("A: Use the workbench").</summary>
    public string Hint { get; set; } = string.Empty;

    /// <summary>Shown at the top right for a short time (such as "Upgraded!").</summary>
    public string Message { get; private set; } = string.Empty;

    private double _messageLeft;

    public bool HasPanel => _panels.Count > 0;

    public IPanel? Top => _panels.Count > 0 ? _panels.Peek() : null;

    public void Open(IPanel panel) => _panels.Push(panel);

    public void Close()
    {
        if (_panels.Count > 0)
        {
            _panels.Pop();
        }
    }

    public void CloseAll() => _panels.Clear();

    public void Say(string message)
    {
        Message = message;
        _messageLeft = 2.5;
    }

    /// <summary>Sets up the view onto the shared garage world.</summary>
    public void Build(World2D world)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        var container = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore };
        container.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(container);

        _viewport = new SubViewport { World2D = world, HandleInputLocally = false };
        container.AddChild(_viewport);
        _camera = new Camera2D
        {
            LimitLeft = (int)GarageRoom.Floor.Position.X - 80,
            LimitRight = (int)GarageRoom.Floor.End.X + 80,
            LimitTop = (int)GarageRoom.Floor.Position.Y - 200,
            LimitBottom = (int)GarageRoom.Floor.End.Y + 80,
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed = 6f,
        };
        _viewport.AddChild(_camera);

        _overlay = new PaneOverlay { Pane = this };
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_overlay);
    }

    /// <summary>Puts this pane in its part of the screen and picks a zoom that suits its size.</summary>
    public void Place(Rect2 rect)
    {
        Position = rect.Position;
        Size = rect.Size;
        var zoom = rect.Size.Y >= 700 ? (rect.Size.X >= 1000 ? 0.85f : 0.75f) : 0.65f;
        _camera.Zoom = Vector2.One * zoom;
    }

    public override void _Process(double delta)
    {
        _messageLeft -= delta;
        _camera.Position = Mech?.Position ?? GarageRoom.Entrance;
        _overlay.QueueRedraw();
    }

    public void DrawOverlay(Control canvas)
    {
        var area = new Rect2(Vector2.Zero, Size);
        canvas.DrawRect(area, Player?.Color ?? Palette.Disabled, false, 4f);

        if (Player is { } player)
        {
            Ui.Text(canvas, player.Character.Name, new Vector2(16, 32), 22, player.Color.Lightened(0.3f));
            Ui.Text(canvas, $"{player.Character.Scrap} scrap", new Vector2(16, 58), 18, Palette.Highlight);
        }

        if (_messageLeft > 0)
        {
            Ui.Text(canvas, Message, new Vector2(Size.X - 16, 32), 20, Palette.Highlight, right: true);
        }

        if (Top is { } panel)
        {
            panel.Draw(canvas, area);
        }
        else if (Hint.Length > 0)
        {
            Ui.Text(canvas, Hint, new Vector2(Size.X / 2, Size.Y - 24), 20, Colors.White, center: true);
        }
    }
}

/// <summary>Draws a pane's text and menus on top of its view of the garage.</summary>
public partial class PaneOverlay : Control
{
    public Pane Pane { get; init; } = null!;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw() => Pane.DrawOverlay(this);
}
