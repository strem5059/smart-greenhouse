using System;
using Godot;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Interfaces;

namespace SmartGreenhouse.Management;

public enum GlyphKind { Overview, Shop, Storage, Finance, Management, Expansion }

public partial class GlyphControl : Control
{
    public GlyphKind Kind { get; set; }
    public Color Tint { get; set; } = PanelTheme.Muted;

    public override void _Draw()
    {
        float margin = Mathf.Min(Size.X, Size.Y) * 0.15f;
        Rect2 box = new Rect2(new Vector2(margin, margin), Size - new Vector2(margin * 2, margin * 2));
        Vector2 center = box.Position + box.Size / 2.0f;
        const float width = 1.6f;
        switch (Kind)
        {
            case GlyphKind.Overview:
                {
                    float half = box.Size.X / 2.0f - 1.5f;
                    for (int row = 0; row < 2; row++)
                    {
                        for (int col = 0; col < 2; col++)
                        {
                            Vector2 at = box.Position + new Vector2(col * (half + 3), row * (half + 3));
                            DrawRect(new Rect2(at, new Vector2(half, half)), Tint, false, width);
                        }
                    }
                    break;
                }
            case GlyphKind.Shop:
                {
                    DrawRect(box, Tint, false, width);
                    DrawLine(box.Position + new Vector2(0, box.Size.Y * 0.35f), box.Position + new Vector2(box.Size.X, box.Size.Y * 0.35f), Tint, width);
                    break;
                }
            case GlyphKind.Storage:
                {
                    DrawRect(box, Tint, false, width);
                    DrawLine(box.Position + new Vector2(0, box.Size.Y * 0.4f), box.Position + new Vector2(box.Size.X, box.Size.Y * 0.4f), Tint, width);
                    break;
                }
            case GlyphKind.Finance:
                {
                    float[] heights = { 0.5f, 0.9f, 0.7f };
                    float barWidth = box.Size.X / 3.0f - 2.0f;
                    for (int i = 0; i < heights.Length; i++)
                    {
                        float h = box.Size.Y * heights[i];
                        Vector2 at = new Vector2(box.Position.X + i * (barWidth + 2.0f), box.Position.Y + box.Size.Y - h);
                        DrawRect(new Rect2(at, new Vector2(barWidth, h)), Tint);
                    }
                    break;
                }
            case GlyphKind.Management:
                {
                    float radius = box.Size.X / 2.0f;
                    DrawArc(center, radius, 0, Mathf.Tau, 28, Tint, width);
                    DrawCircle(center, radius * 0.35f, Tint);
                    break;
                }
            default:
                {
                    DrawRect(box, Tint, false, width);
                    DrawLine(new Vector2(center.X, box.Position.Y + 3), new Vector2(center.X, box.End.Y - 3), Tint, width);
                    DrawLine(new Vector2(box.Position.X + 3, center.Y), new Vector2(box.End.X - 3, center.Y), Tint, width);
                    break;
                }
        }
    }
}

public partial class NavButton : Button
{
    private ColorRect _indicator = null!;
    private GlyphControl _glyph = null!;
    private Label _label = null!;

    public void Init(string title, GlyphKind kind)
    {
        CustomMinimumSize = new Vector2(0, 42);

        _indicator = new ColorRect { Color = PanelTheme.Accent, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        AddChild(_indicator);
        _indicator.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
        _indicator.OffsetRight = 4;

        _glyph = new GlyphControl { Kind = kind, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_glyph);
        _glyph.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterLeft);
        _glyph.OffsetLeft = 14;
        _glyph.OffsetRight = 34;
        _glyph.OffsetTop = -9;
        _glyph.OffsetBottom = 9;

        _label = PanelTheme.MakeLabel(title, 14, PanelTheme.Text);
        _label.MouseFilter = MouseFilterEnum.Ignore;
        _label.VerticalAlignment = VerticalAlignment.Center;
        AddChild(_label);
        _label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _label.OffsetLeft = 44;

        SetActive(false);
    }

    public void SetActive(bool active)
    {
        _indicator.Visible = active;
        _glyph.Tint = active ? PanelTheme.Accent : PanelTheme.Muted;
        _label.AddThemeColorOverride("font_color", active ? PanelTheme.Accent : PanelTheme.Text);
        AddThemeStyleboxOverride("normal", PanelTheme.Box(active ? new Color(0.45f, 0.85f, 0.55f, 0.14f) : Colors.Transparent, Colors.Transparent, 6, 0, 0));
        AddThemeStyleboxOverride("hover", PanelTheme.Box(new Color(1, 1, 1, 0.05f), Colors.Transparent, 6, 0, 0));
        AddThemeStyleboxOverride("pressed", PanelTheme.Box(new Color(1, 1, 1, 0.08f), Colors.Transparent, 6, 0, 0));
        AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        _glyph.QueueRedraw();
    }
}

public partial class ItemIcon : Control
{
    private ItemDefinition? _item;

    public void Init(ItemDefinition item)
    {
        _item = item;
        CustomMinimumSize = new Vector2(44, 44);
        MouseFilter = MouseFilterEnum.Ignore;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_item != null)
        {
            ItemIconDrawer.Draw(this, _item, Size);
        }
    }
}

public partial class StatCard : PanelContainer
{
    private Label _title = null!;
    private Label _value = null!;
    private Label _subtitle = null!;

    public void Init(string title)
    {
        AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.CardFill, PanelTheme.BorderLine, 10, 1, 12));
        MouseFilter = MouseFilterEnum.Ignore;
        var column = PanelTheme.Column(2);
        AddChild(column);
        _title = PanelTheme.MakeLabel(title, 12, PanelTheme.Muted);
        _value = PanelTheme.MakeLabel("-", 22, PanelTheme.Text);
        _subtitle = PanelTheme.MakeLabel(string.Empty, 11, PanelTheme.Muted);
        column.AddChild(_title);
        column.AddChild(_value);
        column.AddChild(_subtitle);
    }

    public void SetValue(string value, string subtitle = "")
    {
        _value.Text = value;
        _subtitle.Text = subtitle;
        _subtitle.Visible = subtitle.Length > 0;
    }
}

public partial class ProductCard : PanelContainer
{
    private int _unitPrice;
    private int _balance;
    private SpinBox _amount = null!;
    private Label _total = null!;
    private Label _status = null!;
    private Button _buy = null!;
    private Action<int> _onBuy = null!;

    public void Init(ItemDefinition item, int unitPrice, Action<int> onBuy)
    {
        _unitPrice = unitPrice;
        _onBuy = onBuy;
        AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.CardFill, PanelTheme.BorderLine, 10, 1, 14));
        var column = PanelTheme.Column(8);
        AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        column.AddChild(header);
        var icon = new ItemIcon();
        icon.Init(item);
        header.AddChild(icon);
        var texts = PanelTheme.Column(2);
        texts.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(texts);
        texts.AddChild(PanelTheme.MakeLabel(item.Name, 16, PanelTheme.Text));
        var description = PanelTheme.MakeLabel(item.Description, 12, PanelTheme.Muted);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        texts.AddChild(description);

        column.AddChild(PanelTheme.MakeLabel($"Precio unitario: {PanelTheme.Money(unitPrice)}", 13, PanelTheme.Text));

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        column.AddChild(row);
        row.AddChild(PanelTheme.MakeLabel("Cantidad", 13, PanelTheme.Muted));
        _amount = new SpinBox { MinValue = 1, MaxValue = 99, Step = 1, Value = 1 };
        _amount.ValueChanged += _ => Refresh(_balance);
        row.AddChild(_amount);
        _total = PanelTheme.MakeLabel(string.Empty, 14, PanelTheme.Text);
        row.AddChild(_total);

        _status = PanelTheme.MakeLabel(string.Empty, 12, PanelTheme.Muted);
        column.AddChild(_status);

        _buy = new Button { Text = "Comprar" };
        PanelTheme.StyleButton(_buy, true);
        _buy.Pressed += () => _onBuy((int)_amount.Value);
        column.AddChild(_buy);
        Refresh(0);
    }

    public void Refresh(int balance)
    {
        _balance = balance;
        int total = (int)_amount.Value * _unitPrice;
        bool affordable = balance >= total;
        _total.Text = $"Total: {PanelTheme.Money(total)}";
        _status.Text = affordable ? "Fondos suficientes" : $"Faltan {PanelTheme.Money(total - balance)}";
        _status.AddThemeColorOverride("font_color", affordable ? PanelTheme.Muted : PanelTheme.Warning);
        _buy.Disabled = !affordable;
    }
}

public partial class OccupancyBar : ProgressBar
{
    public override void _Ready()
    {
        ShowPercentage = false;
        MinValue = 0;
        CustomMinimumSize = new Vector2(0, 12);
        AddThemeStyleboxOverride("background", PanelTheme.Box(new Color(1, 1, 1, 0.08f), Colors.Transparent, 6, 0, 0));
        AddThemeStyleboxOverride("fill", PanelTheme.Box(PanelTheme.Accent, Colors.Transparent, 6, 0, 0));
    }

    public void Set(int occupied, int total)
    {
        MaxValue = Math.Max(1, total);
        Value = occupied;
    }
}

public partial class TableTile : PanelContainer
{
    private Label _label = null!;

    public void Init(int number)
    {
        TooltipText = $"Mesa {number}";
        CustomMinimumSize = new Vector2(72, 40);
        _label = PanelTheme.MakeLabel(string.Empty, 14, PanelTheme.Text);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _label.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_label);
    }

    public void Set(int occupied, int total)
    {
        _label.Text = $"{occupied}/{total}";
        Color fill = occupied == 0 ? PanelTheme.CardFill : occupied == total ? PanelTheme.AccentDeep : new Color(0.12f, 0.30f, 0.20f);
        AddThemeStyleboxOverride("panel", PanelTheme.Box(fill, occupied > 0 ? PanelTheme.Accent : PanelTheme.BorderLine, 8, 1, 6));
    }
}
