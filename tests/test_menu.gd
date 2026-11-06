extends SceneTree

var inventory
var economy
var simulator
var save
var failures := 0


func _init():
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	economy = load("res://scripts/economy/EconomyManager.cs").new()
	economy.name = "EconomyManager"
	get_root().add_child(economy)

	simulator = load("res://scripts/agriculture/GrowthSimulator.cs").new()
	simulator.name = "GrowthSimulator"
	get_root().add_child(simulator)

	save = load("res://scripts/save/SaveManager.cs").new()
	save.name = "SaveManager"
	get_root().add_child(save)

	call_deferred("_run")


func _run():
	var menu = load("res://scenes/menu/main_menu.tscn").instantiate()
	get_root().add_child(menu)
	current_scene = menu
	await create_timer(0.6).timeout

	check(Input.mouse_mode == Input.MOUSE_MODE_VISIBLE, "el cursor está visible en el menú principal")
	check(find_script(menu, "GameHud.cs") == null, "el menú no muestra el HUD del juego")
	check(find_script(menu, "PlayerController.cs") == null, "el menú no contiene jugador")
	check(get_nodes_in_group("player").size() == 0, "no hay jugador activo en el menú")

	save.call("StartNewGame")
	var elapsed := 0.0
	while elapsed < 5.0 and (current_scene == null or current_scene.scene_file_path != "res://scenes/world/greenhouse.tscn"):
		await create_timer(0.1).timeout
		elapsed += 0.1
	check(current_scene != null and current_scene.scene_file_path == "res://scenes/world/greenhouse.tscn", "Nueva partida abre el invernadero")
	await create_timer(0.8).timeout
	check(economy.call("GetBalance") == 100, "la nueva partida empieza con exactamente 100 $")
	check(get_nodes_in_group("player").size() == 1, "al iniciar la partida el jugador está activo")
	check(find_script(current_scene, "GameHud.cs") != null, "al iniciar la partida se muestra el HUD")
	var player = get_first_node_in_group("player")
	check(player != null and player.get("IsInputLocked") == false, "al iniciar la partida los controles del jugador están activos")

	print("Menu test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func find_script(node, script_name: String):
	if node.get_script() != null and str(node.get_script().resource_path).ends_with(script_name):
		return node
	for child in node.get_children():
		var found = find_script(child, script_name)
		if found != null:
			return found
	return null


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
