extends SceneTree

var inventory
var economy
var greenhouse
var failures := 0


func _init():
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	economy = load("res://scripts/economy/EconomyManager.cs").new()
	economy.name = "EconomyManager"
	economy.set("StartingBalance", 40000)
	get_root().add_child(economy)

	var simulator = load("res://scripts/agriculture/GrowthSimulator.cs").new()
	simulator.name = "GrowthSimulator"
	get_root().add_child(simulator)

	greenhouse = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(greenhouse)

	call_deferred("_run")


func _run():
	await create_timer(0.3).timeout
	var fleet = get_first_node_in_group("collector_fleet")
	var player = get_first_node_in_group("player")
	check(player != null, "el jugador está en el invernadero")
	fleet.call("Purchase")
	fleet.call("PurchaseAdministrator")
	await create_timer(0.2).timeout

	var agro = get_first_node_in_group("robot")
	var collector = get_first_node_in_group("collector")
	var admin = get_first_node_in_group("office_administrator")
	check(collector != null and admin != null and agro != null, "existen los tres tipos de robot")

	for robot in [agro, collector, admin]:
		var label = str(robot.name)
		var panel = find_panel(robot)
		check(panel != null, label + ": tiene editor de programa")
		if panel == null:
			continue

		robot.call("Interact")
		check(panel.get_child(0).visible, label + ": E abre el editor")
		check(player.get("IsInputLocked") == true, label + ": el jugador queda bloqueado mientras el editor está abierto")
		check(Input.get_mouse_mode() == Input.MOUSE_MODE_VISIBLE, label + ": el cursor queda libre")

		robot.call("LoadProgram", "void loop() { delay(100000); }")
		panel.call("Close")
		check(not panel.get_child(0).visible, label + ": Cerrar oculta el editor")
		check(player.get("IsInputLocked") == false, label + ": al cerrar se recupera el control del jugador")
		check(robot.get("SavedProgram") == "void loop() { delay(100000); }", label + ": conserva su programa")

	print("Interaction test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func find_panel(node):
	for child in node.get_children():
		if child.get_script() != null and str(child.get_script().resource_path).ends_with("RobotPanel.cs"):
			return child
	return null


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
