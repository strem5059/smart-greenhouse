using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;
using SmartGreenhouse.Management;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class Chest : StaticBody3D, IInteractable
{
	private readonly Dictionary<string, int> _items = new();
	private InventoryManager? _inventory;
	private ChestPanel _panel = null!;

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

	public override void _Ready()
	{
		AddToGroup("chest");
		_inventory = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
		AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(1.0f, 0.6f, 0.7f) },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.5f, 0.34f, 0.2f), Roughness = 0.8f },
			Position = new Vector3(0, 0.3f, 0)
		});
		AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(1.0f, 0.6f, 0.7f) },
			Position = new Vector3(0, 0.3f, 0)
		});
		_panel = new ChestPanel();
		AddChild(_panel);
		_panel.Initialize(this);
	}

	public int Count(string itemId) => _items.TryGetValue(itemId, out int count) ? count : 0;

	public void Deposit(IReadOnlyDictionary<string, int> items)
	{
		foreach (var (itemId, count) in items)
		{
			if (count <= 0)
			{
				continue;
			}

			_items[itemId] = Count(itemId) + count;
		}
	}

	public bool TakeOne(string itemId) => TakeAmount(itemId, 1) == 1;

	public int TakeAll(string itemId) => TakeAmount(itemId, Count(itemId));

	public IReadOnlyDictionary<string, int> Contents => _items;

	public string GetInteractionPrompt() => "[E] Abrir baúl";

	public void Interact()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		_panel.Open(player);
	}

	private int TakeAmount(string itemId, int amount)
	{
		int available = Count(itemId);
		int taken = System.Math.Min(available, amount);
		if (taken <= 0 || _inventory == null)
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

		_inventory.AddItem(itemId, taken);
		return taken;
	}
}

public partial class ChestPanel : CanvasLayer
{
	private Chest _chest = null!;
	private Control _root = null!;
	private VBoxContainer _rows = null!;
	private PlayerController? _player;

	public void Initialize(Chest chest)
	{
		_chest = chest;
		Layer = 17;
		_root = new Control();
		AddChild(_root);
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.Visible = false;

		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
		_root.AddChild(dim);
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var frame = new PanelContainer();
		frame.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.SurfaceFill, PanelTheme.BorderLine, 14, 1, 16));
		_root.AddChild(frame);
		frame.AnchorLeft = 0.3f;
		frame.AnchorRight = 0.7f;
		frame.AnchorTop = 0.25f;
		frame.AnchorBottom = 0.75f;

		var column = PanelTheme.Column(10);
		frame.AddChild(column);
		column.AddChild(PanelTheme.MakeLabel("BAÚL DEL MÓDULO", 18, PanelTheme.Accent));
		_rows = PanelTheme.Column(8);
		_rows.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		column.AddChild(_rows);
		var close = new Button { Text = "Cerrar" };
		PanelTheme.StyleButton(close, false);
		close.Pressed += Close;
		column.AddChild(close);
	}

	public void Open(PlayerController? player)
	{
		_player = player;
		_root.Visible = true;
		_player?.SetInputLocked(true);
		Refresh();
	}

	public void Close()
	{
		if (!_root.Visible)
		{
			return;
		}

		_root.Visible = false;
		_player?.SetInputLocked(false);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_root != null && _root.Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Refresh()
	{
		foreach (Node child in _rows.GetChildren())
		{
			_rows.RemoveChild(child);
			child.QueueFree();
		}

		if (_chest.Total == 0)
		{
			_rows.AddChild(PanelTheme.MakeLabel("Vacío. Los robots dejan aquí las cosechas.", 14, PanelTheme.Muted));
			return;
		}

		foreach (var (itemId, count) in _chest.Contents)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			_rows.AddChild(row);
			var label = PanelTheme.MakeLabel($"{ItemCatalog.NameOf(itemId)} x{count}", 15, PanelTheme.Text);
			label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			row.AddChild(label);
			var one = new Button { Text = "Recoger 1" };
			PanelTheme.StyleButton(one, false);
			one.Pressed += () => { _chest.TakeOne(itemId); Refresh(); };
			row.AddChild(one);
			var all = new Button { Text = "Recoger todo" };
			PanelTheme.StyleButton(all, true);
			all.Pressed += () => { _chest.TakeAll(itemId); Refresh(); };
			row.AddChild(all);
		}
	}
}
