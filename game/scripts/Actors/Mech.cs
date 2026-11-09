using System;
using System.Collections.Generic;
using System.Linq;
using ArmageddonOuttaHere.Combat;
using ArmageddonOuttaHere.Core;
using Godot;

namespace ArmageddonOuttaHere.Actors;

/// <summary>
/// A player's mech. Moves with the left stick and faces the way it last moved. Y fires whatever the selected slot
/// holds, in the facing direction; the slot buttons (B, LB, RB, LT, RT) pick the slot; X punches; A dodge-rolls
/// (no damage while rolling). Drawn as a placeholder cartoon shape in the character's color.
/// </summary>
public partial class Mech : Node2D, IHittable
{
    public const float MaxHealth = 100f;
    private const float Speed = 220f;
    private const float RollSpeed = 520f;
    private const double RollTime = 0.25;
    private const double RollCooldown = 0.6;

    private readonly Dictionary<AttackSpec, double> _cooldowns = new();
    private readonly HashSet<IHittable> _dashHits = new();
    private double _rollLeft;
    private Vector2 _rollDirection = Vector2.Down;
    private double _rollCooldownLeft;
    private double _dashLeft;
    private AttackSpec? _dash;
    private double _slowLeft;
    private float _slowAmount;
    private double _hurtFlash;
    private float _bob;
    private Vector2 _push;

    public Player Player { get; init; } = null!;

    public PadInput Input => Player.Input;

    /// <summary>False in the garage: no shooting or punching there.</summary>
    public bool CanFight { get; set; } = true;

    /// <summary>True while the player has a menu open.</summary>
    public bool Frozen { get; set; }

    /// <summary>Keeps the mech inside the playable area and out of solid things. Set by the scene.</summary>
    public Func<Vector2, Vector2>? Constrain { get; set; }

    public Vector2 Facing { get; private set; } = Vector2.Down;

    public float Health { get; private set; } = MaxHealth;

    public bool IsDown { get; private set; }

    /// <summary>0 to 1: how far a teammate has got reviving this mech.</summary>
    public float ReviveProgress { get; set; }

    /// <summary>Index into the loadout's slot buttons.</summary>
    public int SelectedSlot { get; private set; }

    public bool IsRolling => _rollLeft > 0;

    public float HitRadius => 18f;

    public bool IsAlive => !IsDown;

    public bool IsTeam => true;

    public IReadOnlyList<(SlotButton Button, GearItem Item, int Attack)> Slots => Player.Character.Loadout.Buttons();

    public override void _Ready()
    {
        AddToGroup(IHittable.Group);
        AddToGroup("mechs");
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;
        _rollCooldownLeft -= delta;
        _slowLeft -= delta;
        _hurtFlash -= delta;
        foreach (var key in _cooldowns.Keys.ToList())
        {
            _cooldowns[key] -= delta;
        }

        if (IsDown || Frozen)
        {
            QueueRedraw();
            return;
        }

        // Aim locks while Y is held: once you start firing you keep shooting the same way, even while moving
        // (strafing). Let go of Y and the mech turns to face where it moves again.
        var aimLocked = CanFight && Input.AttackHeld;
        if (Input.Move != Vector2.Zero && !aimLocked)
        {
            Facing = Input.Move.Normalized();
        }

        var speedScale = _slowLeft > 0 ? 1f - _slowAmount : 1f;
        Vector2 velocity;
        if (_dashLeft > 0 && _dash is not null)
        {
            _dashLeft -= delta;
            velocity = Facing * _dash.Speed;
            DashHits();
        }
        else if (IsRolling)
        {
            _rollLeft -= delta;
            velocity = _rollDirection * RollSpeed;
        }
        else
        {
            velocity = Input.Move * Speed * speedScale;
            HandleButtons();
        }

        Position += (velocity + _push) * dt;
        _push = _push.MoveToward(Vector2.Zero, 900f * dt);
        if (Constrain is not null)
        {
            Position = Constrain(Position);
        }

        _bob += dt * (velocity == Vector2.Zero ? 3f : 14f);
        QueueRedraw();
    }

    private void HandleButtons()
    {
        var slots = Slots;
        if (Input.SlotPressed >= 0 && Input.SlotPressed < slots.Count)
        {
            SelectedSlot = Input.SlotPressed;
        }

        SelectedSlot = Mathf.Clamp(SelectedSlot, 0, slots.Count - 1);

        if (Input.RollPressed && _rollCooldownLeft <= 0)
        {
            _rollLeft = RollTime;
            _rollDirection = Input.Move != Vector2.Zero ? Input.Move.Normalized() : Facing;
            _rollCooldownLeft = RollCooldown;
            return;
        }

        if (!CanFight)
        {
            return;
        }

        if (Input.PunchPressed && Ready(Attacks.Punch))
        {
            Hittables.Blast(this, Position + Facing * 30f, Attacks.Punch.Radius, Attacks.Punch, ignore: this, facing: Facing);
        }
        else if (Input.AttackHeld)
        {
            var (_, item, attack) = slots[SelectedSlot];
            var spec = Attacks.For(item, attack);
            if (Ready(spec))
            {
                Use(spec);
            }
        }
    }

    /// <summary>True (and starts the cooldown) when the attack is off cooldown.</summary>
    private bool Ready(AttackSpec spec)
    {
        if (_cooldowns.TryGetValue(spec, out var left) && left > 0)
        {
            return false;
        }

        _cooldowns[spec] = spec.Cooldown;
        return true;
    }

    private void Use(AttackSpec spec)
    {
        switch (spec.Form)
        {
            case AttackForm.Shot:
            case AttackForm.Lob:
                var rng = new RandomNumberGenerator();
                for (var i = 0; i < spec.Count; i++)
                {
                    var angle = Mathf.DegToRad(rng.RandfRange(-spec.Spread, spec.Spread) / 2f);
                    Hittables.WorldOf(this).AddChild(new Projectile
                    {
                        Spec = spec,
                        Direction = Facing.Rotated(angle),
                        Shooter = this,
                        Position = Position + Facing * 30f,
                    });
                }

                break;

            case AttackForm.Nova:
                Hittables.Blast(this, Position, spec.Radius, spec, ignore: this, follow: this, facing: Facing);
                break;

            case AttackForm.Dash:
                _dash = spec;
                _dashLeft = spec.Life;
                _dashHits.Clear();
                break;

            case AttackForm.Chain:
                Chain(spec);
                break;

            case AttackForm.Strike:
                foreach (var target in Enemies().OrderBy(e => e.GlobalPosition.DistanceTo(Position)).Take(spec.Count).ToList())
                {
                    Hittables.Blast(this, target.GlobalPosition, spec.Radius, spec, ignore: this);
                }

                break;
        }
    }

    private IEnumerable<IHittable> Enemies() => Hittables.All(this).Where(h => !h.IsTeam && h.GlobalPosition.DistanceTo(Position) < 700f);

    private void Chain(AttackSpec spec)
    {
        var hit = new HashSet<IHittable>();
        var from = Position;
        for (var i = 0; i < spec.Count; i++)
        {
            var next = Enemies()
                .Where(e => !hit.Contains(e) && e.GlobalPosition.DistanceTo(from) <= spec.Radius)
                .OrderBy(e => e.GlobalPosition.DistanceTo(from))
                .FirstOrDefault();
            if (next is null)
            {
                break;
            }

            hit.Add(next);
            next.Hit(new Hit(spec.Damage, (next.GlobalPosition - from).Normalized() * spec.Knockback, spec.Slow, ByTeam: true));
            Hittables.WorldOf(this).AddChild(new Bolt { From = from, To = next.GlobalPosition, Tint = spec.Color });
            from = next.GlobalPosition;
        }
    }

    private void DashHits()
    {
        foreach (var target in Hittables.Near(this, Position, _dash!.Radius).ToList())
        {
            if (target != this && _dashHits.Add(target))
            {
                target.Hit(new Hit(_dash.Damage, Facing * _dash.Knockback, _dash.Slow, ByTeam: true));
            }
        }
    }

    public void Hit(Hit hit)
    {
        if (IsDown)
        {
            return;
        }

        if (hit.Heal > 0)
        {
            Health = Mathf.Min(MaxHealth, Health + hit.Heal);
            return;
        }

        if (IsRolling || hit.Damage <= 0)
        {
            return;
        }

        Health -= hit.Damage;
        _push += hit.Push;
        _hurtFlash = 0.12;
        if (hit.Slow > 0)
        {
            _slowLeft = 1.5;
            _slowAmount = hit.Slow;
        }

        if (Health <= 0)
        {
            Health = 0;
            IsDown = true;
            ReviveProgress = 0;
        }
    }

    /// <summary>A teammate finished reviving this mech: back up with half health.</summary>
    public void Revive()
    {
        IsDown = false;
        Health = MaxHealth / 2;
        ReviveProgress = 0;
    }

    public override void _Draw()
    {
        var color = Player.Color;
        var outline = color.Darkened(0.55f);

        // Shadow on the ground: the view is slightly angled, so the body sits above it.
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
        DrawCircle(Vector2.Zero, 22f, Palette.Shadow);
        DrawSetTransform(Vector2.Zero);

        if (IsDown)
        {
            DrawDown(color, outline);
            return;
        }

        var lift = IsRolling ? 0f : Mathf.Sin(_bob) * 2f;
        var squash = IsRolling ? new Vector2(1.25f, 0.75f) : new Vector2(1f, 1f + Mathf.Sin(_bob) * 0.04f);
        var body = new Vector2(0, -26f + lift);
        var fill = _hurtFlash > 0 ? Colors.White : color;

        if (_dashLeft > 0 && _dash is not null)
        {
            DrawDash(_dash);
        }

        DrawLine(new Vector2(-9, -12), new Vector2(-11, 0), outline, 6f);
        DrawLine(new Vector2(9, -12), new Vector2(11, 0), outline, 6f);

        DrawSetTransform(body, 0, squash);
        DrawCircle(Vector2.Zero, 20f, outline);
        DrawCircle(Vector2.Zero, 17f, fill);
        DrawCircle(new Vector2(Facing.X * 6f, -4f + Facing.Y * 3f), 7f, new Color(0.75f, 0.95f, 1f));
        DrawSetTransform(Vector2.Zero);

        if (CanFight)
        {
            DrawLine(body, body + Facing * 30f, outline, 7f);
            DrawLine(body, body + Facing * 30f, Colors.DimGray, 4f);
        }

        DrawName(-62);
    }

    /// <summary>
    /// A charge: Lawnmower Larry mows with spinning blades; everything else leaves speed lines, and the Hover
    /// Commander also fires its jets.
    /// </summary>
    private void DrawDash(AttackSpec dash)
    {
        if (dash.Visual == AttackVisual.Blades)
        {
            Blast.DrawSpinningBlades(this, dash.Radius + 10, (float)Time.GetTicksMsec() / 40f, 1f, dash.Color);
            return;
        }

        var back = -Facing;
        var across = Facing.Orthogonal();
        for (var i = -1; i <= 1; i++)
        {
            var start = new Vector2(0, -26) + back * 26 + across * i * 12;
            DrawLine(start, start + back * (40 - Mathf.Abs(i) * 12), dash.Color with { A = 0.8f }, 4f);
        }

        if (dash.Name == "Hover Boost")
        {
            var jet = new Vector2(0, -10) + back * 18;
            DrawColoredPolygon(new[] { jet + across * 8, jet - across * 8, jet + back * 34 }, new Color(1f, 0.6f, 0.15f, 0.9f));
            DrawColoredPolygon(new[] { jet + across * 4, jet - across * 4, jet + back * 20 }, new Color(1f, 0.95f, 0.6f));
        }
    }

    private void DrawDown(Color color, Color outline)
    {
        DrawSetTransform(new Vector2(0, -10), 0, new Vector2(1.3f, 0.7f));
        DrawCircle(Vector2.Zero, 20f, outline);
        DrawCircle(Vector2.Zero, 17f, color.Darkened(0.3f));
        DrawSetTransform(Vector2.Zero);
        DrawString(ThemeDB.FallbackFont, new Vector2(-8, -16), "x x", HorizontalAlignment.Left, -1, 14, Palette.Ink);

        // Revive ring: fills up while a teammate stands close.
        DrawArc(new Vector2(0, -10), 32f, 0, Mathf.Tau, 32, Palette.Shadow, 5f);
        if (ReviveProgress > 0)
        {
            DrawArc(new Vector2(0, -10), 32f, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * ReviveProgress, 32, Palette.Health, 5f);
        }

        DrawName(-48);
    }

    private void DrawName(float y)
    {
        var font = ThemeDB.FallbackFont;
        var name = Player.Character.Name;
        var width = font.GetStringSize(name, HorizontalAlignment.Left, -1, 14).X;
        var at = new Vector2(-width / 2, y);
        DrawStringOutline(font, at, name, HorizontalAlignment.Left, -1, 14, 4, Palette.Ink);
        DrawString(font, at, name, HorizontalAlignment.Left, -1, 14, Player.Color.Lightened(0.3f));
    }
}
