using Godot;

namespace SmartGreenhouse.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float GravityScale = 1.0f;

    private Node3D _head = null!;
    private Camera3D _firstPersonCamera = null!;
    private Camera3D _thirdPersonCamera = null!;

    private enum CameraMode { FirstPerson, ThirdPerson }
    private CameraMode _cameraMode = CameraMode.FirstPerson;

    public override void _Ready()
    {
        _head = GetNode<Node3D>("Head");
        _firstPersonCamera = GetNode<Camera3D>("Head/FirstPersonCamera");
        _thirdPersonCamera = GetNode<Camera3D>("ThirdPersonPivot/ThirdPersonCamera");
        RegisterInputActions();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        UpdateCameraMode();
    }

    private void RegisterInputActions()
    {
        RegisterKeyAction("move_forward", Key.W);
        RegisterKeyAction("move_back", Key.S);
        RegisterKeyAction("move_left", Key.A);
        RegisterKeyAction("move_right", Key.D);
        RegisterKeyAction("jump", Key.Space);
        RegisterKeyAction("change_camera", Key.V);
    }

    private static void RegisterKeyAction(string action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }
        if (!InputMap.ActionHasEvent(action, new InputEventKey { PhysicalKeycode = key }))
        {
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && _cameraMode == CameraMode.FirstPerson)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            _head.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);
            Vector3 headRot = _head.Rotation;
            headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-80), Mathf.DegToRad(80));
            _head.Rotation = headRot;
        }

        if (@event.IsActionPressed("change_camera"))
        {
            ToggleCamera();
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity += GetGravity() * GravityScale * (float)delta;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 direction = Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void ToggleCamera()
    {
        _cameraMode = _cameraMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;
        UpdateCameraMode();
    }

    private void UpdateCameraMode()
    {
        _firstPersonCamera.Current = _cameraMode == CameraMode.FirstPerson;
        _thirdPersonCamera.Current = _cameraMode == CameraMode.ThirdPerson;
    }
}
