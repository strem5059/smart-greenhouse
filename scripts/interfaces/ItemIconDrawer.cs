using Godot;
using SmartGreenhouse.Inventory;

namespace SmartGreenhouse.Interfaces;

public static class ItemIconDrawer
{
	public static void Draw(CanvasItem canvas, ItemDefinition item, Vector2 size)
	{
		Vector2 c = size / 2.0f + new Vector2(0, -6);
		float r = Mathf.Min(size.X, size.Y) * 0.2f;
		switch (item.Category)
		{
			case ItemCategory.Seeds:
				canvas.DrawCircle(c + new Vector2(-r * 0.6f, 0), r * 0.55f, item.IconColor);
				canvas.DrawCircle(c + new Vector2(r * 0.6f, r * 0.3f), r * 0.55f, item.IconColor);
				break;
			case ItemCategory.Crops:
				canvas.DrawCircle(c, r * 1.2f, item.IconColor);
				canvas.DrawCircle(c + new Vector2(0, -r * 0.3f), r * 0.6f, new Color(1, 1, 1, 0.25f));
				break;
			default:
				canvas.DrawRect(new Rect2(c - new Vector2(r, r), new Vector2(r * 2, r * 2)), item.IconColor);
				canvas.DrawRect(new Rect2(c + new Vector2(-r, r * 0.3f), new Vector2(r * 2, r * 0.5f)), new Color(0, 0, 0, 0.25f));
				break;
		}
	}
}
