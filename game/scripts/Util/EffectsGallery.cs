using System;
using System.Linq;
using ArmageddonOuttaHere.Combat;
using Godot;

namespace ArmageddonOuttaHere.Util;

/// <summary>
/// A test scene that replays every attack visual side by side, for checking how they look:
/// <c>godot --path game res://scenes/effects_gallery.tscn</c>.
/// </summary>
public partial class EffectsGallery : Node2D
{
    private static readonly AttackVisual[] BlastStyles = Enum.GetValues<AttackVisual>()
        .Where(v => v is not (AttackVisual.Auto or AttackVisual.Flame or AttackVisual.Laser or AttackVisual.Ice or AttackVisual.Box))
        .ToArray();

    private static readonly AttackVisual[] ShotStyles = { AttackVisual.Flame, AttackVisual.Laser, AttackVisual.Ice, AttackVisual.Box };

    private double _timer;

    public override void _Ready()
    {
        AddToGroup(Hittables.WorldGroup);
        Session.Instance.Dev.ScreenshotIfAsked(this);
    }

    public override void _Process(double delta)
    {
        _timer -= delta;
        if (_timer > 0)
        {
            return;
        }

        _timer = 0.9;
        for (var i = 0; i < BlastStyles.Length; i++)
        {
            AddChild(new Blast
            {
                Position = Cell(i),
                Radius = 70,
                Style = BlastStyles[i],
                Tint = BlastStyles[i] == AttackVisual.Pull ? new Color(0.4f, 1f, 0.5f) : new Color(0.7f, 0.85f, 1f),
                Facing = Vector2.Right,
            });
        }

        for (var i = 0; i < ShotStyles.Length; i++)
        {
            AddChild(new Projectile
            {
                Spec = new AttackSpec("Gallery", AttackForm.Shot, 0, 0, ShotStyles[i] == AttackVisual.Flame ? new Color(1f, 0.55f, 0.15f) : Colors.LightBlue,
                    Speed: 120, Life: 0.9f, Size: 8, Visual: ShotStyles[i]),
                Position = Cell(BlastStyles.Length + i) - new Vector2(50, 0),
                Direction = Vector2.Right,
            });
        }

        QueueRedraw();
    }

    private static Vector2 Cell(int i) => new(160 + i % 5 * 240, 170 + i / 5 * 200);

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), Palette.Ground);
        for (var i = 0; i < BlastStyles.Length + ShotStyles.Length; i++)
        {
            var name = i < BlastStyles.Length ? BlastStyles[i].ToString() : ShotStyles[i - BlastStyles.Length] + " shot";
            UI.Ui.Text(this, name, Cell(i) + new Vector2(0, 80), 18, Colors.White, center: true);
        }
    }
}
