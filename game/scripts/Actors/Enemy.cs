using System;
using System.Linq;
using ArmageddonOuttaHere.Combat;
using ArmageddonOuttaHere.Core;
using Godot;

namespace ArmageddonOuttaHere.Actors;

/// <summary>How tough, fast and rewarding each enemy is. Placeholder numbers until balancing.</summary>
public sealed record EnemyStats(float Health, float Speed, float Damage, float Radius, int Scrap, Color Color, float ShotRange = 0f);

/// <summary>
/// A raccoon, zombie or alien. Chases the nearest player it can see; heads for the base when no player is near.
/// Aliens also shoot from a distance. When it dies, its scrap is split between the players.
/// </summary>
public partial class Enemy : Node2D, IHittable
{
    public const string Group = "enemies";

    /// <summary>How far an enemy can see a player.</summary>
    private const float SightRange = 520f;

    private const double AttackCooldown = 0.8;
    private const double ShotCooldown = 2.2;

    private double _attackLeft;
    private double _shotLeft = 1.5;
    private double _slowLeft;
    private float _slowAmount;
    private double _hurtFlash;
    private Vector2 _push;
    private float _bob;

    public static EnemyStats StatsFor(EnemyKind kind) => kind switch
    {
        EnemyKind.Raccoon => new EnemyStats(20, 150, 6, 14, 3, new Color(0.55f, 0.55f, 0.6f)),
        EnemyKind.Zombie => new EnemyStats(45, 70, 12, 17, 5, new Color(0.5f, 0.75f, 0.4f)),
        _ => new EnemyStats(30, 100, 8, 15, 7, new Color(0.55f, 0.95f, 0.6f), ShotRange: 320),
    };

    public EnemyKind Kind { get; init; }

    public EnemyStats Stats { get; protected set; } = StatsFor(EnemyKind.Raccoon);

    public float Health { get; protected set; }

    /// <summary>The spot being defended. Enemies go for it when no player is in sight.</summary>
    public DefendedSpot? Target { get; set; }

    /// <summary>Called once when the enemy dies, with its scrap.</summary>
    public Action<Enemy>? Died { get; set; }

    /// <summary>Keeps the enemy inside the play area. Set by the level.</summary>
    public Func<Vector2, Vector2>? Constrain { get; set; }

    public float HitRadius => Stats.Radius;

    public bool IsAlive => Health > 0;

    public bool IsTeam => false;

    public override void _Ready()
    {
        if (this is not Boss)
        {
            Stats = StatsFor(Kind);
        }

        Health = Stats.Health;
        AddToGroup(IHittable.Group);
        AddToGroup(Group);
        _bob = (float)GD.RandRange(0, Mathf.Tau);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        var dt = (float)delta;
        _attackLeft -= delta;
        _shotLeft -= delta;
        _slowLeft -= delta;
        _hurtFlash -= delta;

        var target = ChooseTarget();
        var velocity = Vector2.Zero;
        if (target is not null)
        {
            var toTarget = target.GlobalPosition - GlobalPosition;
            var reach = target.HitRadius + Stats.Radius + 4f;
            var keepAway = Stats.ShotRange > 0 && target is Mech ? Stats.ShotRange * 0.7f : 0f;
            if (toTarget.Length() > Mathf.Max(reach, keepAway))
            {
                velocity = toTarget.Normalized() * Stats.Speed * (_slowLeft > 0 ? 1f - _slowAmount : 1f);
            }
            else if (keepAway <= 0 && _attackLeft <= 0)
            {
                _attackLeft = AttackCooldown;
                target.Hit(new Hit(Stats.Damage, toTarget.Normalized() * 150f));
            }

            if (Stats.ShotRange > 0 && _shotLeft <= 0 && toTarget.Length() <= Stats.ShotRange)
            {
                _shotLeft = ShotCooldown;
                Shoot(toTarget.Normalized());
            }

            Act(target, delta);
        }

        Position += (velocity + Separation() + _push) * dt;
        _push = _push.MoveToward(Vector2.Zero, 900f * dt);
        if (Constrain is not null)
        {
            Position = Constrain(Position);
        }

        _bob += dt * 12f;
        QueueRedraw();
    }

    /// <summary>Extra behaviour each frame (bosses use it for their specials).</summary>
    protected virtual void Act(IHittable target, double delta)
    {
    }

    protected void Shoot(Vector2 direction, float damage = 8f, Color? color = null)
    {
        Hittables.WorldOf(this).AddChild(new Projectile
        {
            Spec = new AttackSpec("Alien Blaster", AttackForm.Shot, damage, 0, color ?? new Color(0.9f, 0.3f, 1f), Speed: 350, Life: 1.5f, Size: 6),
            Direction = direction,
            Shooter = this,
            FromEnemy = true,
            Position = Position + direction * 20f,
        });
    }

    private IHittable? ChooseTarget()
    {
        var player = GetTree().GetNodesInGroup("mechs").OfType<Mech>()
            .Where(m => m.IsAlive && m.GlobalPosition.DistanceTo(GlobalPosition) <= SightRange)
            .OrderBy(m => m.GlobalPosition.DistanceTo(GlobalPosition))
            .FirstOrDefault();
        return (IHittable?)player ?? (Target is { IsAlive: true } ? Target : null);
    }

    /// <summary>Pushes away from enemies that are too close, so crowds spread out instead of stacking.</summary>
    private Vector2 Separation()
    {
        var push = Vector2.Zero;
        foreach (var other in GetTree().GetNodesInGroup(Group).OfType<Enemy>())
        {
            if (other == this)
            {
                continue;
            }

            var away = GlobalPosition - other.GlobalPosition;
            var min = Stats.Radius + other.Stats.Radius;
            if (away.LengthSquared() < min * min && away.LengthSquared() > 0.01f)
            {
                push += away.Normalized() * (min - away.Length()) * 8f;
            }
        }

        return push;
    }

    public void Hit(Hit hit)
    {
        if (!IsAlive || hit.Damage <= 0)
        {
            return;
        }

        Health -= hit.Damage;
        _push += hit.Push * (this is Boss ? 0.15f : 1f);
        _hurtFlash = 0.1;
        if (hit.Slow > 0)
        {
            _slowLeft = 2;
            _slowAmount = hit.Slow;
        }

        if (Health <= 0)
        {
            Died?.Invoke(this);
            QueueFree();
        }
    }

    public override void _Draw()
    {
        var r = Stats.Radius;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
        DrawCircle(Vector2.Zero, r * 1.1f, Palette.Shadow);
        DrawSetTransform(Vector2.Zero);

        var fill = _hurtFlash > 0 ? Colors.White : Stats.Color;
        var outline = Stats.Color.Darkened(0.6f);
        var hop = Mathf.Abs(Mathf.Sin(_bob)) * 3f;
        var body = new Vector2(0, -r - hop);
        if (Kind == EnemyKind.Alien)
        {
            body.Y -= 8f;
        }

        DrawCircle(body, r + 2, outline);
        DrawCircle(body, r, fill);
        DrawFace(body, r, outline);

        if (Health < Stats.Health)
        {
            var width = r * 2.2f;
            var bar = new Rect2(body + new Vector2(-width / 2, -r - 12), new Vector2(width, 5));
            DrawRect(bar, Palette.HealthBack);
            DrawRect(bar with { Size = new Vector2(width * Health / Stats.Health, 5) }, new Color(0.95f, 0.3f, 0.25f));
        }
    }

    private void DrawFace(Vector2 body, float r, Color outline)
    {
        switch (Kind)
        {
            case EnemyKind.Raccoon:
                // Bandit mask, ears and a striped tail.
                DrawRect(new Rect2(body + new Vector2(-r * 0.8f, -r * 0.35f), new Vector2(r * 1.6f, r * 0.45f)), Palette.Ink);
                DrawCircle(body + new Vector2(-r * 0.35f, -r * 0.12f), r * 0.15f, Colors.White);
                DrawCircle(body + new Vector2(r * 0.35f, -r * 0.12f), r * 0.15f, Colors.White);
                DrawCircle(body + new Vector2(-r * 0.7f, -r * 0.85f), r * 0.3f, outline);
                DrawCircle(body + new Vector2(r * 0.7f, -r * 0.85f), r * 0.3f, outline);
                break;

            case EnemyKind.Zombie:
                // Droopy eyes and arms stuck out in front.
                DrawCircle(body + new Vector2(-r * 0.35f, -r * 0.2f), r * 0.2f, Colors.Yellow);
                DrawCircle(body + new Vector2(r * 0.35f, -r * 0.1f), r * 0.14f, Colors.Yellow);
                DrawLine(body + new Vector2(0, r * 0.4f), body + new Vector2(r * 0.4f, r * 0.35f), outline, 2f);
                DrawLine(body + new Vector2(-r, 0), body + new Vector2(-r * 1.6f, r * 0.1f), outline, 4f);
                DrawLine(body + new Vector2(r, 0), body + new Vector2(r * 1.6f, r * 0.1f), outline, 4f);
                break;

            default:
                // One big eye and an antenna.
                DrawCircle(body + new Vector2(0, -r * 0.1f), r * 0.45f, Colors.White);
                DrawCircle(body + new Vector2(0, -r * 0.1f), r * 0.22f, Palette.Ink);
                DrawLine(body + new Vector2(0, -r), body + new Vector2(r * 0.3f, -r * 1.6f), outline, 2f);
                DrawCircle(body + new Vector2(r * 0.3f, -r * 1.6f), 3f, new Color(0.9f, 0.3f, 1f));
                break;
        }
    }
}
