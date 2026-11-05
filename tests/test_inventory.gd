extends SceneTree

var inventory
var screen
var failures := 0


func _init():
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	screen = load("res://scripts/interfaces/InventoryScreen.cs").new()
	get_root().add_child(screen)

	call_deferred("_run")


func _run():
	get_root().size = Vector2i(1280, 720)
	check(inventory.call("GetQuantity", "lettuce_seeds") == 5, "semillas iniciales 5")
	check(screen.get("IsOpen") == false, "inventario cerrado al inicio")
	check(screen.visible == false, "inventario oculto al inicio")

	screen.call("Open")
	check(screen.get("IsOpen") == true, "Open abre el inventario")
	check(screen.visible == true, "inventario visible al abrir")
	await process_frame
	await process_frame
	var root = screen.get_child(0)
	var panel = root.get_child(1)
	check(root.mouse_filter == Control.MOUSE_FILTER_IGNORE, "la raíz del inventario no intercepta el ratón")
	check(panel.get_global_rect().end.y <= 720 - 150, "el panel no tapa la zona de la barra rápida en 1280x720")
	screen.call("Open")
	check(screen.get("IsOpen") == true, "Open repetido no altera el estado")

	screen.call("Close")
	check(screen.get("IsOpen") == false, "Close cierra el inventario")

	inventory.call("AddItem", "fertilizer", 2)
	check(inventory.call("GetQuantity", "fertilizer") == 7, "AddItem suma sin duplicar")
	inventory.call("RemoveItem", "fertilizer", 2)
	check(inventory.call("GetQuantity", "fertilizer") == 5, "RemoveItem resta sin afectar a otros objetos")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 5, "otras cantidades intactas")

	print("Inventory test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
