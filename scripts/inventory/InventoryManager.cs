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

    public event System.Action? Changed;

    public IReadOnlyDictionary<string, int> Snapshot => _items;

    public void RestoreState(IEnumerable<KeyValuePair<string, int>> items)
    {
        _items.Clear();
        foreach (var (itemId, amount) in items)
        {
            if (amount > 0)
            {
                _items[itemId] = amount;
            }
        }

        Changed?.Invoke();
    }

    public void ResetToDefaults()
    {
        RestoreState(new Dictionary<string, int>
        {
            { "lettuce_seeds", 5 },
            { "fertilizer", 5 },
            { "lettuce_harvested", 0 }
        });
    }

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
        Changed?.Invoke();
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
        Changed?.Invoke();
        return true;
    }

    public bool HasItem(string itemId, int amount)
    {
        return GetQuantity(itemId) >= amount;
    }
}
