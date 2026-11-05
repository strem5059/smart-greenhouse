extends SceneTree

var inventory
var economy
var greenhouse
var panel
var failures := 0


func _init():
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	economy = load("res://scripts/economy/EconomyManager.cs").new()
	economy.name = "EconomyManager"
	get_root().add_child(economy)

	var simulator = load("res://scripts/agriculture/GrowthSimulator.cs").new()
	simulator.name = "GrowthSimulator"
	get_root().add_child(simulator)

	greenhouse = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(greenhouse)

	call_deferred("_run")


func find_by_script(node: Node, script_name: String) -> Node:
	if node.get_script() != null and str(node.get_script().resource_path).ends_with(script_name):
		return node
	for child in node.get_children():
		var found = find_by_script(child, script_name)
		if found != null:
			return found
	return null


func _run():
	panel = find_by_script(greenhouse, "ManagementPanel.cs")
	check(panel != null, "panel administrativo presente")
	if panel == null:
		quit(1)
		return

	var sizes = [Vector2i(1152, 648), Vector2i(1280, 720), Vector2i(1600, 900)]
	for size in sizes:
		get_root().size = size
		var label = "%dx%d" % [size.x, size.y]

		panel.call("Open", null)
		await create_timer(0.8).timeout
		check(panel.get("State") == 2, "panel abierto en " + label)
		check(economy.call("GetBalance") == 100, "abrir el panel no modifica el saldo en " + label)

		var frame = panel.get_child(0).get_child(1)
		var rect = frame.get_global_rect()
		check(rect.position.x >= 0 and rect.position.y >= 0 and rect.end.x <= size.x and rect.end.y <= size.y, "marco dentro de la ventana en " + label)

		panel.call("Close")
		await create_timer(0.8).timeout
		check(panel.get("State") == 0, "panel cerrado en " + label)

	print("Management panel test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
