using System;
using Godot;
using SmartGreenhouse.Management;
using SmartGreenhouse.Save;

namespace SmartGreenhouse.Menu;

public partial class MainMenu : Control
{
	private SaveManager _save = null!;
	private Button _continue = null!;
	private Control _loadPanel = null!;
	private Control _optionsPanel = null!;
	private VBoxContainer _slotRows = null!;
	private ConfirmationDialog _confirm = null!;
	private string? _pendingDelete;

	public override void _Ready()
	{
		_save = GetNode<SaveManager>("/root/SaveManager");
		Input.MouseMode = Input.MouseModeEnum.Visible;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		BuildBackground();
		BuildMenu();
		BuildLoadPanel();
		BuildOptionsPanel();

		_confirm = new ConfirmationDialog { Title = "Confirmar eliminación" };
		AddChild(_confirm);
		_confirm.Confirmed += () =>
		{
			if (_pendingDelete != null)
			{
				_save.DeleteSlot(_pendingDelete);
				_pendingDelete = null;
				RefreshSlots();
			}
		};

		RefreshContinue();
		if (SaveManager.OpenLoadOnMenu)
		{
			SaveManager.OpenLoadOnMenu = false;
			ShowLoadPanel();
		}
	}

	private void BuildBackground()
	{
		var container = new SubViewportContainer { Stretch = true };
		container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(container);
		var viewport = new SubViewport { OwnWorld3D = true, HandleInputLocally = false };
		container.AddChild(viewport);

		Node3D greenhouse = GD.Load<PackedScene>(SaveManager.GreenhousePath).Instantiate<Node3D>();
		if (greenhouse.GetNodeOrNull("Player") is Node player)
		{
			greenhouse.RemoveChild(player);
			player.Free();
		}

		viewport.AddChild(greenhouse);

		var camera = new Camera3D { Position = new Vector3(0, 9, 19), Fov = 55 };
		viewport.AddChild(camera);
		camera.Current = true;
		camera.LookAt(new Vector3(0, 1, 0));

		var shade = new ColorRect { Color = new Color(0.02f, 0.06f, 0.04f, 0.55f), MouseFilter = MouseFilterEnum.Ignore };
		AddChild(shade);
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	private void BuildMenu()
	{
		var column = PanelTheme.Column(14);
		AddChild(column);
		column.AnchorLeft = 0.06f;
		column.AnchorRight = 0.36f;
		column.AnchorTop = 0.18f;
		column.AnchorBottom = 0.85f;
		column.AddChild(PanelTheme.MakeLabel("SMART GREENHOUSE", 40, PanelTheme.Accent));
		column.AddChild(PanelTheme.MakeLabel("Invernadero agrícola automatizado", 14, PanelTheme.Muted));

		column.AddChild(MakeButton("Nueva partida", () => _save.StartNewGame(), true));
		_continue = MakeButton("Continuar", () =>
		{
			SlotInfo? latest = _save.LatestSlot();
			if (latest != null)
			{
				_save.LoadSlot(latest.Path);
			}
		}, false);
		column.AddChild(_continue);
		column.AddChild(MakeButton("Cargar partida", ShowLoadPanel, false));
		column.AddChild(MakeButton("Opciones", ShowOptions, false));
		column.AddChild(MakeButton("Salir", () => GetTree().Quit(), false));
	}

	private void RefreshContinue()
	{
		_continue.Disabled = _save.LatestSlot() == null;
	}

	private void BuildLoadPanel()
	{
		_loadPanel = PanelFrame();
		AddChild(_loadPanel);
		var column = PanelTheme.Column(10);
		_loadPanel.AddChild(column);
		column.AddChild(PanelTheme.MakeLabel("PARTIDAS GUARDADAS", 22, PanelTheme.Accent));
		var scroll = PanelTheme.Scroll();
		column.AddChild(scroll);
		_slotRows = PanelTheme.Column(8);
		_slotRows.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		scroll.AddChild(_slotRows);
		column.AddChild(MakeButton("Volver", () => _loadPanel.Visible = false, false));
	}

	private void BuildOptionsPanel()
	{
		_optionsPanel = PanelFrame();
		AddChild(_optionsPanel);
		var column = PanelTheme.Column(12);
		_optionsPanel.AddChild(column);
		column.AddChild(PanelTheme.MakeLabel("OPCIONES", 22, PanelTheme.Accent));
		var autosave = new CheckButton { Text = "Guardado automático cada 5 minutos", ButtonPressed = _save.AutosaveEnabled };
		autosave.Toggled += enabled => _save.AutosaveEnabled = enabled;
		column.AddChild(autosave);
		column.AddChild(PanelTheme.MakeLabel("Las partidas se guardan en el equipo, en la carpeta de datos de usuario de Godot.", 13, PanelTheme.Muted));
		column.AddChild(MakeButton("Volver", () => _optionsPanel.Visible = false, false));
	}

	private void ShowLoadPanel()
	{
		_optionsPanel.Visible = false;
		RefreshSlots();
		_loadPanel.Visible = true;
	}

	private void ShowOptions()
	{
		_loadPanel.Visible = false;
		_optionsPanel.Visible = true;
	}

	private void RefreshSlots()
	{
		foreach (Node child in _slotRows.GetChildren())
		{
			_slotRows.RemoveChild(child);
			child.QueueFree();
		}

		var slots = _save.ListSlots();
		if (slots.Count == 0)
		{
			_slotRows.AddChild(PanelTheme.MakeLabel("No hay partidas guardadas.", 14, PanelTheme.Muted));
			return;
		}

		foreach (SlotInfo slot in slots)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			_slotRows.AddChild(row);
			string text = slot.Damaged
				? $"{slot.Name} · archivo dañado o incompatible"
				: $"{slot.Name} · {slot.SavedAt} · {PanelTheme.Money(slot.Balance)}";
			var label = PanelTheme.MakeLabel(text, 14, slot.Damaged ? PanelTheme.Danger : PanelTheme.Text);
			label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			row.AddChild(label);
			var load = MakeButton("Cargar", () => _save.LoadSlot(slot.Path), true);
			load.Disabled = slot.Damaged;
			row.AddChild(load);
			row.AddChild(MakeButton("Eliminar", () =>
			{
				_pendingDelete = slot.Path;
				_confirm.DialogText = $"¿Eliminar permanentemente \"{slot.Name}\"?";
				_confirm.PopupCentered(new Vector2I(460, 140));
			}, false));
		}
	}

	private static Control PanelFrame()
	{
		var frame = new PanelContainer { Visible = false };
		frame.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.SurfaceFill, PanelTheme.BorderLine, 14, 1, 24));
		frame.AnchorLeft = 0.4f;
		frame.AnchorRight = 0.94f;
		frame.AnchorTop = 0.15f;
		frame.AnchorBottom = 0.85f;
		return frame;
	}

	private static Button MakeButton(string text, Action action, bool primary)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(260, 44) };
		PanelTheme.StyleButton(button, primary);
		button.Pressed += action;
		return button;
	}
}
