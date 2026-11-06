using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class CollectorRobot : StaticBody3D, IInteractable, IProgrammableRobot, IRobotHardware
{
	public const float MinSpeed = 0.5f;
	public const float MaxSpeed = 5.0f;
	public const float DefaultSpeed = 3.0f;
	public const int MaxCapacity = 30;
	private const float ArrivalDistance = 0.08f;
	private const string Program = "void loop() {\n    if (buscarBaules() > 0) {\n        recogerCultivos();\n    }\n    if (cargaActual() > 0) {\n        regresarAlmacen();\n        depositar();\n    }\n    delay(1000);\n}\n";

	private enum Goal
	{
		None,
		Chest,
		Warehouse
	}

	private CollectorFleet _fleet = null!;
	private RobotPanel? _panel;
	private readonly Dictionary<string, int> _cargo = new();
	private List<Vector3>? _path;
	private Goal _goal;
	private Chest? _chest;
	private string? _action;
	private string? _actionError;

	public CollectorRobot()
	{
		Interpreter = new RobotInterpreter(this, line => _panel?.AppendConsole(line));
		Interpreter.Load(Program);
	}

	public RobotInterpreter Interpreter { get; }

	public int Id { get; private set; }

	public int Capacity { get; private set; } = MaxCapacity;

	public float Speed { get; private set; } = DefaultSpeed;

	public bool IsActive => Interpreter.IsRunning;

	public bool IsPaused { get; private set; }

	public int CargoTotal
	{
		get
		{
			int total = 0;
			foreach (int count in _cargo.Values)
			{
				total += count;
			}

			return total;
		}
	}

	public IReadOnlyDictionary<string, int> Cargo => _cargo;

	public string? SavedProgram { get; private set; }

	public string ProgramTitle => $"ROBOT RECOLECTOR · RECOL-{Id:00}";

	public string DefaultProgram => Program;

	public string InfoText => $"Ubicación: {LocationName}   ·   Velocidad: {Speed:F1} m/s   ·   Carga: {CargoTotal}/{Capacity}";

	public IReadOnlyList<RobotSetting> Settings => new[]
	{
		new RobotSetting("Velocidad", MinSpeed, MaxSpeed, 0.5, () => Speed, value => SetSpeed((float)value), " m/s"),
		new RobotSetting("Capacidad", 1, MaxCapacity, 1, () => Capacity, value => SetCapacity((int)value))
	};

	public string StateName => !Interpreter.IsRunning
		? (Interpreter.LastError != null ? "Error" : "Detenido")
		: IsPaused ? "Pausado" : _action != null ? "Ejecutando acción" : "Ejecutando";

	public string TaskName => _action ?? (Interpreter.IsRunning ? "Esperando" : "Sin programa en ejecución");

	public string LocationName => _fleet == null ? string.Empty : GreenhouseRouter.LocationName(_fleet.BaseRoot, Position);

	public bool IsBusy => _action != null;

	public string? ActionError => _actionError;

	public void Initialize(CollectorFleet fleet, int id)
	{
		_fleet = fleet;
		Id = id;
		Name = $"Collector{id}";
	}

	public override void _Ready()
	{
		AddToGroup("collector");
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.7f, 0.6f, 0.7f) },
			MaterialOverride = Material(0.25f, 0.45f, 0.8f),
			Position = new Vector3(0, 0.3f, 0)
		});
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.3f, 0.5f) },
			MaterialOverride = Material(0.85f, 0.85f, 0.9f),
			Position = new Vector3(0, 0.75f, 0)
		});
		AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(0.7f, 0.9f, 0.7f) },
			Position = new Vector3(0, 0.45f, 0)
		});
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

		_panel?.Refresh();
	}

	public bool LoadProgram(string source)
	{
		bool loaded = Interpreter.Load(source);
		if (loaded)
		{
			SavedProgram = source;
		}

		return loaded;
	}

	public void Run()
	{
		IsPaused = false;
		CancelAction();
		Interpreter.Start();
	}

	public void StopProgram()
	{
		IsPaused = false;
		Interpreter.Stop();
	}

	public void TogglePause()
	{
		if (Interpreter.IsRunning)
		{
			IsPaused = !IsPaused;
		}
	}

	public void ApplySavedState(Vector3 position, float speed, int capacity, IReadOnlyDictionary<string, int> cargo, string? program, bool running, bool paused)
	{
		Position = position;
		SetSpeed(speed);
		SetCapacity(capacity);
		_cargo.Clear();
		foreach (var (itemId, count) in cargo)
		{
			_cargo[itemId] = count;
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

	public void Start() => Run();

	public void Stop() => StopProgram();

	public string GetInteractionPrompt() => "[E] Programar robot recolector";

	public void Interact()
	{
		_panel?.Open(GetTree().GetFirstNodeInGroup("player") as PlayerController);
	}

	public void SetSpeed(float metersPerSecond)
	{
		Speed = Mathf.Clamp(metersPerSecond, MinSpeed, MaxSpeed);
	}

	public void SetCapacity(int capacity)
	{
		Capacity = Mathf.Clamp(capacity, 1, MaxCapacity);
	}

	public bool IsAction(string name) => name is "recogerCultivos" or "regresarAlmacen" or "depositar";

	public void BeginAction(string action)
	{
		_actionError = null;
		_path = null;
		_chest = null;
		_goal = Goal.None;
		if (action == "depositar")
		{
			Deposit();
			return;
		}

		_action = action;
	}

	public void CancelAction()
	{
		_action = null;
		_actionError = null;
		_path = null;
		_chest = null;
		_goal = Goal.None;
	}

	public bool TryCall(string name, IReadOnlyList<double> args, out double result)
	{
		result = 0;
		switch (name)
		{
			case "configurarVelocidad" when args.Count == 1:
				SetSpeed((float)args[0]);
				return true;
			case "configurarCapacidad" when args.Count == 1:
				SetCapacity((int)args[0]);
				return true;
			case "buscarBaules" when args.Count == 0:
				result = _fleet.ChestsWithStock(Position).Count;
				return true;
			case "cargaActual" when args.Count == 0:
				result = CargoTotal;
				return true;
			default:
				return false;
		}
	}

	private void TickAction(double delta)
	{
		switch (_action)
		{
			case "recogerCultivos":
				TickHarvest(delta);
				break;
			case "regresarAlmacen":
				TickReturn(delta);
				break;
		}
	}

	private void TickHarvest(double delta)
	{
		if (_path != null)
		{
			if (FollowPath(delta) && _chest != null)
			{
				LoadFrom(_chest);
				_path = null;
				_chest = null;
			}

			return;
		}

		if (CargoTotal >= Capacity)
		{
			EndAction();
			return;
		}

		foreach (Chest chest in _fleet.ChestsWithStock(Position))
		{
			List<Vector3>? route = GreenhouseRouter.RouteTo(_fleet.BaseRoot, Position, GreenhouseRouter.BasePosition(_fleet.BaseRoot, chest), AisleNavigator.ChestHalf);
			if (route != null)
			{
				_path = route;
				_chest = chest;
				_goal = Goal.Chest;
				return;
			}
		}

		EndAction();
	}

	private void TickReturn(double delta)
	{
		if (_path == null)
		{
			List<Vector3>? route = GreenhouseRouter.RouteTo(_fleet.BaseRoot, Position, _fleet.Warehouse.Position, Vector2.Zero);
			if (route == null)
			{
				Fail("Sin ruta al almacén");
				return;
			}

			_path = route;
			_goal = Goal.Warehouse;
			return;
		}

		if (FollowPath(delta))
		{
			EndAction();
		}
	}

	private void Deposit()
	{
		if (!GreenhouseRouter.IsInOffice(Position))
		{
			_actionError = "depositar() requiere regresarAlmacen() antes";
			return;
		}

		foreach (var (itemId, count) in _cargo)
		{
			_fleet.Warehouse.Deposit(itemId, count);
		}

		_cargo.Clear();
	}

	private void LoadFrom(Chest chest)
	{
		int room = Capacity - CargoTotal;
		foreach (string itemId in new List<string>(chest.Contents.Keys))
		{
			if (room <= 0)
			{
				break;
			}

			int taken = chest.Withdraw(itemId, room);
			if (taken > 0)
			{
				_cargo[itemId] = _cargo.GetValueOrDefault(itemId) + taken;
				room -= taken;
			}
		}
	}

	private void Fail(string message)
	{
		_actionError = message;
		EndAction();
	}

	private void EndAction()
	{
		_action = null;
		_path = null;
		_chest = null;
		_goal = Goal.None;
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

	private static StandardMaterial3D Material(float r, float g, float b) => new() { AlbedoColor = new Color(r, g, b), Roughness = 0.6f };
}
