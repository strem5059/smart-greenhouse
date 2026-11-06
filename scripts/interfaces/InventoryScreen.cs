using System;
using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Interfaces;

public partial class InventoryScreen : CanvasLayer
{
	private const float ClosedScale = 0.96f;
	private const float FadeTime = 0.18f;

	private static readonly Color Accent = new(0.45f, 0.85f, 0.55f);
	private static readonly Color MutedColor = new(0.62f, 0.72f, 0.64f);

	private readonly Dictionary<string, InventorySlot> _slots = new();
	private InventoryManager? _inventory;
	private Control _panel = null!;
	private InventorySlot? _selected;
	private Label _detailName = null!;
	private Label _detailCategory = null!;
	private Label _detailCount = null!;
	private Label _detailDescription = null!;
	private Tween? _tween;

	public bool IsOpen { get; private set; }

	public override void _Ready()
	{
		Layer = 15;
		Visible = false;
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		BuildLayout();
		if (_inventory != null)
		{
			_inventory.Changed += Refresh;
		}

		Refresh();
	}

	public override void _ExitTree()
	{
		if (_inventory != null)
		{
			_inventory.Changed -= Refresh;
		}
	}

	public void Open()
	{
		if (IsOpen)
		{
			return;
		}

		IsOpen = true;
		Refresh();
		Visible = true;
		_panel.PivotOffset = _panel.Size / 2.0f;
		_panel.Modulate = new Color(1, 1, 1, 0);
		_panel.Scale = new Vector2(ClosedScale, ClosedScale);
		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetParallel(true);
		_tween.TweenProperty(_panel, "modulate:a", 1.0f, FadeTime);
		_tween.TweenProperty(_panel, "scale", Vector2.One, FadeTime)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
	}

	public void Close()
	{
		if (!IsOpen)
		{
			return;
		}

		IsOpen = false;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetParallel(true);
		_tween.TweenProperty(_panel, "modulate:a", 0.0f, FadeTime);
		_tween.TweenProperty(_panel, "scale", new Vector2(ClosedScale, ClosedScale), FadeTime);
		_tween.Finished += () =>
		{
			if (!IsOpen)
			{
				Visible = false;
			}
		};
	}

	private void BuildLayout()
	{
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
		root.AddChild(dim);
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", WindowStyle());
		root.AddChild(_panel);
		_panel.AnchorLeft = 0.5f;
		_panel.AnchorRight = 0.5f;
		_panel.AnchorTop = 0.0f;
		_panel.AnchorBottom = 1.0f;
		_panel.OffsetLeft = -380;
		_panel.OffsetRight = 380;
		_panel.OffsetTop = 56;
		_panel.OffsetBottom = -170;

		var columns = new HBoxContainer();
		columns.AddThemeConstantOverride("separation", 18);
		_panel.AddChild(columns);

		var grids = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		grids.AddThemeConstantOverride("separation", 10);
		columns.AddChild(grids);

		var header = new Label { Text = "INVENTARIO" };
		header.AddThemeColorOverride("font_color", Accent);
		header.AddThemeFontSizeOverride("font_size", 20);
		grids.AddChild(header);

		foreach (ItemCategory category in Enum.GetValues<ItemCategory>())
		{
			var title = new Label { Text = ItemCatalog.CategoryName(category) };
			title.AddThemeColorOverride("font_color", MutedColor);
			title.AddThemeFontSizeOverride("font_size", 14);
			grids.AddChild(title);

			var grid = new GridContainer { Columns = 5 };
			grids.AddChild(grid);
			foreach (ItemDefinition item in ItemCatalog.Items)
			{
				if (item.Category != category)
				{
					continue;
				}

				var slot = new InventorySlot();
				slot.Init(item);
				slot.Selected += Select;
				grid.AddChild(slot);
				_slots[item.Id] = slot;
			}
		}

		var detail = new VBoxContainer { CustomMinimumSize = new Vector2(230, 0) };
		detail.AddThemeConstantOverride("separation", 8);
		columns.AddChild(detail);

		_detailName = AddDetailLabel(detail, 18, Accent);
		_detailCategory = AddDetailLabel(detail, 14, MutedColor);
		_detailCount = AddDetailLabel(detail, 16, Colors.White);
		_detailDescription = AddDetailLabel(detail, 14, MutedColor);
		_detailDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		AddDetailLabel(detail, 13, MutedColor).Text = "TAB o Esc  ·  Cerrar";
	}

	private static Label AddDetailLabel(VBoxContainer parent, int fontSize, Color color)
	{
		var label = new Label();
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		parent.AddChild(label);
		return label;
	}

	private void Refresh()
	{
		if (_inventory == null)
		{
			return;
		}

		foreach (var (id, slot) in _slots)
		{
			slot.SetCount(_inventory.GetQuantity(id));
		}

		ShowDetail(_selected);
	}

	private void Select(InventorySlot slot)
	{
		_selected?.SetSelected(false);
		_selected = slot;
		slot.SetSelected(true);
		ShowDetail(slot);
	}

	private void ShowDetail(InventorySlot? slot)
	{
		if (slot == null)
		{
			_detailName.Text = "Selecciona un objeto";
			_detailCategory.Text = string.Empty;
			_detailCount.Text = string.Empty;
			_detailDescription.Text = "Haz clic en un objeto para ver su descripción.";
			return;
		}

		_detailName.Text = slot.Item.Name;
		_detailCategory.Text = ItemCatalog.CategoryName(slot.Item.Category);
		_detailCount.Text = $"Cantidad: {slot.Count}";
		_detailDescription.Text = slot.Item.Description;
	}

	private static StyleBoxFlat WindowStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.05f, 0.08f, 0.06f, 0.92f),
			BorderColor = new Color(Accent.R, Accent.G, Accent.B, 0.5f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ContentMarginLeft = 18,
			ContentMarginRight = 18,
			ContentMarginTop = 18,
			ContentMarginBottom = 18
		};
	}
}

public partial class InventorySlot : Control
{
	private static readonly Color Accent = new(0.45f, 0.85f, 0.55f);

	private ItemDefinition _item = null!;
	private int _count;
	private bool _selected;
	private Label _countLabel = null!;

	public event Action<InventorySlot>? Selected;

	public ItemDefinition Item => _item;
	public int Count => _count;

	public void Init(ItemDefinition item)
	{
		_item = item;
		CustomMinimumSize = new Vector2(84, 84);
		TooltipText = item.Name;

		_countLabel = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		_countLabel.AddThemeFontSizeOverride("font_size", 16);
		AddChild(_countLabel);
		_countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_countLabel.OffsetRight = -8;
		_countLabel.OffsetBottom = -4;

		SetCount(0);
	}

	public void SetCount(int count)
	{
		_count = count;
		_countLabel.Text = count.ToString();
		Modulate = count > 0 ? Colors.White : new Color(1, 1, 1, 0.35f);
		QueueRedraw();
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		QueueRedraw();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
		{
			Selected?.Invoke(this);
			AcceptEvent();
		}
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (_count <= 0)
		{
			return default;
		}

		var preview = new InventorySlot();
		preview.Init(_item);
		preview.SetCount(_count);
		preview.Modulate = new Color(1, 1, 1, 0.8f);
		SetDragPreview(preview);
		return new Godot.Collections.Dictionary { ["item_id"] = _item.Id };
	}

	public override void _Draw()
	{
		var rect = new Rect2(Vector2.Zero, Size);
		DrawRect(rect, _selected ? new Color(Accent.R, Accent.G, Accent.B, 0.22f) : new Color(1, 1, 1, 0.06f));
		DrawRect(rect, _selected ? Accent : new Color(1, 1, 1, 0.14f), false, _selected ? 2.0f : 1.0f);

		ItemIconDrawer.Draw(this, _item, Size);
	}
}
