using System.Collections.Generic;
using Godot;

namespace SmartGreenhouse.Inventory;

public enum ItemCategory { Seeds, Crops, Fertilizers }

public sealed record ItemDefinition(string Id, string Name, string Description, ItemCategory Category, Color IconColor);

public static class ItemCatalog
{
    public static readonly IReadOnlyList<ItemDefinition> Items = new[]
    {
        new ItemDefinition("lettuce_seeds", "Semillas de lechuga", "Permiten plantar una lechuga en una maceta libre.", ItemCategory.Seeds, new Color(0.55f, 0.85f, 0.4f)),
        new ItemDefinition("lettuce_harvested", "Lechuga", "Cosecha lista para vender.", ItemCategory.Crops, new Color(0.7f, 1.0f, 0.6f)),
        new ItemDefinition("fertilizer", "Fertilizante", "Aumenta los nutrientes de una planta.", ItemCategory.Fertilizers, new Color(0.95f, 0.72f, 0.3f)),
    };

    public static ItemDefinition? Get(string id)
    {
        foreach (ItemDefinition item in Items)
        {
            if (item.Id == id)
            {
                return item;
            }
        }

        return null;
    }

    public static string NameOf(string id) => Get(id)?.Name ?? id;

    public static string CategoryName(ItemCategory category) => category switch
    {
        ItemCategory.Seeds => "Semillas",
        ItemCategory.Crops => "Cultivos cosechados",
        _ => "Fertilizantes"
    };
}
