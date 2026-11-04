using Godot;

namespace SmartGreenhouse.Player;

public partial class Crosshair : Control
{
    private const float Gap = 4.0f;
    private const float ArmLength = 6.0f;
    private const float Thickness = 2.0f;
    private static readonly Color Tint = new(1.0f, 1.0f, 1.0f, 0.9f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        Vector2 c = Size / 2.0f;
        DrawCircle(c, Thickness * 0.5f, Tint);
        DrawLine(c + new Vector2(-Gap - ArmLength, 0), c + new Vector2(-Gap, 0), Tint, Thickness);
        DrawLine(c + new Vector2(Gap, 0), c + new Vector2(Gap + ArmLength, 0), Tint, Thickness);
        DrawLine(c + new Vector2(0, -Gap - ArmLength), c + new Vector2(0, -Gap), Tint, Thickness);
        DrawLine(c + new Vector2(0, Gap), c + new Vector2(0, Gap + ArmLength), Tint, Thickness);
    }
}
