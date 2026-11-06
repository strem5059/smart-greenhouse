using Godot;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Agriculture;

public partial class GrowingStation : StaticBody3D, IInteractable
{
	[Export] public PackedScene? PlantScene;
	[Export] public PackedScene? PotWithSoilScene;
	[Export] public float WaterAmount = 30.0f;
	[Export] public float NutrientAmount = 30.0f;

	private PlantController? _plant;
	private InventoryManager? _inventory;
	private GrowthSimulator? _growthSimulator;
	private Quickbar? _seedBar;

	public bool IsOccupied => _plant != null;

	public bool NeedsWaterNow => _plant != null && PlantedCrop != null && PlantedCrop.NeedsWater && _plant.Data.Water < 20.0f;

	public bool NeedsFertilizerNow => _plant != null && PlantedCrop != null && PlantedCrop.NeedsFertilizer && _plant.Data.Nutrients < 20.0f;

	public void ReceiveWater() => _plant?.AddWater(WaterAmount);

	public void ReceiveNutrients() => _plant?.AddNutrients(NutrientAmount);

	public CropData? PlantData => _plant?.Data;

	public bool RestorePlant(string cropId, GrowthStage stage, float growth, float water, float nutrients, float health)
	{
		CropDefinition? crop = CropCatalog.Get(cropId);
		if (crop == null || _plant != null || !PlantCrop(crop) || _plant == null)
		{
			return false;
		}

		_plant.Data.Stage = stage;
		_plant.Data.GrowthProgress = growth;
		_plant.Data.Water = water;
		_plant.Data.Nutrients = nutrients;
		_plant.Data.Health = health;
		_plant.UpdateVisuals();
		return true;
	}
	public CropData? Crop => _plant?.Data;
	public CropDefinition? PlantedCrop => _plant?.Definition;

	private Quickbar? SeedBar => _seedBar ??= GetTree().GetFirstNodeInGroup("quickbar") as Quickbar;

	public override void _Ready()
	{
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		_growthSimulator = GetNodeOrNull<GrowthSimulator>("/root/GrowthSimulator");
		AddToGroup("growing_station");

		if (PotWithSoilScene != null)
		{
			var pot = PotWithSoilScene.Instantiate<Node3D>();
			AddChild(pot);
		}
	}

	public string GetInteractionPrompt()
	{
		if (_plant == null)
		{
			CropDefinition? crop = CropCatalog.FromSeed(SeedBar?.EquippedItemId);
			if (crop == null)
			{
				return "[E] Sin semilla equipada";
			}

			return _inventory != null && _inventory.HasItem(crop.SeedId, 1)
				? $"[E] Plantar {crop.Name.ToLowerInvariant()}"
				: "[E] Sin semillas";
		}

		if (_plant.Data.IsMature)
		{
			return $"[E] Cosechar {CropName().ToLowerInvariant()}";
		}

		if (_plant.Data.Water < 20.0f)
		{
			return "[E] Regar";
		}

		if (_plant.Data.Nutrients < 20.0f)
		{
			return "[E] Fertilizar";
		}

		return "[E] Estado: consultar planta";
	}

	public void Interact()
	{
		if (_plant == null)
		{
			TryPlant();
			return;
		}

		if (_plant.Data.IsMature)
		{
			Harvest();
			return;
		}

		if (_plant.Data.Water < 20.0f)
		{
			WaterPlant();
			return;
		}

		if (_plant.Data.Nutrients < 20.0f)
		{
			FertilizePlant();
			return;
		}

		GD.Print($"[Estación] Estado: {_plant.Data.Stage}, Agua: {_plant.Data.Water:F0}, Nutrientes: {_plant.Data.Nutrients:F0}, Progreso: {_plant.Data.GrowthProgress:F0}");
	}

	private string CropName()
	{
		return _plant?.Definition?.Name ?? "cultivo";
	}

	private void TryPlant()
	{
		if (_inventory == null || PlantScene == null)
		{
			return;
		}

		string? seedId = SeedBar?.EquippedItemId;
		if (seedId == null)
		{
			Notifications.Publish("Selecciona una semilla en la barra rápida", NoticeKind.Warning);
			return;
		}

		CropDefinition? crop = CropCatalog.FromSeed(seedId);
		if (crop == null)
		{
			Notifications.Publish("Semilla no válida", NoticeKind.Warning);
			return;
		}

		if (!_inventory.RemoveItem(crop.SeedId, 1))
		{
			Notifications.Publish($"Semillas insuficientes: {crop.Name}", NoticeKind.Warning);
			return;
		}

		if (!PlantCrop(crop))
		{
			_inventory.AddItem(crop.SeedId, 1);
			return;
		}
		GD.Print($"[Estación] {crop.Name} plantada.");
		Notifications.Publish($"{crop.Name} plantada", NoticeKind.Info);
	}

	public bool PlantCrop(CropDefinition crop)
	{
		if (_plant != null)
		{
			return false;
		}

		PackedScene? scene = CropCatalog.SceneFor(crop.Id) ?? PlantScene;
		if (scene == null)
		{
			return false;
		}

		var instance = scene.Instantiate<PlantController>();
		instance.Data = new CropData(crop.Id);
		AddChild(instance);
		instance.Position = new Vector3(0, 0.1f, 0);
		_plant = instance;
		CropVisualBuilder.Build(instance, crop.Id);
		_growthSimulator?.RegisterPlant(_plant);
		return true;
	}

	private void WaterPlant()
	{
		_plant?.AddWater(WaterAmount);
		GD.Print("[Estación] Planta regada.");
	}

	private void FertilizePlant()
	{
		if (_inventory == null || !_inventory.RemoveItem("fertilizer", 1))
		{
			GD.Print("[Estación] No tienes fertilizante.");
			Notifications.Publish("Fertilizante insuficiente", NoticeKind.Warning);
			return;
		}

		_plant?.AddNutrients(NutrientAmount);
		GD.Print("[Estación] Fertilizante aplicado.");
	}

	private void Harvest()
	{
		if (_plant == null)
		{
			return;
		}

		CropDefinition? crop = _plant.Definition;
		_growthSimulator?.UnregisterPlant(_plant);
		_plant.QueueFree();
		_plant = null;

		if (crop == null)
		{
			return;
		}

		_inventory?.AddItem(crop.ProductId, crop.Yield);
		_inventory?.AddItem(crop.SeedId, crop.SeedsReturned);
		GD.Print($"[Estación] {crop.Name} cosechada.");
		Notifications.Publish($"+{crop.Yield} {crop.Name}", NoticeKind.Success);
		Notifications.Publish($"+{crop.SeedsReturned} semilla de {crop.Name.ToLowerInvariant()}", NoticeKind.Success);
	}
}
