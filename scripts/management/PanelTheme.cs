using System.Globalization;
using Godot;

namespace SmartGreenhouse.Management;

public static class PanelTheme
{
    public static readonly Color Accent = new(0.45f, 0.85f, 0.55f);
    public static readonly Color AccentDeep = new(0.16f, 0.42f, 0.26f);
    public static readonly Color AccentHover = new(0.22f, 0.52f, 0.33f);
    public static readonly Color Text = new(0.91f, 0.96f, 0.92f);
    public static readonly Color Muted = new(0.55f, 0.68f, 0.58f);
    public static readonly Color Warning = new(0.95f, 0.72f, 0.3f);
    public static readonly Color Danger = new(0.92f, 0.42f, 0.36f);
    public static readonly Color SurfaceFill = new(0.06f, 0.10f, 0.08f, 0.98f);
    public static readonly Color SidebarFill = new(0.04f, 0.075f, 0.06f, 1.0f);
    public static readonly Color CardFill = new(0.09f, 0.15f, 0.12f, 1.0f);
    public static readonly Color BorderLine = new(0.16f, 0.30f, 0.22f, 0.9f);

    public static StyleBoxFlat Box(Color fill, Color border, int radius, int borderWidth, int padding)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = padding,
            ContentMarginRight = padding,
            ContentMarginTop = padding,
            ContentMarginBottom = padding
        };
    }

    public static Label MakeLabel(string text, int size, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    public static VBoxContainer Column(int separation = 6)
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", separation);
        return column;
    }

    public static PanelContainer Card()
    {
        var card = new PanelContainer();
        card.AddThemeStyleboxOverride("panel", Box(CardFill, BorderLine, 10, 1, 14));
        return card;
    }

    public static ScrollContainer Scroll()
    {
        return new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
    }

    public static MarginContainer Margins(int margin, Control child)
    {
        var container = new MarginContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("margin_left", margin);
        container.AddThemeConstantOverride("margin_right", margin);
        container.AddThemeConstantOverride("margin_top", margin);
        container.AddThemeConstantOverride("margin_bottom", margin);
        container.AddChild(child);
        return container;
    }

    public static void StyleButton(Button button, bool primary)
    {
        Color fill = primary ? AccentDeep : CardFill;
        Color line = primary ? Accent : BorderLine;
        button.AddThemeStyleboxOverride("normal", Box(fill, line, 8, 1, 8));
        button.AddThemeStyleboxOverride("hover", Box(primary ? AccentHover : new Color(0.13f, 0.22f, 0.17f), Accent, 8, 1, 8));
        button.AddThemeStyleboxOverride("pressed", Box(new Color(0.10f, 0.30f, 0.18f), Accent, 8, 1, 8));
        button.AddThemeStyleboxOverride("disabled", Box(new Color(0.07f, 0.10f, 0.08f), BorderLine, 8, 1, 8));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeColorOverride("font_color", Text);
        button.AddThemeColorOverride("font_hover_color", Accent);
        button.AddThemeColorOverride("font_pressed_color", Accent);
        button.AddThemeColorOverride("font_disabled_color", Muted);
        button.AddThemeFontSizeOverride("font_size", 14);
        button.CustomMinimumSize = new Vector2(0, 34);
    }

    public static string Money(int value)
    {
        return $"{value.ToString("N0", CultureInfo.InvariantCulture)} $";
    }
}
