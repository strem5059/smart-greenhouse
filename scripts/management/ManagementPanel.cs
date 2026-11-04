using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Economy;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Management;

public partial class ManagementPanel : CanvasLayer
{
    public enum PanelState { Closed, Opening, Open, Closing }

    private const float AnimationTime = 0.6f;
    private const float ClosedScale = 0.92f;
    private static readonly Vector2 WindowSize = new(900, 560);
    private static readonly Color AccentColor = new(0.45f, 0.85f, 0.55f);

    public event System.Action? Closed;
    public PanelState State { get; private set; } = PanelState.Closed;

    private EconomyManager? _economy;
    private InventoryManager? _inventory;
    private PlayerController? _player;
    private Control _root = null!;
    private PanelContainer _window = null!;
    private Label _message = null!;
    private ConfirmationDialog _confirm = null!;
    private System.Action? _pendingAction;
    private Tween? _tween;
    private Label _summaryLabel = null!;
    private Label _stockLabel = null!;
    private Label _financeLabel = null!;
    private Label _managementLabel = null!;
    private Label _sellTotalLabel = null!;
    private SpinBox _sellAmount = null!;
    private Button _sellButton = null!;

    public override void _Ready()
    {
        Layer = 20;
        _economy = GetNodeOrNull<EconomyManager>("/root/EconomyManager");
        _inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
        BuildLayout();
        _root.Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (State == PanelState.Open && @event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open(PlayerController? player)
    {
        if (State != PanelState.Closed)
        {
            return;
        }

        _player = player;
        RefreshAll();
        ShowMessage(string.Empty);
        _root.Visible = true;
        _root.Modulate = new Color(1, 1, 1, 0);
        _window.PivotOffset = WindowSize / 2.0f;
        _window.Scale = new Vector2(ClosedScale, ClosedScale);
        _player?.SetInputLocked(true);
        State = PanelState.Opening;

        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetParallel(true);
        _tween.TweenProperty(_root, "modulate:a", 1.0f, AnimationTime).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_window, "scale", Vector2.One, AnimationTime)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _tween.Finished += () => State = PanelState.Open;
    }

    public void Close()
    {
        if (State != PanelState.Open)
        {
            return;
        }

        State = PanelState.Closing;
        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetParallel(true);
        _tween.TweenProperty(_root, "modulate:a", 0.0f, AnimationTime).SetEase(Tween.EaseType.In);
        _tween.TweenProperty(_window, "scale", new Vector2(ClosedScale, ClosedScale), AnimationTime)
            .SetEase(Tween.EaseType.In);
        _tween.Finished += OnClosed;
    }

    private void OnClosed()
    {
        _root.Visible = false;
        State = PanelState.Closed;
        _player?.SetInputLocked(false);
        Closed?.Invoke();
    }

    private void BuildLayout()
    {
        _root = new Control();
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(dim);
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _window = new PanelContainer();
        _window.AddThemeStyleboxOverride("panel", CreateWindowStyle());
        _root.AddChild(_window);
        _window.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _window.OffsetLeft = -WindowSize.X / 2;
        _window.OffsetRight = WindowSize.X / 2;
        _window.OffsetTop = -WindowSize.Y / 2;
        _window.OffsetBottom = WindowSize.Y / 2;

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "top", "right", "bottom" })
        {
            margin.AddThemeConstantOverride("margin_" + side, 18);
        }
        _window.AddChild(margin);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        margin.AddChild(column);

        var header = new Label { Text = "ADMINISTRACIÓN · SMART GREENHOUSE" };
        header.AddThemeColorOverride("font_color", AccentColor);
        header.AddThemeFontSizeOverride("font_size", 22);
        column.AddChild(header);

        var tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(tabs);
        tabs.AddChild(BuildSummaryTab());
        tabs.AddChild(BuildShopTab());
        tabs.AddChild(BuildStorageTab());
        tabs.AddChild(BuildFinanceTab());
        tabs.AddChild(BuildManagementTab());
        tabs.AddChild(BuildExpansionTab());

        var footer = new HBoxContainer();
        column.AddChild(footer);
        _message = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        footer.AddChild(_message);
        var closeButton = new Button { Text = "[Cerrar administración]" };
        closeButton.Pressed += Close;
        footer.AddChild(closeButton);

        _confirm = new ConfirmationDialog { Title = "Confirmar operación" };
        AddChild(_confirm);
        _confirm.Confirmed += OnConfirmed;
        _confirm.Canceled += () => _pendingAction = null;
    }

    private static VBoxContainer NewTab(string name)
    {
        return new VBoxContainer { Name = name };
    }

    private VBoxContainer BuildSummaryTab()
    {
        var tab = NewTab("Resumen");
        _summaryLabel = new Label();
        tab.AddChild(_summaryLabel);
        return tab;
    }

    private VBoxContainer BuildShopTab()
    {
        var tab = NewTab("Tienda");
        AddShopRow(tab, "lettuce_seeds", "Semillas de lechuga", "Permiten sembrar una maceta libre.");
        AddShopRow(tab, "fertilizer", "Fertilizante", "Aumenta los nutrientes de una planta.");
        return tab;
    }

    private void AddShopRow(VBoxContainer tab, string itemId, string title, string description)
    {
        int unitPrice = _economy?.GetPrice(itemId) ?? 0;
        var row = new HBoxContainer();
        var info = new Label
        {
            Text = $"{title}\n{description}\nPrecio unitario: {unitPrice} $",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        var amount = new SpinBox { MinValue = 1, MaxValue = 99, Step = 1, Value = 1 };
        var total = new Label { Text = $"Total: {unitPrice} $" };
        var buy = new Button { Text = "Comprar" };
        amount.ValueChanged += value => total.Text = $"Total: {unitPrice * (int)value} $";
        buy.Pressed += () => RequestPurchase(itemId, title, (int)amount.Value, unitPrice);
        row.AddChild(info);
        row.AddChild(amount);
        row.AddChild(total);
        row.AddChild(buy);
        tab.AddChild(row);
    }

    private VBoxContainer BuildStorageTab()
    {
        var tab = NewTab("Almacén");
        _stockLabel = new Label();
        tab.AddChild(_stockLabel);
        tab.AddChild(new Label { Text = "\nVender lechugas cosechadas:" });

        var row = new HBoxContainer();
        tab.AddChild(row);
        _sellAmount = new SpinBox { MinValue = 1, MaxValue = 1, Step = 1, Value = 1 };
        _sellTotalLabel = new Label();
        _sellButton = new Button { Text = "Vender" };
        _sellAmount.ValueChanged += _ => UpdateSellTotal();
        _sellButton.Pressed += RequestSale;
        row.AddChild(new Label { Text = "Cantidad:" });
        row.AddChild(_sellAmount);
        row.AddChild(_sellTotalLabel);
        row.AddChild(_sellButton);
        return tab;
    }

    private VBoxContainer BuildFinanceTab()
    {
        var tab = NewTab("Finanzas");
        _financeLabel = new Label();
        tab.AddChild(_financeLabel);
        return tab;
    }

    private VBoxContainer BuildManagementTab()
    {
        var tab = NewTab("Gestión");
        _managementLabel = new Label();
        tab.AddChild(_managementLabel);
        return tab;
    }

    private VBoxContainer BuildExpansionTab()
    {
        var tab = NewTab("Expansión");
        tab.AddChild(new Label { Text = "Sector 2 · +12 macetas de cultivo\nCoste: 250 $\nEstado: bloqueado" });
        tab.AddChild(new Button { Text = "Expandir (disponible en Fase 4)", Disabled = true });
        return tab;
    }

    private void RequestPurchase(string itemId, string title, int quantity, int unitPrice)
    {
        if (_economy == null || _confirm.Visible || _pendingAction != null)
        {
            return;
        }

        var economy = _economy;
        int total = quantity * unitPrice;
        _confirm.DialogText = $"Comprar {quantity} x {title} por {total} $.\nSaldo actual: {economy.Balance} $.";
        _pendingAction = () =>
        {
            economy.BuyItem(itemId, quantity);
            ShowMessage(economy.LastMessage);
            RefreshAll();
        };
        _confirm.PopupCentered(new Vector2I(440, 150));
    }

    private void RequestSale()
    {
        if (_economy == null || _inventory == null || _confirm.Visible || _pendingAction != null)
        {
            return;
        }

        var economy = _economy;
        int quantity = (int)_sellAmount.Value;
        if (quantity <= 0 || quantity > _inventory.GetQuantity("lettuce_harvested"))
        {
            return;
        }

        int total = quantity * economy.LettucePrice;
        _confirm.DialogText = $"Vender {quantity} lechuga(s) por {total} $.";
        _pendingAction = () =>
        {
            economy.SellLettuce(quantity);
            ShowMessage(economy.LastMessage);
            RefreshAll();
        };
        _confirm.PopupCentered(new Vector2I(440, 150));
    }

    private void OnConfirmed()
    {
        var action = _pendingAction;
        _pendingAction = null;
        action?.Invoke();
    }

    private void UpdateSellTotal()
    {
        int price = _economy?.LettucePrice ?? 0;
        _sellTotalLabel.Text = $"Total: {(int)_sellAmount.Value * price} $";
    }

    private void ShowMessage(string text)
    {
        _message.Text = text;
    }

    private void RefreshAll()
    {
        if (_economy == null || _inventory == null)
        {
            return;
        }

        int seeds = _inventory.GetQuantity("lettuce_seeds");
        int fertilizer = _inventory.GetQuantity("fertilizer");
        int harvested = _inventory.GetQuantity("lettuce_harvested");
        var stations = GetStations();
        int occupied = 0;
        foreach (var station in stations)
        {
            if (station.IsOccupied)
            {
                occupied++;
            }
        }

        _summaryLabel.Text =
            $"Dinero disponible: {_economy.Balance} $\n" +
            $"Lechugas cosechadas: {harvested}\n" +
            $"Semillas disponibles: {seeds}\n" +
            $"Fertilizante disponible: {fertilizer}\n" +
            $"Estaciones agrícolas: {stations.Count} (ocupadas: {occupied})\n" +
            $"Estado general: {(occupied > 0 ? "Producción activa" : "Sin cultivos")}";

        _stockLabel.Text =
            $"Semillas de lechuga: {seeds}\n" +
            $"Fertilizante: {fertilizer}\n" +
            $"Lechugas cosechadas: {harvested}";

        _sellAmount.MaxValue = System.Math.Max(1, harvested);
        _sellButton.Disabled = harvested <= 0;
        UpdateSellTotal();

        _financeLabel.Text = BuildFinanceText();
        _managementLabel.Text = BuildManagementText(stations, occupied);
    }

    private string BuildFinanceText()
    {
        int income = 0;
        int spent = 0;
        foreach (var t in _economy!.History)
        {
            if (t.Amount > 0)
            {
                income += t.Amount;
            }
            else
            {
                spent -= t.Amount;
            }
        }

        var lines = new List<string>
        {
            $"Ingresos totales: {income} $",
            $"Gastos totales: {spent} $",
            string.Empty,
            "Últimos movimientos:"
        };

        int history = _economy.History.Count;
        for (int i = history - 1; i >= System.Math.Max(0, history - 12); i--)
        {
            var t = _economy.History[i];
            string sign = t.Amount >= 0 ? "+" : string.Empty;
            lines.Add($"{t.Type} · {t.Description} · x{t.Quantity} · {sign}{t.Amount} $ · saldo {t.BalanceAfter} $");
        }

        if (history == 0)
        {
            lines.Add("Sin movimientos todavía.");
        }

        return string.Join("\n", lines);
    }

    private static string BuildManagementText(List<GrowingStation> stations, int occupied)
    {
        var perTable = new SortedDictionary<int, (int Total, int Occupied)>();
        foreach (var station in stations)
        {
            int index = int.TryParse(station.Name.ToString().Replace("GrowingStation", ""), out int parsed) ? parsed : 0;
            int table = index / 3 + 1;
            perTable.TryGetValue(table, out var entry);
            perTable[table] = (entry.Total + 1, entry.Occupied + (station.IsOccupied ? 1 : 0));
        }

        var lines = new List<string>
        {
            $"Capacidad: {stations.Count} macetas en {perTable.Count} mesas",
            $"Ocupadas: {occupied}   Libres: {stations.Count - occupied}",
            string.Empty
        };
        foreach (var (table, entry) in perTable)
        {
            lines.Add($"Mesa {table}: {entry.Occupied}/{entry.Total} ocupadas");
        }

        return string.Join("\n", lines);
    }

    private List<GrowingStation> GetStations()
    {
        var stations = new List<GrowingStation>();
        foreach (Node node in GetTree().GetNodesInGroup("growing_station"))
        {
            if (node is GrowingStation station)
            {
                stations.Add(station);
            }
        }

        return stations;
    }

    private static StyleBoxFlat CreateWindowStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.09f, 0.08f, 0.97f),
            BorderColor = new Color(0.2f, 0.6f, 0.35f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
    }
}
