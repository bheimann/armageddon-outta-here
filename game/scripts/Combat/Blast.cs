using Godot;

namespace ArmageddonOuttaHere.Combat;

/// <summary>
/// What a blast looks like when it goes off: spinning blades, ice shards, a slamming door, a tractor beam,
/// lightning, a heal, a punch or an explosion. Shapes on the ground are squashed to match the angled view.
/// </summary>
public partial class Blast : Node2D
{
    private const float GroundSquash = 0.6f;

    private readonly RandomNumberGenerator _rng = new();
    private double _age;
    private float[] _puffAngles = System.Array.Empty<float>();

    public float Radius { get; init; } = 50f;

    public Color Tint { get; init; } = Colors.White;

    public AttackVisual Style { get; init; } = AttackVisual.Shockwave;

    /// <summary>The mech to stay centered on (spinning blades turn around the mech as it moves).</summary>
    public Node2D? Follow { get; init; }

    /// <summary>Which way the attacker faces (the punch's fist points this way).</summary>
    public Vector2 Facing { get; init; } = Vector2.Right;

    private double Duration => Style switch
    {
        AttackVisual.Punch => 0.25,
        AttackVisual.Blades => 0.4,
        AttackVisual.Heal => 0.8,
        AttackVisual.Frost or AttackVisual.Pull or AttackVisual.Beam => 0.5,
        AttackVisual.Explosion => 0.55,
        AttackVisual.Lightning => 0.35,
        AttackVisual.Slam => 0.45,
        _ => 0.35,
    };

    public override void _Ready()
    {
        ZIndex = 5;
        _puffAngles = new float[8];
        for (var i = 0; i < _puffAngles.Length; i++)
        {
            _puffAngles[i] = Mathf.Tau * i / _puffAngles.Length + _rng.RandfRange(-0.3f, 0.3f);
        }
    }

    public override void _Process(double delta)
    {
        _age += delta;
        if (Follow is not null && IsInstanceValid(Follow))
        {
            GlobalPosition = Follow.GlobalPosition;
        }

        if (_age >= Duration)
        {
            QueueFree();
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var t = Mathf.Clamp((float)(_age / Duration), 0, 1);
        switch (Style)
        {
            case AttackVisual.Blades: DrawBlades(t); break;
            case AttackVisual.Frost: DrawFrost(t); break;
            case AttackVisual.Slam: DrawSlam(t); break;
            case AttackVisual.Heal: DrawHeal(t); break;
            case AttackVisual.Pull: DrawPull(t); break;
            case AttackVisual.Static: DrawStatic(t); break;
            case AttackVisual.Beam: DrawBeam(t); break;
            case AttackVisual.Lightning: DrawLightning(t); break;
            case AttackVisual.Punch: DrawPunch(t); break;
            case AttackVisual.Explosion: DrawExplosion(t); break;
            default: DrawShockwave(t); break;
        }
    }

    private void Ground() => DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, GroundSquash));

    private void Reset() => DrawSetTransform(Vector2.Zero);

    /// <summary>Four metal blades spinning flat around the mech, with grass flying off.</summary>
    public static void DrawSpinningBlades(CanvasItem canvas, float radius, float angle, float alpha, Color grass)
    {
        canvas.DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, GroundSquash));
        var blur = new Color(0.85f, 0.85f, 0.9f, 0.25f * alpha);
        canvas.DrawArc(Vector2.Zero, radius * 0.8f, angle - 1.2f, angle, 24, blur, radius * 0.35f);
        canvas.DrawArc(Vector2.Zero, radius * 0.8f, angle + Mathf.Pi - 1.2f, angle + Mathf.Pi, 24, blur, radius * 0.35f);
        for (var i = 0; i < 4; i++)
        {
            var a = angle + i * Mathf.Pi / 2;
            var along = Vector2.Right.Rotated(a);
            var across = along.Orthogonal();
            var inner = along * radius * 0.25f;
            var outer = along * radius;
            var blade = new[]
            {
                inner + across * 4, outer + across * 7, outer + along * 6, outer - across * 2, inner - across * 4,
            };
            canvas.DrawColoredPolygon(blade, new Color(0.15f, 0.15f, 0.18f, alpha));
            canvas.DrawColoredPolygon(new[] { inner + across * 2, outer + across * 4, outer, inner - across * 2 },
                new Color(0.82f, 0.84f, 0.9f, alpha));
        }

        canvas.DrawCircle(Vector2.Zero, radius * 0.22f, new Color(0.3f, 0.3f, 0.35f, alpha));
        canvas.DrawSetTransform(Vector2.Zero);
    }

    private void DrawBlades(float t)
    {
        var alpha = t < 0.8f ? 1f : (1 - t) * 5;
        DrawSpinningBlades(this, Radius, t * Mathf.Tau * 3f, alpha, Tint);

        // Clippings flung outward.
        for (var i = 0; i < _puffAngles.Length; i++)
        {
            var p = Vector2.Right.Rotated(_puffAngles[i] + t * 2) * Radius * (0.6f + t * 0.8f);
            DrawCircle(p with { Y = p.Y * GroundSquash } - new Vector2(0, 10), 3, Tint with { A = 1 - t });
        }
    }

    private void DrawFrost(float t)
    {
        var r = Radius * (0.3f + 0.7f * t);
        var alpha = 1 - t * t;
        Ground();
        DrawCircle(Vector2.Zero, r, new Color(0.75f, 0.95f, 1f, 0.25f * alpha));
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(0.85f, 0.98f, 1f, alpha), 3f);
        Reset();

        // Ice shards sticking up out of the ground around the ring.
        for (var i = 0; i < _puffAngles.Length; i++)
        {
            var dir = Vector2.Right.Rotated(_puffAngles[i]);
            var basePoint = new Vector2(dir.X * r, dir.Y * r * GroundSquash);
            var height = 22f * Mathf.Min(1, t * 4) * alpha;
            var shard = new[] { basePoint + new Vector2(-6, 0), basePoint + new Vector2(0, -height), basePoint + new Vector2(6, 0) };
            DrawColoredPolygon(shard, new Color(0.7f, 0.92f, 1f, alpha));
            DrawPolyline(new[] { shard[0], shard[1], shard[2] }, new Color(0.3f, 0.55f, 0.75f, alpha), 1.5f);
        }
    }

    private void DrawSlam(float t)
    {
        // A fridge door swinging down flat, then a shockwave.
        var swing = Mathf.Min(1, t * 4);
        var door = new Rect2(-14, -60, 28, 52);
        DrawSetTransform(Vector2.Zero, Mathf.Lerp(-1.4f, 0, swing), Vector2.One);
        if (t < 0.65f)
        {
            DrawRect(door.Grow(3), Palette.Ink);
            DrawRect(door, new Color(0.95f, 0.95f, 0.97f));
            DrawRect(new Rect2(8, -44, 4, 18), new Color(0.6f, 0.6f, 0.65f));
        }

        Reset();
        DrawShockwave(Mathf.Max(0, (t - 0.2f) / 0.8f));
    }

    private void DrawHeal(float t)
    {
        Ground();
        DrawCircle(Vector2.Zero, Radius, new Color(0.4f, 1f, 0.5f, 0.18f * (1 - t)));
        DrawArc(Vector2.Zero, Radius * (0.5f + 0.5f * t), 0, Mathf.Tau, 48, new Color(0.5f, 1f, 0.6f, 1 - t), 4f);
        Reset();

        // Plus signs floating up.
        for (var i = 0; i < _puffAngles.Length; i++)
        {
            var dir = Vector2.Right.Rotated(_puffAngles[i]);
            var p = new Vector2(dir.X, dir.Y * GroundSquash) * Radius * 0.6f - new Vector2(0, 20 + t * 60 + i % 3 * 10);
            var c = new Color(0.4f, 1f, 0.5f, 1 - t);
            DrawRect(new Rect2(p - new Vector2(7, 2.5f), new Vector2(14, 5)), c);
            DrawRect(new Rect2(p - new Vector2(2.5f, 7), new Vector2(5, 14)), c);
        }
    }

    private void DrawPull(float t)
    {
        // Rings shrinking inward with arrows: everything is being pulled to the middle.
        Ground();
        for (var i = 0; i < 3; i++)
        {
            var phase = Mathf.PosMod(1 - t * 1.5f + i / 3f, 1f);
            DrawArc(Vector2.Zero, Radius * phase, 0, Mathf.Tau, 48, Tint with { A = 0.9f * (1 - phase) + 0.1f }, 3f);
        }

        for (var i = 0; i < 6; i++)
        {
            var dir = Vector2.Right.Rotated(Mathf.Tau * i / 6 + t);
            var tip = dir * Radius * (0.45f - 0.2f * t);
            var back = dir * Radius * (0.75f - 0.2f * t);
            var side = dir.Orthogonal() * 8;
            DrawColoredPolygon(new[] { tip, back + side, back - side }, Tint with { A = 1 - t });
        }

        Reset();
    }

    private void DrawStatic(float t)
    {
        var r = Radius * (0.6f + 0.4f * t);
        var alpha = 1 - t;
        Ground();
        DrawCircle(Vector2.Zero, r, Tint with { A = 0.15f * alpha });
        Reset();

        // Crackling arcs around the ring.
        var points = new Vector2[25];
        for (var i = 0; i < points.Length; i++)
        {
            var a = Mathf.Tau * i / (points.Length - 1);
            var jitter = _rng.RandfRange(0.85f, 1.15f);
            points[i] = new Vector2(Mathf.Cos(a) * r * jitter, Mathf.Sin(a) * r * jitter * GroundSquash) - new Vector2(0, 14);
        }

        DrawPolyline(points, Colors.White with { A = alpha }, 2f);
        DrawPolyline(points, Tint with { A = alpha * 0.6f }, 5f);
    }

    private void DrawBeam(float t)
    {
        // A column of light from a saucer overhead.
        var alpha = t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f;
        var width = Radius * 0.9f;
        DrawRect(new Rect2(-width, -700, width * 2, 700), Tint with { A = 0.3f * alpha });
        DrawRect(new Rect2(-width * 0.5f, -700, width, 700), Colors.White with { A = 0.25f * alpha });
        Ground();
        DrawCircle(Vector2.Zero, width, Tint with { A = 0.4f * alpha });
        Reset();
    }

    private void DrawLightning(float t)
    {
        // A bolt from the sky, then a crackle on the ground.
        var alpha = 1 - t;
        var points = new Vector2[9];
        for (var i = 0; i < points.Length; i++)
        {
            var y = -600 + 600 * i / (float)(points.Length - 1);
            points[i] = new Vector2(i == 0 || i == points.Length - 1 ? 0 : _rng.RandfRange(-25, 25), y);
        }

        DrawPolyline(points, Tint with { A = alpha * 0.6f }, 10f);
        DrawPolyline(points, Colors.White with { A = alpha }, 4f);
        DrawExplosion(Mathf.Min(1, t * 1.3f));
    }

    private void DrawPunch(float t)
    {
        // A cartoon fist punching out with a "pow" star.
        var center = Facing * (10 + 20 * t) - new Vector2(0, 26);
        var star = new Vector2[16];
        for (var i = 0; i < star.Length; i++)
        {
            var r = i % 2 == 0 ? 30f : 14f;
            star[i] = center + Vector2.Right.Rotated(Mathf.Tau * i / star.Length) * r * (0.6f + 0.6f * t);
        }

        DrawColoredPolygon(star, new Color(1f, 0.85f, 0.3f, 1 - t));
        DrawCircle(center, 12, Palette.Ink);
        DrawCircle(center, 10, new Color(0.95f, 0.8f, 0.65f));
        var across = Facing.Orthogonal();
        for (var k = -1; k <= 1; k++)
        {
            DrawCircle(center + Facing * 8 + across * k * 6, 4, new Color(0.9f, 0.75f, 0.6f));
        }
    }

    private void DrawShockwave(float t)
    {
        var r = Radius * (0.4f + 0.6f * t);
        var alpha = 1 - t;
        Ground();
        DrawCircle(Vector2.Zero, r, Tint with { A = 0.2f * alpha });
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, Tint with { A = alpha }, 6f * alpha + 1);
        Reset();

        // Dust puffs kicked up around the edge.
        foreach (var angle in _puffAngles)
        {
            var dir = Vector2.Right.Rotated(angle);
            var p = new Vector2(dir.X, dir.Y * GroundSquash) * r - new Vector2(0, 6);
            DrawCircle(p, 6 + 6 * t, new Color(0.85f, 0.8f, 0.7f, 0.7f * alpha));
        }
    }

    private void DrawExplosion(float t)
    {
        // Yellow-orange fireballs that swell, then turn to grey smoke.
        var fire = new Color(1f, 0.75f, 0.2f).Lerp(new Color(0.9f, 0.3f, 0.1f), Mathf.Min(1, t * 2));
        var smoke = new Color(0.4f, 0.38f, 0.36f, 0.8f * (1 - t));
        var colour = t < 0.65f ? fire : fire.Lerp(smoke, (t - 0.65f) / 0.35f);
        for (var i = 0; i < _puffAngles.Length; i++)
        {
            var dir = Vector2.Right.Rotated(_puffAngles[i]);
            var p = new Vector2(dir.X, dir.Y * GroundSquash) * Radius * 0.55f * t - new Vector2(0, 20 + 30 * t);
            DrawCircle(p, Radius * (0.2f + 0.25f * t), colour);
        }

        DrawCircle(new Vector2(0, -20 - 30 * t), Radius * 0.35f * (1 - t * 0.5f), t < 0.4f ? Colors.White : colour);
        Ground();
        DrawArc(Vector2.Zero, Radius * (0.5f + 0.5f * t), 0, Mathf.Tau, 48, new Color(1f, 0.6f, 0.2f, 1 - t), 4f);
        Reset();
    }
}
