using Godot;

namespace ArmageddonOuttaHere;

/// <summary>The game's colors in one place, so the cartoon look stays consistent.</summary>
public static class Palette
{
    private static readonly (string Name, Color Color)[] Players =
    {
        ("Red", new Color(0.95f, 0.35f, 0.3f)),
        ("Blue", new Color(0.3f, 0.6f, 0.95f)),
        ("Green", new Color(0.4f, 0.85f, 0.35f)),
        ("Yellow", new Color(0.98f, 0.8f, 0.25f)),
        ("Purple", new Color(0.7f, 0.45f, 0.95f)),
        ("Orange", new Color(1f, 0.6f, 0.2f)),
        ("Pink", new Color(1f, 0.55f, 0.8f)),
        ("Teal", new Color(0.25f, 0.85f, 0.8f)),
    };

    public static int PlayerColorCount => Players.Length;

    public static Color Player(int index) => Players[Wrap(index)].Color;

    public static string PlayerName(int index) => Players[Wrap(index)].Name;

    public static int Wrap(int index) => ((index % Players.Length) + Players.Length) % Players.Length;

    public static readonly Color Ground = new(0.8f, 0.68f, 0.48f);
    public static readonly Color GroundLine = new(0.7f, 0.58f, 0.4f);
    public static readonly Color Ink = new(0.15f, 0.12f, 0.1f);
    public static readonly Color Shadow = new(0, 0, 0, 0.25f);
    public static readonly Color Health = new(0.35f, 0.85f, 0.35f);
    public static readonly Color HealthBack = new(0.2f, 0.1f, 0.1f, 0.8f);
    public static readonly Color Panel = new(0.1f, 0.08f, 0.07f, 0.85f);
    public static readonly Color Highlight = new(1f, 0.85f, 0.3f);
    public static readonly Color Disabled = new(0.55f, 0.5f, 0.45f);
}
