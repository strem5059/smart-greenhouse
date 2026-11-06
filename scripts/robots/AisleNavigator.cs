using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Agriculture;

namespace SmartGreenhouse.Robots;

public static class AisleNavigator
{
	public const float Cell = 0.25f;
	public const float RobotRadius = 0.4f;
	public static readonly Vector2 StationHalf = new(0.25f, 0.25f);
	public static readonly Vector2 ChestHalf = new(0.5f, 0.35f);

	private const float MinX = -9.85f;
	private const float MinZ = -5.85f;
	private const int Cols = 79;
	private const int Rows = 47;

	public static List<Vector3>? PlanToTarget(Node3D parent, Vector3 from, Vector3 target, Vector2 half)
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
			List<Vector3>? path = FindPath(parent, from, candidate);
			if (path != null)
			{
				return path;
			}
		}

		return null;
	}

	public static List<Vector3>? FindPath(Node3D parent, Vector3 from, Vector3 to)
	{
		bool[] blocked = BuildGrid(parent);
		int start = CellIndex(from);
		int goal = CellIndex(to);
		if (start < 0 || goal < 0 || blocked[start] || blocked[goal])
		{
			return null;
		}

		int count = Cols * Rows;
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

			int ci = current % Cols;
			int cj = current / Cols;
			for (int k = 0; k < 4; k++)
			{
				int ni = ci + (k == 0 ? 1 : k == 1 ? -1 : 0);
				int nj = cj + (k == 2 ? 1 : k == 3 ? -1 : 0);
				if (ni < 0 || ni >= Cols || nj < 0 || nj >= Rows)
				{
					continue;
				}

				int next = nj * Cols + ni;
				if (blocked[next])
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

		if (came[goal] == -1)
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

	private static bool[] BuildGrid(Node3D parent)
	{
		var obstacles = new List<(Vector2 Center, Vector2 Half)>();
		foreach (Node child in parent.GetChildren())
		{
			if (child is RobotController || child is not Node3D node)
			{
				continue;
			}

			string name = child.Name.ToString();
			Vector2 center = new(node.Position.X, node.Position.Z);
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
				obstacles.Add((center, StationHalf));
			}
			else if (child is Chest)
			{
				obstacles.Add((center, ChestHalf));
			}
			else if (name.StartsWith("Column"))
			{
				obstacles.Add((center, new Vector2(0.1f, 0.1f)));
			}
		}

		var blocked = new bool[Cols * Rows];
		for (int j = 0; j < Rows; j++)
		{
			for (int i = 0; i < Cols; i++)
			{
				Vector3 point = CellCenter(j * Cols + i);
				foreach (var (center, half) in obstacles)
				{
					if (Mathf.Abs(point.X - center.X) < half.X + RobotRadius && Mathf.Abs(point.Z - center.Y) < half.Y + RobotRadius)
					{
						blocked[j * Cols + i] = true;
						break;
					}
				}
			}
		}

		return blocked;
	}

	private static int CellIndex(Vector3 point)
	{
		int i = Mathf.RoundToInt((point.X - MinX) / Cell);
		int j = Mathf.RoundToInt((point.Z - MinZ) / Cell);
		if (i < 0 || i >= Cols || j < 0 || j >= Rows)
		{
			return -1;
		}

		return j * Cols + i;
	}

	private static Vector3 CellCenter(int index)
	{
		return new Vector3(MinX + (index % Cols) * Cell, 0, MinZ + (index / Cols) * Cell);
	}

	private static float Heuristic(int index, int goal)
	{
		return (System.Math.Abs(index % Cols - goal % Cols) + System.Math.Abs(index / Cols - goal / Cols)) * Cell;
	}
}
