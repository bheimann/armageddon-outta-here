using System.Linq;
using ArmageddonOuttaHere.Actors;
using ArmageddonOuttaHere.Combat;
using ArmageddonOuttaHere.Core;
using ArmageddonOuttaHere.Levels;
using Godot;

namespace ArmageddonOuttaHere.UI;

/// <summary>
/// The level's on-screen info: each player's panel in a corner (health, scrap, the 5 slots), the base's health,
/// the wave or boss at the top, banners in the middle, and the win or lose screen.
/// </summary>
public partial class LevelHud : Control
{
    private static readonly string[] SlotLabels = { "B", "LB", "RB", "LT", "RT" };

    private string _banner = string.Empty;
    private double _bannerLeft;

    public Level Level { get; set; } = null!;

    public void Banner(string text)
    {
        _banner = text;
        _bannerLeft = 2.5;
    }

    public override void _Process(double delta)
    {
        _bannerLeft -= delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Level is null)
        {
            return;
        }

        var size = GetViewportRect().Size;
        DrawCrateArrows(size);
        DrawTop(size);
        for (var i = 0; i < Level.Mechs.Count; i++)
        {
            DrawPlayer(Level.Mechs[i], i, size);
        }

        if (_bannerLeft > 0 && !string.IsNullOrEmpty(_banner))
        {
            var alpha = Mathf.Clamp((float)_bannerLeft * 2, 0, 1);
            Text(_banner, new Vector2(size.X / 2, size.Y * 0.32f), 40, Palette.Highlight with { A = alpha }, center: true);
        }

        if (Level.State is LevelState.Won or LevelState.Lost)
        {
            DrawEnd(size);
        }
    }

    private void DrawTop(Vector2 size)
    {
        var mid = size.X / 2;
        var info = Level.Info;
        var line = Level.State switch
        {
            LevelState.Intro => $"Level {info.Number}: {info.Name}",
            LevelState.Break when Level.WaveIndex < Level.WaveCount =>
                $"Wave {Level.WaveIndex + 1} in {Mathf.CeilToInt(10 - Level.StateTime)}",
            LevelState.Break => $"Boss in {Mathf.CeilToInt(10 - Level.StateTime)}",
            LevelState.Boss => info.Boss,
            _ => $"Wave {Mathf.Min(Level.WaveIndex + 1, Level.WaveCount)} of {Level.WaveCount}",
        };
        Text(line, new Vector2(mid, 34), 26, Colors.White, center: true);

        var flash = Level.Spot.UnderAttack && Time.GetTicksMsec() / 250 % 2 == 0;
        var barRect = new Rect2(mid - 160, 46, 320, 14);
        Bar(barRect, Level.Spot.Health / DefendedSpot.MaxHealth, flash ? new Color(1f, 0.35f, 0.3f) : new Color(0.4f, 0.7f, 1f));
        if (Level.Spot.UnderAttack)
        {
            DrawRect(barRect.Grow(4), new Color(1f, 0.3f, 0.25f), false, 3f);
            Text($"The {info.Spot.ToLower()} is under attack!", new Vector2(mid, 80), 20,
                flash ? new Color(1f, 0.4f, 0.35f) : Colors.White, center: true);
        }
        else
        {
            Text($"{info.Spot}", new Vector2(mid, 78), 16, Colors.White, center: true);
        }

        if (Level.CurrentBoss is { } boss)
        {
            Bar(new Rect2(mid - 260, 92, 520, 16), boss.Health / boss.Stats.Health, new Color(0.95f, 0.3f, 0.25f));
        }
    }

    private void DrawPlayer(Mech mech, int index, Vector2 size)
    {
        const float width = 270f;
        const float height = 104f;
        var right = index % 2 == 1;
        var bottom = index >= 2;
        var at = new Vector2(right ? size.X - width - 12 : 12, bottom ? size.Y - height - 12 : 12);
        var panel = new Rect2(at, new Vector2(width, height));
        DrawRect(panel, Palette.Panel);
        DrawRect(panel, mech.Player.Color, false, 3f);

        var character = mech.Player.Character;
        Text(character.Name, at + new Vector2(10, 24), 18, mech.Player.Color.Lightened(0.3f));
        Text($"{character.Scrap} scrap", at + new Vector2(width - 10, 24), 16, Palette.Highlight, alignRight: true);

        if (mech.IsDown)
        {
            Text("DOWN - stand next to them to revive", at + new Vector2(10, 48), 13, new Color(1f, 0.5f, 0.45f));
        }
        else
        {
            Bar(new Rect2(at + new Vector2(10, 34), new Vector2(width - 20, 12)), mech.Health / Mech.MaxHealth, Palette.Health);
        }

        // The 5 slot buttons and what each one selects.
        var slots = mech.Slots;
        var box = (width - 20 - 4 * 4) / 5f;
        for (var s = 0; s < 5; s++)
        {
            var rect = new Rect2(at + new Vector2(10 + s * (box + 4), 56), new Vector2(box, 40));
            var used = s < slots.Count;
            DrawRect(rect, used ? new Color(0.25f, 0.22f, 0.2f) : new Color(0.15f, 0.13f, 0.12f));
            if (used && s == mech.SelectedSlot)
            {
                DrawRect(rect, Palette.Highlight, false, 3f);
            }

            Text(SlotLabels[s], rect.Position + new Vector2(4, 13), 11, Palette.Disabled);
            if (used)
            {
                var (_, item, attack) = slots[s];
                var name = Attacks.For(item, attack).Name;
                Text(Short(name), rect.Position + new Vector2(box / 2, 32), 11, Colors.White, center: true);
            }
        }
    }

    private void DrawEnd(Vector2 size)
    {
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0, 0, 0, 0.55f));
        var won = Level.State == LevelState.Won;
        Text(won ? "Level complete!" : $"The {Level.Info.Spot.ToLower()} fell!", size / 2 - new Vector2(0, 60), 54,
            won ? Palette.Highlight : new Color(1f, 0.45f, 0.4f), center: true);
        var next = won && Level.Info.Number < LevelCatalog.All.Count ? $"Level {Level.Info.Number + 1} unlocked. " : string.Empty;
        Text(next + "Everyone keeps their scrap and finds.", size / 2, 24, Colors.White, center: true);
        var finds = Level.Finds.Count == 0 ? "No crates opened" : "Found: " + string.Join(", ", Level.Finds);
        Text(finds, size / 2 + new Vector2(0, 40), 20, Palette.Highlight, center: true);
        Text("Back to the garage...  (A)", size / 2 + new Vector2(0, 90), 20, Palette.Disabled, center: true);
    }

    /// <summary>Arrows at the screen edge pointing to supply crates that are off screen.</summary>
    private void DrawCrateArrows(Vector2 size)
    {
        if (Level.State is LevelState.Won or LevelState.Lost)
        {
            return;
        }

        var toScreen = Level.GetViewport().GetCanvasTransform();
        var center = size / 2;
        var inner = new Rect2(Vector2.Zero, size).Grow(-40);
        var pulse = 0.75f + 0.25f * Mathf.Sin(Time.GetTicksMsec() / 150f);
        foreach (var crate in Level.Crates)
        {
            var at = toScreen * crate.GlobalPosition;
            if (inner.HasPoint(at))
            {
                continue;
            }

            // Where the line from the middle of the screen to the crate crosses the screen edge.
            var direction = (at - center).Normalized();
            var scale = Mathf.Min(
                Mathf.Abs(direction.X) > 0.001f ? (inner.Size.X / 2) / Mathf.Abs(direction.X) : float.MaxValue,
                Mathf.Abs(direction.Y) > 0.001f ? (inner.Size.Y / 2) / Mathf.Abs(direction.Y) : float.MaxValue);
            var tip = center + direction * scale;
            var side = direction.Orthogonal() * 14 * pulse;
            var back = tip - direction * 30 * pulse;
            DrawColoredPolygon(new[] { tip + direction * 4, back + side * 1.2f, back - side * 1.2f }, Palette.Ink);
            DrawColoredPolygon(new[] { tip, back + side, back - side }, Palette.Highlight);
            Text("Crate", back - direction * 22 + new Vector2(0, 6), 14, Palette.Highlight, center: true);
        }
    }

    private void Bar(Rect2 rect, float fraction, Color color)
    {
        DrawRect(rect.Grow(2), Palette.Ink);
        DrawRect(rect, Palette.HealthBack);
        DrawRect(rect with { Size = new Vector2(rect.Size.X * Mathf.Clamp(fraction, 0, 1), rect.Size.Y) }, color);
    }

    private void Text(string text, Vector2 at, int fontSize, Color color, bool center = false, bool alignRight = false)
    {
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X;
        var x = center ? at.X - width / 2 : alignRight ? at.X - width : at.X;
        var pos = new Vector2(x, at.Y);
        DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, fontSize, 5, Palette.Ink with { A = color.A });
        DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    private static string Short(string name) =>
        name.Length <= 9 ? name : string.Concat(name.Split(' ', '-').Where(w => w.Length > 0).Select(w => w[0]));
}
