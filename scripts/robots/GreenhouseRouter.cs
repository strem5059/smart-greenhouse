using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Agriculture;

namespace SmartGreenhouse.Robots;

public static class GreenhouseRouter
{
	public const float Cell = AisleNavigator.Cell;
	public static readonly Vector3 DoorPoint = new(10.2f, 0, -3.0f);
	public static readonly Vector3 WarehouseSpot = new(11.3f, 0, 3.9f);

	private const float RoomHalfX = 10.0f;
	private const float RoomHalfZ = 6.0f;
	private const float Margin = 1.0f;
	private const float OfficeX = 10.0f;

	public static bool IsInOffice(Vector3 point) => point.X > OfficeX;

	public static Vector3 BasePosition(Node3D baseRoot, Node3D node)
	{
		return node.GetParent() is Node3D parent && parent != baseRoot
			? parent.Position + node.Position
			: node.Position;
	}

	public static List<Vector3>? RouteTo(Node3D baseRoot, Vector3 from, Vector3 target, Vector2 half)
	{
		bool fromOffice = IsInOffice(from);
		if (IsInOffice(target))
		{
			if (fromOffice)
			{
				return new List<Vector3> { target };
			}

			List<Vector3>? toDoor = new Grid(baseRoot).Find(from, DoorPoint);
			toDoor?.Add(target);
			return toDoor;
		}

		List<Vector3>? route = new Grid(baseRoot).FindNear(fromOffice ? DoorPoint : from, target, half);
		if (route != null && fromOffice)
		{
			route.Insert(0, DoorPoint);
		}

		return route;
	}

	public static string LocationName(Node3D baseRoot, Vector3 point)
	{
		if (IsInOffice(point))
		{
			return "Oficina";
		}

		foreach (Node child in baseRoot.GetChildren())
		{
			if (child is Node3D room && child.Name.ToString().StartsWith("Module") && Inside(room.Position, point))
			{
				return "Módulo " + child.Name.ToString().Substring(6);
			}
		}

		return Inside(Vector3.Zero, point) ? "Base" : "Exterior";
	}

	private static bool Inside(Vector3 room, Vector3 point)
	{
		return Mathf.Abs(point.X - room.X) <= RoomHalfX + Margin && Mathf.Abs(point.Z - room.Z) <= RoomHalfZ + Margin;
	}

	private sealed class Grid
	{
		private readonly int _cols;
		private readonly int _rows;
		private readonly float _minX;
		private readonly float _minZ;
		private readonly bool[] _blocked;

		public Grid(Node3D baseRoot)
		{
			var rooms = new List<Vector3> { Vector3.Zero };
			foreach (Node child in baseRoot.GetChildren())
			{
				if (child is Node3D room && child.Name.ToString().StartsWith("Module"))
				{
					rooms.Add(room.Position);
				}
			}

			float minX = float.MaxValue;
			float maxX = float.MinValue;
			float minZ = float.MaxValue;
			float maxZ = float.MinValue;
			foreach (Vector3 room in rooms)
			{
				minX = Mathf.Min(minX, room.X - RoomHalfX - Margin);
				maxX = Mathf.Max(maxX, room.X + RoomHalfX + Margin);
				minZ = Mathf.Min(minZ, room.Z - RoomHalfZ - Margin);
				maxZ = Mathf.Max(maxZ, room.Z + RoomHalfZ + Margin);
			}

			_minX = minX;
			_minZ = minZ;
			_cols = Mathf.CeilToInt((maxX - minX) / Cell) + 1;
			_rows = Mathf.CeilToInt((maxZ - minZ) / Cell) + 1;
			_blocked = new bool[_cols * _rows];
			System.Array.Fill(_blocked, true);

			foreach (Vector3 room in rooms)
			{
				MarkWalkable(room.X - RoomHalfX, room.X + RoomHalfX, room.Z - RoomHalfZ, room.Z + RoomHalfZ);
			}

			MarkWalkable(9.6f, DoorPoint.X + 0.3f, DoorPoint.Z - 0.6f, DoorPoint.Z + 0.6f);

			var obstacles = new List<(Vector2 Center, Vector2 Half)>();
			Collect(baseRoot, Vector3.Zero, obstacles);
			foreach (Node child in baseRoot.GetChildren())
			{
				if (child is Node3D room && child.Name.ToString().StartsWith("Module"))
				{
					Collect(room, room.Position, obstacles);
				}
			}

			foreach (var (center, half) in obstacles)
			{
				Block(center, half);
			}
		}

		public List<Vector3>? FindNear(Vector3 from, Vector3 target, Vector2 half)
		{
			float reach = Mathf.Max(half.X, half.Y) + 0.7f;
			Vector3[] candidates =
			{
				target + new Vector3(0, 0, reach),
				target - new Vector3(0, 0, reach),
				target + new Vector3(reach, 0, 0),
				target - new Vector3(reach, 0, 0),
			};
			foreach (Vector3 candidate in candidates)
			{
				List<Vector3>? path = Find(from, candidate);
				if (path != null)
				{
					return path;
				}
			}

			return null;
		}

		public List<Vector3>? Find(Vector3 from, Vector3 to)
		{
			int start = CellIndex(from);
			int goal = CellIndex(to);
			if (start < 0 || goal < 0 || _blocked[goal])
			{
				return null;
			}

			int count = _blocked.Length;
			var cost = new float[count];
			var came = new int[count];
			for (int i = 0; i < count; i++)
			{
				cost[i] = float.MaxValue;
				came[i] = -1;
			}

			var open = new PriorityQueue<int, float>();
			cost[start] = 0;
			open.Enqueue(start, Heuristic(start, goal));
			while (open.Count > 0)
			{
				int current = open.Dequeue();
				if (current == goal)
				{
					break;
				}

				int ci = current % _cols;
				int cj = current / _cols;
				for (int k = 0; k < 4; k++)
				{
					int ni = ci + (k == 0 ? 1 : k == 1 ? -1 : 0);
					int nj = cj + (k == 2 ? 1 : k == 3 ? -1 : 0);
					if (ni < 0 || ni >= _cols || nj < 0 || nj >= _rows)
					{
						continue;
					}

					int next = nj * _cols + ni;
					if (_blocked[next])
					{
						continue;
					}

					float candidate = cost[current] + Cell;
					if (candidate < cost[next])
					{
						cost[next] = candidate;
						came[next] = current;
						open.Enqueue(next, candidate + Heuristic(next, goal));
					}
				}
			}

			if (goal != start && came[goal] == -1)
			{
				return null;
			}

			var path = new List<Vector3>();
			for (int cell = goal; cell != start; cell = came[cell])
			{
				path.Add(CellCenter(cell));
			}

			path.Reverse();
			return path;
		}

		private static void Collect(Node3D root, Vector3 offset, List<(Vector2 Center, Vector2 Half)> obstacles)
		{
			foreach (Node child in root.GetChildren())
			{
				if (child is not Node3D node)
				{
					continue;
				}

				string name = child.Name.ToString();
				Vector2 center = new(offset.X + node.Position.X, offset.Z + node.Position.Z);
				if (name.StartsWith("Table"))
				{
					obstacles.Add((center, new Vector2(1.4f, 0.5f)));
				}
				else if (name.StartsWith("Channel"))
				{
					obstacles.Add((center, new Vector2(1.3f, 0.35f)));
				}
				else if (child is GrowingStation)
				{
					obstacles.Add((center, AisleNavigator.StationHalf));
				}
				else if (child is Chest)
				{
					obstacles.Add((center, AisleNavigator.ChestHalf));
				}
				else if (name.StartsWith("Column"))
				{
					obstacles.Add((center, new Vector2(0.1f, 0.1f)));
				}
			}
		}

		private void MarkWalkable(float x0, float x1, float z0, float z1)
		{
			int i0 = Mathf.Max(0, Mathf.CeilToInt((x0 - _minX) / Cell));
			int i1 = Mathf.Min(_cols - 1, Mathf.FloorToInt((x1 - _minX) / Cell));
			int j0 = Mathf.Max(0, Mathf.CeilToInt((z0 - _minZ) / Cell));
			int j1 = Mathf.Min(_rows - 1, Mathf.FloorToInt((z1 - _minZ) / Cell));
			for (int j = j0; j <= j1; j++)
			{
				for (int i = i0; i <= i1; i++)
				{
					_blocked[j * _cols + i] = false;
				}
			}
		}

		private void Block(Vector2 center, Vector2 half)
		{
			Vector2 reach = half + new Vector2(AisleNavigator.RobotRadius, AisleNavigator.RobotRadius);
			int i0 = Mathf.Max(0, Mathf.FloorToInt((center.X - reach.X - _minX) / Cell) - 1);
			int i1 = Mathf.Min(_cols - 1, Mathf.CeilToInt((center.X + reach.X - _minX) / Cell) + 1);
			int j0 = Mathf.Max(0, Mathf.FloorToInt((center.Y - reach.Y - _minZ) / Cell) - 1);
			int j1 = Mathf.Min(_rows - 1, Mathf.CeilToInt((center.Y + reach.Y - _minZ) / Cell) + 1);
			for (int j = j0; j <= j1; j++)
			{
				for (int i = i0; i <= i1; i++)
				{
					Vector3 point = CellCenter(j * _cols + i);
					if (Mathf.Abs(point.X - center.X) < reach.X && Mathf.Abs(point.Z - center.Y) < reach.Y)
					{
						_blocked[j * _cols + i] = true;
					}
				}
			}
		}

		private int CellIndex(Vector3 point)
		{
			int i = Mathf.RoundToInt((point.X - _minX) / Cell);
			int j = Mathf.RoundToInt((point.Z - _minZ) / Cell);
			if (i < 0 || i >= _cols || j < 0 || j >= _rows)
			{
				return -1;
			}

			return j * _cols + i;
		}

		private Vector3 CellCenter(int index)
		{
			return new Vector3(_minX + (index % _cols) * Cell, 0, _minZ + (index / _cols) * Cell);
		}

		private float Heuristic(int index, int goal)
		{
			return (System.Math.Abs(index % _cols - goal % _cols) + System.Math.Abs(index / _cols - goal / _cols)) * Cell;
		}
	}
}
