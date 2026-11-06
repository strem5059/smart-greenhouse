using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Economy;
using SmartGreenhouse.Expansion;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Menu;
using SmartGreenhouse.Robots;

namespace SmartGreenhouse.Save;

public sealed class SaveData
{
	public int Version { get; set; } = SaveManager.CurrentVersion;
	public string Name { get; set; } = string.Empty;
	public string SavedAt { get; set; } = string.Empty;
	public int Balance { get; set; }
	public List<HistoryEntry> History { get; set; } = new();
	public Dictionary<string, int> Inventory { get; set; } = new();
	public List<ModuleEntry> Modules { get; set; } = new();
	public List<StationEntry> Stations { get; set; } = new();
	public List<ChestEntry> Chests { get; set; } = new();
	public Dictionary<string, int> Warehouse { get; set; } = new();
	public List<AgroEntry> AgroRobots { get; set; } = new();
	public List<CollectorEntry> Collectors { get; set; } = new();
	public AdministratorEntry? Administrator { get; set; }
}

public sealed class HistoryEntry
{
	public string Type { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public int Amount { get; set; }
	public int BalanceAfter { get; set; }
}

public sealed class ModuleEntry
{
	public int Id { get; set; }
	public int SourceId { get; set; }
	public int Direction { get; set; }
	public int CellX { get; set; }
	public int CellY { get; set; }
}

public sealed class StationEntry
{
	public string Name { get; set; } = string.Empty;
	public string CropId { get; set; } = string.Empty;
	public int Stage { get; set; }
	public float Growth { get; set; }
	public float Water { get; set; }
	public float Nutrients { get; set; }
	public float Health { get; set; }
}

public sealed class ChestEntry
{
	public string Owner { get; set; } = "Base";
	public Dictionary<string, int> Items { get; set; } = new();
}

public sealed class AgroEntry
{
	public string Owner { get; set; } = "Base";
	public Vector3Entry Position { get; set; } = new();
	public float Speed { get; set; }
	public int Capacity { get; set; }
	public Dictionary<string, int> Cargo { get; set; } = new();
	public Dictionary<string, int> Seeds { get; set; } = new();
	public int Fertilizer { get; set; }
	public bool Harvest { get; set; }
	public string? Program { get; set; }
	public bool Running { get; set; }
	public bool Paused { get; set; }
}

public sealed class CollectorEntry
{
	public int Id { get; set; }
	public Vector3Entry Position { get; set; } = new();
	public float Speed { get; set; }
	public int Capacity { get; set; }
	public Dictionary<string, int> Cargo { get; set; } = new();
	public string? Program { get; set; }
	public bool Running { get; set; }
	public bool Paused { get; set; }
}

public sealed class AdministratorEntry
{
	public int Multiplier { get; set; }
	public int Reserve { get; set; }
	public float ReviewSeconds { get; set; }
	public bool AutoSell { get; set; }
	public int MinimumSaleUnits { get; set; }
	public Dictionary<string, int> Minimums { get; set; } = new();
	public Dictionary<string, int> SaleReserves { get; set; } = new();
	public Dictionary<string, int> SalePriorities { get; set; } = new();
	public int Bought { get; set; }
	public int Sold { get; set; }
	public string? Program { get; set; }
	public bool Running { get; set; }
	public bool Paused { get; set; }
}

public sealed class Vector3Entry
{
	public float X { get; set; }
	public float Y { get; set; }
	public float Z { get; set; }
}

public sealed record SlotInfo(string Path, string Name, string SavedAt, int Balance, bool Damaged);

public partial class SaveManager : Node
{
	public const int CurrentVersion = 1;
	public const string GreenhousePath = "res://scenes/world/greenhouse.tscn";
	public const string MenuPath = "res://scenes/menu/main_menu.tscn";
	public const double AutosaveSeconds = 300.0;

	public static string SaveDirectory = "user://saves";
	public static string SettingsPath = "user://settings.json";
	public static bool OpenLoadOnMenu;

	private string? _slotPath;
	private string _slotName = string.Empty;
	private SaveData? _pending;
	private int _framesSinceLoad;
	private double _autosaveTimer;
	private bool _autosaveEnabled = true;

	public bool AutosaveEnabled
	{
		get => _autosaveEnabled;
		set
		{
			_autosaveEnabled = value;
			WriteSettings();
		}
	}

	public bool HasSlotInGame => _slotPath != null;

	public bool IsInGame => GetTree().CurrentScene?.SceneFilePath == GreenhousePath;

	public override void _Ready()
	{
		AddToGroup("save_manager");
		ProcessMode = ProcessModeEnum.Always;
		ReadSettings();
		AddChild(new PauseMenu());
	}

	public override void _Process(double delta)
	{
		if (_pending != null)
		{
			_framesSinceLoad++;
			Node? scene = GetTree().CurrentScene;
			if (_framesSinceLoad > 3 && scene != null && scene.SceneFilePath == GreenhousePath && GetTree().GetFirstNodeInGroup("collector_fleet") != null)
			{
				SaveData data = _pending;
				_pending = null;
				ApplyTo(scene, data);
			}

			return;
		}

		if (IsInGame && !GetTree().Paused && _autosaveEnabled)
		{
			_autosaveTimer += delta;
			if (_autosaveTimer >= AutosaveSeconds)
			{
				_autosaveTimer = 0;
				SaveCurrentGame(out _);
			}
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			if (IsInGame)
			{
				SaveCurrentGame(out _);
			}

			GetTree().Quit();
		}
	}

	public void StartNewGame()
	{
		EconomyManager economy = GetNode<EconomyManager>("/root/EconomyManager");
		InventoryManager inventory = GetNode<InventoryManager>("/root/InventoryManager");
		economy.ResetToStart();
		inventory.ResetToDefaults();
		GetNode<GrowthSimulator>("/root/GrowthSimulator").ClearPlants();
		_slotPath = null;
		_slotName = $"Granja {ListSlots().Count + 1}";
		_autosaveTimer = 0;
		_pending = null;
		GetTree().ChangeSceneToFile(GreenhousePath);
	}

	public void LoadSlot(string path)
	{
		SaveData? data = ReadSlot(path, out string? error);
		if (data == null)
		{
			Notifications.Publish(error ?? "No se pudo leer la partida", NoticeKind.Warning);
			return;
		}

		GetNode<GrowthSimulator>("/root/GrowthSimulator").ClearPlants();
		_slotPath = path;
		_slotName = data.Name;
		_pending = data;
		_framesSinceLoad = 0;
		_autosaveTimer = 0;
		GetTree().ChangeSceneToFile(GreenhousePath);
	}

	public void ReturnToMenu()
	{
		GetNode<GrowthSimulator>("/root/GrowthSimulator").ClearPlants();
		_slotPath = null;
		_pending = null;
		GetTree().ChangeSceneToFile(MenuPath);
	}

	public bool SaveCurrentGame(out string message)
	{
		Node? scene = GetTree().CurrentScene;
		if (scene == null || scene.SceneFilePath != GreenhousePath || _pending != null)
		{
			message = "No hay una partida activa que guardar.";
			return false;
		}

		try
		{
			SaveData data = Snapshot(scene);
			_slotPath ??= $"{SaveDirectory}/{Guid.NewGuid():N}.json";
			Write(_slotPath, data);
			message = $"Partida guardada: {data.Name}";
			return true;
		}
		catch (Exception error)
		{
			message = "Error al guardar: " + error.Message;
			return false;
		}
	}

	public bool WriteSlot(Node greenhouse, string path)
	{
		try
		{
			Write(path, Snapshot(greenhouse));
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool ReadSlotInto(Node greenhouse, string path)
	{
		SaveData? data = ReadSlot(path, out _);
		if (data == null)
		{
			return false;
		}

		ApplyTo(greenhouse, data);
		return true;
	}

	public bool DeleteSlot(string path)
	{
		using DirAccess? dir = DirAccess.Open(SaveDirectory);
		return dir != null && dir.Remove(path.GetFile()) == Error.Ok;
	}

	public List<SlotInfo> ListSlots()
	{
		var slots = new List<SlotInfo>();
		using DirAccess? dir = DirAccess.Open(SaveDirectory);
		if (dir == null)
		{
			return slots;
		}

		foreach (string file in dir.GetFiles())
		{
			if (!file.EndsWith(".json"))
			{
				continue;
			}

			string path = $"{SaveDirectory}/{file}";
			SaveData? data = ReadSlot(path, out _);
			slots.Add(data == null
				? new SlotInfo(path, file, string.Empty, 0, true)
				: new SlotInfo(path, data.Name, data.SavedAt, data.Balance, false));
		}

		slots.Sort((a, b) => string.CompareOrdinal(b.SavedAt, a.SavedAt));
		return slots;
	}

	public SlotInfo? LatestSlot() => ListSlots().FirstOrDefault(slot => !slot.Damaged);

	public SaveData Snapshot(Node greenhouse)
	{
		EconomyManager economy = GetNode<EconomyManager>("/root/EconomyManager");
		InventoryManager inventory = GetNode<InventoryManager>("/root/InventoryManager");
		var data = new SaveData
		{
			Name = _slotName,
			SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
			Balance = economy.Balance,
			Inventory = new Dictionary<string, int>(inventory.Snapshot)
		};

		foreach (TransactionData entry in economy.History)
		{
			data.History.Add(new HistoryEntry { Type = entry.Type, Description = entry.Description, Quantity = entry.Quantity, Amount = entry.Amount, BalanceAfter = entry.BalanceAfter });
		}

		if (greenhouse.GetTree().GetFirstNodeInGroup("expansion_manager") is ExpansionManager expansion)
		{
			foreach (GreenhouseModule module in expansion.Modules.Where(module => module.Id > 0))
			{
				data.Modules.Add(new ModuleEntry { Id = module.Id, SourceId = module.SourceId, Direction = (int)module.Direction, CellX = module.Cell.X, CellY = module.Cell.Y });
			}
		}

		foreach (Node node in GetTree().GetNodesInGroup("growing_station"))
		{
			if (node is GrowingStation station && station.PlantData is CropData plant)
			{
				data.Stations.Add(new StationEntry { Name = station.Name.ToString(), CropId = plant.CropType, Stage = (int)plant.Stage, Growth = plant.GrowthProgress, Water = plant.Water, Nutrients = plant.Nutrients, Health = plant.Health });
			}
		}

		foreach (Node node in GetTree().GetNodesInGroup("chest"))
		{
			if (node is Chest chest)
			{
				data.Chests.Add(new ChestEntry { Owner = OwnerOf(chest), Items = new Dictionary<string, int>(chest.Contents) });
			}
		}

		if (GetTree().GetFirstNodeInGroup("central_warehouse") is CentralWarehouse warehouse)
		{
			data.Warehouse = new Dictionary<string, int>(warehouse.Contents);
		}

		foreach (Node node in GetTree().GetNodesInGroup("robot"))
		{
			if (node is RobotController robot)
			{
				data.AgroRobots.Add(new AgroEntry
				{
					Owner = OwnerOf(robot),
					Position = Vec(robot.Position),
					Speed = robot.Speed,
					Capacity = robot.CapacityLimit,
					Cargo = new Dictionary<string, int>(robot.CargoItems),
					Seeds = new Dictionary<string, int>(robot.SeedItems),
					Fertilizer = robot.FertilizerStock,
					Harvest = robot.HarvestEnabled,
					Program = robot.SavedProgram,
					Running = robot.IsRunning,
					Paused = robot.IsPaused
				});
			}
		}

		foreach (Node node in GetTree().GetNodesInGroup("collector"))
		{
			if (node is CollectorRobot collector)
			{
				data.Collectors.Add(new CollectorEntry
				{
					Id = collector.Id,
					Position = Vec(collector.Position),
					Speed = collector.Speed,
					Capacity = collector.Capacity,
					Cargo = new Dictionary<string, int>(collector.Cargo),
					Program = collector.SavedProgram,
					Running = collector.IsActive,
					Paused = collector.IsPaused
				});
			}
		}

		if (GetTree().GetFirstNodeInGroup("office_administrator") is AdministrationRobot admin)
		{
			var entry = new AdministratorEntry
			{
				Multiplier = admin.Multiplier,
				Reserve = admin.MoneyReserve,
				ReviewSeconds = admin.ReviewSeconds,
				AutoSell = admin.AutoSell,
				MinimumSaleUnits = admin.MinimumSaleUnits,
				Bought = admin.TotalBought,
				Sold = admin.TotalSold,
				Program = admin.SavedProgram,
				Running = admin.IsActive,
				Paused = admin.IsPaused
			};
			foreach (string id in AdministrationRobot.SupplyIds)
			{
				entry.Minimums[id] = admin.MinimumFor(id);
			}

			foreach (CropDefinition crop in CropCatalog.Crops)
			{
				entry.SaleReserves[crop.ProductId] = admin.SaleReserveFor(crop.ProductId);
				entry.SalePriorities[crop.ProductId] = admin.SalePriorityFor(crop.ProductId);
			}

			data.Administrator = entry;
		}

		return data;
	}

	public void ApplyTo(Node greenhouse, SaveData data)
	{
		EconomyManager economy = GetNode<EconomyManager>("/root/EconomyManager");
		InventoryManager inventory = GetNode<InventoryManager>("/root/InventoryManager");
		economy.RestoreState(data.Balance, data.History.Select(entry => new TransactionData(entry.Type, entry.Description, entry.Quantity, entry.Amount, entry.BalanceAfter)));
		inventory.RestoreState(data.Inventory);

		if (greenhouse.GetTree().GetFirstNodeInGroup("expansion_manager") is ExpansionManager expansion)
		{
			foreach (ModuleEntry module in data.Modules.OrderBy(module => module.Id))
			{
				expansion.RestoreModule(module.Id, new Vector2I(module.CellX, module.CellY), module.SourceId, (ExpansionDirection)module.Direction);
			}
		}

		foreach (StationEntry entry in data.Stations)
		{
			if (greenhouse.FindChild(entry.Name, true, false) is GrowingStation station)
			{
				station.RestorePlant(entry.CropId, (GrowthStage)entry.Stage, entry.Growth, entry.Water, entry.Nutrients, entry.Health);
			}
		}

		foreach (ChestEntry entry in data.Chests)
		{
			if (RootFor(greenhouse, entry.Owner)?.GetChildren().OfType<Chest>().FirstOrDefault() is Chest chest)
			{
				foreach (var (itemId, count) in entry.Items)
				{
					chest.Store(itemId, count);
				}
			}
		}

		if (greenhouse.GetTree().GetFirstNodeInGroup("central_warehouse") is CentralWarehouse warehouse)
		{
			foreach (var (itemId, count) in data.Warehouse)
			{
				warehouse.Deposit(itemId, count);
			}
		}

		foreach (AgroEntry entry in data.AgroRobots)
		{
			if (RootFor(greenhouse, entry.Owner)?.GetChildren().OfType<RobotController>().FirstOrDefault() is RobotController robot)
			{
				robot.ApplySavedState(ToVector(entry.Position), entry.Speed, entry.Capacity, entry.Cargo, entry.Seeds, entry.Fertilizer, entry.Harvest, entry.Program, entry.Running, entry.Paused);
			}
		}

		if (greenhouse.GetTree().GetFirstNodeInGroup("collector_fleet") is CollectorFleet fleet)
		{
			foreach (CollectorEntry entry in data.Collectors)
			{
				CollectorRobot robot = fleet.CreateCollector(entry.Id);
				robot.ApplySavedState(ToVector(entry.Position), entry.Speed, entry.Capacity, entry.Cargo, entry.Program, entry.Running, entry.Paused);
			}

			if (data.Administrator is AdministratorEntry admin)
			{
				AdministrationRobot administrator = fleet.CreateAdministrator();
				administrator.SetMultiplier(admin.Multiplier);
				administrator.SetReserve(admin.Reserve);
				administrator.SetReviewSeconds(admin.ReviewSeconds);
				administrator.SetAutoSell(admin.AutoSell);
				administrator.SetMinimumSaleUnits(admin.MinimumSaleUnits);
				foreach (var (itemId, value) in admin.Minimums)
				{
					administrator.SetMinimum(itemId, value);
				}

				foreach (var (productId, value) in admin.SaleReserves)
				{
					administrator.SetSaleReserve(productId, value);
				}

				foreach (var (productId, value) in admin.SalePriorities)
				{
					administrator.SetSalePriority(productId, value);
				}

				administrator.ApplySavedState(admin.Bought, admin.Sold, admin.Program, admin.Running, admin.Paused);
			}
		}

		_slotName = data.Name;
		Notifications.Publish($"Partida cargada: {data.Name}", NoticeKind.Success);
	}

	private static string OwnerOf(Node node)
	{
		return node.GetParent() is Node3D parent && parent.Name.ToString().StartsWith("Module") ? parent.Name.ToString() : "Base";
	}

	private static Node? RootFor(Node greenhouse, string owner)
	{
		return owner == "Base" ? greenhouse : greenhouse.GetNodeOrNull(owner);
	}

	private static Vector3Entry Vec(Vector3 value) => new() { X = value.X, Y = value.Y, Z = value.Z };

	private static Vector3 ToVector(Vector3Entry value) => new(value.X, value.Y, value.Z);

	private static void Write(string path, SaveData data)
	{
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SaveDirectory));
		string temp = path + ".tmp";
		string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
		using (FileAccess file = FileAccess.Open(temp, FileAccess.ModeFlags.Write))
		{
			file.StoreString(json);
		}

		using DirAccess dir = DirAccess.Open(SaveDirectory);
		dir.Remove(path.GetFile());
		dir.Rename(temp.GetFile(), path.GetFile());
	}

	private static SaveData? ReadSlot(string path, out string? error)
	{
		error = null;
		try
		{
			if (!FileAccess.FileExists(path))
			{
				error = "La partida no existe.";
				return null;
			}

			using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			SaveData? data = JsonSerializer.Deserialize<SaveData>(file.GetAsText());
			if (data == null)
			{
				error = "La partida está vacía o dañada.";
				return null;
			}

			if (data.Version > CurrentVersion)
			{
				error = "La partida fue creada con una versión más reciente del juego.";
				return null;
			}

			return data;
		}
		catch (Exception)
		{
			error = "La partida está dañada y no se puede cargar.";
			return null;
		}
	}

	private void ReadSettings()
	{
		try
		{
			if (!FileAccess.FileExists(SettingsPath))
			{
				return;
			}

			using FileAccess file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
			using JsonDocument document = JsonDocument.Parse(file.GetAsText());
			if (document.RootElement.TryGetProperty("autosave", out JsonElement autosave))
			{
				_autosaveEnabled = autosave.GetBoolean();
			}
		}
		catch (Exception)
		{
			_autosaveEnabled = true;
		}
	}

	private void WriteSettings()
	{
		using FileAccess file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
		file.StoreString(JsonSerializer.Serialize(new Dictionary<string, bool> { ["autosave"] = _autosaveEnabled }));
	}
}
