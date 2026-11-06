using System.Collections.Generic;
using System.Linq;
using Godot;
using SmartGreenhouse.Economy;

namespace SmartGreenhouse.Robots;

public sealed record SupplyRequest(RobotController Robot, string ItemId, int Quantity);

public partial class CollectorFleet : Node
{
	public const int RobotPrice = 12000;
	public const int AdministratorPrice = 20000;

	private readonly List<CollectorRobot> _robots = new();
	private readonly List<SupplyRequest> _requests = new();

	public IReadOnlyList<SupplyRequest> Requests => _requests;

	public int RequestCount => _requests.Count;
	private EconomyManager? _economy;

	public CentralWarehouse Warehouse { get; } = new();

	public AdministrationRobot? Administrator { get; private set; }

	public Node3D BaseRoot { get; private set; } = null!;

	public IReadOnlyList<CollectorRobot> Robots => _robots;

	public override void _Ready()
	{
		AddToGroup("collector_fleet");
		BaseRoot = GetParent<Node3D>();
		_economy = GetNodeOrNull<EconomyManager>("/root/EconomyManager");
		BaseRoot.CallDeferred(Node.MethodName.AddChild, Warehouse);
	}

	public bool Purchase()
	{
		if (_economy == null || !_economy.TrySpend("Robots", "Robot recolector", RobotPrice))
		{
			return false;
		}

		var robot = new CollectorRobot();
		robot.Initialize(this, _robots.Count + 1);
		robot.Position = GreenhouseRouter.WarehouseSpot;
		robot.Run();
		_robots.Add(robot);
		BaseRoot.CallDeferred(Node.MethodName.AddChild, robot);
		return true;
	}

	public bool PurchaseAdministrator()
	{
		if (Administrator != null || _economy == null || !_economy.TrySpend("Robots", "Robot administrativo", AdministratorPrice))
		{
			return false;
		}

		var administrator = new AdministrationRobot();
		administrator.Initialize(this);
		administrator.Run();
		Administrator = administrator;
		BaseRoot.CallDeferred(Node.MethodName.AddChild, administrator);
		return true;
	}

	public void RequestSupply(RobotController robot, string itemId, int quantity)
	{
		int index = _requests.FindIndex(request => request.Robot == robot && request.ItemId == itemId);
		if (index >= 0)
		{
			if (quantity > _requests[index].Quantity)
			{
				_requests[index] = _requests[index] with { Quantity = quantity };
			}

			return;
		}

		if (quantity > 0)
		{
			_requests.Add(new SupplyRequest(robot, itemId, quantity));
		}
	}

	public void CompleteRequest(SupplyRequest request) => _requests.Remove(request);

	public void CancelRequests(RobotController robot) => _requests.RemoveAll(request => request.Robot == robot);

	public CollectorRobot CreateCollector(int id)
	{
		var robot = new CollectorRobot();
		robot.Initialize(this, id);
		robot.Position = GreenhouseRouter.WarehouseSpot;
		_robots.Add(robot);
		BaseRoot.AddChild(robot);
		return robot;
	}

	public AdministrationRobot CreateAdministrator()
	{
		var administrator = new AdministrationRobot();
		administrator.Initialize(this);
		Administrator = administrator;
		BaseRoot.AddChild(administrator);
		return administrator;
	}

	public List<Chest> ChestsWithStock(Vector3 from)
	{
		var chests = new List<Chest>();
		foreach (Node node in GetTree().GetNodesInGroup("chest"))
		{
			if (node is Chest chest && chest.Total > 0)
			{
				chests.Add(chest);
			}
		}

		return chests.OrderBy(chest => GreenhouseRouter.BasePosition(BaseRoot, chest).DistanceTo(from)).ToList();
	}
}
