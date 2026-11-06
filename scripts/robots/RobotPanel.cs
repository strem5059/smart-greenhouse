using Godot;
using SmartGreenhouse.Management;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Robots;

public partial class RobotPanel : CanvasLayer
{
	private const string DefaultProgram = "void loop() {\n    configurarVelocidad(3.0);\n\n    if (hayPlantasEnModulo()) {\n        activarCosecha();\n        recolectar();\n        replantar();\n        moverABaul();\n    }\n\n    delay(1000);\n}\n";

	private RobotController _robot = null!;
	private Control _root = null!;
	private Label _status = null!;
	private Label _info = null!;
	private TextEdit _editor = null!;
	private TextEdit _console = null!;
	private Label _feedback = null!;
	private Button _run = null!;
	private Button _stop = null!;
	private PlayerController? _player;

	public void Initialize(RobotController robot)
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
		header.AddChild(PanelTheme.MakeLabel("ROBOT AGRÍCOLA · AGRO-01", 18, PanelTheme.Accent));
		_status = PanelTheme.MakeLabel(string.Empty, 14, PanelTheme.Text);
		_status.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(_status);
		var close = new Button { Text = "Cerrar" };
		PanelTheme.StyleButton(close, false);
		close.Pressed += Close;
		header.AddChild(close);

		_info = PanelTheme.MakeLabel(string.Empty, 13, PanelTheme.Muted);
		column.AddChild(_info);

		var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 12);
		column.AddChild(body);

		var left = PanelTheme.Column(4);
		left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		left.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		body.AddChild(left);
		left.AddChild(PanelTheme.MakeLabel("Programa (sintaxis Arduino simplificada)", 13, PanelTheme.Muted));
		_editor = new TextEdit { Text = DefaultProgram, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
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
		string line = running ? $"   ·   Línea {_robot.CurrentLine}" : string.Empty;
		_info.Text = $"Módulo: {_robot.ModuleName}   ·   Velocidad: {_robot.Speed:F1} m/s   ·   Carga: {_robot.CargoTotal}/{RobotController.Capacity}{line}";
		_run.Disabled = running;
		_stop.Disabled = !running;
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
		ShowFeedback(ok, ok ? "Programa válido." : $"Error en línea {_robot.ErrorLine}: {_robot.LastError}");
	}

	private void RunProgram()
	{
		if (!_robot.LoadProgram(_editor.Text))
		{
			ShowFeedback(false, $"Error en línea {_robot.ErrorLine}: {_robot.LastError}");
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
