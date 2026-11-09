using Godot;

namespace ArmageddonOuttaHere.Combat;

/// <summary>A lightning bolt between two points (Sparky's chain attacks).</summary>
public partial class Bolt : Node2D
{
    private const double Duration = 0.2;
    private double _age;

    public Vector2 From { get; init; }

    public Vector2 To { get; init; }

    public Color Tint { get; init; } = Colors.White;

    public override void _Process(double delta)
    {
        _age += delta;
        if (_age >= Duration)
        {
            QueueFree();
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var rng = new RandomNumberGenerator();
        var points = new Vector2[8];
        var lift = new Vector2(0, -24);
        for (var i = 0; i < points.Length; i++)
        {
            var t = i / (float)(points.Length - 1);
            var jitter = i == 0 || i == points.Length - 1 ? Vector2.Zero : new Vector2(rng.RandfRange(-8, 8), rng.RandfRange(-8, 8));
            points[i] = From.Lerp(To, t) + lift + jitter;
        }

        DrawPolyline(points, Tint with { A = 1 - (float)(_age / Duration) }, 3f);
    }
}

/// <summary>Text that floats up and fades: damage numbers, "+5 scrap", "Found: The Fridge!".</summary>
public partial class FloatingText : Node2D
{
    private const double Duration = 1.4;
    private double _age;

    public string Text { get; init; } = string.Empty;

    public Color Tint { get; init; } = Colors.White;

    public int FontSize { get; init; } = 22;

    public override void _Ready() => ZIndex = 100;

    public override void _Process(double delta)
    {
        _age += delta;
        Position += new Vector2(0, -40f * (float)delta);
        if (_age >= Duration)
        {
            QueueFree();
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var alpha = 1 - (float)(_age / Duration);
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(Text, HorizontalAlignment.Left, -1, FontSize).X;
        var at = new Vector2(-width / 2, -50);
        DrawStringOutline(font, at, Text, HorizontalAlignment.Left, -1, FontSize, 6, Palette.Ink with { A = alpha });
        DrawString(font, at, Text, HorizontalAlignment.Left, -1, FontSize, Tint with { A = alpha });
    }
}
