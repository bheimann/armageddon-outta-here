using Godot;

namespace ArmageddonOuttaHere;

/// <summary>A placeholder pistol shot. Flies straight and disappears after a short time.</summary>
public partial class Bullet : Node2D
{
    private const float Speed = 700f;
    private double _life = 0.8;

    public Vector2 Direction { get; set; } = Vector2.Right;

    public Color Tint { get; set; } = Colors.Yellow;

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
        _life -= delta;
        if (_life <= 0)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 6f, Tint.Darkened(0.4f));
        DrawCircle(Vector2.Zero, 4f, Tint.Lightened(0.5f));
    }
}
