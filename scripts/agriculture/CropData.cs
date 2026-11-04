using Godot;

namespace SmartGreenhouse.Agriculture;

public enum GrowthStage
{
	Seed,
	Seedling,
	Growing,
	Mature
}

public partial class CropData : Resource
{
	[Export] public string PlantId { get; set; } = string.Empty;
	[Export] public string CropType { get; set; } = "lettuce";
	[Export] public GrowthStage Stage { get; set; } = GrowthStage.Seed;
	[Export] public float GrowthProgress { get; set; } = 0.0f;
	[Export] public float Health { get; set; } = 100.0f;
	[Export] public float Water { get; set; } = 50.0f;
	[Export] public float Nutrients { get; set; } = 50.0f;
	public bool IsMature => Stage == GrowthStage.Mature;

	public const float MaxWater = 100.0f;
	public const float MaxNutrients = 100.0f;
	public const float MaxHealth = 100.0f;

	public void ClampValues()
	{
		Water = Mathf.Clamp(Water, 0.0f, MaxWater);
		Nutrients = Mathf.Clamp(Nutrients, 0.0f, MaxNutrients);
		Health = Mathf.Clamp(Health, 0.0f, MaxHealth);
	}
}
