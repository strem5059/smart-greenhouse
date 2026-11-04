using Godot;

namespace SmartGreenhouse.Agriculture;

public partial class PlantController : Node3D
{
    [Export] public CropData Data { get; set; } = null!;

    private Node3D _seedVisual = null!;
    private Node3D _seedlingVisual = null!;
    private Node3D _growingVisual = null!;
    private Node3D _matureVisual = null!;

    public override void _Ready()
    {
        _seedVisual = GetNodeOrNull<Node3D>("SeedVisual");
        _seedlingVisual = GetNodeOrNull<Node3D>("SeedlingVisual");
        _growingVisual = GetNodeOrNull<Node3D>("GrowingVisual");
        _matureVisual = GetNodeOrNull<Node3D>("MatureVisual");
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        SetVisual(_seedVisual, Data.Stage == GrowthStage.Seed);
        SetVisual(_seedlingVisual, Data.Stage == GrowthStage.Seedling);
        SetVisual(_growingVisual, Data.Stage == GrowthStage.Growing);
        SetVisual(_matureVisual, Data.Stage == GrowthStage.Mature);
    }

    private static void SetVisual(Node3D? visual, bool active)
    {
        if (visual != null)
        {
            visual.Visible = active;
        }
    }

    public void AddWater(float amount)
    {
        Data.Water += amount;
        Data.ClampValues();
    }

    public void AddNutrients(float amount)
    {
        Data.Nutrients += amount;
        Data.ClampValues();
    }
}
