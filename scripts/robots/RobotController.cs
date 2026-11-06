using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class RobotController : StaticBody3D, IInteractable, IRobotHardware
{
	public const float IrrigationRate = 8.0f;
	public const int Capacity = 6;
	private const float WalkSpeed = 1.5f;
	private const float ArrivalDistance = 0.6f;

	[Export] public string TargetStation = "GrowingStation0";

	private GrowingStation? _station;
	private RobotPanel _panel = null!;
	private bool _irrigating;
	private InventoryManager? _inventory;
	private GrowingStation? _target;
	private CropDefinition? _lastCrop;
	private List<Vector3>? _path;
	private Chest? _chest;
	private float _idle;
	private string _task = "Buscando";
	private readonly Dictionary<string, int> _cargo = new();
	private readonly Dictionary<string, int> _seeds = new();

	public RobotInterpreter Interpreter { get; private set; } = null!;

	public string StationName => TargetStation;

	public string TaskName => _task;

	public string ModuleName => GetParent() is Node3D parent && parent.Name.ToString().StartsWith("Module")
		? "Módulo " + parent.Name.ToString().Substring(6)
		: "Base";

	public int CargoTotal => Sum(_cargo);

	public int ChestTotal => GetParent().GetNodeOrNull<Chest>("Chest")?.Total ?? 0;

	public bool IsRunning => Interpreter.IsRunning;

	public int ErrorLine => Interpreter.ErrorLine;

	public string LastError => Interpreter.LastError ?? string.Empty;

	public string StateName => Interpreter.LastError != null
		? "Error"
		: Interpreter.IsRunning
			? (Interpreter.IsWaiting ? "Esperando" : "Ejecutando")
			: Interpreter.IsLoaded ? "Preparado" : "Inactivo";

	public override void _Ready()
	{
		AddToGroup("robot");
		BuildBody();
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		_station = GetParent().GetNodeOrNull<GrowingStation>(TargetStation);
		Interpreter = new RobotInterpreter(this, line => _panel.AppendConsole(line));
		_panel = new RobotPanel();
		AddChild(_panel);
		_panel.Initialize(this);
	}

	public override void _Process(double delta)
	{
		CropData? crop = _station?.Crop;
		if (_irrigating && crop != null)
		{
			crop.Water += IrrigationRate * (float)delta;
			crop.ClampValues();
		}

		Interpreter.Update(delta);
		TickAutonomy(delta);
		_panel.Refresh();
	}

	public float ReadHumidity() => _station?.Crop?.Water ?? 0.0f;

	public void SetIrrigation(bool on)
	{
		_irrigating = on;
	}

	public bool LoadProgram(string source) => Interpreter.Load(source);

	public void Run()
	{
		if (_station?.Crop == null)
		{
			_panel.AppendConsole("Aviso: la estación no tiene cultivo; la humedad leída será 0.");
		}

		Interpreter.Start();
	}

	public void StopProgram() => Interpreter.Stop();

	public int CargoOf(string itemId) => _cargo.TryGetValue(itemId, out int count) ? count : 0;

	public string GetInteractionPrompt() => "[E] Abrir panel del robot";

	public void Interact()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		_panel.Open(player);
	}

	private void TickAutonomy(double delta)
	{
		switch (_task)
		{
			case "Buscando":
				_target = FindMatureStation();
				if (_target != null)
				{
					_task = "Desplazándose";
				}
				else if (_cargo.Count > 0)
				{
					_task = "Transportando";
				}
				else
				{
					_idle = 0;
					_task = "Esperando";
				}

				break;
			case "Esperando":
				_idle += (float)delta;
				if (_idle >= 2.0f)
				{
					_task = "Buscando";
				}

				break;
			case "Desplazándose":
				if (_target == null || !_target.IsOccupied)
				{
					_task = "Buscando";
					_path = null;
				}
				else if (_path == null && !PlanTo(_target.Position, AisleNavigator.StationHalf))
				{
					Halt("Sin ruta por los pasillos hasta la estación " + _target.Name);
				}
				else if (FollowPath(delta))
				{
					_task = "Cosechando";
					_path = null;
				}

				break;
			case "Cosechando":
				if (_target != null)
				{
					Harvest(_target);
				}

				_task = _lastCrop != null && _seeds.GetValueOrDefault(_lastCrop.SeedId) > 0 ? "Replantando" : "Buscando";
				break;
			case "Replantando":
				if (_target != null)
				{
					Replant(_target);
				}

				_task = "Buscando";
				break;
			case "Transportando":
				_chest ??= GetParent().GetNodeOrNull<Chest>("Chest");
				if (_chest == null)
				{
					_task = "Buscando";
				}
				else if (_path == null && !PlanTo(_chest.Position, AisleNavigator.ChestHalf))
				{
					Halt("Sin ruta por los pasillos hasta el baúl");
				}
				else if (FollowPath(delta))
				{
					_task = "Depositando";
					_path = null;
				}

				break;
			case "Sin ruta":
				_idle += (float)delta;
				if (_idle >= 5.0f)
				{
					_task = "Buscando";
				}

				break;
			case "Depositando":
				if (_chest != null)
				{
					_chest.Deposit(_cargo);
				}

				_cargo.Clear();
				_task = "Buscando";
				break;
		}

		if (_task == "Buscando" && CargoTotal >= Capacity)
		{
			_task = "Transportando";
		}
	}

	private GrowingStation? FindMatureStation()
	{
		if (CargoTotal >= Capacity)
		{
			return null;
		}

		foreach (Node child in GetParent().GetChildren())
		{
			if (child is GrowingStation station && station.Crop != null && station.Crop.IsMature)
			{
				return station;
			}
		}

		return null;
	}

	private void Harvest(GrowingStation station)
	{
		CropDefinition? crop = station.PlantedCrop;
		if (crop == null || _inventory == null)
		{
			return;
		}

		_lastCrop = crop;
		int product = _inventory.GetQuantity(crop.ProductId);
		int seed = _inventory.GetQuantity(crop.SeedId);
		station.Interact();
		Move(_cargo, crop.ProductId, _inventory.GetQuantity(crop.ProductId) - product);
		Move(_seeds, crop.SeedId, _inventory.GetQuantity(crop.SeedId) - seed);
	}

	private void Move(Dictionary<string, int> into, string itemId, int gained)
	{
		if (gained <= 0 || _inventory == null || !_inventory.RemoveItem(itemId, gained))
		{
			return;
		}

		into[itemId] = into.GetValueOrDefault(itemId) + gained;
	}

	private void Replant(GrowingStation station)
	{
		if (_lastCrop == null || !station.PlantCrop(_lastCrop))
		{
			return;
		}

		_seeds[_lastCrop.SeedId] -= 1;
	}

	private bool PlanTo(Vector3 targetPosition, Vector2 footprint)
	{
		_path = AisleNavigator.PlanToTarget(GetParent() as Node3D ?? this, Position, targetPosition, footprint);
		return _path != null;
	}

	private bool FollowPath(double delta)
	{
		if (_path == null || _path.Count == 0)
		{
			return true;
		}

		Vector3 next = _path[0];
		Vector3 step = new Vector3(next.X - Position.X, 0, next.Z - Position.Z);
		float distance = step.Length();
		float move = WalkSpeed * (float)delta;
		if (distance <= Mathf.Max(move, 0.02f))
		{
			Position = new Vector3(next.X, Position.Y, next.Z);
			_path.RemoveAt(0);
			return _path.Count == 0;
		}

		Position += step / distance * move;
		return false;
	}

	private void Halt(string message)
	{
		_task = "Sin ruta";
		_idle = 0;
		_path = null;
		_target = null;
		_panel.AppendConsole(message);
	}

	private static int Sum(Dictionary<string, int> values)
	{
		int total = 0;
		foreach (int value in values.Values)
		{
			total += value;
		}

		return total;
	}

	private void BuildBody()
	{
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.7f, 0.9f, 0.7f) },
			MaterialOverride = Material(0.32f, 0.38f, 0.42f),
			Position = new Vector3(0, 0.45f, 0)
		});
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.12f, 0.02f) },
			MaterialOverride = Material(0.45f, 0.85f, 0.55f),
			Position = new Vector3(0, 0.6f, 0.36f)
		});
		AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = 0.22f, Height = 0.44f },
			MaterialOverride = Material(0.8f, 0.82f, 0.84f),
			Position = new Vector3(0, 1.1f, 0)
		});
		AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(0.7f, 0.9f, 0.7f) },
			Position = new Vector3(0, 0.45f, 0)
		});
	}

	private static StandardMaterial3D Material(float r, float g, float b) => new() { AlbedoColor = new Color(r, g, b), Roughness = 0.6f };
}
