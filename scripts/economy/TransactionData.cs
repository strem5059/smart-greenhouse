namespace SmartGreenhouse.Economy;

public sealed class TransactionData
{
    public string Type { get; }
    public string Description { get; }
    public int Quantity { get; }
    public int Amount { get; }
    public int BalanceAfter { get; }

    public TransactionData(string type, string description, int quantity, int amount, int balanceAfter)
    {
        Type = type;
        Description = description;
        Quantity = quantity;
        Amount = amount;
        BalanceAfter = balanceAfter;
    }
}
