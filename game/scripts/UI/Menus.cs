using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArmageddonOuttaHere.UI;

/// <summary>Something a player is looking at in their part of the screen: a menu or the name keyboard.</summary>
public interface IPanel
{
    /// <summary>Reacts to this frame's input (A selects, B goes back, the stick moves).</summary>
    void Handle(PadInput input);

    void Draw(Control canvas, Rect2 area);
}

/// <summary>One line in a menu. <paramref name="Side"/> handles left / right (such as changing a color).</summary>
public sealed record MenuItem(string Text, Action? Select = null, bool Enabled = true, Action<int>? Side = null);

/// <summary>
/// A list menu. Its items are rebuilt every frame from <see cref="Items"/>, so it always shows the latest scrap,
/// levels and loadout.
/// </summary>
public sealed class Menu : IPanel
{
    private int _index;

    public Menu(string title, Func<IReadOnlyList<MenuItem>> items, Action? back)
    {
        Title = title;
        Items = items;
        Back = back;
    }

    public string Title { get; }

    /// <summary>A line under the title (scrap, slots used, the result of the last action...).</summary>
    public Func<string>? Subtitle { get; init; }

    public Func<IReadOnlyList<MenuItem>> Items { get; }

    /// <summary>B: usually closes the menu.</summary>
    public Action? Back { get; }

    public void Handle(PadInput input)
    {
        var items = Items();
        if (items.Count == 0)
        {
            if (input.CancelPressed)
            {
                Back?.Invoke();
            }

            return;
        }

        _index = Mathf.PosMod(_index + input.NavigateY, items.Count);
        var item = items[_index];
        if (input.NavigateX != 0 && item.Enabled)
        {
            item.Side?.Invoke(input.NavigateX);
        }

        if (input.AcceptPressed && item.Enabled)
        {
            item.Select?.Invoke();
        }
        else if (input.CancelPressed)
        {
            Back?.Invoke();
        }
    }

    public void Draw(Control canvas, Rect2 area)
    {
        var items = Items();
        _index = items.Count == 0 ? 0 : Mathf.Clamp(_index, 0, items.Count - 1);
        var subtitle = Subtitle?.Invoke();
        const int lineHeight = 30;
        var header = 52 + (subtitle is null ? 0 : 26);
        const int footer = 34;

        // Long lists turn into a spinner: an odd number of rows with the selected item always in the middle.
        const int maxRows = 9;
        var fits = Mathf.Clamp((int)((area.Size.Y - 40 - header - footer) / lineHeight), 3, maxRows);
        var spinner = items.Count > fits;
        var rows = spinner ? (fits % 2 == 1 ? fits : fits - 1) : items.Count;

        var width = Mathf.Min(area.Size.X - 40, 560);
        var height = header + rows * lineHeight + footer;
        var box = new Rect2(area.Position + (area.Size - new Vector2(width, height)) / 2, new Vector2(width, height));
        Ui.Box(canvas, box);

        Ui.Text(canvas, Title, new Vector2(box.Position.X + 20, box.Position.Y + 40), 26, Palette.Highlight);
        if (subtitle is not null)
        {
            Ui.Text(canvas, subtitle, new Vector2(box.Position.X + 20, box.Position.Y + 66), 16, Colors.LightGray);
        }

        var listTop = box.Position.Y + header;
        if (spinner)
        {
            DrawSpinner(canvas, items, box, listTop, rows, lineHeight);
        }
        else
        {
            _scroll = _index;
            for (var i = 0; i < items.Count; i++)
            {
                DrawRow(canvas, items[i], i == _index, box, listTop + (i + 1) * lineHeight, 1f);
            }
        }

        Ui.Text(canvas, "A: select    B: back", new Vector2(box.Position.X + 20, box.End.Y - 12), 13, Palette.Disabled);
    }

    /// <summary>How far the spinner has scrolled; slides toward the selected item so the list visibly shifts.</summary>
    private float _scroll;

    private void DrawSpinner(Control canvas, IReadOnlyList<MenuItem> items, Rect2 box, float listTop, int rows, int lineHeight)
    {
        // Snap on big jumps (wrapping from the bottom to the top), slide on single steps.
        _scroll = Mathf.Abs(_scroll - _index) > 2 ? _index : Mathf.Lerp(_scroll, _index, 0.3f);
        var half = rows / 2;
        var centerY = listTop + (half + 1) * lineHeight;

        // The selection bar stays put in the middle; the items move past it.
        canvas.DrawRect(new Rect2(box.Position.X + 10, centerY - 22, box.Size.X - 20, 28), new Color(1, 1, 1, 0.08f));
        Ui.Text(canvas, ">", new Vector2(box.Position.X + 16, centerY), 20, Palette.Highlight);

        for (var i = 0; i < items.Count; i++)
        {
            var offset = i - _scroll;
            if (Mathf.Abs(offset) > half + 0.5f)
            {
                continue;
            }

            var fade = 1f - Mathf.Abs(offset) / (half + 1) * 0.7f;
            DrawRow(canvas, items[i], i == _index, box, centerY + offset * lineHeight, fade, marker: false);
        }

        if (_index > half)
        {
            Ui.Text(canvas, "▲", new Vector2(box.End.X - 30, listTop + lineHeight - 4), 16, Palette.Disabled);
        }

        if (_index < items.Count - 1 - half)
        {
            Ui.Text(canvas, "▼", new Vector2(box.End.X - 30, listTop + rows * lineHeight), 16, Palette.Disabled);
        }

        Ui.Text(canvas, $"{_index + 1} / {items.Count}", new Vector2(box.End.X - 20, box.End.Y - 12), 13, Palette.Disabled, right: true);
    }

    private static void DrawRow(Control canvas, MenuItem item, bool selected, Rect2 box, float y, float alpha, bool marker = true)
    {
        var color = !item.Enabled ? Palette.Disabled : selected ? Colors.White : Colors.LightGray;
        if (selected && marker)
        {
            canvas.DrawRect(new Rect2(box.Position.X + 10, y - 22, box.Size.X - 20, 28), new Color(1, 1, 1, 0.08f));
            Ui.Text(canvas, ">", new Vector2(box.Position.X + 16, y), 20, Palette.Highlight);
        }

        Ui.Text(canvas, item.Text, new Vector2(box.Position.X + 38, y), 19, color with { A = alpha });
    }
}

/// <summary>An on-screen keyboard for typing a character's name with a controller.</summary>
public sealed class NameEntry : IPanel
{
    public const int MaxLength = 12;

    private static readonly string[] Rows = { "ABCDEFG", "HIJKLMN", "OPQRSTU", "VWXYZ-'" };
    private const int ActionRow = 4;

    private readonly Action<string> _done;
    private readonly Action _back;
    private string _name;
    private int _row;
    private int _col;

    public NameEntry(string title, string start, Action<string> done, Action back)
    {
        Title = title;
        _name = start;
        _done = done;
        _back = back;
    }

    public string Title { get; }

    /// <summary>Action row: space, delete, done.</summary>
    private static readonly string[] Actions = { "Space", "Delete", "Done" };

    public void Handle(PadInput input)
    {
        _row = Mathf.PosMod(_row + input.NavigateY, Rows.Length + 1);
        var width = _row == ActionRow ? Actions.Length : Rows[_row].Length;
        _col = Mathf.PosMod(Mathf.Min(_col, width - 1) + input.NavigateX, width);

        if (input.AcceptPressed)
        {
            if (_row < ActionRow)
            {
                Type(Rows[_row][_col].ToString());
            }
            else if (_col == 0)
            {
                Type(" ");
            }
            else if (_col == 1)
            {
                Delete();
            }
            else if (_name.Trim().Length > 0)
            {
                _done(_name.Trim());
            }
        }
        else if (input.CancelPressed)
        {
            if (_name.Length > 0)
            {
                Delete();
            }
            else
            {
                _back();
            }
        }
    }

    private void Type(string text)
    {
        if (_name.Length < MaxLength)
        {
            // Capital first letter, lowercase after, so "DUSTY" types as "Dusty".
            _name += _name.Length == 0 || _name.EndsWith(' ') ? text : text.ToLowerInvariant();
        }
    }

    private void Delete() => _name = _name.Length > 0 ? _name[..^1] : _name;

    public void Draw(Control canvas, Rect2 area)
    {
        const int key = 44;
        var width = Mathf.Min(area.Size.X - 40, 7 * (key + 6) + 40);
        var height = 120 + (Rows.Length + 1) * (key + 6) + 40;
        var box = new Rect2(area.Position + (area.Size - new Vector2(width, height)) / 2, new Vector2(width, height));
        Ui.Box(canvas, box);
        Ui.Text(canvas, Title, box.Position + new Vector2(20, 38), 24, Palette.Highlight);

        var field = new Rect2(box.Position + new Vector2(20, 54), new Vector2(width - 40, 40));
        canvas.DrawRect(field, new Color(0, 0, 0, 0.5f));
        var cursor = Time.GetTicksMsec() / 500 % 2 == 0 ? "_" : " ";
        Ui.Text(canvas, _name + cursor, field.Position + new Vector2(10, 29), 24, Colors.White);

        var top = box.Position.Y + 110;
        for (var r = 0; r < Rows.Length; r++)
        {
            for (var c = 0; c < Rows[r].Length; c++)
            {
                var rect = new Rect2(box.Position.X + 20 + c * (key + 6), top + r * (key + 6), key, key);
                DrawKey(canvas, rect, Rows[r][c].ToString(), r == _row && c == _col);
            }
        }

        var actionWidth = (width - 40 - 12) / 3f;
        for (var c = 0; c < Actions.Length; c++)
        {
            var rect = new Rect2(box.Position.X + 20 + c * (actionWidth + 6), top + ActionRow * (key + 6), actionWidth, key);
            DrawKey(canvas, rect, Actions[c], _row == ActionRow && c == _col);
        }

        Ui.Text(canvas, "A: type    B: delete / back", new Vector2(box.Position.X + 20, box.End.Y - 12), 13, Palette.Disabled);
    }

    private static void DrawKey(Control canvas, Rect2 rect, string label, bool selected)
    {
        canvas.DrawRect(rect, selected ? Palette.Highlight : new Color(0.25f, 0.22f, 0.2f));
        Ui.Text(canvas, label, rect.Position + new Vector2(rect.Size.X / 2, rect.Size.Y / 2 + 7), 18,
            selected ? Palette.Ink : Colors.White, center: true);
    }
}

/// <summary>Small drawing helpers shared by the screens.</summary>
public static class Ui
{
    public static void Box(CanvasItem canvas, Rect2 box)
    {
        canvas.DrawRect(box.Grow(3), Palette.Ink);
        canvas.DrawRect(box, new Color(0.13f, 0.11f, 0.1f, 0.95f));
    }

    public static void Text(CanvasItem canvas, string text, Vector2 at, int size, Color color, bool center = false, bool right = false)
    {
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        var pos = new Vector2(center ? at.X - width / 2 : right ? at.X - width : at.X, at.Y);
        canvas.DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, size, 5, Palette.Ink with { A = color.A });
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, size, color);
    }
}
