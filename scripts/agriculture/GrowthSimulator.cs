using Godot;
using System.Collections.Generic;

namespace SmartGreenhouse.Agriculture;

public partial class GrowthSimulator : Node
{
	public const float ReferenceGrowthRate = 15.0f;
	public const float ReferenceWaterConsumption = 1.0f;
	public const float ReferenceNutrientConsumption = 0.5f;

	[Export] public float SimulationIntervalSeconds = 1.0f;
	[Export] public float GrowthRate = 15.0f;
	[Export] public float WaterConsumption = 1.0f;
	[Export] public float NutrientConsumption = 0.5f;
	[Export] public float HealthDecayWhenStarving = 3.0f;
	[Export] public float HealthRecoveryWhenHealthy = 1.0f;

	private double _elapsed = 0.0;
	private readonly List<PlantController> _plants = new();

	public void ClearPlants()
	{
		_plants.Clear();
	}

	public void RegisterPlant(PlantController plant)
	{
		if (!_plants.Contains(plant))
		{
			_plants.Add(plant);
		}
	}

	public void UnregisterPlant(PlantController plant)
	{
		_plants.Remove(plant);
	}

	public override void _Process(double delta)
	{
		_elapsed += delta;
		if (_elapsed < SimulationIntervalSeconds)
		{
			return;
		}

		_elapsed = 0.0;
		TickAllPlants();
	}

	private void TickAllPlants()
	{
		foreach (var plant in _plants)
		{
			if (plant == null || plant.Data == null)
			{
				continue;
			}

			TickPlant(plant.Data, plant.Definition);
			plant.UpdateVisuals();
		}
	}

	private void TickPlant(CropData data, CropDefinition? definition)
	{
		if (data.IsMature)
		{
			return;
		}

		bool hasWater = data.Water > 0.0f;
		bool hasNutrients = data.Nutrients > 0.0f;

		if (hasWater && hasNutrients)
		{
			data.GrowthProgress += definition?.GrowthRate ?? GrowthRate;
			data.Water -= WaterConsumption;
			data.Nutrients -= definition?.NutrientConsumption ?? NutrientConsumption;
			data.Health += HealthRecoveryWhenHealthy;
		}
		else
		{
			data.Health -= HealthDecayWhenStarving;
		}

		data.ClampValues();

		if (data.GrowthProgress >= 100.0f)
		{
			AdvanceStage(data);
		}
	}

	private static void AdvanceStage(CropData data)
	{
		data.GrowthProgress = 0.0f;
		data.Stage = data.Stage switch
		{
			GrowthStage.Seed => GrowthStage.Seedling,
			GrowthStage.Seedling => GrowthStage.Growing,
			GrowthStage.Growing => GrowthStage.Mature,
			_ => data.Stage
		};
	}
}
