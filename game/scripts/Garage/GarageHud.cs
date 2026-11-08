using ArmageddonOuttaHere.UI;
using Godot;

namespace ArmageddonOuttaHere.Garage;

/// <summary>Shown over the garage when nobody has joined yet: the title and "Press Start to join".</summary>
public partial class GarageHud : Control
{
    public GarageScene Garage { get; set; } = null!;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (Garage is null)
        {
            return;
        }

        var size = GetViewportRect().Size;
        if (Garage.PaneCount == 3)
        {
            // The empty quarter invites a fourth player.
            var empty = new Rect2(size / 2, size / 2);
            DrawRect(empty, new Color(0.13f, 0.11f, 0.1f));
            Ui.Text(this, "Press Start to join", empty.GetCenter(), 26, Colors.White, center: true);
            return;
        }

        if (Garage.PaneCount > 0)
        {
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, size), new Color(0, 0, 0, 0.35f));
        Ui.Text(this, "ARMAGEDDON OUTTA HERE", new Vector2(size.X / 2, size.Y * 0.38f), 64, Palette.Highlight, center: true);
        var pulse = 0.6f + 0.4f * Mathf.Sin(Time.GetTicksMsec() / 300f);
        Ui.Text(this, "Press Start to join", new Vector2(size.X / 2, size.Y * 0.55f), 32, Colors.White with { A = pulse }, center: true);
        Ui.Text(this, $"Save {Session.Instance.Slot}", new Vector2(size.X / 2, size.Y * 0.62f), 20, Colors.LightGray, center: true);
        Ui.Text(this, "Keyboard: Enter", new Vector2(size.X / 2, size.Y - 30), 16, Palette.Disabled, center: true);
    }
}
