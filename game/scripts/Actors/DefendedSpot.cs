using ArmageddonOuttaHere.Combat;
using Godot;

namespace ArmageddonOuttaHere.Actors;

/// <summary>
/// The spot a level is about: a gas station, a radio tower or a school bus. If it's destroyed, the level is lost.
/// It slowly repairs itself between waves, and faster while players stand next to it.
/// </summary>
public partial class DefendedSpot : Node2D, IHittable
{
    public const float MaxHealth = 600f;

    /// <summary>Repair per second between waves, with nobody helping.</summary>
    public const float AutoRepair = 8f;

    /// <summary>Repair per second for each player standing next to it.</summary>
    public const float PlayerRepair = 25f;

    /// <summary>How close a player has to stand to repair it.</summary>
    public const float RepairRange = 130f;

    private double _hurtFlash;

    /// <summary>Seconds since an enemy last hit it (for the "under attack" warning).</summary>
    public double SinceHit { get; private set; } = 99;

    /// <summary>True for a couple of seconds after each hit.</summary>
    public bool UnderAttack => SinceHit < 2.5;

    public string Spot { get; init; } = "Gas station";

    public float Health { get; private set; } = MaxHealth;

    public float HitRadius => 70f;

    /// <summary>Solid area mechs can't walk through.</summary>
    public float SolidRadius => 60f;

    public bool IsAlive => Health > 0;

    public bool IsTeam => true;

    /// <summary>True while players are repairing it (for the sparkle).</summary>
    public bool BeingRepaired { get; set; }

    public override void _Ready() => AddToGroup(IHittable.Group);

    public override void _Process(double delta)
    {
        _hurtFlash -= delta;
        SinceHit += delta;
        QueueRedraw();
    }

    public void Hit(Hit hit)
    {
        // Players can never damage the spot they are defending.
        if (!IsAlive || hit.Damage <= 0 || hit.ByTeam)
        {
            return;
        }

        Health = Mathf.Max(0, Health - hit.Damage);
        _hurtFlash = 0.1;
        SinceHit = 0;
    }

    public void Repair(float amount) => Health = Mathf.Min(MaxHealth, Health + amount);

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
        DrawCircle(Vector2.Zero, 90f, Palette.Shadow);
        DrawSetTransform(Vector2.Zero);

        var flash = _hurtFlash > 0;
        switch (Spot)
        {
            case "Radio tower":
                DrawRadioTower(flash);
                break;
            case "School bus":
                DrawBus(flash);
                break;
            default:
                DrawGasStation(flash);
                break;
        }

        if (UnderAttack && Time.GetTicksMsec() / 250 % 2 == 0)
        {
            // A flashing warning sign over the spot while enemies are hitting it.
            var top = new Vector2(0, Spot == "Radio tower" ? -250 : -160);
            DrawCircle(top, 22, Palette.Ink);
            DrawCircle(top, 19, new Color(0.95f, 0.25f, 0.2f));
            UI.Ui.Text(this, "!", top + new Vector2(0, 10), 30, Colors.White, center: true);
        }

        if (BeingRepaired)
        {
            var t = (float)Time.GetTicksMsec() / 150f;
            DrawString(ThemeDB.FallbackFont, new Vector2(40 + Mathf.Sin(t) * 6, -120), "+", HorizontalAlignment.Left, -1, 28, Palette.Health);
        }
    }

    private void DrawGasStation(bool flash)
    {
        var wall = flash ? Colors.White : new Color(0.9f, 0.9f, 0.85f);
        DrawRect(new Rect2(-70, -90, 140, 90), Palette.Ink);
        DrawRect(new Rect2(-66, -86, 132, 84), wall);
        DrawRect(new Rect2(-80, -110, 160, 26), new Color(0.85f, 0.25f, 0.2f));
        DrawString(ThemeDB.FallbackFont, new Vector2(-28, -90), "GAS", HorizontalAlignment.Left, -1, 20, Colors.White);
        DrawRect(new Rect2(-20, -50, 40, 48), new Color(0.4f, 0.6f, 0.8f));
        DrawRect(new Rect2(-100, -45, 18, 45), new Color(0.85f, 0.25f, 0.2f));
        DrawRect(new Rect2(82, -45, 18, 45), new Color(0.85f, 0.25f, 0.2f));
    }

    private void DrawRadioTower(bool flash)
    {
        var metal = flash ? Colors.White : new Color(0.75f, 0.3f, 0.25f);
        DrawRect(new Rect2(-60, -50, 120, 50), Palette.Ink);
        DrawRect(new Rect2(-56, -46, 112, 44), flash ? Colors.White : new Color(0.7f, 0.7f, 0.65f));
        DrawLine(new Vector2(-30, -50), new Vector2(0, -200), metal, 6f);
        DrawLine(new Vector2(30, -50), new Vector2(0, -200), metal, 6f);
        for (var y = -80; y > -190; y -= 30)
        {
            var half = 30f * (200 + y) / 150f;
            DrawLine(new Vector2(-half, y), new Vector2(half, y), metal, 3f);
        }

        DrawCircle(new Vector2(0, -205), 7f, new Color(1f, 0.3f, 0.3f));
    }

    private void DrawBus(bool flash)
    {
        var paint = flash ? Colors.White : new Color(0.98f, 0.78f, 0.15f);
        DrawRect(new Rect2(-110, -80, 220, 76), Palette.Ink);
        DrawRect(new Rect2(-106, -76, 212, 68), paint);
        for (var x = -95; x < 80; x += 35)
        {
            DrawRect(new Rect2(x, -68, 26, 22), new Color(0.5f, 0.7f, 0.85f));
        }

        DrawRect(new Rect2(-106, -30, 212, 6), Palette.Ink);
        DrawCircle(new Vector2(-70, -4), 14f, Palette.Ink);
        DrawCircle(new Vector2(70, -4), 14f, Palette.Ink);
    }
}
