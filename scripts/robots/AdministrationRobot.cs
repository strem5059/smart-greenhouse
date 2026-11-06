using System.Collections.Generic;
using System.Linq;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Economy;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Management;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class AdministrationRobot : StaticBody3D, IInteractable, IProgrammableRobot, IRobotHardware
{
	public const int DefaultMinimum = 10;
	public const int DefaultMultiplier = 3;
	public const int DefaultReserve = 5000;
	public const int DefaultSaleReserve = 5;
	public const int DefaultMinimumSaleUnits = 5;
	public const float DefaultReviewSeconds = 3.0f;
	private const int MaxLog = 8;
	private const string Program = "void loop() {\n    venderCosechas();\n    atenderPeticiones();\n    if (comprobarExistencias() > 0) {\n        comprarInsumos();\n    }\n    esperarRevision();\n}\n";

	private CollectorFleet _fleet = null!;
	private EconomyManager? _economy;
	private InventoryManager? _inventory;
	private RobotPanel? _panel;
	private readonly Dictionary<string, int> _minimums = new();
	private readonly Dictionary<string, int> _saleReserves = new();
	private readonly Dictionary<string, int> _salePriorities = new();
	private readonly List<string> _pending = new();
	private readonly List<string> _log = new();
	private string? _action;
	private string? _actionError;
	private float _wait;
	private bool _blocked;

	public AdministrationRobot()
	{
		Interpreter = new RobotInterpreter(this, line => _panel?.AppendConsole(line));
		Interpreter.Load(Program);
		Position = new Vector3(11.8f, 0, -2.2f);
	}

	public RobotInterpreter Interpreter { get; }

	public static IReadOnlyList<string> SupplyIds { get; } = BuildSupplyIds();

	public int Multiplier { get; private set; } = DefaultMultiplier;

	public int MoneyReserve { get; private set; } = DefaultReserve;

	public float ReviewSeconds { get; private set; } = DefaultReviewSeconds;

	public bool AutoSell { get; private set; } = true;

	public int MinimumSaleUnits { get; private set; } = DefaultMinimumSaleUnits;

	public string LastAction { get; private set; } = "Sin acciones todavía";

	public int TotalBought { get; private set; }

	public int TotalSold { get; private set; }

	public IReadOnlyList<string> Pending => _pending;

	public IReadOnlyList<string> Log => _log;

	public bool IsActive => Interpreter.IsRunning;

	public bool IsPaused { get; private set; }

	public string? SavedProgram { get; private set; }

	public string ProgramTitle => "ROBOT ADMINISTRATIVO · OFICINA";

	public string DefaultProgram => Program;

	public string InfoText => $"Dinero: {PanelTheme.Money(_economy?.Balance ?? 0)}   ·   Reserva: {PanelTheme.Money(MoneyReserve)}   ·   Ventas automáticas: {(AutoSell ? "sí" : "no")}";

	public string StateName => !Interpreter.IsRunning
		? (Interpreter.LastError != null ? "Error" : "Detenido")
		: IsPaused ? "Pausado" : "Supervisando";

	public string TaskName => _action ?? (Interpreter.IsRunning ? "Supervisando" : "Sin programa en ejecución");

	public IReadOnlyList<RobotSetting> Settings => new[]
	{
		new RobotSetting("Frecuencia de revisión", 1, 60, 1, () => ReviewSeconds, value => SetReviewSeconds((float)value), " s")
	};

	public bool IsBusy => _action != null;

	public string? ActionError => _actionError;

	public void Initialize(CollectorFleet fleet)
	{
		_fleet = fleet;
		Name = "OfficeAdministrator";
	}

	public override void _Ready()
	{
		AddToGroup("office_administrator");
		_economy = GetNodeOrNull<EconomyManager>("/root/EconomyManager");
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.9f, 0.5f) },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.7f, 0.6f, 0.35f), Roughness = 0.6f },
			Position = new Vector3(0, 0.45f, 0)
		});
		AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = 0.2f, Height = 0.4f },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.95f), Roughness = 0.6f },
			Position = new Vector3(0, 1.05f, 0)
		});
		AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(0.8f, 0.9f, 0.5f) },
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
				_wait -= (float)delta;
				if (_wait <= 0)
				{
					EndAction();
				}
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

	public void ApplySavedState(int bought, int sold, string? program, bool running, bool paused)
	{
		TotalBought = bought;
		TotalSold = sold;
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

	public string GetInteractionPrompt() => "[E] Programar robot administrativo";

	public void Interact()
	{
		_panel?.Open(GetTree().GetFirstNodeInGroup("player") as PlayerController);
	}

	public int MinimumFor(string itemId) => _minimums.GetValueOrDefault(itemId, DefaultMinimum);

	public int TargetFor(string itemId) => MinimumFor(itemId) * Multiplier;

	public void SetMinimum(string itemId, int value) => _minimums[itemId] = System.Math.Clamp(value, 0, 200);

	public void SetMultiplier(int value) => Multiplier = System.Math.Clamp(value, 1, 10);

	public void SetReserve(int value) => MoneyReserve = System.Math.Clamp(value, 0, 1000000);

	public void SetReviewSeconds(float value) => ReviewSeconds = Mathf.Clamp(value, 1f, 60f);

	public int SaleReserveFor(string productId) => _saleReserves.GetValueOrDefault(productId, DefaultSaleReserve);

	public void SetSaleReserve(string productId, int value) => _saleReserves[productId] = System.Math.Clamp(value, 0, 10000);

	public int SalePriorityFor(string productId) => _salePriorities.TryGetValue(productId, out int priority) ? priority : DefaultPriority(productId);

	public void SetSalePriority(string productId, int value) => _salePriorities[productId] = System.Math.Clamp(value, 1, 99);

	public void SetAutoSell(bool enabled) => AutoSell = enabled;

	public void SetMinimumSaleUnits(int value) => MinimumSaleUnits = System.Math.Clamp(value, 1, 10000);

	public bool IsAction(string name) => name == "esperarRevision";

	public void BeginAction(string action)
	{
		_actionError = null;
		if (action == "esperarRevision")
		{
			_action = action;
			_wait = ReviewSeconds;
		}
	}

	public void CancelAction()
	{
		_action = null;
		_actionError = null;
		_wait = 0;
	}

	public bool TryCall(string name, IReadOnlyList<double> args, out double result)
	{
		result = 0;
		switch (name)
		{
			case "configurarFrecuencia" when args.Count == 1:
				SetReviewSeconds((float)args[0]);
				return true;
			case "comprobarExistencias" when args.Count == 0:
				result = LowSupplies();
				return true;
			case "comprarInsumos" when args.Count == 0:
				PurchaseSupplies();
				return true;
			case "venderCosechas" when args.Count == 0:
				result = SellSurplus();
				return true;
			case "atenderPeticiones" when args.Count == 0:
				FulfillRequests();
				return true;
			case "reservarDinero" when args.Count == 1:
				SetReserve((int)args[0]);
				return true;
			case "reservarCosecha" when args.Count == 1:
				foreach (CropDefinition crop in CropCatalog.Crops)
				{
					SetSaleReserve(crop.ProductId, (int)args[0]);
				}

				return true;
			case "activarVentas" when args.Count == 1:
				SetAutoSell(args[0] != 0);
				return true;
			default:
				return false;
		}
	}

	private void EndAction()
	{
		_action = null;
		_wait = 0;
	}

	private int LowSupplies()
	{
		int count = 0;
		foreach (string id in SupplyIds)
		{
			if (_inventory != null && _inventory.GetQuantity(id) < MinimumFor(id))
			{
				count++;
			}
		}

		return count;
	}

	private void PurchaseSupplies()
	{
		if (_economy == null || _inventory == null || _fleet == null)
		{
			return;
		}

		var needs = new List<(string Id, int Quantity, int Cost)>();
		foreach (string id in SupplyIds)
		{
			int stock = _inventory.GetQuantity(id);
			int price = _economy.GetPrice(id);
			if (stock >= MinimumFor(id) || price <= 0)
			{
				continue;
			}

			int quantity = TargetFor(id) - stock;
			needs.Add((id, quantity, quantity * price));
		}

		_pending.Clear();
		if (needs.Count == 0)
		{
			_blocked = false;
			LastAction = "Existencias de insumos correctas";
			return;
		}

		int cost = needs.Sum(need => need.Cost);
		int shortfall = cost + MoneyReserve - _economy.Balance;
		if (shortfall > 0 && !RaiseFunds(shortfall))
		{
			foreach (var need in needs)
			{
				_pending.Add(ItemCatalog.NameOf(need.Id));
			}

			if (!_blocked)
			{
				_blocked = true;
				Report("Dinero insuficiente para reponer insumos; se espera a tener excedentes vendibles");
				Notifications.Publish("Robot administrativo: dinero insuficiente para reponer insumos", NoticeKind.Warning);
			}

			return;
		}

		_blocked = false;
		foreach (var need in needs)
		{
			string name = ItemCatalog.NameOf(need.Id);
			if (_economy.BuyItem(need.Id, need.Quantity))
			{
				TotalBought += need.Cost;
				Report($"Compra: {need.Quantity} x {name} por {need.Cost} $");
			}
			else
			{
				_pending.Add(name);
			}
		}
	}

	private void FulfillRequests()
	{
		if (_economy == null || _inventory == null || _fleet == null)
		{
			return;
		}

		foreach (SupplyRequest request in _fleet.Requests.ToList())
		{
			int price = _economy.GetPrice(request.ItemId);
			int missing = System.Math.Max(0, request.Quantity - _inventory.GetQuantity(request.ItemId));
			if (missing > 0)
			{
				int cost = missing * price;
				int shortfall = cost + MoneyReserve - _economy.Balance;
				if (price <= 0 || (shortfall > 0 && !RaiseFunds(shortfall)))
				{
					string waiting = $"{ItemCatalog.NameOf(request.ItemId)} para {request.Robot.ModuleName}";
					if (!_pending.Contains(waiting))
					{
						_pending.Add(waiting);
					}

					continue;
				}

				if (!_economy.BuyItem(request.ItemId, missing))
				{
					continue;
				}

				TotalBought += cost;
			}

			if (!_inventory.RemoveItem(request.ItemId, request.Quantity))
			{
				continue;
			}

			request.Robot.ReceiveSupply(request.ItemId, request.Quantity);
			_fleet.CompleteRequest(request);
			Report($"Entrega a {request.Robot.ModuleName}: {request.Quantity} x {ItemCatalog.NameOf(request.ItemId)}");
		}
	}

	private int SellSurplus()
	{
		if (!AutoSell || _economy == null || _fleet == null)
		{
			return 0;
		}

		int total = 0;
		foreach (CropDefinition crop in CropCatalog.Crops)
		{
			total += Sellable(crop.ProductId);
		}

		if (total < MinimumSaleUnits)
		{
			return 0;
		}

		int revenue = 0;
		foreach (CropDefinition crop in SalesOrder())
		{
			int units = Sellable(crop.ProductId);
			if (units > 0)
			{
				revenue += SellUnits(crop.ProductId, units);
			}
		}

		return revenue;
	}

	private bool RaiseFunds(int amount)
	{
		if (!AutoSell || _economy == null)
		{
			return false;
		}

		int potential = 0;
		foreach (CropDefinition crop in SalesOrder())
		{
			potential += Sellable(crop.ProductId) * _economy.GetSalePrice(crop.ProductId);
		}

		if (potential < amount)
		{
			return false;
		}

		int remaining = amount;
		foreach (CropDefinition crop in SalesOrder())
		{
			int price = _economy.GetSalePrice(crop.ProductId);
			if (remaining <= 0 || price <= 0)
			{
				continue;
			}

			int units = System.Math.Min(Sellable(crop.ProductId), (remaining + price - 1) / price);
			if (units > 0)
			{
				remaining -= SellUnits(crop.ProductId, units);
			}
		}

		return remaining <= 0;
	}

	private int SellUnits(string productId, int units)
	{
		int taken = _fleet.Warehouse.Take(productId, units);
		if (taken <= 0)
		{
			return 0;
		}

		int price = _economy!.GetSalePrice(productId);
		if (!_economy.SellFromWarehouse(productId, taken))
		{
			_fleet.Warehouse.Deposit(productId, taken);
			return 0;
		}

		int revenue = taken * price;
		TotalSold += revenue;
		Report($"Venta desde el almacén: {taken} x {ItemCatalog.NameOf(productId)} por {revenue} $");
		return revenue;
	}

	private int Sellable(string productId)
	{
		return System.Math.Max(0, _fleet.Warehouse.Count(productId) - SaleReserveFor(productId));
	}

	private IEnumerable<CropDefinition> SalesOrder()
	{
		return CropCatalog.Crops.OrderBy(crop => SalePriorityFor(crop.ProductId));
	}

	private void Report(string text)
	{
		LastAction = text;
		_log.Insert(0, text);
		if (_log.Count > MaxLog)
		{
			_log.RemoveAt(_log.Count - 1);
		}
	}

	private static int DefaultPriority(string productId)
	{
		int index = 0;
		foreach (CropDefinition crop in CropCatalog.Crops)
		{
			index++;
			if (crop.ProductId == productId)
			{
				return index;
			}
		}

		return 99;
	}

	private static List<string> BuildSupplyIds()
	{
		var ids = new List<string> { "fertilizer" };
		foreach (CropDefinition crop in CropCatalog.Crops)
		{
			ids.Add(crop.SeedId);
		}

		return ids;
	}
}
