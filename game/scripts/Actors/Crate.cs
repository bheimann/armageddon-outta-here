using System;
using System.Linq;
using Godot;

namespace ArmageddonOuttaHere.Actors;

/// <summary>A supply crate dropped during a wave. The first mech to touch it opens it for the team.</summary>
public partial class Crate : Node2D
{
    private const float PickupRange = 40f;
    private double _age;

    public const string Group = "crates";

    private bool _opened;

    /// <summary>Called once when a mech reaches the crate (or the level ends with it unopened).</summary>
    public Action<Crate>? Opened { get; set; }

    public override void _Ready() => AddToGroup(Group);

    public override void _PhysicsProcess(double delta)
    {
        _age += delta;
        var mech = GetTree().GetNodesInGroup("mechs").OfType<Mech>()
            .FirstOrDefault(m => m.IsAlive && m.GlobalPosition.DistanceTo(GlobalPosition) <= PickupRange);
        if (mech is not null)
        {
            Open();
        }

        QueueRedraw();
    }

    public void Open()
    {
        if (_opened)
        {
            return;
        }

        _opened = true;
        RemoveFromGroup(Group);
        Opened?.Invoke(this);
        QueueFree();
    }

    public override void _Draw()
    {
        // Drops in from the sky, then sits there glowing so players notice it.
        var fall = Mathf.Max(0, 1 - (float)_age / 0.5f);
        var lift = -fall * 300f;
        var pulse = 0.5f + 0.5f * Mathf.Sin((float)_age * 5f);

        DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
        DrawCircle(Vector2.Zero, 26f, Palette.Shadow);
        DrawArc(Vector2.Zero, 34f + pulse * 6f, 0, Mathf.Tau, 32, Palette.Highlight with { A = 0.6f }, 4f);
        DrawSetTransform(Vector2.Zero);

        var box = new Rect2(-20, -40 + lift, 40, 36);
        DrawRect(box.Grow(3), Palette.Ink);
        DrawRect(box, new Color(0.75f, 0.55f, 0.3f));
        DrawLine(box.Position, box.End, new Color(0.55f, 0.38f, 0.2f), 4f);
        DrawLine(new Vector2(box.Position.X, box.End.Y), new Vector2(box.End.X, box.Position.Y), new Color(0.55f, 0.38f, 0.2f), 4f);
        DrawString(ThemeDB.FallbackFont, new Vector2(-6, -16 + lift), "?", HorizontalAlignment.Left, -1, 22, Palette.Highlight);
    }
}
