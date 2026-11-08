using Godot;

namespace ArmageddonOuttaHere;

/// <summary>
/// One player's controls: an Xbox-style controller (such as a Logitech F310 in X mode), or the keyboard.
/// Call <see cref="Poll"/> once per physics frame; "Pressed" means pressed this frame.
/// </summary>
/// <remarks>
/// Keyboard: WASD move, J attack, Space roll, K punch, 1-5 slots, Enter start and accept, Escape cancel.
/// </remarks>
public sealed class PadInput
{
    /// <summary>Device id used for the keyboard player.</summary>
    public const int Keyboard = -1;

    private const float Deadzone = 0.3f;

    private readonly bool[] _slotHeld = new bool[5];
    private bool _rollHeld;
    private bool _punchHeld;
    private bool _startHeld;
    private bool _acceptHeld;
    private bool _cancelHeld;
    private int _navXWas;
    private int _navYWas;

    public PadInput(int device)
    {
        Device = device;
    }

    public int Device { get; }

    /// <summary>A pretend controller (for automatic checks): ignores real input.</summary>
    public bool IsFake => Device >= 100;

    public Vector2 Move { get; set; }

    /// <summary>Y: fire the selected weapon or suit attack (held fires repeatedly).</summary>
    public bool AttackHeld { get; set; }

    /// <summary>A: dodge roll.</summary>
    public bool RollPressed { get; private set; }

    /// <summary>X: punch.</summary>
    public bool PunchPressed { get; private set; }

    /// <summary>Start: join.</summary>
    public bool StartPressed { get; private set; }

    /// <summary>A in menus.</summary>
    public bool AcceptPressed { get; private set; }

    /// <summary>B in menus.</summary>
    public bool CancelPressed { get; private set; }

    /// <summary>The slot button pressed this frame (0 = B, 1 = LB, 2 = RB, 3 = LT, 4 = RT), or -1.</summary>
    public int SlotPressed { get; private set; } = -1;

    /// <summary>-1 or +1 on the frame the stick is pushed left or right, otherwise 0.</summary>
    public int NavigateX { get; private set; }

    /// <summary>-1 (up) or +1 (down) on the frame the stick is pushed up or down, otherwise 0.</summary>
    public int NavigateY { get; private set; }

    public void Poll()
    {
        if (IsFake)
        {
            return;
        }

        var slots = new bool[5];
        bool roll, punch, start, accept, cancel, attack;
        if (Device == Keyboard)
        {
            Move = new Vector2(Axis(Key.A, Key.D), Axis(Key.W, Key.S)).Normalized();
            attack = Down(Godot.Key.J);
            roll = Down(Godot.Key.Space);
            punch = Down(Godot.Key.K);
            start = Down(Godot.Key.Enter);
            accept = Down(Godot.Key.Enter) || Down(Godot.Key.Space) || Down(Godot.Key.J);
            cancel = Down(Godot.Key.Escape) || Down(Godot.Key.Backspace);
            slots[0] = Down(Godot.Key.Key1);
            slots[1] = Down(Godot.Key.Key2);
            slots[2] = Down(Godot.Key.Key3);
            slots[3] = Down(Godot.Key.Key4);
            slots[4] = Down(Godot.Key.Key5);
        }
        else
        {
            var stick = new Vector2(Input.GetJoyAxis(Device, JoyAxis.LeftX), Input.GetJoyAxis(Device, JoyAxis.LeftY));
            var dpad = new Vector2(
                Button(JoyButton.DpadRight) - Button(JoyButton.DpadLeft),
                Button(JoyButton.DpadDown) - Button(JoyButton.DpadUp));
            Move = stick.Length() >= Deadzone ? stick.LimitLength(1f) : dpad.Normalized();
            attack = Held(JoyButton.Y);
            roll = Held(JoyButton.A);
            punch = Held(JoyButton.X);
            start = Held(JoyButton.Start);
            accept = Held(JoyButton.A);
            cancel = Held(JoyButton.B);
            slots[0] = Held(JoyButton.B);
            slots[1] = Held(JoyButton.LeftShoulder);
            slots[2] = Held(JoyButton.RightShoulder);
            slots[3] = Input.GetJoyAxis(Device, JoyAxis.TriggerLeft) > 0.5f;
            slots[4] = Input.GetJoyAxis(Device, JoyAxis.TriggerRight) > 0.5f;
        }

        AttackHeld = attack;
        RollPressed = roll && !_rollHeld;
        PunchPressed = punch && !_punchHeld;
        StartPressed = start && !_startHeld;
        AcceptPressed = accept && !_acceptHeld;
        CancelPressed = cancel && !_cancelHeld;
        _rollHeld = roll;
        _punchHeld = punch;
        _startHeld = start;
        _acceptHeld = accept;
        _cancelHeld = cancel;

        SlotPressed = -1;
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] && !_slotHeld[i] && SlotPressed < 0)
            {
                SlotPressed = i;
            }

            _slotHeld[i] = slots[i];
        }

        var navX = Move.X > 0.5f ? 1 : Move.X < -0.5f ? -1 : 0;
        var navY = Move.Y > 0.5f ? 1 : Move.Y < -0.5f ? -1 : 0;
        NavigateX = navX != _navXWas ? navX : 0;
        NavigateY = navY != _navYWas ? navY : 0;
        _navXWas = navX;
        _navYWas = navY;
    }

    private bool Held(JoyButton button) => Input.IsJoyButtonPressed(Device, button);

    private float Button(JoyButton button) => Held(button) ? 1 : 0;

    private static bool Down(Key key) => Input.IsPhysicalKeyPressed(key);

    private static float Axis(Key negative, Key positive) => (Down(positive) ? 1 : 0) - (Down(negative) ? 1 : 0);
}
