using System.Collections.Generic;
using Godot;

namespace SmartGreenhouse.Agriculture;

public sealed record CropDefinition(
    string Id,
    string Name,
    string Description,
    string SeedId,
    string ProductId,
    int SeedPrice,
    int SalePrice,
    float GrowthRate = GrowthSimulator.ReferenceGrowthRate,
    float NutrientConsumption = GrowthSimulator.ReferenceNutrientConsumption,
    int Yield = 1,
    int SeedsReturned = 1,
    bool NeedsWater = true,
    bool NeedsFertilizer = true);

public static class CropCatalog
{
    public static readonly IReadOnlyList<CropDefinition> Crops = new[]
    {
        new CropDefinition("lettuce", "Lechuga", "Hojas verdes en roseta compacta.", "lettuce_seeds", "lettuce_harvested", 4, 12, NeedsWater: false, NeedsFertilizer: false),
        new CropDefinition("carrot", "Zanahoria", "Raíz naranja alargada con follaje fino.", "carrot_seeds", "carrot_harvested", 5, 15, GrowthRate: 10.0f, NeedsFertilizer: false),
        new CropDefinition("tomato", "Tomate", "Tallo alto con frutos rojos.", "tomato_seeds", "tomato_harvested", 10, 16, GrowthRate: 5.0f, NutrientConsumption: 1.0f, Yield: 2),
        new CropDefinition("cucumber", "Pepino", "Enredadera con pepinos alargados.", "cucumber_seeds", "cucumber_harvested", 8, 24, GrowthRate: 6.25f),
        new CropDefinition("spinach", "Espinaca", "Hojas lanceoladas bajas de color verde intenso.", "spinach_seeds", "spinach_harvested", 3, 6, GrowthRate: 25.0f, NeedsFertilizer: false),
    };

    public static bool IsValid { get; }

    static CropCatalog()
    {
        IsValid = Validate();
    }

    public static CropDefinition? Get(string? cropId)
    {
        if (cropId == null)
        {
            return null;
        }

        foreach (CropDefinition crop in Crops)
        {
            if (crop.Id == cropId)
            {
                return crop;
            }
        }

        return null;
    }

    public static CropDefinition? FromSeed(string? seedId)
    {
        foreach (CropDefinition crop in Crops)
        {
            if (crop.SeedId == seedId)
            {
                return crop;
            }
        }

        return null;
    }

    public static CropDefinition? FromProduct(string? productId)
    {
        foreach (CropDefinition crop in Crops)
        {
            if (crop.ProductId == productId)
            {
                return crop;
            }
        }

        return null;
    }

    public static PackedScene? SceneFor(string cropId) => ResourceLoader.Load<PackedScene>($"res://scenes/crops/{cropId}.tscn");

    private static bool Validate()
    {
        var ids = new HashSet<string>();
        var seeds = new HashSet<string>();
        var products = new HashSet<string>();
        foreach (CropDefinition crop in Crops)
        {
            if (string.IsNullOrEmpty(crop.Id)
                || string.IsNullOrEmpty(crop.SeedId)
                || string.IsNullOrEmpty(crop.ProductId)
                || crop.SeedPrice <= 0
                || crop.SalePrice <= 0
                || crop.GrowthRate <= 0
                || !ids.Add(crop.Id)
                || !seeds.Add(crop.SeedId)
                || !products.Add(crop.ProductId))
            {
                return false;
            }
        }

        return true;
    }
}
