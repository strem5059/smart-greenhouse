using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Agriculture;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Economy;

public partial class EconomyManager : Node
{
	[Export] public int StartingBalance = 100;
	[Export] public int FertilizerPrice = 6;
	[Export] public int ExpansionPrice = 250;

	private readonly List<TransactionData> _history = new();
	private InventoryManager? _inventory;
	private int _balance;
	private bool _started;

	public string LastMessage { get; private set; } = string.Empty;
	public IReadOnlyList<TransactionData> History => _history;
	public event System.Action<int>? BalanceChanged;

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

	public void RestoreState(int balance, IEnumerable<TransactionData> history)
	{
		_started = true;
		_balance = balance;
		_history.Clear();
		_history.AddRange(history);
		BalanceChanged?.Invoke(_balance);
	}

	public void ResetToStart()
	{
		RestoreState(StartingBalance, System.Array.Empty<TransactionData>());
	}

	public int GetPrice(string itemId)
	{
		return itemId switch
		{
			"fertilizer" => FertilizerPrice,
			_ => CropCatalog.FromSeed(itemId)?.SeedPrice ?? 0
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
			Notifications.Publish("Fondos insuficientes", NoticeKind.Warning);
			return false;
		}

		SetBalance(_balance - total);
		_inventory.AddItem(itemId, quantity);
		Record("Compra", ItemName(itemId), quantity, -total);
		LastMessage = $"Compra realizada: {quantity} x {ItemName(itemId)}.";
		Notifications.Publish($"Compra realizada: {quantity} x {ItemName(itemId)}", NoticeKind.Success);
		return true;
	}

	public bool SellLettuce(int quantity) => SellHarvest("lettuce_harvested", quantity);

	public bool SellFromWarehouse(string productId, int quantity)
	{
		EnsureStarted();
		int price = GetSalePrice(productId);
		if (price <= 0 || quantity <= 0)
		{
			LastMessage = "Operación no válida.";
			return false;
		}

		int total = price * quantity;
		SetBalance(_balance + total);
		Record("Venta", ItemName(productId), quantity, total);
		LastMessage = $"Venta desde el almacén: {quantity} x {ItemName(productId)} por {total} $.";
		return true;
	}

	public bool SellHarvest(string productId, int quantity)
	{
		EnsureStarted();
		int price = GetSalePrice(productId);
		if (_inventory == null || price <= 0 || quantity <= 0)
		{
			LastMessage = "Operación no válida.";
			return false;
		}

		if (!_inventory.RemoveItem(productId, quantity))
		{
			LastMessage = "No hay existencias suficientes.";
			Notifications.Publish("Sin cosechas para vender", NoticeKind.Warning);
			return false;
		}

		int total = price * quantity;
		SetBalance(_balance + total);
		Record("Venta", ItemName(productId), quantity, total);
		LastMessage = $"Venta realizada: {quantity} x {ItemName(productId)} por {total} $.";
		Notifications.Publish($"Venta realizada: +{total} $", NoticeKind.Success);
		return true;
	}

	public int GetSalePrice(string productId) => CropCatalog.FromProduct(productId)?.SalePrice ?? 0;

	public bool TrySpend(string type, string description, int amount)
	{
		EnsureStarted();
		if (amount <= 0 || Balance < amount)
		{
			LastMessage = "Saldo insuficiente.";
			return false;
		}

		SetBalance(_balance - amount);
		Record(type, description, 1, -amount);
		LastMessage = $"{description}: -{amount} $.";
		return true;
	}

	private void SetBalance(int value)
	{
		_balance = value;
		BalanceChanged?.Invoke(_balance);
	}

	private void Record(string type, string description, int quantity, int amount)
	{
		_history.Add(new TransactionData(type, description, quantity, amount, Balance));
	}

	private static string ItemName(string itemId)
	{
		return ItemCatalog.NameOf(itemId);
	}
}
