using System.Collections.Generic;
using System.Linq;
using ArmageddonOuttaHere.Actors;
using Godot;

namespace ArmageddonOuttaHere.Combat;

/// <summary>
/// A shot. Hits enemies, and teammates too (friendly fire): a teammate takes the damage and the shot keeps flying.
/// A shot with a blast radius explodes on its first enemy, or where it lands when lobbed.
/// </summary>
public partial class Projectile : Node2D
{
    private readonly HashSet<IHittable> _alreadyHit = new();
    private double _age;

    public AttackSpec Spec { get; init; } = Attacks.Punch;

    public Vector2 Direction { get; init; } = Vector2.Right;

    /// <summary>Who fired it. Never hit by its own shot.</summary>
    public IHittable? Shooter { get; init; }

    /// <summary>Enemy shots hurt only the team; players' shots hurt everyone but the shooter.</summary>
    public bool FromEnemy { get; init; }

    public override void _PhysicsProcess(double delta)
    {
        _age += delta;
        var lob = Spec.Form == AttackForm.Lob;
        GlobalPosition += Direction * Spec.Speed * (float)delta;

        if (_age >= Spec.Life)
        {
            if (lob || Spec.Radius > 0)
            {
                Explode();
            }

            QueueFree();
            return;
        }

        if (!lob)
        {
            CheckHits();
        }

        QueueRedraw();
    }

    private void CheckHits()
    {
        foreach (var target in Hittables.Near(this, GlobalPosition, Spec.Size).ToList())
        {
            if (target == Shooter || _alreadyHit.Contains(target) || (FromEnemy && !target.IsTeam))
            {
                continue;
            }

            // The defended spot is solid: every shot stops on it. Only enemy shots damage it.
            if (target is DefendedSpot spot)
            {
                if (FromEnemy)
                {
                    spot.Hit(new Hit(Spec.Damage, Vector2.Zero));
                }
                else if (Spec.Radius > 0)
                {
                    Explode();
                }

                QueueFree();
                return;
            }

            if (Spec.Radius > 0 && !target.IsTeam)
            {
                Explode();
                QueueFree();
                return;
            }

            _alreadyHit.Add(target);
            target.Hit(new Hit(Spec.Damage, Direction * Spec.Knockback, Spec.Slow, ByTeam: !FromEnemy));

            // Shots stop on enemies (unless they pierce) but pass through teammates after hurting them.
            if (!target.IsTeam && !Spec.Pierce)
            {
                QueueFree();
                return;
            }
        }
    }

    private void Explode() => Hittables.Blast(this, GlobalPosition, Spec.Radius, Spec, ignore: null);

    public override void _Draw()
    {
        var lift = 0f;
        if (Spec.Form == AttackForm.Lob)
        {
            // Lobbed shots arc up and come back down.
            var t = (float)(_age / Spec.Life);
            lift = -Mathf.Sin(t * Mathf.Pi) * 60f;
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
            DrawCircle(Vector2.Zero, Spec.Size, Palette.Shadow);
            DrawSetTransform(Vector2.Zero);
        }

        var center = new Vector2(0, lift - (Spec.Form == AttackForm.Lob ? 0 : 20f));
        var age = (float)(_age / Spec.Life);
        switch (Spec.Visual)
        {
            case AttackVisual.Flame:
                // A puff of fire that grows and burns from yellow to red, then fades to smoke.
                var fire = new Color(1f, 0.9f, 0.3f).Lerp(new Color(0.9f, 0.25f, 0.1f), age);
                var size = Spec.Size * (0.5f + age);
                DrawCircle(center, size, (age < 0.7f ? fire : new Color(0.35f, 0.3f, 0.3f)) with { A = 1 - age * 0.8f });
                DrawCircle(center + new Vector2(0, -size * 0.3f), size * 0.5f, new Color(1f, 0.95f, 0.6f, 1 - age));
                break;

            case AttackVisual.Laser:
                // A bright streak along the direction of travel.
                var tail = center - Direction * 26;
                DrawLine(tail, center, Spec.Color with { A = 0.5f }, Spec.Size * 2);
                DrawLine(tail, center, Colors.White, Spec.Size * 0.7f);
                break;

            case AttackVisual.Ice:
                // A spinning ice crystal.
                DrawSetTransform(center, (float)_age * 12f, Vector2.One);
                var s = Spec.Size + 2;
                DrawColoredPolygon(new[] { new Vector2(0, -s), new Vector2(s * 0.6f, 0), new Vector2(0, s), new Vector2(-s * 0.6f, 0) }, Spec.Color);
                DrawLine(new Vector2(0, -s), new Vector2(0, s), Colors.White, 1.5f);
                DrawSetTransform(Vector2.Zero);
                break;

            case AttackVisual.Box:
                // A tumbling parcel, letter or grocery bag.
                DrawSetTransform(center, (float)_age * 8f, Vector2.One);
                var half = Spec.Size;
                DrawRect(new Rect2(-half - 2, -half - 2, half * 2 + 4, half * 2 + 4), Palette.Ink);
                DrawRect(new Rect2(-half, -half, half * 2, half * 2), Spec.Color);
                DrawLine(new Vector2(-half, 0), new Vector2(half, 0), Spec.Color.Darkened(0.4f), 2f);
                DrawSetTransform(Vector2.Zero);
                break;

            default:
                DrawCircle(center, Spec.Size + 2, Spec.Color.Darkened(0.5f));
                DrawCircle(center, Spec.Size, Spec.Color);
                DrawCircle(center + new Vector2(-1, -1) * Spec.Size * 0.3f, Spec.Size * 0.4f, Spec.Color.Lightened(0.6f));
                break;
        }
    }
}
