using System.Collections.Generic;
using Godot;

namespace SmartGreenhouse.Robots;

public partial class CentralWarehouse : Node3D
{
	private readonly Dictionary<string, int> _items = new();

	public CentralWarehouse()
	{
		Position = GreenhouseRouter.WarehouseSpot;
	}

	public IReadOnlyDictionary<string, int> Contents => _items;

	public int Total
	{
		get
		{
			int total = 0;
			foreach (int count in _items.Values)
			{
				total += count;
			}

			return total;
		}
	}

	public int Count(string itemId) => _items.TryGetValue(itemId, out int count) ? count : 0;

	public void Deposit(string itemId, int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		_items[itemId] = Count(itemId) + amount;
	}

	public int Take(string itemId, int amount)
	{
		int available = Count(itemId);
		int taken = System.Math.Min(available, amount);
		if (taken <= 0)
		{
			return 0;
		}

		if (taken == available)
		{
			_items.Remove(itemId);
		}
		else
		{
			_items[itemId] = available - taken;
		}

		return taken;
	}

	public override void _Ready()
	{
		AddToGroup("central_warehouse");
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(1.2f, 1.4f, 0.6f) },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.58f, 0.6f), Roughness = 0.6f },
			Position = new Vector3(0, 0.7f, 0)
		});
	}
}
