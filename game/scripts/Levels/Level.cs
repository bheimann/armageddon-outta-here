using System.Collections.Generic;
using System.Linq;
using ArmageddonOuttaHere.Actors;
using ArmageddonOuttaHere.Combat;
using ArmageddonOuttaHere.Core;
using ArmageddonOuttaHere.UI;
using Godot;

namespace ArmageddonOuttaHere.Levels;

public enum LevelState
{
    Intro,
    Wave,
    Break,
    Boss,
    Won,
    Lost,
}

/// <summary>
/// One level: defend the spot in the middle against waves of enemies, then beat the boss. Everyone shares one
/// screen. Downed players are revived by a teammate standing next to them. Players keep everything they earn,
/// win or lose.
/// </summary>
public partial class Level : Node2D
{
    public static readonly Rect2 Arena = new(-1200, -750, 2400, 1500);

    private const double IntroSeconds = 3;
    private const double BreakSeconds = 10;
    private const double EndSeconds = 6;
    private const double CrateEvery = 25;
    private const float ReviveRange = 55f;
    private const double ReviveSeconds = 2;

    private readonly List<Mech> _mechs = new();
    private readonly Queue<EnemyKind> _toSpawn = new();
    private readonly ScrapPot _scrap = new();
    private readonly System.Random _random = new();

    private Node2D _world = null!;
    private Camera2D _camera = null!;
    private LevelHud _hud = null!;
    private DefendedSpot _spot = null!;
    private Boss? _boss;
    private IReadOnlyList<Wave> _waves = null!;
    private double _stateTime;
    private double _spawnTimer;
    private double _spawnInterval;
    private double _crateTimer = CrateEvery / 2;
    private bool _leaving;

    public LevelInfo Info { get; private set; } = LevelCatalog.Get(1);

    public LevelState State { get; private set; }

    /// <summary>0-based index of the current wave.</summary>
    public int WaveIndex { get; private set; }

    public int WaveCount => _waves.Count;

    public double StateTime => _stateTime;

    public DefendedSpot Spot => _spot;

    public Boss? CurrentBoss => _boss is not null && IsInstanceValid(_boss) ? _boss : null;

    public IReadOnlyList<Mech> Mechs => _mechs;

    private static Session Session => Session.Instance;

    public override void _Ready()
    {
        Info = LevelCatalog.Get(Session.CurrentLevel);
        _world = GetNode<Node2D>("World");
        _world.AddToGroup(Hittables.WorldGroup);
        _camera = GetNode<Camera2D>("Camera");
        _hud = GetNode<LevelHud>("Hud/LevelHud");
        _hud.Level = this;

        Session.AddDevPlayers();
        if (Session.Players.Count == 0)
        {
            Session.Join(new PadInput(PadInput.Keyboard), Session.Save.Characters[0]);
        }

        _spot = new DefendedSpot { Spot = Info.Spot };
        _world.AddChild(_spot);

        for (var i = 0; i < Session.Players.Count; i++)
        {
            var angle = Mathf.Pi / 2 + (i - (Session.Players.Count - 1) / 2f) * 0.6f;
            var mech = new Mech
            {
                Player = Session.Players[i],
                Position = Vector2.Right.Rotated(angle) * 160f,
                Constrain = ConstrainMech,
            };
            _mechs.Add(mech);
            _world.AddChild(mech);
        }

        _waves = LevelCatalog.WavesFor(Info, _mechs.Count);
        WaveIndex = Mathf.Clamp(Session.Dev.Wave - 1, 0, _waves.Count);
        _camera.Position = Vector2.Zero;
        _camera.Zoom = Vector2.One * 0.8f;
        Session.Dev.ScreenshotIfAsked(this);
        GD.Print($"Level {Info.Number} ({Info.Name}) with {_mechs.Count} players");
    }

    public override void _PhysicsProcess(double delta)
    {
        _stateTime += delta;
        foreach (var mech in _mechs)
        {
            mech.Input.Poll();
            if (mech.Input.IsFake)
            {
                Bot(mech);
            }
        }

        switch (State)
        {
            case LevelState.Intro when _stateTime >= IntroSeconds:
                StartWaveOrBoss();
                break;
            case LevelState.Wave:
                SpawnFromQueue(delta);
                DropCrates(delta);
                if (_toSpawn.Count == 0 && !EnemiesLeft())
                {
                    WaveIndex++;
                    SetState(LevelState.Break);
                    _hud.Banner(WaveIndex < _waves.Count ? "Wave cleared!" : "Here comes the boss...");
                }

                break;
            case LevelState.Break:
                _spot.Repair(DefendedSpot.AutoRepair * (float)delta);
                if (_stateTime >= BreakSeconds)
                {
                    StartWaveOrBoss();
                }

                break;
            case LevelState.Boss:
                DropCrates(delta);
                if (CurrentBoss is null)
                {
                    Win();
                }

                break;
            case LevelState.Won:
            case LevelState.Lost:
                if (!_leaving && (_stateTime >= EndSeconds || (_stateTime > 1.5 && _mechs.Any(m => m.Input.AcceptPressed))))
                {
                    _leaving = true;
                    Session.GoToGarage();
                }

                return;
        }

        Revive(delta);
        Repair(delta);
        if (State is not (LevelState.Won or LevelState.Lost) && (!_spot.IsAlive || _mechs.All(m => m.IsDown)))
        {
            Lose();
        }

        UpdateCamera(delta);
    }

    private void SetState(LevelState state)
    {
        State = state;
        _stateTime = 0;
        GD.Print($"Level state: {state} (wave {WaveIndex + 1}/{_waves.Count})");
    }

    private void StartWaveOrBoss()
    {
        if (WaveIndex >= _waves.Count)
        {
            SetState(LevelState.Boss);
            SpawnBoss();
            return;
        }

        var wave = _waves[WaveIndex];
        var kinds = wave.Enemies.SelectMany(e => Enumerable.Repeat(e.Key, e.Value)).OrderBy(_ => _random.Next()).ToList();
        _toSpawn.Clear();
        foreach (var kind in kinds)
        {
            _toSpawn.Enqueue(kind);
        }

        _spawnInterval = wave.SpawnSeconds / Mathf.Max(1, kinds.Count);
        _spawnTimer = 0;
        SetState(LevelState.Wave);
        _hud.Banner($"Wave {WaveIndex + 1}");
    }

    private void SpawnFromQueue(double delta)
    {
        _spawnTimer -= delta;
        while (_spawnTimer <= 0 && _toSpawn.Count > 0)
        {
            _spawnTimer += _spawnInterval;
            Spawn(_toSpawn.Dequeue(), EdgePoint());
        }
    }

    private Enemy Spawn(EnemyKind kind, Vector2 at)
    {
        var enemy = new Enemy
        {
            Kind = kind,
            Position = at,
            Target = _spot,
            Died = OnEnemyDied,
            Constrain = p => p.Clamp(Arena.Position, Arena.End),
        };
        _world.AddChild(enemy);
        return enemy;
    }

    private void SpawnBoss()
    {
        _boss = new Boss
        {
            Kind = Info.Enemy,
            BossName = Info.Boss,
            PlayerCount = _mechs.Count,
            Position = new Vector2(0, Arena.Position.Y + 120),
            Target = _spot,
            Died = OnEnemyDied,
            Constrain = p => p.Clamp(Arena.Position, Arena.End),
            Summon = (kind, count, near) =>
            {
                for (var i = 0; i < count; i++)
                {
                    Spawn(kind, near + Vector2.Right.Rotated(Mathf.Tau * i / count) * 90f);
                }
            },
        };
        _world.AddChild(_boss);
        _hud.Banner(Info.Boss + "!");
    }

    private Vector2 EdgePoint()
    {
        var side = _random.Next(4);
        var t = (float)_random.NextDouble();
        return side switch
        {
            0 => new Vector2(Mathf.Lerp(Arena.Position.X, Arena.End.X, t), Arena.Position.Y),
            1 => new Vector2(Mathf.Lerp(Arena.Position.X, Arena.End.X, t), Arena.End.Y),
            2 => new Vector2(Arena.Position.X, Mathf.Lerp(Arena.Position.Y, Arena.End.Y, t)),
            _ => new Vector2(Arena.End.X, Mathf.Lerp(Arena.Position.Y, Arena.End.Y, t)),
        };
    }

    private bool EnemiesLeft() => GetTree().GetNodesInGroup(Enemy.Group).OfType<Enemy>().Any(e => e.IsAlive);

    private IReadOnlyList<Character> Characters => _mechs.Select(m => m.Player.Character).ToList();

    private void OnEnemyDied(Enemy enemy)
    {
        _scrap.Share(enemy.Stats.Scrap, Characters);
        Float(enemy.GlobalPosition, $"+{enemy.Stats.Scrap} scrap", Palette.Highlight);
    }

    private void DropCrates(double delta)
    {
        _crateTimer -= delta;
        if (_crateTimer > 0)
        {
            return;
        }

        _crateTimer = CrateEvery;
        var spot = new Vector2(
            Mathf.Lerp(Arena.Position.X + 150, Arena.End.X - 150, (float)_random.NextDouble()),
            Mathf.Lerp(Arena.Position.Y + 150, Arena.End.Y - 150, (float)_random.NextDouble()));
        if (spot.Length() < 200)
        {
            spot = spot.Normalized() * 250 + new Vector2(250, 0);
        }

        _world.AddChild(new Crate { Position = spot, Opened = OpenCrate });
        _hud.Banner("Supply crate!");
    }

    private void OpenCrate(Crate crate)
    {
        var loot = SupplyCrate.Open(Session.Save.Armory, _random);
        if (loot.Item is { } item)
        {
            Float(crate.GlobalPosition, $"Found: {item.Type.Name}!", Palette.Highlight, 26);
            _hud.Banner($"Found: {item.Type.Name}! Equip it at the workbench.");
        }
        else
        {
            _scrap.Share(loot.Scrap, Characters);
            Float(crate.GlobalPosition, $"+{loot.Scrap} scrap", Palette.Highlight, 26);
        }
    }

    private void Revive(double delta)
    {
        foreach (var down in _mechs.Where(m => m.IsDown))
        {
            var helped = _mechs.Any(m => m.IsAlive && m.GlobalPosition.DistanceTo(down.GlobalPosition) <= ReviveRange);
            down.ReviveProgress = helped
                ? down.ReviveProgress + (float)(delta / ReviveSeconds)
                : Mathf.Max(0, down.ReviveProgress - (float)delta * 0.25f);
            if (down.ReviveProgress >= 1)
            {
                down.Revive();
                Float(down.GlobalPosition, "Back up!", Palette.Health);
            }
        }
    }

    private void Repair(double delta)
    {
        var helpers = _mechs.Count(m => m.IsAlive && m.GlobalPosition.DistanceTo(_spot.GlobalPosition) <= DefendedSpot.RepairRange);
        _spot.BeingRepaired = helpers > 0 && _spot.Health < DefendedSpot.MaxHealth;
        _spot.Repair(helpers * DefendedSpot.PlayerRepair * (float)delta);
    }

    private void Win()
    {
        SetState(LevelState.Won);
        Session.Save.LevelBeaten(Info.Number);
        Session.SaveNow();
    }

    private void Lose()
    {
        SetState(LevelState.Lost);
        GD.Print(_spot.IsAlive ? "Lost: every player is down" : $"Lost: the {Info.Spot.ToLower()} was destroyed");
        Session.SaveNow();
    }

    private Vector2 ConstrainMech(Vector2 p)
    {
        p = p.Clamp(Arena.Position + new Vector2(20, 20), Arena.End - new Vector2(20, 20));
        var fromSpot = p - _spot.Position;
        var min = _spot.SolidRadius + 18f;
        return fromSpot.Length() < min ? _spot.Position + fromSpot.Normalized() * min : p;
    }

    private void UpdateCamera(double delta)
    {
        var alive = _mechs.Where(m => m.IsAlive).ToList();
        var points = (alive.Count > 0 ? alive : _mechs).Select(m => m.Position).Append(_spot.Position).ToList();
        if (CurrentBoss is { } boss)
        {
            // Keep the boss in view, with room above it for its name.
            points.Add(boss.Position);
            points.Add(boss.Position - new Vector2(0, boss.Stats.Radius * 2 + 120));
        }
        var box = new Rect2(points[0], Vector2.Zero);
        foreach (var p in points.Skip(1))
        {
            box = box.Expand(p);
        }

        var needed = box.Size + new Vector2(400, 320);
        var screen = GetViewportRect().Size;
        var minZoom = Mathf.Min(screen.X / Arena.Size.X, screen.Y / Arena.Size.Y);
        var zoom = Mathf.Clamp(Mathf.Min(screen.X / needed.X, screen.Y / needed.Y), minZoom, 1f);
        var half = screen / zoom / 2;
        var center = box.GetCenter();
        center = new Vector2(
            half.X * 2 >= Arena.Size.X ? Arena.GetCenter().X : Mathf.Clamp(center.X, Arena.Position.X + half.X, Arena.End.X - half.X),
            half.Y * 2 >= Arena.Size.Y ? Arena.GetCenter().Y : Mathf.Clamp(center.Y, Arena.Position.Y + half.Y, Arena.End.Y - half.Y));
        var t = 1f - Mathf.Exp(-4f * (float)delta);
        _camera.Position = _camera.Position.Lerp(center, t);
        _camera.Zoom = Vector2.One * Mathf.Lerp(_camera.Zoom.X, zoom, t);
    }

    private void Float(Vector2 at, string text, Color color, int size = 20) =>
        _world.AddChild(new FloatingText { Position = at, Text = text, Tint = color, FontSize = size });

    /// <summary>Pretend players (for automatic checks) walk toward the nearest enemy and keep firing.</summary>
    private void Bot(Mech mech)
    {
        var enemy = GetTree().GetNodesInGroup(Enemy.Group).OfType<Enemy>()
            .Where(e => e.IsAlive)
            .OrderBy(e => e.GlobalPosition.DistanceTo(mech.GlobalPosition))
            .FirstOrDefault();
        var down = _mechs.FirstOrDefault(m => m.IsDown);
        var goal = down?.GlobalPosition ?? enemy?.GlobalPosition ?? _spot.GlobalPosition + new Vector2(0, 150);
        var to = goal - mech.GlobalPosition;
        var stopAt = down is not null ? 30f : enemy is not null ? 220f : 40f;
        mech.Input.Move = to.Length() > stopAt ? to.Normalized() : enemy is not null ? to.Normalized() * 0.01f : Vector2.Zero;
        // Aim locks while firing, so let go briefly to turn toward a target in a different direction.
        var aimedAtEnemy = enemy is not null && mech.Facing.AngleTo(to.Normalized()) is > -0.3f and < 0.3f;
        mech.Input.AttackHeld = enemy is not null && down is null && aimedAtEnemy;
    }

    public override void _Draw()
    {
        DrawRect(Arena.Grow(400), Palette.Ground.Darkened(0.25f));
        DrawRect(Arena, Palette.Ground);
        for (var x = Arena.Position.X; x <= Arena.End.X; x += 150)
        {
            DrawLine(new Vector2(x, Arena.Position.Y), new Vector2(x, Arena.End.Y), Palette.GroundLine, 2f);
        }

        for (var y = Arena.Position.Y; y <= Arena.End.Y; y += 150)
        {
            DrawLine(new Vector2(Arena.Position.X, y), new Vector2(Arena.End.X, y), Palette.GroundLine, 2f);
        }

        DrawRect(Arena, Palette.Ink, false, 6f);
    }
}
