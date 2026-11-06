using System;
using Godot;
using SmartGreenhouse.Management;
using SmartGreenhouse.Save;

namespace SmartGreenhouse.Menu;

public partial class PauseMenu : CanvasLayer
{
	private Control _root = null!;
	private Label _message = null!;
	private ConfirmationDialog _confirm = null!;
	private Action? _pendingAction;

	public override void _Ready()
	{
		AddToGroup("pause_menu");
		ProcessMode = ProcessModeEnum.Always;
		Layer = 30;

		_root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		AddChild(_root);
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.Visible = false;

		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = Control.MouseFilterEnum.Stop };
		_root.AddChild(dim);
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var frame = new PanelContainer();
		frame.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.SurfaceFill, PanelTheme.BorderLine, 14, 1, 24));
		_root.AddChild(frame);
		frame.AnchorLeft = 0.35f;
		frame.AnchorRight = 0.65f;
		frame.AnchorTop = 0.2f;
		frame.AnchorBottom = 0.8f;

		var column = PanelTheme.Column(12);
		frame.AddChild(column);
		column.AddChild(PanelTheme.MakeLabel("PAUSA", 22, PanelTheme.Accent));
		column.AddChild(MakeButton("Continuar", Close, true));
		column.AddChild(MakeButton("Guardar partida", Save, false));
		column.AddChild(MakeButton("Cargar partida", LoadOtherGame, false));
		column.AddChild(MakeButton("Volver al menú principal", BackToMenu, false));
		column.AddChild(MakeButton("Salir del juego", QuitGame, false));
		_message = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Muted);
		_message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		column.AddChild(_message);

		_confirm = new ConfirmationDialog { Title = "Confirmar" };
		AddChild(_confirm);
		_confirm.Confirmed += () =>
		{
			Action? action = _pendingAction;
			_pendingAction = null;
			action?.Invoke();
		};
		_confirm.Canceled += () => _pendingAction = null;
	}

	public void Open()
	{
		if (_root.Visible || !((SaveManager)GetParent()).IsInGame)
		{
			return;
		}

		_message.Text = string.Empty;
		_root.Visible = true;
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void Close()
	{
		if (!_root.Visible)
		{
			return;
		}

		_root.Visible = false;
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_root.Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private static Button MakeButton(string text, Action action, bool primary)
	{
		var button = new Button { Text = text };
		PanelTheme.StyleButton(button, primary);
		button.Pressed += action;
		return button;
	}

	private void Save()
	{
		((SaveManager)GetParent()).SaveCurrentGame(out string message);
		_message.Text = message;
	}

	private void LoadOtherGame()
	{
		Ask("Se perderá el progreso no guardado de esta partida. ¿Ir a la lista de partidas?", () =>
		{
			SaveManager.OpenLoadOnMenu = true;
			Close();
			((SaveManager)GetParent()).ReturnToMenu();
		});
	}

	private void BackToMenu()
	{
		Ask("Se perderá el progreso no guardado. ¿Volver al menú principal?", () =>
		{
			Close();
			((SaveManager)GetParent()).ReturnToMenu();
		});
	}

	private void QuitGame()
	{
		Ask("¿Salir del juego? La partida se guardará antes de cerrar.", () =>
		{
			((SaveManager)GetParent()).SaveCurrentGame(out _);
			GetTree().Quit();
		});
	}

	private void Ask(string text, Action action)
	{
		_confirm.DialogText = text;
		_pendingAction = action;
		_confirm.PopupCentered(new Vector2I(480, 150));
	}
}
