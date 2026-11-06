using System.Collections.Generic;
using System.Globalization;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Economy;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Interfaces;

public partial class GameHud : CanvasLayer
{
	private const int MaxNotices = 4;
	private const float NoticeHold = 2.2f;
	private const float LowLevel = 20.0f;

	private static readonly Color Accent = new(0.45f, 0.85f, 0.55f);
	private static readonly Color TextColor = new(0.93f, 0.97f, 0.93f);
	private static readonly Color MutedColor = new(0.62f, 0.72f, 0.64f);
	private static readonly Color SuccessColor = new(0.5f, 1.0f, 0.6f);
	private static readonly Color WarningColor = new(1.0f, 0.78f, 0.35f);
	private static readonly Color InfoColor = new(0.62f, 0.85f, 1.0f);
	private static readonly Color DangerColor = new(1.0f, 0.5f, 0.45f);
	private static readonly Color DarkText = new(0.04f, 0.08f, 0.05f);
	private static readonly string[] CropRowTitles = { "Crecimiento", "Agua", "Nutrientes", "Salud" };

	private EconomyManager? _economy;
	private InventoryManager? _inventory;
	private IInteractable? _observed;
	private string _promptText = string.Empty;
	private int _shownBalance;
	private Tween? _promptTween;
	private Tween? _moneyTween;

	private Crosshair _crosshair = null!;
	private Label _moneyLabel = null!;
	private Label _seedsLabel = null!;
	private Label _fertilizerLabel = null!;
	private Label _harvestLabel = null!;
	private readonly List<Label> _resourceLabels = new();
	private PanelContainer _promptCard = null!;
	private PanelContainer _keyBadge = null!;
	private Label _actionLabel = null!;
	private PanelContainer _cropCard = null!;
	private Label _cropName = null!;
	private Label _cropStage = null!;
	private readonly List<(Label Label, ProgressBar Bar)> _cropRows = new();
	private VBoxContainer _noticeStack = null!;
	private readonly List<Control> _secondary = new();

	public override void _Ready()
	{
		Layer = 10;
		_economy = GetNodeOrNull<EconomyManager>("/root/EconomyManager");
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		BuildLayout();

		if (_economy != null)
		{
			SetMoney(_economy.Balance, false);
			_economy.BalanceChanged += OnBalanceChanged;
		}

		Notifications.Published += OnNoticePublished;
	}

	public override void _ExitTree()
	{
		Notifications.Published -= OnNoticePublished;
		if (_economy != null)
		{
			_economy.BalanceChanged -= OnBalanceChanged;
		}
	}

	public override void _Process(double delta)
	{
		UpdateInventory();
		UpdatePrompt();
		UpdateCrop();
	}

	public void SetObserved(IInteractable? target)
	{
		_observed = target;
	}

	public void SetCrosshairVisible(bool visible)
	{
		_crosshair.Visible = visible;
	}

	public void SetHudVisible(bool visible)
	{
		Visible = visible;
	}

	public void SetInventoryMode(bool inventoryOpen)
	{
		foreach (Control control in _secondary)
		{
			control.Visible = !inventoryOpen;
		}
	}

	private void BuildLayout()
	{
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_crosshair = new Crosshair();
		root.AddChild(_crosshair);
		_crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		AddSecondary(root, BuildResourceStack());
		AddSecondary(root, BuildControlsHint());
		_noticeStack = new VBoxContainer();
		_noticeStack.AddThemeConstantOverride("separation", 8);
		Place(_noticeStack, Control.LayoutPreset.CenterTop, -180, 24, 180, 300);
		AddSecondary(root, _noticeStack);
		AddSecondary(root, BuildPrompt());
		AddSecondary(root, BuildCropCard());
		root.AddChild(BuildQuickbar());
	}

	private Control BuildResourceStack()
	{
		var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		stack.AddThemeConstantOverride("separation", 8);

		var money = Card();
		var moneyRow = new HBoxContainer();
		moneyRow.AddThemeConstantOverride("separation", 10);
		var coin = new PanelContainer { CustomMinimumSize = new Vector2(32, 32), MouseFilter = Control.MouseFilterEnum.Ignore };
		coin.AddThemeStyleboxOverride("panel", RoundStyle(Accent, 16));
		var sign = new Label { Text = "$", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		StyleLabel(sign, 18, DarkText);
		coin.AddChild(sign);
		_moneyLabel = new Label { Text = "0 $" };
		StyleLabel(_moneyLabel, 24, TextColor);
		moneyRow.AddChild(coin);
		moneyRow.AddChild(_moneyLabel);
		money.AddChild(moneyRow);
		stack.AddChild(money);

		var inventory = Card();
		var rows = new VBoxContainer();
		rows.AddThemeConstantOverride("separation", 4);
		_seedsLabel = AddResourceRow(rows, Accent);
		_fertilizerLabel = AddResourceRow(rows, new Color(0.95f, 0.72f, 0.3f));
		_harvestLabel = AddResourceRow(rows, new Color(0.75f, 1.0f, 0.65f));
		_resourceLabels.Add(_seedsLabel);
		_resourceLabels.Add(_fertilizerLabel);
		_resourceLabels.Add(_harvestLabel);
		foreach (CropDefinition crop in CropCatalog.Crops)
		{
			if (crop.Id != "lettuce")
			{
				_resourceLabels.Add(AddResourceRow(rows, ItemCatalog.Get(crop.ProductId)?.IconColor ?? Accent));
			}
		}
		inventory.AddChild(rows);
		stack.AddChild(inventory);

		Place(stack, Control.LayoutPreset.TopLeft, 20, 20, 240, 200);
		return stack;
	}

	private static Label AddResourceRow(VBoxContainer parent, Color iconColor)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(new ColorRect { Color = iconColor, CustomMinimumSize = new Vector2(10, 10), MouseFilter = Control.MouseFilterEnum.Ignore });
		var label = new Label();
		StyleLabel(label, 15, TextColor);
		row.AddChild(label);
		parent.AddChild(row);
		return label;
	}

	private Control BuildControlsHint()
	{
		var card = Card();
		var label = new Label { Text = "V  Cambiar cámara\nEsc  Liberar ratón\nTab Inventario" };
		StyleLabel(label, 14, MutedColor);
		card.AddChild(label);
		Place(card, Control.LayoutPreset.TopRight, -240, 20, -20, 74);
		return card;
	}

	private Control BuildPrompt()
	{
		_promptCard = Card();
		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 12);

		_keyBadge = new PanelContainer { CustomMinimumSize = new Vector2(34, 30), MouseFilter = Control.MouseFilterEnum.Ignore };
		_keyBadge.AddThemeStyleboxOverride("panel", RoundStyle(Accent, 6));
		var key = new Label { Text = "E", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		StyleLabel(key, 18, DarkText);
		_keyBadge.AddChild(key);

		_actionLabel = new Label();
		StyleLabel(_actionLabel, 18, TextColor);

		row.AddChild(_keyBadge);
		row.AddChild(_actionLabel);
		_promptCard.AddChild(row);
		_promptCard.Visible = false;
		Place(_promptCard, Control.LayoutPreset.CenterBottom, -220, -206, 220, -158);
		return _promptCard;
	}

	private void AddSecondary(Control root, Control control)
	{
		root.AddChild(control);
		_secondary.Add(control);
	}

	private Control BuildQuickbar()
	{
		var bar = new QuickbarBar();
		Place(bar, Control.LayoutPreset.CenterBottom, -150, -150, 150, -36);
		return bar;
	}

	private Control BuildCropCard()
	{
		_cropCard = Card();
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 6);

		_cropName = new Label();
		StyleLabel(_cropName, 18, Accent);
		_cropStage = new Label();
		StyleLabel(_cropStage, 14, MutedColor);
		column.AddChild(_cropName);
		column.AddChild(_cropStage);

		foreach (string _ in CropRowTitles)
		{
			_cropRows.Add(AddBarRow(column));
		}

		_cropCard.AddChild(column);
		_cropCard.Visible = false;
		Place(_cropCard, Control.LayoutPreset.CenterRight, -300, -150, -20, 150);
		return _cropCard;
	}

	private static (Label Label, ProgressBar Bar) AddBarRow(VBoxContainer parent)
	{
		var label = new Label();
		StyleLabel(label, 14, TextColor);
		var bar = new ProgressBar
		{
			ShowPercentage = false,
			MinValue = 0,
			MaxValue = 100,
			CustomMinimumSize = new Vector2(0, 8),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		bar.AddThemeStyleboxOverride("background", RoundStyle(new Color(1, 1, 1, 0.12f), 4));
		bar.AddThemeStyleboxOverride("fill", RoundStyle(Accent, 4));
		parent.AddChild(label);
		parent.AddChild(bar);
		return (label, bar);
	}

	private void UpdateInventory()
	{
		if (_inventory == null)
		{
			return;
		}

		int seeds = _inventory.GetQuantity("lettuce_seeds");
		int fertilizer = _inventory.GetQuantity("fertilizer");
		int harvest = _inventory.GetQuantity("lettuce_harvested");
		SetText(_seedsLabel, $"{seeds}  Semillas");
		SetText(_fertilizerLabel, $"{fertilizer}  Fertilizante");
		SetText(_harvestLabel, $"{harvest}  Lechugas");
		int i = 3;
		foreach (CropDefinition crop in CropCatalog.Crops)
		{
			if (crop.Id != "lettuce")
			{
				SetText(_resourceLabels[i], $"{_inventory.GetQuantity(crop.ProductId)}  {crop.Name}");
				i++;
			}
		}
	}

	private void UpdatePrompt()
	{
		string prompt = _observed?.GetInteractionPrompt() ?? string.Empty;
		_promptCard.Visible = prompt.Length > 0;
		if (prompt == _promptText)
		{
			return;
		}

		_promptText = prompt;
		if (prompt.Length == 0)
		{
			return;
		}

		bool hasKey = prompt.StartsWith("[E] ", System.StringComparison.Ordinal);
		string action = hasKey ? prompt.Substring(4) : prompt;
		bool unavailable = action.StartsWith("Sin ", System.StringComparison.Ordinal);
		_keyBadge.Visible = hasKey;
		_actionLabel.Text = action.ToUpperInvariant();
		_actionLabel.Modulate = unavailable ? MutedColor : Colors.White;
		_keyBadge.Modulate = unavailable ? MutedColor : Colors.White;

		_promptTween?.Kill();
		_promptCard.Modulate = new Color(1, 1, 1, 0.35f);
		_promptTween = CreateTween();
		_promptTween.TweenProperty(_promptCard, "modulate", Colors.White, 0.18f);
	}

	private void UpdateCrop()
	{
		CropData? crop = (_observed as GrowingStation)?.Crop;
		_cropCard.Visible = crop != null;
		if (crop == null)
		{
			return;
		}

		_cropName.Text = CropName(crop.CropType);
		_cropStage.Text = $"{StageName(crop.Stage)}  ·  Etapa {(int)crop.Stage + 1} de 4";
		SetCropRow(0, crop.IsMature ? 100.0f : crop.GrowthProgress);
		SetCropRow(1, crop.Water);
		SetCropRow(2, crop.Nutrients);
		SetCropRow(3, crop.Health);
	}

	private void SetCropRow(int index, float value)
	{
		var (label, bar) = _cropRows[index];
		bar.Value = value;
		label.Text = $"{CropRowTitles[index]}  {Mathf.RoundToInt(value)}%";
		label.Modulate = index > 0 && value < LowLevel ? DangerColor : Colors.White;
	}

	private void OnBalanceChanged(int balance)
	{
		SetMoney(balance, true);
	}

	private void SetMoney(int value, bool animate)
	{
		int previous = _shownBalance;
		_shownBalance = value;
		_moneyLabel.Text = $"{value.ToString("N0", CultureInfo.InvariantCulture)} $";

		if (!animate || !IsInsideTree() || value == previous)
		{
			return;
		}

		_moneyLabel.PivotOffset = _moneyLabel.Size / 2.0f;
		_moneyLabel.Modulate = value > previous ? SuccessColor : DangerColor;
		_moneyLabel.Scale = new Vector2(1.12f, 1.12f);
		_moneyTween?.Kill();
		_moneyTween = CreateTween();
		_moneyTween.SetParallel(true);
		_moneyTween.TweenProperty(_moneyLabel, "modulate", Colors.White, 0.6f);
		_moneyTween.TweenProperty(_moneyLabel, "scale", Vector2.One, 0.35f)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
	}

	private void OnNoticePublished(string text, NoticeKind kind)
	{
		if (!Visible)
		{
			return;
		}

		var card = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
		card.AddThemeStyleboxOverride("panel", NoticeStyle(KindColor(kind)));
		var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		StyleLabel(label, 17, TextColor);
		card.AddChild(label);

		while (_noticeStack.GetChildCount() >= MaxNotices)
		{
			Node oldest = _noticeStack.GetChild(0);
			_noticeStack.RemoveChild(oldest);
			oldest.QueueFree();
		}

		_noticeStack.AddChild(card);
		Tween tween = card.CreateTween();
		tween.TweenProperty(card, "modulate:a", 1.0f, 0.25f);
		tween.TweenInterval(NoticeHold);
		tween.TweenProperty(card, "modulate:a", 0.0f, 0.45f);
		tween.TweenCallback(Callable.From(card.QueueFree));
	}

	private static Color KindColor(NoticeKind kind) => kind switch
	{
		NoticeKind.Success => SuccessColor,
		NoticeKind.Warning => WarningColor,
		_ => InfoColor
	};

	private static string CropName(string cropType) => CropCatalog.Get(cropType)?.Name ?? cropType;

	private static string StageName(GrowthStage stage) => stage switch
	{
		GrowthStage.Seed => "Semilla",
		GrowthStage.Seedling => "Plántula",
		GrowthStage.Growing => "Crecimiento",
		GrowthStage.Mature => "Madura",
		_ => stage.ToString()
	};

	private static void SetText(Label label, string text)
	{
		if (label.Text != text)
		{
			label.Text = text;
		}
	}

	private static void Place(Control control, Control.LayoutPreset preset, float left, float top, float right, float bottom)
	{
		control.SetAnchorsAndOffsetsPreset(preset);
		control.OffsetLeft = left;
		control.OffsetTop = top;
		control.OffsetRight = right;
		control.OffsetBottom = bottom;
		control.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	private static PanelContainer Card()
	{
		var card = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		card.AddThemeStyleboxOverride("panel", CardStyle());
		return card;
	}

	private static void StyleLabel(Label label, int size, Color color)
	{
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
	}

	private static StyleBoxFlat CardStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.07f, 0.05f, 0.72f),
			BorderColor = new Color(Accent.R, Accent.G, Accent.B, 0.35f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		};
	}

	private static StyleBoxFlat NoticeStyle(Color accent)
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.04f, 0.07f, 0.05f, 0.85f),
			BorderColor = accent,
			BorderWidthLeft = 4,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 14,
			ContentMarginRight = 14,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		};
	}

	private static StyleBoxFlat RoundStyle(Color color, int radius)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius
		};
	}
}
