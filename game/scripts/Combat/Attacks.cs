using System.Collections.Generic;
using ArmageddonOuttaHere.Core;
using Godot;

namespace ArmageddonOuttaHere.Combat;

public enum AttackForm
{
    /// <summary>Projectiles fired the way the mech faces.</summary>
    Shot,

    /// <summary>A projectile that explodes where it lands.</summary>
    Lob,

    /// <summary>A blast around the mech itself.</summary>
    Nova,

    /// <summary>The mech charges forward, hitting everything it runs into.</summary>
    Dash,

    /// <summary>Lightning that jumps from enemy to enemy.</summary>
    Chain,

    /// <summary>Explosions dropped on the nearest enemies.</summary>
    Strike,
}

/// <summary>
/// What one attack does. Guns have one; suits have one per level. Placeholder numbers until balancing, and the
/// suits themselves are placeholders that will probably be replaced.
/// </summary>
public sealed record AttackSpec(
    string Name,
    AttackForm Form,
    float Damage,
    float Cooldown,
    Color Color,
    float Speed = 700f,
    int Count = 1,
    float Spread = 0f,
    float Life = 0.8f,
    float Size = 5f,
    float Radius = 0f,
    float Knockback = 120f,
    float Slow = 0f,
    bool Pierce = false,
    float Heal = 0f,
    AttackVisual Visual = AttackVisual.Auto);

/// <summary>How an attack looks, so blades read as blades and ice as ice, not just a flash.</summary>
public enum AttackVisual
{
    /// <summary>Picked from the form: blasts are shockwaves, explosive shots and strikes are explosions.</summary>
    Auto,
    Blades,
    Frost,
    Slam,
    Heal,
    Pull,
    Static,
    Beam,
    Lightning,
    Shockwave,
    Punch,
    Explosion,
    Flame,
    Laser,
    Ice,
    Box,
}

public static class Attacks
{
    private static readonly Color Brass = new(1f, 0.85f, 0.4f);
    private static readonly Color Ice = new(0.6f, 0.95f, 1f);
    private static readonly Color Fire = new(1f, 0.55f, 0.15f);
    private static readonly Color Grass = new(0.5f, 0.9f, 0.3f);
    private static readonly Color Spark = new(0.7f, 0.8f, 1f);
    private static readonly Color AlienGreen = new(0.4f, 1f, 0.5f);

    private static readonly Dictionary<string, AttackSpec[]> ByGear = new()
    {
        ["pistol"] = new[] { new AttackSpec("Pistol", AttackForm.Shot, 12, 0.3f, Brass) },
        ["nail-gun"] = new[] { new AttackSpec("Nail Gun", AttackForm.Shot, 7, 0.1f, Colors.LightGray, Speed: 800, Spread: 6, Size: 4) },
        ["shotgun"] = new[] { new AttackSpec("Shotgun", AttackForm.Shot, 8, 0.7f, Brass, Speed: 650, Count: 6, Spread: 30, Life: 0.35f, Size: 4) },
        ["ray-gun"] = new[] { new AttackSpec("Ray Gun", AttackForm.Shot, 20, 0.45f, AlienGreen, Speed: 900, Pierce: true, Size: 6) },

        ["minigun"] = new[] { new AttackSpec("Minigun", AttackForm.Shot, 6, 0.05f, Brass, Spread: 10, Size: 4) },
        ["rpg"] = new[] { new AttackSpec("RPG", AttackForm.Shot, 40, 1.2f, Fire, Speed: 450, Radius: 90, Knockback: 300, Size: 8) },
        ["grenade-launcher"] = new[] { new AttackSpec("Grenade Launcher", AttackForm.Lob, 30, 0.8f, Colors.DarkOliveGreen, Speed: 380, Life: 0.7f, Radius: 80, Knockback: 250, Size: 7) },
        ["flamethrower"] = new[] { new AttackSpec("Flamethrower", AttackForm.Shot, 3, 0.04f, Fire, Speed: 300, Spread: 25, Life: 0.35f, Size: 10, Pierce: true, Knockback: 10) },

        ["fridge"] = new[]
        {
            new AttackSpec("Freezer Beam", AttackForm.Shot, 4, 0.06f, Ice, Speed: 600, Life: 0.5f, Pierce: true, Slow: 0.5f, Knockback: 0),
            new AttackSpec("Ice Cube Toss", AttackForm.Lob, 25, 0.9f, Ice, Speed: 400, Life: 0.6f, Radius: 60, Slow: 0.6f, Size: 9),
            new AttackSpec("Door Slam", AttackForm.Nova, 25, 1.2f, Colors.White, Radius: 90, Knockback: 350),
            new AttackSpec("Deep Freeze", AttackForm.Nova, 15, 4f, Ice, Radius: 180, Slow: 0.85f, Knockback: 0),
        },
        ["lawnmower-larry"] = new[]
        {
            new AttackSpec("Blade Spin", AttackForm.Nova, 10, 0.25f, Grass, Radius: 70, Knockback: 80),
            new AttackSpec("Grass Clippings", AttackForm.Shot, 5, 0.3f, Grass, Count: 5, Spread: 40, Life: 0.4f, Size: 4),
            new AttackSpec("Mow Down", AttackForm.Dash, 20, 1.5f, Grass, Speed: 600, Life: 0.35f, Radius: 40, Knockback: 200),
            new AttackSpec("Turbo Mulch", AttackForm.Nova, 30, 3f, Grass, Radius: 130, Knockback: 250),
        },
        ["mailbox"] = new[]
        {
            new AttackSpec("Package Toss", AttackForm.Lob, 25, 0.8f, new Color(0.75f, 0.55f, 0.3f), Speed: 420, Life: 0.6f, Radius: 60, Size: 9),
            new AttackSpec("Junk Mail", AttackForm.Shot, 8, 0.35f, Colors.White, Count: 3, Spread: 20, Size: 5),
            new AttackSpec("Express Delivery", AttackForm.Strike, 30, 2.5f, Fire, Count: 3, Radius: 60),
            new AttackSpec("Return to Sender", AttackForm.Nova, 10, 2f, Colors.White, Radius: 120, Knockback: 450),
        },
        ["grill-sergeant"] = new[]
        {
            new AttackSpec("Grill Flames", AttackForm.Shot, 3, 0.05f, Fire, Speed: 320, Spread: 20, Life: 0.35f, Size: 10, Pierce: true, Knockback: 10),
            new AttackSpec("Hot Dog Launcher", AttackForm.Shot, 15, 0.4f, new Color(0.85f, 0.4f, 0.3f), Speed: 600, Size: 7),
            new AttackSpec("Cookout", AttackForm.Nova, 0, 6f, Colors.Pink, Radius: 160, Knockback: 0, Heal: 30),
            new AttackSpec("Charcoal Bomb", AttackForm.Lob, 35, 1.5f, Colors.DimGray, Speed: 380, Life: 0.7f, Radius: 90, Size: 9),
        },
        ["shopping-cart"] = new[]
        {
            new AttackSpec("Ram", AttackForm.Dash, 18, 0.8f, Colors.Silver, Speed: 700, Life: 0.25f, Radius: 36, Knockback: 300),
            new AttackSpec("Bag Toss", AttackForm.Shot, 12, 0.35f, new Color(0.9f, 0.85f, 0.6f), Speed: 550, Size: 7),
            new AttackSpec("Runaway Cart", AttackForm.Dash, 30, 2f, Colors.Silver, Speed: 900, Life: 0.5f, Radius: 44, Knockback: 400),
            new AttackSpec("Clean Up Aisle 5", AttackForm.Nova, 30, 3f, Colors.LightBlue, Radius: 140, Knockback: 300),
        },
        ["sparky"] = new[]
        {
            new AttackSpec("Zap", AttackForm.Chain, 14, 0.5f, Spark, Count: 3, Radius: 220),
            new AttackSpec("Static Field", AttackForm.Nova, 8, 1f, Spark, Radius: 110, Slow: 0.4f, Knockback: 40),
            new AttackSpec("Overcharge", AttackForm.Chain, 20, 1.5f, Spark, Count: 7, Radius: 260),
            new AttackSpec("Thunder Strike", AttackForm.Strike, 40, 3f, Spark, Count: 4, Radius: 70),
        },
        ["hover-commander"] = new[]
        {
            new AttackSpec("Hover Blaster", AttackForm.Shot, 10, 0.2f, Colors.Cyan, Speed: 850, Size: 5),
            new AttackSpec("Drone Strike", AttackForm.Strike, 25, 1.5f, Fire, Count: 2, Radius: 60),
            new AttackSpec("Hover Boost", AttackForm.Dash, 10, 1f, Colors.Cyan, Speed: 800, Life: 0.3f, Radius: 30, Knockback: 150),
            new AttackSpec("Air Raid", AttackForm.Strike, 30, 4f, Fire, Count: 6, Radius: 70),
        },
        ["probe-reverser"] = new[]
        {
            new AttackSpec("Tractor Beam", AttackForm.Nova, 5, 1f, AlienGreen, Radius: 200, Knockback: -300),
            new AttackSpec("Probe Laser", AttackForm.Shot, 16, 0.3f, AlienGreen, Speed: 950, Pierce: true, Size: 5),
            new AttackSpec("Abduct", AttackForm.Nova, 40, 2.5f, AlienGreen, Radius: 100, Knockback: 50),
            new AttackSpec("Saucer Slam", AttackForm.Lob, 60, 3f, AlienGreen, Speed: 350, Life: 0.8f, Radius: 130, Knockback: 350, Size: 12),
        },
    };

    /// <summary>How each named attack looks.</summary>
    private static readonly Dictionary<string, AttackVisual> Visuals = new()
    {
        ["Flamethrower"] = AttackVisual.Flame,
        ["Grill Flames"] = AttackVisual.Flame,
        ["Ray Gun"] = AttackVisual.Laser,
        ["Probe Laser"] = AttackVisual.Laser,
        ["Hover Blaster"] = AttackVisual.Laser,
        ["Freezer Beam"] = AttackVisual.Ice,
        ["Ice Cube Toss"] = AttackVisual.Frost,
        ["Door Slam"] = AttackVisual.Slam,
        ["Deep Freeze"] = AttackVisual.Frost,
        ["Blade Spin"] = AttackVisual.Blades,
        ["Mow Down"] = AttackVisual.Blades,
        ["Turbo Mulch"] = AttackVisual.Blades,
        ["Package Toss"] = AttackVisual.Box,
        ["Junk Mail"] = AttackVisual.Box,
        ["Bag Toss"] = AttackVisual.Box,
        ["Cookout"] = AttackVisual.Heal,
        ["Static Field"] = AttackVisual.Static,
        ["Thunder Strike"] = AttackVisual.Lightning,
        ["Tractor Beam"] = AttackVisual.Pull,
        ["Abduct"] = AttackVisual.Beam,
    };

    /// <summary>The attack for one slot button. Guns get stronger with each upgrade; suits get more attacks.</summary>
    public static AttackSpec For(GearItem item, int attack)
    {
        var specs = ByGear.TryGetValue(item.Type.Id, out var found) ? found : ByGear["pistol"];
        var spec = specs[Mathf.Clamp(attack, 0, specs.Length - 1)];
        spec = spec with { Visual = Visuals.GetValueOrDefault(spec.Name, spec.Visual) };
        return item.Type.Kind == GearKind.Suit
            ? spec
            : spec with { Damage = spec.Damage * (1f + 0.25f * (item.Level - 1)) };
    }

    /// <summary>The punch on X. Every mech has it.</summary>
    public static readonly AttackSpec Punch = new("Punch", AttackForm.Nova, 15, 0.4f, Colors.White, Radius: 45, Knockback: 260, Visual: AttackVisual.Punch);
}
