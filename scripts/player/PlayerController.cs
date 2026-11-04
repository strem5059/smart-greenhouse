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

    private Node3D _head = null!;
    private Camera3D _firstPersonCamera = null!;
    private Camera3D _thirdPersonCamera = null!;

    private enum CameraMode { FirstPerson, ThirdPerson }
    private CameraMode _cameraMode = CameraMode.FirstPerson;

    [Export] public float InteractionRange = 2.5f;
    private Label _interactionLabel = null!;
    private IInteractable? _currentInteractable;

    private CanvasLayer _hudCanvas = null!;
    private Label _inventoryLabel = null!;
    private Label _controlsLabel = null!;
    private InventoryManager? _inventoryManager;

    public override void _Ready()
    {
        _head = GetNode<Node3D>("Head");
        _firstPersonCamera = GetNode<Camera3D>("Head/FirstPersonCamera");
        _thirdPersonCamera = GetNode<Camera3D>("ThirdPersonPivot/ThirdPersonCamera");
        RegisterInputActions();
        _inventoryManager = GetNodeOrNull<InventoryManager>("/root/InventoryManager");
        SetupHUD();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        UpdateCameraMode();
    }

    private void SetupHUD()
    {
        _hudCanvas = new CanvasLayer
        {
            Layer = 10
        };
        AddChild(_hudCanvas);

        // Interaction prompt (bottom center)
        _interactionLabel = new Label
        {
            AnchorsPreset = (int)Control.LayoutPreset.CenterBottom,
            OffsetTop = -60,
            OffsetBottom = -30,
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "[E] Interactuar"
        };
        _interactionLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 0.9f));
        _interactionLabel.AddThemeFontSizeOverride("font_size", 20);
        _hudCanvas.AddChild(_interactionLabel);
        _interactionLabel.Hide();

        // Controls hint (top right)
        _controlsLabel = new Label
        {
            AnchorsPreset = (int)Control.LayoutPreset.TopRight,
            OffsetLeft = -220,
            OffsetTop = 20,
            OffsetRight = -20,
            OffsetBottom = 80,
            HorizontalAlignment = HorizontalAlignment.Right,
            Text = "V — Cambiar cámara\nEsc — Liberar ratón"
        };
        _controlsLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 0.85f));
        _controlsLabel.AddThemeFontSizeOverride("font_size", 14);
        _hudCanvas.AddChild(_controlsLabel);

        // Inventory summary (top left)
        _inventoryLabel = new Label
        {
            AnchorsPreset = (int)Control.LayoutPreset.TopLeft,
            OffsetLeft = 20,
            OffsetTop = 20,
            OffsetRight = 220,
            OffsetBottom = 100
        };
        _inventoryLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.95f, 0.85f));
        _inventoryLabel.AddThemeFontSizeOverride("font_size", 14);
        _hudCanvas.AddChild(_inventoryLabel);

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
        var camera = GetActiveCamera();
        if (camera == null)
        {
            return;
        }

        var spaceState = GetWorld3D().DirectSpaceState;
        var from = camera.GlobalPosition;
        var to = from + (-camera.GlobalTransform.Basis.Z) * InteractionRange;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;

        var result = spaceState.IntersectRay(query);
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
        _cameraMode = _cameraMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;
        UpdateCameraMode();
    }

    private void UpdateCameraMode()
    {
        _firstPersonCamera.Current = _cameraMode == CameraMode.FirstPerson;
        _thirdPersonCamera.Current = _cameraMode == CameraMode.ThirdPerson;
    }
}
