namespace ArmageddonOuttaHere.Core;

public enum EnemyKind
{
    Raccoon,
    Zombie,
    Alien,
}

/// <summary>One level: the spot to defend, who attacks it, how many waves, and the boss at the end.</summary>
public sealed record LevelInfo(int Number, string Name, string Spot, EnemyKind Enemy, string Boss, int Waves);

/// <summary>One wave: how many of each enemy, spawned over <see cref="SpawnSeconds"/>.</summary>
public sealed record Wave(int Number, IReadOnlyDictionary<EnemyKind, int> Enemies, double SpawnSeconds)
{
    public int Total => Enemies.Values.Sum();
}

public static class LevelCatalog
{
    public static IReadOnlyList<LevelInfo> All { get; } = new[]
    {
        new LevelInfo(1, "Gas Station Getaway", "Gas station", EnemyKind.Raccoon, "The Trash Panda King", 3),
        new LevelInfo(2, "Dead Air", "Radio tower", EnemyKind.Zombie, "Big Mall Cop", 4),
        new LevelInfo(3, "Last Bus Out", "School bus", EnemyKind.Alien, "The Probe-ssor", 5),
    };

    public static LevelInfo Get(int number) => All[number - 1];

    /// <summary>
    /// The waves for a level. Each wave is bigger than the last, more players means more enemies, and later levels
    /// mix in the enemies from earlier ones. Tuned so a level lasts about 5 minutes at the start and up to 10 later.
    /// </summary>
    public static IReadOnlyList<Wave> WavesFor(LevelInfo level, int players)
    {
        players = Math.Clamp(players, 1, 4);
        var playerScale = 1 + 0.5 * (players - 1);
        var waves = new List<Wave>();
        for (var n = 1; n <= level.Waves; n++)
        {
            var main = (int)Math.Round((5 + 3 * n + 2 * (level.Number - 1)) * playerScale);
            var enemies = new Dictionary<EnemyKind, int> { [level.Enemy] = main };
            foreach (var earlier in All.Where(l => l.Number < level.Number))
            {
                enemies[earlier.Enemy] = (int)Math.Round((1 + n) * playerScale);
            }

            waves.Add(new Wave(n, enemies, 30 + 5 * n));
        }

        return waves;
    }
}
