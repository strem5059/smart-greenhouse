using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class RobotController : StaticBody3D, IInteractable, IRobotHardware, IProgrammableRobot
{
	public const int DefaultCapacity = 6;
	public const int MinCapacity = 1;
	public const int MaxCapacity = 12;
	public const float MinSpeed = 0.5f;
	public const float MaxSpeed = 5.0f;
	public const float DefaultSpeed = 1.5f;
	private const float ArrivalDistance = 0.08f;

	private RobotPanel _panel = null!;
	private InventoryManager? _inventory;
	private Chest? _chest;
	private readonly Dictionary<string, int> _cargo = new();
	private readonly Dictionary<string, int> _seeds = new();
	private List<Vector3>? _path;
	private GrowingStation? _target;
	private CropDefinition? _lastCrop;
	private string? _action;
	private string? _actionError;
	private bool _harvestEnabled;

	public RobotInterpreter Interpreter { get; private set; } = null!;

	public float Speed { get; private set; } = DefaultSpeed;

	public int CapacityLimit { get; private set; } = DefaultCapacity;

	public bool IsPaused { get; private set; }

	public bool HarvestEnabled => _harvestEnabled;

	public bool IsBusy => _action != null;

	public string? ActionError => _actionError;

	public string TaskName => _action ?? (Interpreter.IsRunning ? "Esperando" : "Sin programa en ejecución");

	public string ModuleName => GetParent() is Node3D parent && parent.Name.ToString().StartsWith("Module")
		? "Módulo " + parent.Name.ToString().Substring(6)
		: "Base";

	public int CargoTotal => Sum(_cargo);

	public int ChestTotal => GetParent().GetNodeOrNull<Chest>("Chest")?.Total ?? 0;

	public bool IsRunning => Interpreter.IsRunning;

	public int ErrorLine => Interpreter.ErrorLine;

	public int CurrentLine => Interpreter.CurrentLine;

	public string LastError => Interpreter.LastError ?? string.Empty;

	public string StateName => Interpreter.LastError != null
		? "Error"
		: IsPaused
			? "Pausado"
			: Interpreter.IsRunning
				? (Interpreter.IsWaiting || IsBusy ? "Ejecutando acción" : "Ejecutando")
				: Interpreter.IsLoaded ? "Preparado" : "Inactivo";

	public override void _Ready()
	{
		AddToGroup("robot");
		BuildBody();
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		Interpreter = new RobotInterpreter(this, line => _panel.AppendConsole(line));
		_panel = new RobotPanel();
		AddChild(_panel);
		_panel.Initialize(this);
	}

	public override void _Process(double delta)
	{
		if (!IsPaused)
		{
			if (_action != null)
			{
				TickAction(delta);
			}

			Interpreter.Update(delta);
		}

		_panel.Refresh();
	}

	public string? SavedProgram { get; private set; }

	public string ProgramTitle => "ROBOT AGRÍCOLA · AGRO-01";

	public string DefaultProgram => "void loop() {\n    configurarVelocidad(3.0);\n\n    if (hayPlantasEnModulo()) {\n        int i = 0;\n        while (i < numeroPlantas()) {\n            if (necesitaRiego(i)) {\n                regar(i);\n            }\n\n            if (necesitaFertilizante(i)) {\n                if (hayFertilizante() == 0) {\n                    Peticion(Fertilizante);\n                    esperarRecurso(Fertilizante);\n                }\n\n                fertilizar(i);\n            }\n\n            i = i + 1;\n        }\n\n        activarCosecha();\n        recolectar();\n        replantar();\n        moverABaul();\n    }\n\n    delay(1000);\n}\n";

	public string InfoText => $"Módulo: {ModuleName}   ·   Velocidad: {Speed:F1} m/s   ·   Carga: {CargoTotal}/{CapacityLimit}";

	public IReadOnlyList<RobotSetting> Settings => new[]
	{
		new RobotSetting("Velocidad", MinSpeed, MaxSpeed, 0.5, () => Speed, value => SetSpeed((float)value), " m/s"),
		new RobotSetting("Carga máx.", MinCapacity, MaxCapacity, 1, () => CapacityLimit, value => SetCapacity((int)value))
	};

	public bool LoadProgram(string source)
	{
		bool loaded = Interpreter.Load(source);
		if (loaded)
		{
			SavedProgram = source;
		}

		return loaded;
	}

	public bool IsAction(string name) => name is "recolectar" or "replantar" or "moverABaul" or "esperarRecurso";

	public int FertilizerStock { get; private set; }

	public IReadOnlyDictionary<string, int> CargoItems => _cargo;

	public IReadOnlyDictionary<string, int> SeedItems => _seeds;

	public void ApplySavedState(Vector3 position, float speed, int capacity, IReadOnlyDictionary<string, int> cargo, IReadOnlyDictionary<string, int> seeds, int fertilizer, bool harvestEnabled, string? program, bool running, bool paused)
	{
		Position = position;
		SetSpeed(speed);
		SetCapacity(capacity);
		_cargo.Clear();
		foreach (var (itemId, count) in cargo)
		{
			_cargo[itemId] = count;
		}

		_seeds.Clear();
		foreach (var (itemId, count) in seeds)
		{
			_seeds[itemId] = count;
		}

		FertilizerStock = fertilizer;
		if (harvestEnabled)
		{
			EnableHarvest();
		}

		if (!string.IsNullOrEmpty(program))
		{
			LoadProgram(program);
		}

		if (running)
		{
			Run();
			if (paused)
			{
				TogglePause();
			}
		}
	}

	public void ReceiveSupply(string itemId, int quantity)
	{
		if (itemId == "fertilizer" && quantity > 0)
		{
			FertilizerStock += quantity;
		}
	}

	private CollectorFleet? Fleet => GetTree().GetFirstNodeInGroup("collector_fleet") as CollectorFleet;

	private List<GrowingStation> PlantsInModule() => ModuleStations().FindAll(station => station.IsOccupied);

	private GrowingStation? PlantAt(IReadOnlyList<double> args)
	{
		List<GrowingStation> plants = PlantsInModule();
		int index = (int)args[0];
		return index >= 0 && index < plants.Count ? plants[index] : null;
	}

	private void RequestFertilizer()
	{
		int needed = PlantsInModule().FindAll(station => station.NeedsFertilizerNow).Count - FertilizerStock;
		if (needed > 0)
		{
			Fleet?.RequestSupply(this, "fertilizer", needed);
		}
	}

	public bool TryCall(string name, IReadOnlyList<double> args, out double result)
	{
		result = 0;
		switch (name)
		{
			case "hayPlantasEnModulo" when args.Count == 0:
				result = HasPlantsInModule() ? 1 : 0;
				return true;
			case "configurarVelocidad" when args.Count == 1:
				SetSpeed((float)args[0]);
				return true;
			case "numeroPlantas" when args.Count == 0:
				result = PlantsInModule().Count;
				return true;
			case "necesitaRiego" when args.Count == 1:
				result = PlantAt(args)?.NeedsWaterNow == true ? 1 : 0;
				return true;
			case "regar" when args.Count == 1:
				if (PlantAt(args) is { NeedsWaterNow: true } watered)
				{
					watered.ReceiveWater();
				}

				return true;
			case "necesitaFertilizante" when args.Count == 1:
				result = PlantAt(args)?.NeedsFertilizerNow == true ? 1 : 0;
				return true;
			case "fertilizar" when args.Count == 1:
				if (FertilizerStock > 0 && PlantAt(args) is { NeedsFertilizerNow: true } fertilized)
				{
					FertilizerStock--;
					fertilized.ReceiveNutrients();
				}

				return true;
			case "hayFertilizante" when args.Count == 0:
				result = FertilizerStock > 0 ? 1 : 0;
				return true;
			case "Peticion" when args.Count == 1 && args[0] == 1:
				RequestFertilizer();
				return true;
			case "activarCosecha" when args.Count == 0:
				EnableHarvest();
				return true;
			default:
				return false;
		}
	}

	public void Run()
	{
		IsPaused = false;
		Interpreter.Start();
	}

	public void StopProgram()
	{
		IsPaused = false;
		Interpreter.Stop();
	}

	public void TogglePause()
	{
		if (!Interpreter.IsRunning)
		{
			return;
		}

		IsPaused = !IsPaused;
	}

	public void SetSpeed(float metersPerSecond)
	{
		Speed = Mathf.Clamp(metersPerSecond, MinSpeed, MaxSpeed);
	}

	public void SetCapacity(int capacity)
	{
		CapacityLimit = Mathf.Clamp(capacity, MinCapacity, MaxCapacity);
	}

	public bool HasPlantsInModule() => ModuleStations().Exists(station => station.IsOccupied);

	public void EnableHarvest()
	{
		_harvestEnabled = true;
	}

	public void BeginAction(string action)
	{
		_actionError = null;
		_path = null;
		_target = null;
		if (action == "recolectar" && !_harvestEnabled)
		{
			Fail("recolectar() requiere activarCosecha() antes");
			return;
		}

		_action = action;
	}

	public void CancelAction()
	{
		Fleet?.CancelRequests(this);
		_action = null;
		_actionError = null;
		_path = null;
		_target = null;
	}

	public string GetInteractionPrompt() => "[E] Abrir panel del robot";

	public void Interact()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		_panel.Open(player);
	}

	private void TickAction(double delta)
	{
		switch (_action)
		{
			case "recolectar":
				TickHarvest(delta);
				break;
			case "replantar":
				TickReplant(delta);
				break;
			case "moverABaul":
				TickDeposit(delta);
				break;
			case "esperarRecurso":
				if (FertilizerStock > 0)
				{
					EndAction();
				}

				break;
		}
	}

	private void TickHarvest(double delta)
	{
		if (_target == null)
		{
			_target = ModuleStations().Find(station => station.Crop != null && station.Crop.IsMature);
			if (_target == null || CargoTotal >= CapacityLimit)
			{
				EndAction();
				return;
			}

			if (!PlanTo(_target.Position, AisleNavigator.StationHalf))
			{
				Fail("Sin ruta por los pasillos hasta la planta madura");
				return;
			}
		}

		if (FollowPath(delta))
		{
			Harvest(_target);
			_target = null;
			_path = null;
		}
	}

	private void TickReplant(double delta)
	{
		if (_target == null)
		{
			string? seedId = FirstSeed();
			CropDefinition? crop = CropCatalog.FromSeed(seedId);
			_target = ModuleStations().Find(station => !station.IsOccupied);
			if (crop == null || _target == null)
			{
				EndAction();
				return;
			}

			_lastCrop = crop;
			if (!PlanTo(_target.Position, AisleNavigator.StationHalf))
			{
				Fail("Sin ruta por los pasillos hasta la estación libre");
				return;
			}
		}

		if (FollowPath(delta))
		{
			if (_lastCrop != null && _target.PlantCrop(_lastCrop))
			{
				_seeds[_lastCrop.SeedId] -= 1;
			}

			_target = null;
			_path = null;
		}
	}

	private void TickDeposit(double delta)
	{
		if (CargoTotal == 0)
		{
			EndAction();
			return;
		}

		_chest ??= GetParent().GetNodeOrNull<Chest>("Chest");
		if (_chest == null)
		{
			Fail("No hay baúl en este módulo");
			return;
		}

		if (_path == null && !PlanTo(_chest.Position, AisleNavigator.ChestHalf))
		{
			Fail("Sin ruta por los pasillos hasta el baúl");
			return;
		}

		if (FollowPath(delta))
		{
			_chest.Deposit(_cargo);
			_cargo.Clear();
			EndAction();
		}
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

	private string? FirstSeed()
	{
		foreach (var (seedId, count) in _seeds)
		{
			if (count > 0)
			{
				return seedId;
			}
		}

		return null;
	}

	private List<GrowingStation> ModuleStations()
	{
		var stations = new List<GrowingStation>();
		foreach (Node child in GetParent().GetChildren())
		{
			if (child is GrowingStation station)
			{
				stations.Add(station);
			}
		}

		return stations;
	}

	private void EndAction()
	{
		_action = null;
		_path = null;
		_target = null;
	}

	private void Fail(string message)
	{
		_actionError = message;
		_panel.AppendConsole(message);
		EndAction();
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
		float move = Speed * (float)delta;
		if (distance <= Mathf.Max(move, ArrivalDistance))
		{
			Position = new Vector3(next.X, Position.Y, next.Z);
			_path.RemoveAt(0);
			return _path.Count == 0;
		}

		Position += step / distance * move;
		return false;
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
