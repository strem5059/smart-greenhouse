using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Economy;
using SmartGreenhouse.Expansion;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Player;
using SmartGreenhouse.Robots;

namespace SmartGreenhouse.Management;

public partial class ManagementPanel : CanvasLayer
{
    public enum PanelState { Closed, Opening, Open, Closing }

    private const float AnimationTime = 0.6f;
    private const float ClosedScale = 0.92f;
    private const int StationsPerTable = 3;

    private static readonly string[] PageNames = { "Resumen", "Tienda", "Almacén", "Finanzas", "Gestión", "Expansión" };

    private static readonly Dictionary<string, string> PageSubtitles = new()
    {
        ["Resumen"] = "Estado general del invernadero",
        ["Tienda"] = "Compra semillas y fertilizante",
        ["Almacén"] = "Existencias y venta de cosechas",
        ["Finanzas"] = "Saldo, ingresos, gastos y movimientos",
        ["Gestión"] = "Ocupación de módulos y mesas",
        ["Expansión"] = "Amplía el invernadero dentro del límite de beta"
    };

    private static readonly Dictionary<string, GlyphKind> PageGlyphs = new()
    {
        ["Resumen"] = GlyphKind.Overview,
        ["Tienda"] = GlyphKind.Shop,
        ["Almacén"] = GlyphKind.Storage,
        ["Finanzas"] = GlyphKind.Finance,
        ["Gestión"] = GlyphKind.Management,
        ["Expansión"] = GlyphKind.Expansion
    };

    public event System.Action? Closed;
    public PanelState State { get; private set; } = PanelState.Closed;

    private EconomyManager? _economy;
    private InventoryManager? _inventory;
    private PlayerController? _player;
    private ExpansionManager? _expansion;
    private Control _root = null!;
    private PanelContainer _frame = null!;
    private Label _balanceLabel = null!;
    private Label _sectionTitle = null!;
    private Label _subtitleLabel = null!;
    private Label _message = null!;
    private Label _modulesLabel = null!;
    private ConfirmationDialog _confirm = null!;
    private System.Action? _pendingAction;
    private Tween? _tween;
    private readonly Dictionary<string, Control> _pages = new();
    private readonly Dictionary<string, NavButton> _navButtons = new();

    private StatCard _cardMoney = null!;
    private StatCard _cardHarvest = null!;
    private StatCard _cardSeeds = null!;
    private StatCard _cardFertilizer = null!;
    private OccupancyBar _occupancy = null!;
    private Label _occupancyLabel = null!;
    private Label _stateLabel = null!;
    private VBoxContainer _recentList = null!;

    private readonly List<ProductCard> _productCards = new();

    private readonly Dictionary<string, StatCard> _stockCards = new();
    private SpinBox _sellAmount = null!;
    private OptionButton _sellProduct = null!;
    private Label _sellTotalLabel = null!;
    private Button _sellButton = null!;

    private StatCard _cardBalance = null!;
    private StatCard _cardIncome = null!;
    private StatCard _cardExpense = null!;
    private GridContainer _ledger = null!;

    private StatCard _cardCapacity = null!;
    private StatCard _cardOccupied = null!;
    private StatCard _cardFree = null!;
    private VBoxContainer _tableRows = null!;

    private ExpansionDiagram _diagram = null!;
    private StatCard _cardCost = null!;
    private StatCard _cardExpansionBalance = null!;
    private StatCard _cardCapacityAdded = null!;
    private StatCard _cardLimit = null!;
    private VBoxContainer _expansionRows = null!;

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
        _frame.PivotOffset = _frame.Size / 2.0f;
        _frame.Scale = new Vector2(ClosedScale, ClosedScale);
        _player?.SetInputLocked(true);
        State = PanelState.Opening;

        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetParallel(true);
        _tween.TweenProperty(_root, "modulate:a", 1.0f, AnimationTime).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_frame, "scale", Vector2.One, AnimationTime)
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
        _tween.TweenProperty(_frame, "scale", new Vector2(ClosedScale, ClosedScale), AnimationTime)
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

        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(dim);
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _frame = new PanelContainer();
        _frame.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.SurfaceFill, PanelTheme.BorderLine, 14, 1, 0));
        _root.AddChild(_frame);
        _frame.AnchorLeft = 0.06f;
        _frame.AnchorRight = 0.94f;
        _frame.AnchorTop = 0.07f;
        _frame.AnchorBottom = 0.93f;

        var column = PanelTheme.Column(0);
        _frame.AddChild(column);
        column.AddChild(BuildHeader());

        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 0);
        column.AddChild(body);
        body.AddChild(BuildSidebar());
        body.AddChild(PanelTheme.Margins(20, BuildContent()));

        column.AddChild(BuildFooter());

        _confirm = new ConfirmationDialog { Title = "Confirmar operación" };
        AddChild(_confirm);
        _confirm.Confirmed += OnConfirmed;
        _confirm.Canceled += () => _pendingAction = null;

        SelectPage("Resumen");
    }

    private Control BuildHeader()
    {
        var header = new PanelContainer { CustomMinimumSize = new Vector2(0, 64) };
        header.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.CardFill, PanelTheme.BorderLine, 0, 0, 12));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        header.AddChild(row);

        row.AddChild(PanelTheme.MakeLabel("■  SMART GREENHOUSE", 16, PanelTheme.Accent));
        row.AddChild(PanelTheme.MakeLabel("Administración", 13, PanelTheme.Muted));

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(spacer);

        var coin = new PanelContainer { CustomMinimumSize = new Vector2(26, 26) };
        coin.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.Accent, Colors.Transparent, 13, 0, 0));
        var sign = PanelTheme.MakeLabel("$", 14, new Color(0.04f, 0.08f, 0.05f));
        sign.HorizontalAlignment = HorizontalAlignment.Center;
        sign.VerticalAlignment = VerticalAlignment.Center;
        coin.AddChild(sign);
        row.AddChild(coin);

        _balanceLabel = PanelTheme.MakeLabel("0 $", 20, PanelTheme.Text);
        row.AddChild(_balanceLabel);

        var close = new Button { Text = "Cerrar" };
        PanelTheme.StyleButton(close, false);
        close.Pressed += Close;
        row.AddChild(close);
        return header;
    }

    private Control BuildSidebar()
    {
        var side = new PanelContainer { CustomMinimumSize = new Vector2(190, 0) };
        side.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.SidebarFill, PanelTheme.BorderLine, 0, 0, 10));
        var column = PanelTheme.Column(6);
        side.AddChild(column);

        foreach (string name in PageNames)
        {
            var nav = new NavButton();
            nav.Init(name, PageGlyphs[name]);
            nav.Pressed += () => SelectPage(name);
            column.AddChild(nav);
            _navButtons[name] = nav;
        }

        var spacer = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(spacer);
        _modulesLabel = PanelTheme.MakeLabel("Módulos 1 / 4", 13, PanelTheme.Muted);
        column.AddChild(_modulesLabel);
        return side;
    }

    private Control BuildContent()
    {
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 12);

        var titles = PanelTheme.Column(2);
        _sectionTitle = PanelTheme.MakeLabel(string.Empty, 22, PanelTheme.Accent);
        _subtitleLabel = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Muted);
        titles.AddChild(_sectionTitle);
        titles.AddChild(_subtitleLabel);
        content.AddChild(titles);

        var host = PanelTheme.Column(0);
        host.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddChild(host);

        foreach (string name in PageNames)
        {
            Control page = name switch
            {
                "Resumen" => BuildOverview(),
                "Tienda" => BuildShop(),
                "Almacén" => BuildStorage(),
                "Finanzas" => BuildFinance(),
                "Gestión" => BuildManagement(),
                _ => BuildExpansion()
            };
            page.Visible = false;
            host.AddChild(page);
            _pages[name] = page;
        }

        return content;
    }

    private Control BuildFooter()
    {
        var footer = new PanelContainer { CustomMinimumSize = new Vector2(0, 40) };
        footer.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.CardFill, PanelTheme.BorderLine, 0, 0, 10));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        footer.AddChild(row);

        _message = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Accent);
        _message.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(_message);
        row.AddChild(PanelTheme.MakeLabel("Esc  cerrar", 12, PanelTheme.Muted));
        return footer;
    }

    private void SelectPage(string name)
    {
        _sectionTitle.Text = name;
        _subtitleLabel.Text = PageSubtitles[name];
        foreach (var (page, control) in _pages)
        {
            control.Visible = page == name;
        }

        foreach (var (page, nav) in _navButtons)
        {
            nav.SetActive(page == name);
        }
    }

    private static VBoxContainer PageColumn()
    {
        var page = PanelTheme.Column(14);
        page.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        return page;
    }

    private static StatCard NewStat(Node parent, string title)
    {
        var card = new StatCard();
        card.Init(title);
        parent.AddChild(card);
        return card;
    }

    private Control BuildOverview()
    {
        var page = PageColumn();

        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        _cardMoney = NewStat(grid, "Dinero disponible");
        _cardHarvest = NewStat(grid, "Cosechas disponibles");
        _cardSeeds = NewStat(grid, "Semillas totales");
        _cardFertilizer = NewStat(grid, "Fertilizante");
        page.AddChild(grid);

        var row = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);
        page.AddChild(row);

        var greenhouse = PanelTheme.Card();
        greenhouse.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(greenhouse);
        var greenhouseColumn = PanelTheme.Column(8);
        greenhouse.AddChild(greenhouseColumn);
        greenhouseColumn.AddChild(PanelTheme.MakeLabel("Invernadero", 14, PanelTheme.Accent));
        _occupancyLabel = PanelTheme.MakeLabel(string.Empty, 14, PanelTheme.Text);
        greenhouseColumn.AddChild(_occupancyLabel);
        _occupancy = new OccupancyBar();
        greenhouseColumn.AddChild(_occupancy);
        _stateLabel = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Muted);
        greenhouseColumn.AddChild(_stateLabel);

        var recent = PanelTheme.Card();
        recent.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(recent);
        var recentColumn = PanelTheme.Column(6);
        recent.AddChild(recentColumn);
        recentColumn.AddChild(PanelTheme.MakeLabel("Últimos movimientos", 14, PanelTheme.Accent));
        _recentList = PanelTheme.Column(4);
        recentColumn.AddChild(_recentList);
        return page;
    }

    private Control BuildShop()
    {
        var page = PageColumn();
        var scroll = PanelTheme.Scroll();
        page.AddChild(scroll);

        var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(grid);

        if (_economy == null)
        {
            return page;
        }

        foreach (ItemDefinition item in ItemCatalog.Items)
        {
            int price = _economy.GetPrice(item.Id);
            if (price <= 0)
            {
                continue;
            }

            var card = new ProductCard();
            card.Init(item, price, quantity => RequestPurchase(item.Id, item.Name, quantity, price));
            grid.AddChild(card);
            _productCards.Add(card);
        }

        return page;
    }

    private Control BuildStorage()
    {
        var page = PageColumn();

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        foreach (ItemDefinition item in ItemCatalog.Items)
        {
            _stockCards[item.Id] = NewStat(grid, item.Name);
        }

        page.AddChild(grid);

        var sale = PanelTheme.Card();
        page.AddChild(sale);
        var saleColumn = PanelTheme.Column(10);
        sale.AddChild(saleColumn);
        saleColumn.AddChild(PanelTheme.MakeLabel("Vender cosechas", 14, PanelTheme.Accent));

        _sellProduct = new OptionButton();
        foreach (CropDefinition crop in CropCatalog.Crops)
        {
            _sellProduct.AddItem(crop.Name);
        }
        _sellProduct.ItemSelected += _ => UpdateSellTotal();
        saleColumn.AddChild(_sellProduct);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        saleColumn.AddChild(row);
        row.AddChild(PanelTheme.MakeLabel("Cantidad", 14, PanelTheme.Text));
        _sellAmount = new SpinBox { MinValue = 1, MaxValue = 1, Step = 1, Value = 1 };
        _sellAmount.ValueChanged += _ => UpdateSellTotal();
        row.AddChild(_sellAmount);
        _sellTotalLabel = PanelTheme.MakeLabel(string.Empty, 14, PanelTheme.Text);
        row.AddChild(_sellTotalLabel);
        _sellButton = new Button { Text = "Vender" };
        PanelTheme.StyleButton(_sellButton, true);
        _sellButton.Pressed += RequestSale;
        row.AddChild(_sellButton);
        return page;
    }

    private Control BuildFinance()
    {
        var page = PageColumn();

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        _cardBalance = NewStat(grid, "Saldo actual");
        _cardIncome = NewStat(grid, "Ingresos totales");
        _cardExpense = NewStat(grid, "Gastos totales");
        page.AddChild(grid);

        var card = PanelTheme.Card();
        card.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        page.AddChild(card);
        var column = PanelTheme.Column(8);
        card.AddChild(column);
        column.AddChild(PanelTheme.MakeLabel("Últimos movimientos", 14, PanelTheme.Accent));

        var scroll = PanelTheme.Scroll();
        column.AddChild(scroll);
        _ledger = new GridContainer { Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _ledger.AddThemeConstantOverride("h_separation", 16);
        _ledger.AddThemeConstantOverride("v_separation", 6);
        scroll.AddChild(_ledger);
        return page;
    }

    private Control BuildManagement()
    {
        var page = PageColumn();

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        _cardCapacity = NewStat(grid, "Capacidad");
        _cardOccupied = NewStat(grid, "Ocupadas");
        _cardFree = NewStat(grid, "Libres");
        page.AddChild(grid);

        var scroll = PanelTheme.Scroll();
        page.AddChild(scroll);
        _tableRows = PanelTheme.Column(12);
        _tableRows.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_tableRows);
        return page;
    }

    private Control BuildExpansion()
    {
        var page = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 14);

        var left = PanelTheme.Card();
        left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        left.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        page.AddChild(left);
        _diagram = new ExpansionDiagram { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        left.AddChild(_diagram);

        var right = PanelTheme.Column(10);
        right.CustomMinimumSize = new Vector2(330, 0);
        page.AddChild(right);

        var stats = new GridContainer { Columns = 2 };
        stats.AddThemeConstantOverride("h_separation", 10);
        stats.AddThemeConstantOverride("v_separation", 10);
        right.AddChild(stats);
        _cardCost = NewStat(stats, "Coste");
        _cardExpansionBalance = NewStat(stats, "Saldo disponible");
        _cardCapacityAdded = NewStat(stats, "Capacidad adicional");
        _cardLimit = NewStat(stats, "Límite de beta");

        var scroll = PanelTheme.Scroll();
        right.AddChild(scroll);
        _expansionRows = PanelTheme.Column(8);
        _expansionRows.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_expansionRows);
        return page;
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
        if (quantity <= 0 || quantity > _inventory.GetQuantity(SellProductId()))
        {
            return;
        }

        string productId = SellProductId();
        int total = quantity * economy.GetSalePrice(productId);
        _confirm.DialogText = $"Vender {quantity} x {ItemCatalog.NameOf(productId)} por {total} $.";
        _pendingAction = () =>
        {
            economy.SellHarvest(productId, quantity);
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

    private string SellProductId()
    {
        int index = Mathf.Clamp(_sellProduct.Selected, 0, CropCatalog.Crops.Count - 1);
        return CropCatalog.Crops[index].ProductId;
    }

    private void UpdateSellTotal()
    {
        int price = _economy?.GetSalePrice(SellProductId()) ?? 0;
        _sellTotalLabel.Text = $"Total: {PanelTheme.Money((int)_sellAmount.Value * price)}";
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

        int balance = _economy.Balance;
        int seeds = _inventory.GetQuantity("lettuce_seeds");
        int fertilizer = _inventory.GetQuantity("fertilizer");
        int harvested = _inventory.GetQuantity("lettuce_harvested");
        var stations = GetStations();
        int occupied = stations.Count(s => s.IsOccupied);

        _balanceLabel.Text = PanelTheme.Money(balance);
        _cardMoney.SetValue(PanelTheme.Money(balance));
        int harvestTotal = 0;
        int seedTotal = 0;
        foreach (CropDefinition crop in CropCatalog.Crops)
        {
            harvestTotal += _inventory.GetQuantity(crop.ProductId);
            seedTotal += _inventory.GetQuantity(crop.SeedId);
        }

        _cardHarvest.SetValue(harvestTotal.ToString());
        _cardSeeds.SetValue(seedTotal.ToString());
        _cardFertilizer.SetValue(fertilizer.ToString());
        _occupancyLabel.Text = $"Estaciones ocupadas {occupied} de {stations.Count}";
        _occupancy.Set(occupied, stations.Count);
        _stateLabel.Text = occupied > 0 ? "Producción activa" : "Sin cultivos";
        RefreshRecent();

        foreach (ProductCard card in _productCards)
        {
            card.Refresh(balance);
        }

        foreach (ItemDefinition item in ItemCatalog.Items)
        {
            if (_stockCards.TryGetValue(item.Id, out StatCard? stock))
            {
                stock.SetValue(_inventory.GetQuantity(item.Id).ToString());
            }
        }

        _sellAmount.MaxValue = Math.Max(1, _inventory.GetQuantity(SellProductId()));
        _sellButton.Disabled = _inventory.GetQuantity(SellProductId()) <= 0;
        UpdateSellTotal();

        RefreshFinance();
        RefreshManagement(stations);
        RefreshExpansion();
    }

    private void RefreshRecent()
    {
        ClearChildren(_recentList);
        var history = _economy!.History;
        if (history.Count == 0)
        {
            _recentList.AddChild(PanelTheme.MakeLabel("Sin movimientos todavía.", 13, PanelTheme.Muted));
            return;
        }

        for (int i = history.Count - 1; i >= Math.Max(0, history.Count - 3); i--)
        {
            var entry = history[i];
            string sign = entry.Amount >= 0 ? "+" : string.Empty;
            Color color = entry.Amount >= 0 ? PanelTheme.Accent : PanelTheme.Danger;
            _recentList.AddChild(PanelTheme.MakeLabel($"{entry.Description}  ·  {sign}{PanelTheme.Money(entry.Amount)}", 13, color));
        }
    }

    private void RefreshFinance()
    {
        var history = _economy!.History;
        int income = 0;
        int spent = 0;
        foreach (var entry in history)
        {
            if (entry.Amount > 0)
            {
                income += entry.Amount;
            }
            else
            {
                spent -= entry.Amount;
            }
        }

        _cardBalance.SetValue(PanelTheme.Money(_economy.Balance));
        _cardIncome.SetValue(PanelTheme.Money(income), "acumulado");
        _cardExpense.SetValue(PanelTheme.Money(spent), "acumulado");

        ClearChildren(_ledger);
        foreach (string header in new[] { "Tipo", "Descripción", "Cantidad", "Importe", "Saldo" })
        {
            _ledger.AddChild(PanelTheme.MakeLabel(header, 12, PanelTheme.Muted));
        }

        if (history.Count == 0)
        {
            _ledger.AddChild(PanelTheme.MakeLabel("Sin movimientos todavía.", 13, PanelTheme.Muted));
            for (int i = 0; i < 4; i++)
            {
                _ledger.AddChild(new Control());
            }

            return;
        }

        for (int i = history.Count - 1; i >= Math.Max(0, history.Count - 12); i--)
        {
            var entry = history[i];
            _ledger.AddChild(PanelTheme.MakeLabel(entry.Type, 13, PanelTheme.Muted));
            var description = PanelTheme.MakeLabel(entry.Description, 13, PanelTheme.Text);
            description.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _ledger.AddChild(description);
            _ledger.AddChild(PanelTheme.MakeLabel(entry.Quantity.ToString(), 13, PanelTheme.Text));
            _ledger.AddChild(PanelTheme.MakeLabel(PanelTheme.Money(entry.Amount), 13, entry.Amount >= 0 ? PanelTheme.Accent : PanelTheme.Danger));
            _ledger.AddChild(PanelTheme.MakeLabel(PanelTheme.Money(entry.BalanceAfter), 13, PanelTheme.Text));
        }
    }

    private void RefreshManagement(List<GrowingStation> stations)
    {
        int occupied = stations.Count(s => s.IsOccupied);
        var modules = new SortedDictionary<int, SortedDictionary<int, (int Occupied, int Total)>>();
        foreach (GrowingStation station in stations)
        {
            int index = StationIndex(station);
            int module = index / ExpansionManager.StationsPerModule;
            int table = (index % ExpansionManager.StationsPerModule) / StationsPerTable;
            if (!modules.TryGetValue(module, out var tables))
            {
                tables = new SortedDictionary<int, (int Occupied, int Total)>();
                modules[module] = tables;
            }

            tables.TryGetValue(table, out var entry);
            tables[table] = (entry.Occupied + (station.IsOccupied ? 1 : 0), entry.Total + 1);
        }

        int tableCount = modules.Values.Sum(t => t.Count);
        _cardCapacity.SetValue(stations.Count.ToString(), $"{tableCount} mesas");
        _cardOccupied.SetValue(occupied.ToString(), occupied > 0 ? "En producción" : "Ninguna");
        _cardFree.SetValue((stations.Count - occupied).ToString(), "disponibles");

        ClearChildren(_tableRows);
        foreach (Node node in GetTree().GetNodesInGroup("robot"))
        {
            if (node is RobotController robot)
            {
                _tableRows.AddChild(PanelTheme.MakeLabel($"{robot.ModuleName} · Robot: {robot.TaskName} · Carga {robot.CargoTotal}/{RobotController.Capacity} · Baúl: {robot.ChestTotal} uds", 13, PanelTheme.Accent));
            }
        }
        foreach (var (module, tables) in modules)
        {
            var row = PanelTheme.Column(4);
            _tableRows.AddChild(row);
            row.AddChild(PanelTheme.MakeLabel(module == 0 ? "Base" : $"Módulo {module}", 13, PanelTheme.Accent));

            var tiles = new HBoxContainer();
            tiles.AddThemeConstantOverride("separation", 8);
            row.AddChild(tiles);
            foreach (var (table, entry) in tables)
            {
                var tile = new TableTile();
                tile.Init(table + 1);
                tile.Set(entry.Occupied, entry.Total);
                tiles.AddChild(tile);
            }
        }
    }

    private void RefreshExpansion()
    {
        _expansion ??= GetTree().GetFirstNodeInGroup("expansion_manager") as ExpansionManager;
        if (_expansion == null || _economy == null)
        {
            return;
        }

        int cost = _economy.ExpansionPrice;
        int balance = _economy.Balance;
        var options = _expansion.GetOptions();

        _modulesLabel.Text = $"Módulos {_expansion.ModuleCount} / {ExpansionManager.MaxModules}";
        _diagram.SetData(_expansion.Modules, options);
        _cardCost.SetValue(PanelTheme.Money(cost), "por ampliación");
        _cardExpansionBalance.SetValue(PanelTheme.Money(balance));
        _cardCapacityAdded.SetValue($"+{ExpansionManager.StationsPerModule}", "macetas");
        _cardLimit.SetValue($"{_expansion.ModuleCount} de {ExpansionManager.MaxModules}", _expansion.CanExpand ? "Puedes ampliar" : "Límite alcanzado");

        ClearChildren(_expansionRows);
        foreach (ExpansionOption option in options)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            _expansionRows.AddChild(row);

            var info = PanelTheme.Column(2);
            info.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(info);

            string status = option.Status switch
            {
                ExpansionStatus.Available => "Disponible",
                ExpansionStatus.Occupied => "Ocupada",
                ExpansionStatus.LimitReached => "Límite de beta alcanzado",
                _ => "Bloqueada por obstáculo"
            };
            Color color = option.Status switch
            {
                ExpansionStatus.Available => PanelTheme.Accent,
                ExpansionStatus.LimitReached => PanelTheme.Warning,
                ExpansionStatus.Blocked => PanelTheme.Danger,
                _ => PanelTheme.Muted
            };
            string funds = option.Status == ExpansionStatus.Available && balance < cost
                ? $"  ·  faltan {PanelTheme.Money(cost - balance)}"
                : string.Empty;

            info.AddChild(PanelTheme.MakeLabel($"{ExpansionManager.DirectionLabel(option.Direction)} · desde {option.Source.Name}", 13, PanelTheme.Text));
            info.AddChild(PanelTheme.MakeLabel($"{status}{funds}", 12, color));

            var buy = new Button { Text = "Ampliar" };
            PanelTheme.StyleButton(buy, true);
            buy.Disabled = option.Status != ExpansionStatus.Available || balance < cost;
            buy.Pressed += () => RequestExpansion(option);
            row.AddChild(buy);
        }
    }

    private void RequestExpansion(ExpansionOption option)
    {
        if (_expansion == null || _economy == null || _confirm.Visible || _pendingAction != null)
        {
            return;
        }

        int cost = _economy.ExpansionPrice;
        int balance = _economy.Balance;
        if (option.Status != ExpansionStatus.Available)
        {
            ShowMessage("Esta dirección no está disponible.");
            return;
        }

        if (balance < cost)
        {
            ShowMessage($"Fondos insuficientes: faltan {PanelTheme.Money(cost - balance)}.");
            return;
        }

        var expansion = _expansion;
        _confirm.DialogText =
            $"Ampliar: {ExpansionManager.DirectionLabel(option.Direction)} (desde {option.Source.Name})\n" +
            $"Coste: {cost} $\n" +
            $"Saldo actual: {balance} $\n" +
            $"Saldo restante: {balance - cost} $\n" +
            $"Capacidad adicional: {ExpansionManager.StationsPerModule} macetas";
        _pendingAction = () =>
        {
            expansion.Purchase(option);
            ShowMessage(expansion.LastMessage);
            RefreshAll();
        };
        _confirm.PopupCentered(new Vector2I(480, 180));
    }

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static int StationIndex(GrowingStation station)
    {
        return int.TryParse(station.Name.ToString().Replace("GrowingStation", string.Empty), out int index) ? index : 0;
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
}
