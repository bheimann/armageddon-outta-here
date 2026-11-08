using System;
using ArmageddonOuttaHere.Combat;
using ArmageddonOuttaHere.Core;
using Godot;

namespace ArmageddonOuttaHere.Actors;

/// <summary>
/// The boss at the end of a level. Big, tough, and with specials:
/// the Trash Panda King throws trash and calls raccoon swarms, Big Mall Cop blows his whistle and charges with
/// a pack of zombies, and the Probe-ssor sprays shots and beams players up to him.
/// </summary>
public partial class Boss : Enemy
{
    private double _specialLeft = 4;
    private double _chargeLeft;
    private Vector2 _chargeDirection;

    public string BossName { get; init; } = "Boss";

    /// <summary>Health goes up with the number of players.</summary>
    public int PlayerCount { get; init; } = 1;

    /// <summary>Called when the boss wants minions: kind and how many.</summary>
    public Action<EnemyKind, int, Vector2>? Summon { get; set; }

    public override void _Ready()
    {
        var baseStats = StatsFor(Kind);
        var health = Kind switch
        {
            EnemyKind.Raccoon => 700f,
            EnemyKind.Zombie => 1000f,
            _ => 1300f,
        };
        Stats = baseStats with
        {
            Health = health * (1 + 0.5f * (PlayerCount - 1)),
            Speed = baseStats.Speed * 0.6f,
            Damage = baseStats.Damage * 2.5f,
            Radius = baseStats.Radius * 3f,
            Scrap = 100,
            ShotRange = 0,
        };
        base._Ready();
    }

    protected override void Act(IHittable target, double delta)
    {
        _specialLeft -= delta;
        if (_chargeLeft > 0)
        {
            _chargeLeft -= delta;
            Position += _chargeDirection * 420f * (float)delta;
        }

        if (_specialLeft > 0)
        {
            return;
        }

        var toTarget = (target.GlobalPosition - GlobalPosition).Normalized();
        switch (Kind)
        {
            case EnemyKind.Raccoon:
                _specialLeft = 5;
                Summon?.Invoke(EnemyKind.Raccoon, 4, GlobalPosition);
                for (var i = -1; i <= 1; i++)
                {
                    Shoot(toTarget.Rotated(i * 0.3f), 12f, new Color(0.3f, 0.3f, 0.3f));
                }

                break;

            case EnemyKind.Zombie:
                _specialLeft = 5;
                _chargeLeft = 1.0;
                _chargeDirection = toTarget;
                Summon?.Invoke(EnemyKind.Zombie, 3, GlobalPosition);
                break;

            default:
                _specialLeft = 4;
                for (var i = 0; i < 10; i++)
                {
                    Shoot(Vector2.Right.Rotated(Mathf.Tau * i / 10f), 10f);
                }

                if (target is Mech mech)
                {
                    // Beam the player up and drop them next to the saucer.
                    mech.Hit(new Hit(15, (GlobalPosition - mech.GlobalPosition) * 2.5f));
                }

                break;
        }
    }

    public override void _Draw()
    {
        base._Draw();
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(BossName, HorizontalAlignment.Left, -1, 18).X;
        var at = new Vector2(-width / 2, -Stats.Radius * 2 - 30);
        DrawStringOutline(font, at, BossName, HorizontalAlignment.Left, -1, 18, 6, Palette.Ink);
        DrawString(font, at, BossName, HorizontalAlignment.Left, -1, 18, Palette.Highlight);

        // A crown, a cap or a saucer rim so each boss reads at a glance.
        var top = new Vector2(0, -Stats.Radius * 2 - 4);
        switch (Kind)
        {
            case EnemyKind.Raccoon:
                DrawColoredPolygon(new[] { top + new Vector2(-20, 0), top + new Vector2(-20, -18), top + new Vector2(-8, -8), top + new Vector2(0, -22), top + new Vector2(8, -8), top + new Vector2(20, -18), top + new Vector2(20, 0) }, Palette.Highlight);
                break;
            case EnemyKind.Zombie:
                DrawRect(new Rect2(top + new Vector2(-24, -10), new Vector2(48, 12)), new Color(0.15f, 0.2f, 0.45f));
                DrawRect(new Rect2(top + new Vector2(-30, 0), new Vector2(60, 5)), new Color(0.1f, 0.12f, 0.3f));
                break;
            default:
                DrawSetTransform(new Vector2(0, -Stats.Radius), 0, new Vector2(1f, 0.3f));
                DrawCircle(Vector2.Zero, Stats.Radius * 1.6f, new Color(0.6f, 0.65f, 0.7f, 0.85f));
                DrawSetTransform(Vector2.Zero);
                break;
        }
    }
}
