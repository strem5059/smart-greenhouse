using Godot;
using SmartGreenhouse.Interfaces;
using SmartGreenhouse.Management;
using SmartGreenhouse.Player;

namespace SmartGreenhouse.Office;

public partial class ComputerInteraction : StaticBody3D, IInteractable
{
    private const float ScreenTime = 0.3f;
    private const float ScreenMaxEnergy = 1.4f;

    private ManagementPanel _panel = null!;
    private StandardMaterial3D? _screenMaterial;
    private Tween? _screenTween;

    public override void _Ready()
    {
        _screenMaterial = GetNodeOrNull<MeshInstance3D>("Screen")?.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
        SetScreenEnergy(0.0f);

        _panel = new ManagementPanel();
        AddChild(_panel);
        _panel.Closed += () => TweenScreen(0.0f);
    }

    public string GetInteractionPrompt()
    {
        return "[E] Abrir administración";
    }

    public void Interact()
    {
        if (_panel.State != ManagementPanel.PanelState.Closed)
        {
            return;
        }

        var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _screenTween?.Kill();
        _screenTween = CreateTween();
        _screenTween.TweenMethod(Callable.From<float>(SetScreenEnergy), 0.0f, ScreenMaxEnergy, ScreenTime);
        _screenTween.TweenCallback(Callable.From(() => _panel.Open(player)));
    }

    private void TweenScreen(float target)
    {
        _screenTween?.Kill();
        _screenTween = CreateTween();
        _screenTween.TweenMethod(Callable.From<float>(SetScreenEnergy), _screenMaterial?.EmissionEnergyMultiplier ?? 0.0f, target, ScreenTime);
    }

    private void SetScreenEnergy(float energy)
    {
        if (_screenMaterial == null)
        {
            return;
        }

        _screenMaterial.EmissionEnabled = energy > 0.0f;
        _screenMaterial.EmissionEnergyMultiplier = energy;
    }
}
