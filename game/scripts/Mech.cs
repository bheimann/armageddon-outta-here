using Godot;

namespace ArmageddonOuttaHere;

/// <summary>
/// A player's mech. Moves with the left stick, faces the way it last moved, fires in that direction with Y and
/// dodge-rolls with A. Drawn as a placeholder cartoon shape in the player's color until real art exists.
/// </summary>
public partial class Mech : CharacterBody2D
{
    private const float Speed = 220f;
    private const float RollSpeed = 520f;
    private const double RollTime = 0.25;
    private const double RollCooldown = 0.6;
    private const double FireCooldown = 0.18;

    private double _rollLeft;
    private double _rollCooldownLeft;
    private double _fireCooldownLeft;
    private float _bob;

    public PadInput Input { get; set; } = new(PadInput.Keyboard);

    public Color PlayerColor { get; set; } = Colors.White;

    /// <summary>Which way the mech is facing (and aiming).</summary>
    public Vector2 Facing { get; private set; } = Vector2.Right;

    public bool IsRolling => _rollLeft > 0;

    public override void _Ready()
    {
        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = 18f } };
        AddChild(shape);
        MotionMode = MotionModeEnum.Floating;
    }

    public override void _PhysicsProcess(double delta)
    {
        _rollCooldownLeft -= delta;
        _fireCooldownLeft -= delta;

        if (Input.Move != Vector2.Zero)
        {
            Facing = Input.Move.Normalized();
        }

        if (IsRolling)
        {
            _rollLeft -= delta;
            Velocity = Facing * RollSpeed;
        }
        else
        {
            Velocity = Input.Move * Speed;
            if (Input.RollPressed && _rollCooldownLeft <= 0)
            {
                _rollLeft = RollTime;
                _rollCooldownLeft = RollCooldown;
            }
            else if (Input.AttackPressed && _fireCooldownLeft <= 0)
            {
                Fire();
            }
        }

        MoveAndSlide();
        _bob += (float)delta * (Velocity == Vector2.Zero ? 3f : 14f);
        QueueRedraw();
    }

    private void Fire()
    {
        _fireCooldownLeft = FireCooldown;
        GetParent().AddChild(new Bullet
        {
            Position = Position + Facing * 30f + new Vector2(0, -24f),
            Direction = Facing,
            Tint = PlayerColor,
        });
    }

    public override void _Draw()
    {
        // Shadow on the ground: the view is slightly angled, so the body sits above it.
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
        DrawCircle(Vector2.Zero, 22f, new Color(0, 0, 0, 0.25f));
        DrawSetTransform(Vector2.Zero);

        var lift = IsRolling ? 0f : Mathf.Sin(_bob) * 2f;
        var squash = IsRolling ? new Vector2(1.25f, 0.75f) : new Vector2(1f, 1f + Mathf.Sin(_bob) * 0.04f);
        var body = new Vector2(0, -26f + lift);
        var outline = PlayerColor.Darkened(0.5f);

        // Legs.
        DrawLine(new Vector2(-9, -12), new Vector2(-11, 0), outline, 6f);
        DrawLine(new Vector2(9, -12), new Vector2(11, 0), outline, 6f);

        // Body and cockpit.
        DrawSetTransform(body, 0, squash);
        DrawCircle(Vector2.Zero, 20f, outline);
        DrawCircle(Vector2.Zero, 17f, PlayerColor);
        DrawCircle(new Vector2(Facing.X * 6f, -4f + Facing.Y * 3f), 7f, new Color(0.75f, 0.95f, 1f));
        DrawSetTransform(Vector2.Zero);

        // Gun arm, pointing the way the mech faces.
        DrawLine(body, body + Facing * 30f, outline, 7f);
        DrawLine(body, body + Facing * 30f, Colors.DimGray, 4f);
    }
}
