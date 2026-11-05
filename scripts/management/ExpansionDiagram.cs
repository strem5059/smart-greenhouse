using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Expansion;

namespace SmartGreenhouse.Management;

public partial class ExpansionDiagram : Control
{
    private const float Gap = 8.0f;
    private const float MaxTile = 120.0f;

    private readonly List<(Vector2I Cell, string Name)> _modules = new();
    private readonly List<Vector2I> _openings = new();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(260, 200);
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public void SetData(IEnumerable<GreenhouseModule> modules, IEnumerable<ExpansionOption> options)
    {
        _modules.Clear();
        foreach (GreenhouseModule module in modules)
        {
            _modules.Add((module.Cell, module.Name));
        }

        _openings.Clear();
        foreach (ExpansionOption option in options)
        {
            if (option.Status == ExpansionStatus.Available)
            {
                _openings.Add(option.Target);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_modules.Count == 0)
        {
            return;
        }

        int minX = 0, maxX = 0, minY = 0, maxY = 0;
        foreach ((Vector2I cell, string _) in _modules)
        {
            minX = Mathf.Min(minX, cell.X);
            maxX = Mathf.Max(maxX, cell.X);
            minY = Mathf.Min(minY, cell.Y);
            maxY = Mathf.Max(maxY, cell.Y);
        }

        foreach (Vector2I opening in _openings)
        {
            minX = Mathf.Min(minX, opening.X);
            maxX = Mathf.Max(maxX, opening.X);
            minY = Mathf.Min(minY, opening.Y);
            maxY = Mathf.Max(maxY, opening.Y);
        }

        int columns = maxX - minX + 1;
        int rows = maxY - minY + 1;
        float tile = Mathf.Min(Mathf.Min((Size.X - 24) / columns, (Size.Y - 24) / rows) - Gap, MaxTile);
        Vector2 gridSize = new Vector2(columns * (tile + Gap) - Gap, rows * (tile + Gap) - Gap);
        Vector2 origin = (Size - gridSize) / 2.0f;

        Rect2 TileRect(Vector2I cell)
        {
            Vector2 at = origin + new Vector2((cell.X - minX) * (tile + Gap), (cell.Y - minY) * (tile + Gap));
            return new Rect2(at, new Vector2(tile, tile));
        }

        Font font = ThemeDB.FallbackFont;
        Color openingColor = new Color(PanelTheme.Accent.R, PanelTheme.Accent.G, PanelTheme.Accent.B, 0.7f);

        foreach (Vector2I opening in _openings)
        {
            Rect2 rect = TileRect(opening);
            DrawRect(rect, openingColor, false, 2.0f);
            DrawString(font, rect.Position + new Vector2(0, tile / 2 + 7), "+", HorizontalAlignment.Center, tile, 20, PanelTheme.Accent);
        }

        foreach ((Vector2I cell, string name) in _modules)
        {
            Rect2 rect = TileRect(cell);
            DrawRect(rect, PanelTheme.AccentDeep);
            DrawRect(rect, PanelTheme.Accent, false, 1.5f);
            DrawString(font, rect.Position + new Vector2(0, tile / 2 + 5), name, HorizontalAlignment.Center, tile, 14, PanelTheme.Text);
        }
    }
}
