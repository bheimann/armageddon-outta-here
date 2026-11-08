using System.Collections.Generic;
using Godot;

namespace ArmageddonOuttaHere.Garage;

/// <summary>Something in the garage a player can walk up to and use with A.</summary>
public enum StationKind
{
    Workbench,
    MissionBoard,
    Wardrobe,
    FilingCabinet,
}

public sealed record Station(StationKind Kind, string Name, Vector2 At);

/// <summary>Draws the garage: concrete floor, back wall, the stations, the side door and some clutter.</summary>
public partial class GarageRoom : Node2D
{
    public static readonly Rect2 Floor = new(-700, -400, 1400, 850);

    /// <summary>Where the side door is, on the right wall.</summary>
    public static readonly Vector2 Door = new(700, 150);

    /// <summary>Where a player appears after walking in.</summary>
    public static readonly Vector2 Entrance = new(590, 150);

    public static readonly IReadOnlyList<Station> Stations = new[]
    {
        new Station(StationKind.Workbench, "Workbench", new Vector2(-460, -330)),
        new Station(StationKind.MissionBoard, "Mission board", new Vector2(-40, -360)),
        new Station(StationKind.Wardrobe, "Wardrobe", new Vector2(380, -330)),
        new Station(StationKind.FilingCabinet, "Filing cabinet", new Vector2(-590, 170)),
    };

    private static readonly Color Concrete = new(0.62f, 0.62f, 0.6f);
    private static readonly Color Wall = new(0.72f, 0.6f, 0.48f);
    private static readonly Color Wood = new(0.6f, 0.42f, 0.25f);

    public override void _Draw()
    {
        DrawRect(Floor.Grow(600), new Color(0.2f, 0.18f, 0.16f));
        DrawRect(Floor, Concrete);
        for (var x = Floor.Position.X; x < Floor.End.X; x += 200)
        {
            DrawLine(new Vector2(x, Floor.Position.Y), new Vector2(x, Floor.End.Y), Concrete.Darkened(0.08f), 2f);
        }

        // Back wall with a window, and the oil stain every garage has.
        DrawRect(new Rect2(Floor.Position.X, Floor.Position.Y - 160, Floor.Size.X, 160), Wall);
        DrawRect(new Rect2(Floor.Position.X, Floor.Position.Y - 12, Floor.Size.X, 12), Wall.Darkened(0.3f));
        DrawRect(new Rect2(140, -530, 120, 80), Palette.Ink);
        DrawRect(new Rect2(146, -524, 108, 68), new Color(0.95f, 0.55f, 0.3f));
        DrawSetTransform(new Vector2(60, 120), 0, new Vector2(1.6f, 0.6f));
        DrawCircle(Vector2.Zero, 60, new Color(0.25f, 0.25f, 0.25f, 0.35f));
        DrawSetTransform(Vector2.Zero);

        DrawWorkbench(Stations[0].At);
        DrawMissionBoard(Stations[1].At);
        DrawWardrobe(Stations[2].At);
        DrawFilingCabinet(Stations[3].At);
        DrawDoor();
        DrawClutter();

        foreach (var station in Stations)
        {
            UI.Ui.Text(this, station.Name, station.At + new Vector2(0, -110), 20, Colors.White, center: true);
        }

        DrawRect(Floor, Palette.Ink, false, 6f);
    }

    private void DrawWorkbench(Vector2 at)
    {
        DrawRect(new Rect2(at + new Vector2(-110, -40), new Vector2(220, 50)), Palette.Ink);
        DrawRect(new Rect2(at + new Vector2(-106, -36), new Vector2(212, 20)), Wood);
        DrawRect(new Rect2(at + new Vector2(-100, -16), new Vector2(12, 24)), Wood.Darkened(0.3f));
        DrawRect(new Rect2(at + new Vector2(88, -16), new Vector2(12, 24)), Wood.Darkened(0.3f));
        // Pegboard with tools.
        DrawRect(new Rect2(at + new Vector2(-100, -100), new Vector2(200, 56)), new Color(0.75f, 0.62f, 0.45f));
        DrawLine(at + new Vector2(-70, -90), at + new Vector2(-70, -55), Colors.DimGray, 6f);
        DrawLine(at + new Vector2(-20, -92), at + new Vector2(10, -60), Colors.IndianRed, 6f);
        DrawCircle(at + new Vector2(50, -72), 14, Colors.DimGray);
    }

    private void DrawMissionBoard(Vector2 at)
    {
        DrawRect(new Rect2(at + new Vector2(-90, -100), new Vector2(180, 90)), Palette.Ink);
        DrawRect(new Rect2(at + new Vector2(-86, -96), new Vector2(172, 82)), new Color(0.75f, 0.55f, 0.35f));
        DrawRect(new Rect2(at + new Vector2(-70, -86), new Vector2(46, 56)), Colors.WhiteSmoke);
        DrawRect(new Rect2(at + new Vector2(-14, -80), new Vector2(46, 56)), new Color(1f, 0.95f, 0.6f));
        DrawRect(new Rect2(at + new Vector2(40, -88), new Vector2(36, 46)), Colors.WhiteSmoke);
        DrawCircle(at + new Vector2(-47, -84), 4, Colors.Red);
        DrawCircle(at + new Vector2(9, -78), 4, Colors.Red);
        DrawCircle(at + new Vector2(58, -86), 4, Colors.Red);
    }

    private void DrawWardrobe(Vector2 at)
    {
        DrawRect(new Rect2(at + new Vector2(-60, -120), new Vector2(120, 130)), Palette.Ink);
        DrawRect(new Rect2(at + new Vector2(-56, -116), new Vector2(112, 122)), Wood.Lightened(0.15f));
        DrawLine(at + new Vector2(0, -116), at + new Vector2(0, 6), Palette.Ink, 3f);
        DrawCircle(at + new Vector2(-8, -50), 4, Palette.Highlight);
        DrawCircle(at + new Vector2(8, -50), 4, Palette.Highlight);
        // A paint can for changing colors.
        DrawRect(new Rect2(at + new Vector2(70, -30), new Vector2(34, 38)), Palette.Ink);
        DrawRect(new Rect2(at + new Vector2(73, -27), new Vector2(28, 32)), new Color(0.3f, 0.6f, 0.95f));
    }

    private void DrawFilingCabinet(Vector2 at)
    {
        DrawRect(new Rect2(at + new Vector2(-40, -110), new Vector2(80, 120)), Palette.Ink);
        DrawRect(new Rect2(at + new Vector2(-36, -106), new Vector2(72, 112)), new Color(0.55f, 0.6f, 0.62f));
        for (var i = 0; i < 3; i++)
        {
            var y = -96 + i * 36;
            DrawRect(new Rect2(at + new Vector2(-30, y), new Vector2(60, 30)), new Color(0.48f, 0.52f, 0.55f));
            DrawRect(new Rect2(at + new Vector2(-10, y + 12), new Vector2(20, 5)), Palette.Ink);
        }
    }

    private void DrawDoor()
    {
        DrawRect(new Rect2(Door + new Vector2(-14, -70), new Vector2(28, 110)), Palette.Ink);
        DrawRect(new Rect2(Door + new Vector2(-10, -66), new Vector2(20, 102)), Wood);
        DrawCircle(Door + new Vector2(-4, -10), 4, Palette.Highlight);
        UI.Ui.Text(this, "Side door", Door + new Vector2(-30, -82), 18, Colors.White, right: true);
    }

    private void DrawClutter()
    {
        // A lawnmower, a stack of tires and some boxes, to make it feel lived in.
        var mower = new Vector2(-300, 300);
        DrawRect(new Rect2(mower + new Vector2(-40, -30), new Vector2(80, 34)), new Color(0.3f, 0.6f, 0.3f));
        DrawLine(mower + new Vector2(30, -30), mower + new Vector2(70, -90), Palette.Ink, 5f);
        DrawCircle(mower + new Vector2(-28, 6), 10, Palette.Ink);
        DrawCircle(mower + new Vector2(28, 6), 10, Palette.Ink);

        var tires = new Vector2(320, 340);
        for (var i = 0; i < 3; i++)
        {
            DrawSetTransform(tires + new Vector2(0, -i * 16), 0, new Vector2(1f, 0.45f));
            DrawCircle(Vector2.Zero, 36, Palette.Ink);
            DrawCircle(Vector2.Zero, 16, Concrete.Darkened(0.2f));
            DrawSetTransform(Vector2.Zero);
        }

        DrawRect(new Rect2(-660, 330, 70, 60), new Color(0.7f, 0.55f, 0.35f));
        DrawRect(new Rect2(-620, 300, 60, 50), new Color(0.75f, 0.6f, 0.4f));
    }
}
