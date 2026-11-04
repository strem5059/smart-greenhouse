using Godot;
using SmartGreenhouse.Agriculture;
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

    public override void _Ready()
    {
        _inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
        _growthSimulator = GetNodeOrNull<GrowthSimulator>("/root/GrowthSimulator");

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
            return _inventory != null && _inventory.HasItem("lettuce_seeds", 1)
                ? "[E] Plantar lechuga"
                : "[E] Sin semillas";
        }

        if (_plant.Data.IsMature)
        {
            return "[E] Cosechar lechuga";
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

    private void TryPlant()
    {
        if (_inventory == null || PlantScene == null)
        {
            return;
        }

        if (!_inventory.RemoveItem("lettuce_seeds", 1))
        {
            GD.Print("[Estación] No tienes semillas de lechuga.");
            return;
        }

        var instance = PlantScene.Instantiate<PlantController>();
        instance.Data = new CropData();
        AddChild(instance);
        instance.Position = new Vector3(0, 0.1f, 0);
        _plant = instance;

        _growthSimulator?.RegisterPlant(_plant);
        GD.Print("[Estación] Lechuga plantada.");
    }

    private void WaterPlant()
    {
        if (_inventory == null || !_inventory.RemoveItem("water", 1))
        {
            _plant?.AddWater(WaterAmount);
            GD.Print("[Estación] Planta regada.");
            return;
        }

        _plant?.AddWater(WaterAmount);
    }

    private void FertilizePlant()
    {
        if (_inventory == null || !_inventory.RemoveItem("fertilizer", 1))
        {
            GD.Print("[Estación] No tienes fertilizante.");
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

        _growthSimulator?.UnregisterPlant(_plant);
        _plant.QueueFree();
        _plant = null;
        _inventory?.AddItem("lettuce_harvested", 1);
        _inventory?.AddItem("lettuce_seeds", 2);
        GD.Print("[Estación] Lechuga cosechada.");
    }
}
