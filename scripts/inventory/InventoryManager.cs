using Godot;
using System.Collections.Generic;

namespace SmartGreenhouse.Inventory;

public partial class InventoryManager : Node
{
    private readonly Dictionary<string, int> _items = new()
    {
        { "lettuce_seeds", 5 },
        { "fertilizer", 5 },
        { "lettuce_harvested", 0 }
    };

    public int GetQuantity(string itemId)
    {
        return _items.TryGetValue(itemId, out int value) ? value : 0;
    }

    public void AddItem(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (!_items.ContainsKey(itemId))
        {
            _items[itemId] = 0;
        }

        _items[itemId] += amount;
        GD.Print($"[Inventory] +{amount} {itemId} (total: {_items[itemId]})");
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (GetQuantity(itemId) < amount)
        {
            return false;
        }

        _items[itemId] -= amount;
        GD.Print($"[Inventory] -{amount} {itemId} (total: {_items[itemId]})");
        return true;
    }

    public bool HasItem(string itemId, int amount)
    {
        return GetQuantity(itemId) >= amount;
    }
}
