using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Economy;

public partial class EconomyManager : Node
{
    [Export] public int StartingBalance = 100;
    [Export] public int LettucePrice = 12;
    [Export] public int SeedPrice = 4;
    [Export] public int FertilizerPrice = 6;

    private readonly List<TransactionData> _history = new();
    private InventoryManager? _inventory;
    private int _balance;
    private bool _started;

    public string LastMessage { get; private set; } = string.Empty;
    public IReadOnlyList<TransactionData> History => _history;

    public int Balance
    {
        get
        {
            EnsureStarted();
            return _balance;
        }
    }

    public override void _Ready()
    {
        EnsureStarted();
    }

    private void EnsureStarted()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _balance = StartingBalance;
        _inventory ??= GetNodeOrNull<InventoryManager>("/root/InventoryManager");
    }

    public int GetBalance()
    {
        return Balance;
    }

    public int GetPrice(string itemId)
    {
        return itemId switch
        {
            "lettuce_seeds" => SeedPrice,
            "fertilizer" => FertilizerPrice,
            _ => 0
        };
    }

    public bool BuyItem(string itemId, int quantity)
    {
        EnsureStarted();
        int unitPrice = GetPrice(itemId);
        if (_inventory == null || unitPrice <= 0 || quantity <= 0)
        {
            LastMessage = "Operación no válida.";
            return false;
        }

        int total = unitPrice * quantity;
        if (Balance < total)
        {
            LastMessage = "Saldo insuficiente.";
            return false;
        }

        _balance -= total;
        _inventory.AddItem(itemId, quantity);
        Record("Compra", ItemName(itemId), quantity, -total);
        LastMessage = $"Compra realizada: {quantity} x {ItemName(itemId)}.";
        return true;
    }

    public bool SellLettuce(int quantity)
    {
        EnsureStarted();
        if (_inventory == null || quantity <= 0)
        {
            LastMessage = "Operación no válida.";
            return false;
        }

        if (!_inventory.RemoveItem("lettuce_harvested", quantity))
        {
            LastMessage = "No hay existencias suficientes.";
            return false;
        }

        int total = LettucePrice * quantity;
        _balance += total;
        Record("Venta", "Lechuga", quantity, total);
        LastMessage = $"Venta realizada: {quantity} lechuga(s) por {total} $.";
        return true;
    }

    private void Record(string type, string description, int quantity, int amount)
    {
        _history.Add(new TransactionData(type, description, quantity, amount, Balance));
    }

    private static string ItemName(string itemId)
    {
        return itemId switch
        {
            "lettuce_seeds" => "Semillas de lechuga",
            "fertilizer" => "Fertilizante",
            "lettuce_harvested" => "Lechuga",
            _ => itemId
        };
    }
}
