using Godot;
using SmartGreenhouse.Interfaces;

namespace SmartGreenhouse.Inventory;

public partial class Quickbar : Node
{
    public const int SlotCount = 3;

    private readonly string?[] _slots = new string?[SlotCount];
    private InventoryManager? _inventory;
    private int _selected;

    public event System.Action? Changed;

    public int SelectedIndex => _selected;
    public string? EquippedItemId => _slots[_selected];

    public override void _Ready()
    {
        AddToGroup("quickbar");
        _inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
    }

    public string? GetSlot(int index) => _slots[index];

    public bool Assign(int index, string itemId)
    {
        if (ItemCatalog.Get(itemId) is not { Category: ItemCategory.Seeds })
        {
            Notifications.Publish("Solo se pueden asignar semillas", NoticeKind.Warning);
            return false;
        }

        if (_inventory == null || _inventory.GetQuantity(itemId) <= 0)
        {
            Notifications.Publish($"Sin existencias de {ItemCatalog.NameOf(itemId)}", NoticeKind.Warning);
            return false;
        }

        _slots[index] = itemId;
        Changed?.Invoke();
        return true;
    }

    public void Swap(int first, int second)
    {
        (_slots[first], _slots[second]) = (_slots[second], _slots[first]);
        Changed?.Invoke();
    }

    public void Select(int index)
    {
        if (index == _selected)
        {
            return;
        }

        _selected = index;
        Changed?.Invoke();
    }
}
