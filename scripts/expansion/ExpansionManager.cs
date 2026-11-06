using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Economy;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Robots;

namespace SmartGreenhouse.Expansion;

public enum ExpansionDirection { Left, Right, Front, Back }

public enum ExpansionStatus { Available, Occupied, Blocked, LimitReached }

public sealed class GreenhouseModule
{
    public GreenhouseModule(int id, Vector2I cell, Node3D root)
    {
        Id = id;
        Cell = cell;
        Root = root;
    }

    public int Id { get; }

    public int SourceId { get; set; }

    public ExpansionDirection Direction { get; set; }
    public Vector2I Cell { get; }
    public Node3D Root { get; }
    public string Name => Id == 0 ? "Base" : $"Módulo {Id}";
}

public sealed record ExpansionOption(GreenhouseModule Source, ExpansionDirection Direction, Vector2I Target, ExpansionStatus Status, int Cost);

public partial class ExpansionManager : Node
{
    public const float ModuleWidth = 20.0f;
    public const float ModuleDepth = 12.0f;
    private const float HalfWidth = ModuleWidth / 2.0f;
    public const int StationsPerModule = 24;
    public const int MaxModules = 4;

    private const float Margin = 0.15f;

    private static readonly Dictionary<ExpansionDirection, Vector2I> Offsets = new()
    {
        [ExpansionDirection.Left] = new Vector2I(-1, 0),
        [ExpansionDirection.Right] = new Vector2I(1, 0),
        [ExpansionDirection.Front] = new Vector2I(0, -1),
        [ExpansionDirection.Back] = new Vector2I(0, 1)
    };

    private static readonly Dictionary<ExpansionDirection, string[]> FaceNodes = new()
    {
        [ExpansionDirection.Left] = new[] { "GlassWallLeft" },
        [ExpansionDirection.Right] = new[] { "GlassWallRightN", "GlassWallRightS", "GlassWallRightFull" },
        [ExpansionDirection.Front] = new[] { "GlassWallFront" },
        [ExpansionDirection.Back] = new[] { "GlassWallBack" }
    };

    private readonly List<GreenhouseModule> _modules = new();
    private readonly List<Node> _templates = new();
    private readonly List<GrowingStation> _stationTemplates = new();
    private Node3D _baseRoot = null!;
    private Shape3D? _stationShape;
    private EconomyManager? _economy;
    private int _nextModuleId = 1;
    private int _nextStationIndex;

    public string LastMessage { get; private set; } = string.Empty;
    public int ModuleCount => _modules.Count;
    public bool CanExpand => _modules.Count < MaxModules;
    public IReadOnlyList<GreenhouseModule> Modules => _modules;

    public override void _Ready()
    {
        AddToGroup("expansion_manager");
        _baseRoot = GetParent<Node3D>();
        _baseRoot.CallDeferred(Node.MethodName.AddChild, new CollectorFleet());
        _economy = GetNodeOrNull<EconomyManager>("/root/EconomyManager");
        _modules.Add(new GreenhouseModule(0, Vector2I.Zero, _baseRoot));

        foreach (Node child in _baseRoot.GetChildren())
        {
            if (child is GrowingStation station)
            {
                _stationTemplates.Add(station);
            }
            else if (child is StaticBody3D body && child is not RobotController && child is not Chest)
            {
                _templates.Add(body.Duplicate());
            }
        }

        _stationTemplates.Sort((a, b) => StationIndex(a).CompareTo(StationIndex(b)));
        if (_stationTemplates.Count > 0)
        {
            _stationShape = _stationTemplates[0].GetNode<CollisionShape3D>("Collision").Shape;
            _nextStationIndex = _stationTemplates.Max(StationIndex) + 1;
        }
    }

    public override void _ExitTree()
    {
        foreach (Node template in _templates)
        {
            template.Free();
        }
        _templates.Clear();
    }

    public IReadOnlyList<ExpansionOption> GetOptions()
    {
        var obstacles = GetObstacleRects();
        int cost = _economy?.ExpansionPrice ?? 0;
        var options = new List<ExpansionOption>();
        var targets = new HashSet<Vector2I>();
        var baseModule = _modules[0];

        foreach (ExpansionDirection direction in Enum.GetValues<ExpansionDirection>())
        {
            Vector2I target = Offsets[direction];
            targets.Add(target);
            options.Add(new ExpansionOption(baseModule, direction, target, StatusOf(target, obstacles), cost));
        }

        foreach (GreenhouseModule module in _modules.Skip(1))
        {
            foreach (ExpansionDirection direction in Enum.GetValues<ExpansionDirection>())
            {
                Vector2I target = module.Cell + Offsets[direction];
                if (_modules.Any(m => m.Cell == target) || !targets.Add(target))
                {
                    continue;
                }

                options.Add(new ExpansionOption(module, direction, target, StatusOf(target, obstacles), cost));
            }
        }

        return options;
    }

    public int GetBaseDirectionStatus(int direction)
    {
        var option = GetOptions().First(o => o.Source.Id == 0 && o.Direction == (ExpansionDirection)direction);
        return (int)option.Status;
    }

    public bool PurchaseFrom(int moduleId, int direction)
    {
        var option = GetOptions().FirstOrDefault(o => o.Source.Id == moduleId && o.Direction == (ExpansionDirection)direction);
        if (option == null)
        {
            LastMessage = "Dirección no válida.";
            return false;
        }

        return Purchase(option);
    }

    public bool PurchaseFromBase(int direction)
    {
        var option = GetOptions().FirstOrDefault(o => o.Source.Id == 0 && o.Direction == (ExpansionDirection)direction);
        if (option == null)
        {
            LastMessage = "Dirección no válida.";
            return false;
        }

        return Purchase(option);
    }

    public bool Purchase(ExpansionOption requested)
    {
        if (!CanExpand)
        {
            LastMessage = $"Límite de beta alcanzado ({MaxModules} módulos).";
            return false;
        }

        ExpansionOption? current = GetOptions().FirstOrDefault(o => o.Target == requested.Target);
        if (current == null || current.Status != ExpansionStatus.Available)
        {
            LastMessage = "Esa dirección no está disponible.";
            return false;
        }

        if (_economy == null)
        {
            LastMessage = "Economía no disponible.";
            return false;
        }

        int cost = _economy.ExpansionPrice;
        if (_economy.Balance < cost)
        {
            LastMessage = $"Fondos insuficientes: faltan {cost - _economy.Balance} $.";
            return false;
        }

        int moduleId = _nextModuleId;
        Node3D root = BuildModuleRoot(current, moduleId, _nextStationIndex);
        string label = $"{DirectionLabel(current.Direction)} desde {current.Source.Name}";

        if (!_economy.TrySpend("Expansión", $"Ampliación {label}", cost))
        {
            root.Free();
            LastMessage = _economy.LastMessage;
            return false;
        }

        _baseRoot.AddChild(root);
        RemoveFace(current.Source, current.Direction);
        _modules.Add(new GreenhouseModule(moduleId, current.Target, root) { SourceId = current.Source.Id, Direction = current.Direction });
        _nextModuleId++;
        _nextStationIndex += _stationTemplates.Count;
        LastMessage = $"Ampliación construida: {label}. Saldo: {_economy.Balance} $.";
        Notifications.Publish($"Expansión adquirida: {DirectionLabel(current.Direction)}", NoticeKind.Success);
        return true;
    }

    public bool RestoreModule(int moduleId, Vector2I target, int sourceId, ExpansionDirection direction)
    {
        GreenhouseModule? source = _modules.Find(module => module.Id == sourceId);
        if (source == null || _modules.Exists(module => module.Id == moduleId))
        {
            return false;
        }

        var option = new ExpansionOption(source, direction, target, ExpansionStatus.Available, 0);
        Node3D root = BuildModuleRoot(option, moduleId, _nextStationIndex);
        _baseRoot.AddChild(root);
        RemoveFace(source, direction);
        _modules.Add(new GreenhouseModule(moduleId, target, root) { SourceId = sourceId, Direction = direction });
        _nextModuleId = System.Math.Max(_nextModuleId, moduleId + 1);
        _nextStationIndex += _stationTemplates.Count;
        return true;
    }

    public static string DirectionLabel(ExpansionDirection direction) => direction switch
    {
        ExpansionDirection.Left => "Izquierda",
        ExpansionDirection.Right => "Derecha",
        ExpansionDirection.Front => "Frente",
        _ => "Atrás"
    };

    private Node3D BuildModuleRoot(ExpansionOption option, int moduleId, int firstStationIndex)
    {
        var root = new Node3D
        {
            Name = $"Module{moduleId}",
            Position = new Vector3(option.Target.X * ModuleWidth, 0, option.Target.Y * ModuleDepth)
        };

        string[] sharedFace = FaceNodes[Opposite(option.Direction)];
        foreach (Node template in _templates)
        {
            string name = template.Name.ToString();
            if (sharedFace.Contains(name) || name == "GlassWallRightN" || name == "GlassWallRightS")
            {
                continue;
            }

            root.AddChild(template.Duplicate());
        }

        if (!sharedFace.Contains("GlassWallRightFull"))
        {
            root.AddChild(CreateRightWall());
        }

        for (int i = 0; i < _stationTemplates.Count; i++)
        {
            GrowingStation source = _stationTemplates[i];
            var station = new GrowingStation
            {
                Name = $"GrowingStation{firstStationIndex + i}",
                Transform = source.Transform,
                PlantScene = source.PlantScene,
                PotWithSoilScene = source.PotWithSoilScene,
                WaterAmount = source.WaterAmount,
                NutrientAmount = source.NutrientAmount
            };
            station.AddChild(new CollisionShape3D { Name = "Collision", Shape = _stationShape });
            root.AddChild(station);
        }

        root.AddChild(new RobotController { Position = new Vector3(-7.2f, 0, -4) });
        root.AddChild(new Chest { Name = "Chest", Position = new Vector3(-7.2f, 0, 4) });
        return root;
    }

    private Node3D CreateRightWall()
    {
        Node? left = _templates.FirstOrDefault(t => t.Name.ToString() == "GlassWallLeft");
        var wall = (Node3D)left!.Duplicate();
        wall.Name = "GlassWallRightFull";
        wall.Transform = new Transform3D(Basis.Identity, new Vector3(HalfWidth, 2.5f, 0));
        return wall;
    }

    private static void RemoveFace(GreenhouseModule module, ExpansionDirection direction)
    {
        foreach (string name in FaceNodes[direction])
        {
            module.Root.GetNodeOrNull(name)?.QueueFree();
        }
    }

    private ExpansionStatus StatusOf(Vector2I target, List<Rect2> obstacles)
    {
        if (_modules.Any(m => m.Cell == target))
        {
            return ExpansionStatus.Occupied;
        }

        if (!CanExpand)
        {
            return ExpansionStatus.LimitReached;
        }

        Rect2 footprint = FootprintOf(target);
        return obstacles.Any(r => r.Intersects(footprint)) ? ExpansionStatus.Blocked : ExpansionStatus.Available;
    }

    private List<Rect2> GetObstacleRects()
    {
        var rects = new List<Rect2>();
        var office = _baseRoot.GetNodeOrNull<Node3D>("Office");
        if (office != null)
        {
            CollectRects(office, rects);
        }

        return rects;
    }

    private static void CollectRects(Node node, List<Rect2> rects)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh != null)
        {
            Aabb box = mesh.GlobalTransform * mesh.Mesh.GetAabb();
            rects.Add(new Rect2(box.Position.X, box.Position.Z, box.Size.X, box.Size.Z));
        }

        foreach (Node child in node.GetChildren())
        {
            CollectRects(child, rects);
        }
    }

    private static Rect2 FootprintOf(Vector2I cell)
    {
        return new Rect2(
            cell.X * ModuleWidth - ModuleWidth / 2 + Margin,
            cell.Y * ModuleDepth - ModuleDepth / 2 + Margin,
            ModuleWidth - 2 * Margin,
            ModuleDepth - 2 * Margin);
    }

    private static ExpansionDirection Opposite(ExpansionDirection direction) => direction switch
    {
        ExpansionDirection.Left => ExpansionDirection.Right,
        ExpansionDirection.Right => ExpansionDirection.Left,
        ExpansionDirection.Front => ExpansionDirection.Back,
        _ => ExpansionDirection.Front
    };

    private static int StationIndex(GrowingStation station)
    {
        return int.TryParse(station.Name.ToString().Replace("GrowingStation", string.Empty), out int index) ? index : 0;
    }
}
