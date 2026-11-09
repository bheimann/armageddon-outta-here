using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArmageddonOuttaHere.Combat;

/// <summary>Anything that can be hit: mechs, enemies and the base.</summary>
public interface IHittable
{
    const string Group = "hittable";

    Vector2 GlobalPosition { get; }

    /// <summary>How close a hit has to land, in pixels.</summary>
    float HitRadius { get; }

    bool IsAlive { get; }

    /// <summary>True for players' mechs and the base; false for enemies.</summary>
    bool IsTeam { get; }

    void Hit(Hit hit);
}

/// <summary>One hit landing on something.</summary>
/// <remarks><paramref name="ByTeam"/> is true for damage from players (shots, blasts, dashes, punches).</remarks>
public readonly record struct Hit(float Damage, Vector2 Push, float Slow = 0f, float Heal = 0f, bool ByTeam = false);

public static class Hittables
{
    /// <summary>Group of the node that holds everything in the game world (mechs, enemies, shots).</summary>
    public const string WorldGroup = "world";

    public static Node WorldOf(Node anyNode) => anyNode.GetTree().GetFirstNodeInGroup(WorldGroup);

    public static IEnumerable<IHittable> All(Node anyNode) =>
        anyNode.GetTree().GetNodesInGroup(IHittable.Group).OfType<IHittable>().Where(h => h.IsAlive);

    public static IEnumerable<IHittable> Near(Node anyNode, Vector2 point, float radius) =>
        All(anyNode).Where(h => h.GlobalPosition.DistanceTo(point) <= radius + h.HitRadius);

    /// <summary>
    /// Damages everything within <paramref name="radius"/> of <paramref name="center"/>, pushing it outward (or inward
    /// when the knockback is negative). Hits teammates too (friendly fire), except <paramref name="ignore"/>.
    /// Healing blasts heal the team instead of hurting it.
    /// </summary>
    public static void Blast(Node anyNode, Vector2 center, float radius, AttackSpec spec, IHittable? ignore, bool hurtsTeam = true)
    {
        foreach (var target in Near(anyNode, center, radius).ToList())
        {
            if (target == ignore && spec.Heal <= 0)
            {
                continue;
            }

            var away = (target.GlobalPosition - center).Normalized();
            if (spec.Heal > 0)
            {
                if (target.IsTeam)
                {
                    target.Hit(new Hit(0, Vector2.Zero, Heal: spec.Heal));
                }

                continue;
            }

            if (target.IsTeam && !hurtsTeam)
            {
                continue;
            }

            target.Hit(new Hit(spec.Damage, away * spec.Knockback, spec.Slow, ByTeam: true));
        }

        WorldOf(anyNode).AddChild(new Blast
        {
            GlobalPosition = center,
            Radius = radius,
            Tint = spec.Color,
        });
    }
}
