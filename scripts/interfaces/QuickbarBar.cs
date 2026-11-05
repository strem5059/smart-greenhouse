using Godot;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Interfaces;

public partial class QuickbarBar : VBoxContainer
{
    private static readonly Color MutedColor = new(0.62f, 0.72f, 0.64f);

    private readonly QuickbarSlot[] _slots = new QuickbarSlot[Quickbar.SlotCount];
    private Quickbar? _quickbar;
    private InventoryManager? _inventory;
    private Label _equippedLabel = null!;

    public Quickbar? State => _quickbar;
    public InventoryManager? Inventory => _inventory;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _quickbar = GetTree().GetFirstNodeInGroup("quickbar") as Quickbar;
        _inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");

        _equippedLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        _equippedLabel.AddThemeFontSizeOverride("font_size", 14);
        _equippedLabel.AddThemeColorOverride("font_color", MutedColor);
        AddChild(_equippedLabel);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 10);
        AddChild(row);
        for (int i = 0; i < Quickbar.SlotCount; i++)
        {
            var slot = new QuickbarSlot();
            slot.Init(this, i);
            row.AddChild(slot);
            _slots[i] = slot;
        }

        if (_quickbar != null)
        {
            _quickbar.Changed += Refresh;
        }

        if (_inventory != null)
        {
            _inventory.Changed += Refresh;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        if (_quickbar != null)
        {
            _quickbar.Changed -= Refresh;
        }

        if (_inventory != null)
        {
            _inventory.Changed -= Refresh;
        }
    }

    public void Refresh()
    {
        if (_quickbar == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            string? id = _quickbar.GetSlot(i);
            int count = id == null || _inventory == null ? 0 : _inventory.GetQuantity(id);
            _slots[i].Display(id, _quickbar.SelectedIndex == i, count);
        }

        string? equipped = _quickbar.EquippedItemId;
        _equippedLabel.Text = equipped == null
            ? "Sin semilla equipada"
            : $"{ItemCatalog.NameOf(equipped)}  ·  {_inventory?.GetQuantity(equipped) ?? 0}";
    }
}

public partial class QuickbarSlot : Control
{
    private static readonly Color Accent = new(0.45f, 0.85f, 0.55f);
    private static readonly Color MutedColor = new(0.62f, 0.72f, 0.64f);

    private QuickbarBar _bar = null!;
    private int _index;
    private string? _itemId;
    private bool _selected;
    private int _count;
    private Label _numberLabel = null!;
    private Label _countLabel = null!;

    public void Init(QuickbarBar bar, int index)
    {
        _bar = bar;
        _index = index;
        CustomMinimumSize = new Vector2(76, 72);
        TooltipText = $"Espacio {index + 1}";

        _numberLabel = new Label { Text = (index + 1).ToString(), MouseFilter = MouseFilterEnum.Ignore };
        _numberLabel.AddThemeFontSizeOverride("font_size", 14);
        _numberLabel.AddThemeColorOverride("font_color", MutedColor);
        _countLabel = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        _countLabel.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_numberLabel);
        AddChild(_countLabel);

        _numberLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _numberLabel.OffsetLeft = 6;
        _numberLabel.OffsetTop = 2;
        _countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _countLabel.OffsetRight = -6;
        _countLabel.OffsetBottom = -4;
    }

    public void Display(string? itemId, bool selected, int count)
    {
        _itemId = itemId;
        _selected = selected;
        _count = count;
        _countLabel.Text = itemId == null ? string.Empty : count.ToString();
        Modulate = itemId != null && count <= 0 ? new Color(1, 1, 1, 0.4f) : Colors.White;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, _selected ? new Color(Accent.R, Accent.G, Accent.B, 0.22f) : new Color(0, 0, 0, 0.45f));
        DrawRect(rect, _selected ? Accent : new Color(1, 1, 1, 0.14f), false, _selected ? 2.0f : 1.0f);

        ItemDefinition? item = _itemId == null ? null : ItemCatalog.Get(_itemId);
        if (item != null)
        {
            ItemIconDrawer.Draw(this, item, Size);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            _bar.State?.Select(_index);
            AcceptEvent();
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (_itemId == null)
        {
            return default;
        }

        var preview = new QuickbarSlot();
        preview.Init(_bar, _index);
        preview.Display(_itemId, false, _count);
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["from_slot"] = _index };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
        {
            return false;
        }

        Godot.Collections.Dictionary dict = data.AsGodotDictionary();
        return dict.ContainsKey("item_id") || dict.ContainsKey("from_slot");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        Godot.Collections.Dictionary dict = data.AsGodotDictionary();
        if (dict.ContainsKey("from_slot"))
        {
            _bar.State?.Swap(dict["from_slot"].AsInt32(), _index);
        }
        else if (dict.ContainsKey("item_id"))
        {
            _bar.State?.Assign(_index, dict["item_id"].AsString());
        }
    }
}
