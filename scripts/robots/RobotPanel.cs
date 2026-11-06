using System.Collections.Generic;
using Godot;
using SmartGreenhouse.Management;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class RobotPanel : CanvasLayer
{
	private IProgrammableRobot _robot = null!;
	private Control _root = null!;
	private Label _status = null!;
	private Label _info = null!;
	private TextEdit _editor = null!;
	private TextEdit _console = null!;
	private Label _feedback = null!;
	private Button _run = null!;
	private Button _pause = null!;
	private Button _stop = null!;
	private PlayerController? _player;
	private readonly List<(RobotSetting Setting, SpinBox Input)> _settings = new();

	public void Initialize(IProgrammableRobot robot)
	{
		_robot = robot;
		Layer = 18;

		_root = new Control();
		AddChild(_root);
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.Visible = false;

		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
		_root.AddChild(dim);
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var frame = new PanelContainer();
		frame.AddThemeStyleboxOverride("panel", PanelTheme.Box(PanelTheme.SurfaceFill, PanelTheme.BorderLine, 14, 1, 14));
		_root.AddChild(frame);
		frame.AnchorLeft = 0.14f;
		frame.AnchorRight = 0.86f;
		frame.AnchorTop = 0.1f;
		frame.AnchorBottom = 0.9f;

		var column = PanelTheme.Column(8);
		frame.AddChild(column);

		var header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 16);
		column.AddChild(header);
		header.AddChild(PanelTheme.MakeLabel(_robot.ProgramTitle, 18, PanelTheme.Accent));
		_status = PanelTheme.MakeLabel(string.Empty, 14, PanelTheme.Text);
		_status.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(_status);
		var close = new Button { Text = "Cerrar" };
		PanelTheme.StyleButton(close, false);
		close.Pressed += Close;
		header.AddChild(close);

		_info = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Muted);
		column.AddChild(_info);

		var settingsRow = new HBoxContainer();
		settingsRow.AddThemeConstantOverride("separation", 10);
		column.AddChild(settingsRow);
		foreach (RobotSetting setting in _robot.Settings)
		{
			settingsRow.AddChild(PanelTheme.MakeLabel(setting.Label, 13, PanelTheme.Muted));
			var input = new SpinBox { MinValue = setting.Min, MaxValue = setting.Max, Step = setting.Step, Value = setting.Read(), Suffix = setting.Suffix };
			input.ValueChanged += value => setting.Write(value);
			settingsRow.AddChild(input);
			_settings.Add((setting, input));
		}

		var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 12);
		column.AddChild(body);

		var left = PanelTheme.Column(4);
		left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		left.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		body.AddChild(left);
		left.AddChild(PanelTheme.MakeLabel("Programa (sintaxis Arduino simplificada)", 13, PanelTheme.Muted));
		_editor = new TextEdit { Text = _robot.SavedProgram ?? _robot.DefaultProgram, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		left.AddChild(_editor);

		var right = PanelTheme.Column(4);
		right.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		right.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		body.AddChild(right);
		right.AddChild(PanelTheme.MakeLabel("Consola", 13, PanelTheme.Muted));
		_console = new TextEdit { Editable = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		right.AddChild(_console);

		var footer = new HBoxContainer();
		footer.AddThemeConstantOverride("separation", 10);
		column.AddChild(footer);
		var validate = new Button { Text = "Validar" };
		PanelTheme.StyleButton(validate, false);
		validate.Pressed += Validate;
		footer.AddChild(validate);
		_run = new Button { Text = "Ejecutar" };
		PanelTheme.StyleButton(_run, true);
		_run.Pressed += RunProgram;
		footer.AddChild(_run);
		_pause = new Button { Text = "Pausar" };
		PanelTheme.StyleButton(_pause, false);
		_pause.Pressed += () => { _robot.TogglePause(); Refresh(); };
		footer.AddChild(_pause);
		_stop = new Button { Text = "Detener" };
		PanelTheme.StyleButton(_stop, false);
		_stop.Pressed += _robot.StopProgram;
		footer.AddChild(_stop);
		_feedback = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Muted);
		_feedback.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		footer.AddChild(_feedback);
	}

	public void Open(PlayerController? player)
	{
		_player = player ?? GetTree().GetFirstNodeInGroup("player") as PlayerController;
		_root.Visible = true;
		_player?.SetInputLocked(true);
		foreach (var (setting, input) in _settings)
		{
			input.SetValueNoSignal(setting.Read());
		}

		Refresh();
		_editor.CallDeferred(Control.MethodName.GrabFocus);
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

	public void AppendConsole(string line)
	{
		_console.Text += line + "\n";
	}

	public void Refresh()
	{
		if (_root == null || !_root.Visible)
		{
			return;
		}

		bool running = _robot.Interpreter.IsRunning;
		_status.Text = $"Estado: {_robot.StateName}   ·   Acción: {_robot.TaskName}";
		string line = running ? $"   ·   Línea {_robot.Interpreter.CurrentLine}" : string.Empty;
		_info.Text = _robot.InfoText + line;
		_run.Disabled = running;
		_stop.Disabled = !running;
		_pause.Disabled = !running;
		_pause.Text = _robot.IsPaused ? "Reanudar" : "Pausar";
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_root != null && _root.Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Validate()
	{
		bool ok = _robot.LoadProgram(_editor.Text);
		ShowFeedback(ok, ok ? "Programa válido." : $"Error en línea {_robot.Interpreter.ErrorLine}: {_robot.Interpreter.LastError}");
	}

	private void RunProgram()
	{
		if (!_robot.LoadProgram(_editor.Text))
		{
			ShowFeedback(false, $"Error en línea {_robot.Interpreter.ErrorLine}: {_robot.Interpreter.LastError}");
			return;
		}

		_robot.Run();
		ShowFeedback(true, "Programa en ejecución.");
	}

	private void ShowFeedback(bool ok, string text)
	{
		_feedback.Text = text;
		_feedback.AddThemeColorOverride("font_color", ok ? PanelTheme.Accent : PanelTheme.Danger);
	}
}
