using Godot;

namespace ArmageddonOuttaHere;

/// <summary>
/// One player's controls: an Xbox-style controller (such as a Logitech F310), or the keyboard.
/// Call <see cref="Poll"/> once per physics frame; "Pressed" means pressed this frame.
/// </summary>
public sealed class PadInput
{
    /// <summary>Device id used for the keyboard player.</summary>
    public const int Keyboard = -1;

    private const float Deadzone = 0.25f;

    private bool _attackHeld;
    private bool _rollHeld;
    private bool _punchHeld;
    private bool _startHeld;

    public PadInput(int device)
    {
        Device = device;
    }

    public int Device { get; }

    public Vector2 Move { get; private set; }

    /// <summary>Y: fire the selected weapon.</summary>
    public bool AttackPressed { get; private set; }

    /// <summary>A: dodge roll (and accept in menus).</summary>
    public bool RollPressed { get; private set; }

    /// <summary>X: punch.</summary>
    public bool PunchPressed { get; private set; }

    /// <summary>Start: join.</summary>
    public bool StartPressed { get; private set; }

    public void Poll()
    {
        bool attack, roll, punch, start;
        if (Device == Keyboard)
        {
            Move = new Vector2(Axis(Key.A, Key.D), Axis(Key.W, Key.S)).Normalized();
            attack = Input.IsPhysicalKeyPressed(Key.J);
            roll = Input.IsPhysicalKeyPressed(Key.Space);
            punch = Input.IsPhysicalKeyPressed(Key.K);
            start = Input.IsPhysicalKeyPressed(Key.Enter);
        }
        else
        {
            var stick = new Vector2(Input.GetJoyAxis(Device, JoyAxis.LeftX), Input.GetJoyAxis(Device, JoyAxis.LeftY));
            Move = stick.Length() < Deadzone ? Vector2.Zero : stick.LimitLength(1f);
            attack = Input.IsJoyButtonPressed(Device, JoyButton.Y);
            roll = Input.IsJoyButtonPressed(Device, JoyButton.A);
            punch = Input.IsJoyButtonPressed(Device, JoyButton.X);
            start = Input.IsJoyButtonPressed(Device, JoyButton.Start);
        }

        AttackPressed = attack && !_attackHeld;
        RollPressed = roll && !_rollHeld;
        PunchPressed = punch && !_punchHeld;
        StartPressed = start && !_startHeld;
        _attackHeld = attack;
        _rollHeld = roll;
        _punchHeld = punch;
        _startHeld = start;
    }

    private static float Axis(Key negative, Key positive) =>
        (Input.IsPhysicalKeyPressed(positive) ? 1 : 0) - (Input.IsPhysicalKeyPressed(negative) ? 1 : 0);
}
