using Godot;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float GravityScale = 1.0f;
    [Export] public float TurnSpeed = 10.0f;
    [Export] public float ThirdPersonHeight = 1.6f;

    private const float FirstPersonPitchLimitDeg = 80.0f;
    private const float ThirdPersonPitchLimitDeg = 60.0f;

    private Node3D _head = null!;
    private Camera3D _firstPersonCamera = null!;
    private Node3D _thirdPersonPivot = null!;
    private Node3D _thirdPersonPitch = null!;
    private Camera3D _thirdPersonCamera = null!;

    private enum CameraMode { FirstPerson, ThirdPerson }
    private CameraMode _cameraMode = CameraMode.FirstPerson;

    [Export] public float InteractionRange = 2.5f;
    private Label _interactionLabel = null!;
    private IInteractable? _currentInteractable;

    private CanvasLayer _hudCanvas = null!;
    private Label _inventoryLabel = null!;
    private Label _controlsLabel = null!;
    private Crosshair _crosshair = null!;
    private InventoryManager? _inventoryManager;
    private bool _inputLocked;

    public override void _Ready()
    {
        _head = GetNode<Node3D>("Head");
        _firstPersonCamera = GetNode<Camera3D>("Head/FirstPersonCamera");
        _thirdPersonPivot = GetNode<Node3D>("ThirdPersonPivot");
        _thirdPersonPitch = GetNode<Node3D>("ThirdPersonPivot/ThirdPersonPitch");
        _thirdPersonCamera = GetNode<Camera3D>("ThirdPersonPivot/ThirdPersonPitch/SpringArm3D/ThirdPersonCamera");
        _thirdPersonPivot.TopLevel = true;
        AddToGroup("player");
        RegisterInputActions();
        _inventoryManager = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
        SetupHUD();
        SetMouseCaptured(true);
        UpdateCameraMode();
    }

    private void SetupHUD()
    {
        _hudCanvas = new CanvasLayer
        {
            Layer = 10
        };
        AddChild(_hudCanvas);

        _crosshair = new Crosshair();
        _hudCanvas.AddChild(_crosshair);
        _crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _interactionLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "[E] Interactuar"
        };
        _interactionLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 0.9f));
        _interactionLabel.AddThemeFontSizeOverride("font_size", 20);
        _hudCanvas.AddChild(_interactionLabel);
        _interactionLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _interactionLabel.OffsetLeft = -200;
        _interactionLabel.OffsetRight = 200;
        _interactionLabel.OffsetTop = -60;
        _interactionLabel.OffsetBottom = -30;
        _interactionLabel.Hide();

        _controlsLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Text = "V — Cambiar cámara\nEsc — Liberar ratón"
        };
        _controlsLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 0.85f));
        _controlsLabel.AddThemeFontSizeOverride("font_size", 14);
        _hudCanvas.AddChild(_controlsLabel);
        _controlsLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _controlsLabel.OffsetLeft = -220;
        _controlsLabel.OffsetTop = 20;
        _controlsLabel.OffsetRight = -20;
        _controlsLabel.OffsetBottom = 80;

        _inventoryLabel = new Label();
        _inventoryLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.95f, 0.85f));
        _inventoryLabel.AddThemeFontSizeOverride("font_size", 14);
        _hudCanvas.AddChild(_inventoryLabel);
        _inventoryLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        _inventoryLabel.OffsetLeft = 20;
        _inventoryLabel.OffsetTop = 20;
        _inventoryLabel.OffsetRight = 220;
        _inventoryLabel.OffsetBottom = 100;

        UpdateInventoryDisplay();
    }

    private void RegisterInputActions()
    {
        RegisterKeyAction("move_forward", Key.W);
        RegisterKeyAction("move_back", Key.S);
        RegisterKeyAction("move_left", Key.A);
        RegisterKeyAction("move_right", Key.D);
        RegisterKeyAction("jump", Key.Space);
        RegisterKeyAction("change_camera", Key.V);
        RegisterKeyAction("interact", Key.E);
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
        if (_inputLocked)
        {
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (_cameraMode == CameraMode.FirstPerson)
            {
                RotateY(-mouseMotion.Relative.X * MouseSensitivity);
                _head.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);
                Vector3 headRot = _head.Rotation;
                headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-FirstPersonPitchLimitDeg), Mathf.DegToRad(FirstPersonPitchLimitDeg));
                _head.Rotation = headRot;
            }
            else
            {
                _thirdPersonPivot.RotateY(-mouseMotion.Relative.X * MouseSensitivity);
                _thirdPersonPitch.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);
                Vector3 pitchRot = _thirdPersonPitch.Rotation;
                pitchRot.X = Mathf.Clamp(pitchRot.X, Mathf.DegToRad(-ThirdPersonPitchLimitDeg), Mathf.DegToRad(ThirdPersonPitchLimitDeg));
                _thirdPersonPitch.Rotation = pitchRot;
            }
        }

        if (@event.IsActionPressed("change_camera"))
        {
            ToggleCamera();
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            SetMouseCaptured(Input.MouseMode != Input.MouseModeEnum.Captured);
        }

        if (@event.IsActionPressed("interact") && _currentInteractable != null)
        {
            _currentInteractable.Interact();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity += GetGravity() * GravityScale * (float)delta;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor() && !_inputLocked)
        {
            velocity.Y = JumpVelocity;
        }

        Vector2 inputDir = _inputLocked ? Vector2.Zero : Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Basis moveBasis = _cameraMode == CameraMode.ThirdPerson ? _thirdPersonPivot.GlobalTransform.Basis : Transform.Basis;
        Vector3 direction = moveBasis * new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

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

        if (_cameraMode == CameraMode.ThirdPerson && direction != Vector3.Zero)
        {
            float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetYaw, TurnSpeed * (float)delta), 0);
        }

        _thirdPersonPivot.GlobalPosition = GlobalPosition + new Vector3(0, ThirdPersonHeight, 0);
        UpdateInteraction();
        UpdateInventoryDisplay();
    }

    private void UpdateInventoryDisplay()
    {
        if (_inventoryManager == null || _inventoryLabel == null)
        {
            return;
        }

        int seeds = _inventoryManager.GetQuantity("lettuce_seeds");
        int fertilizer = _inventoryManager.GetQuantity("fertilizer");
        int harvest = _inventoryManager.GetQuantity("lettuce_harvested");
        _inventoryLabel.Text = $"SEMILLAS       {seeds}\nFERTILIZANTE   {fertilizer}\nCOSECHA        {harvest}";
    }

    private void UpdateInteraction()
    {
        if (_inputLocked)
        {
            _currentInteractable = null;
            _interactionLabel.Hide();
            return;
        }

        var camera = GetActiveCamera();
        if (camera == null)
        {
            return;
        }

        var from = camera.GlobalPosition;
        float cameraOffset = _cameraMode == CameraMode.ThirdPerson ? from.DistanceTo(GlobalPosition) : 0.0f;
        var to = from + (-camera.GlobalTransform.Basis.Z) * (InteractionRange + cameraOffset);
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count > 0 && result["collider"].AsGodotObject() is IInteractable interactable)
        {
            _currentInteractable = interactable;
            _interactionLabel.Text = interactable.GetInteractionPrompt();
            _interactionLabel.Show();
        }
        else
        {
            _currentInteractable = null;
            _interactionLabel.Hide();
        }
    }

    private Camera3D? GetActiveCamera()
    {
        if (_firstPersonCamera.Current)
        {
            return _firstPersonCamera;
        }

        if (_thirdPersonCamera.Current)
        {
            return _thirdPersonCamera;
        }

        return null;
    }

    private void ToggleCamera()
    {
        if (_cameraMode == CameraMode.FirstPerson)
        {
            _thirdPersonPivot.Rotation = new Vector3(0, Rotation.Y, 0);
            _thirdPersonPitch.Rotation = new Vector3(Mathf.Clamp(_head.Rotation.X, Mathf.DegToRad(-ThirdPersonPitchLimitDeg), Mathf.DegToRad(ThirdPersonPitchLimitDeg)), 0, 0);
            _cameraMode = CameraMode.ThirdPerson;
        }
        else
        {
            Rotation = new Vector3(0, _thirdPersonPivot.Rotation.Y, 0);
            _head.Rotation = new Vector3(Mathf.Clamp(_thirdPersonPitch.Rotation.X, Mathf.DegToRad(-FirstPersonPitchLimitDeg), Mathf.DegToRad(FirstPersonPitchLimitDeg)), 0, 0);
            _cameraMode = CameraMode.FirstPerson;
        }

        UpdateCameraMode();
    }

    private void UpdateCameraMode()
    {
        _firstPersonCamera.Current = _cameraMode == CameraMode.FirstPerson;
        _thirdPersonCamera.Current = _cameraMode == CameraMode.ThirdPerson;
    }

    public void SetInputLocked(bool locked)
    {
        _inputLocked = locked;
        _currentInteractable = null;
        _interactionLabel.Hide();

        if (locked)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _crosshair.Visible = false;
            Velocity = new Vector3(0, Velocity.Y, 0);
        }
        else
        {
            SetMouseCaptured(true);
        }
    }

    private void SetMouseCaptured(bool captured)
    {
        Input.MouseMode = captured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        _crosshair.Visible = captured;
    }
}
