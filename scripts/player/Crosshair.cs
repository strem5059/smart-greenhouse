using Godot;

namespace SmartGreenhouse.Player;

public partial class Crosshair : Control
{
    private const float Gap = 4.0f;
    private const float ArmLength = 6.0f;
    private const float Thickness = 2.0f;
    private const float OutlineExtra = 2.0f;
    private static readonly Color Tint = new(1.0f, 1.0f, 1.0f, 0.92f);
    private static readonly Color Outline = new(0.02f, 0.05f, 0.03f, 0.6f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        Vector2 c = Size / 2.0f;
        DrawArms(c, Outline, Thickness + OutlineExtra);
        DrawCircle(c, Thickness * 0.5f + OutlineExtra * 0.5f, Outline);
        DrawArms(c, Tint, Thickness);
        DrawCircle(c, Thickness * 0.5f, Tint);
    }

    private void DrawArms(Vector2 c, Color color, float width)
    {
        DrawLine(c + new Vector2(-Gap - ArmLength, 0), c + new Vector2(-Gap, 0), color, width);
        DrawLine(c + new Vector2(Gap, 0), c + new Vector2(Gap + ArmLength, 0), color, width);
        DrawLine(c + new Vector2(0, -Gap - ArmLength), c + new Vector2(0, -Gap), color, width);
        DrawLine(c + new Vector2(0, Gap), c + new Vector2(0, Gap + ArmLength), color, width);
    }
}
