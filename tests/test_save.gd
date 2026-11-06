extends SceneTree

var inventory
var economy
var simulator
var save
var greenhouse
var failures := 0
const PATH_A = "user://saves/test_save_a.json"
const PATH_B = "user://saves/test_save_b.json"


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
	check(economy.get("StartingBalance") == 100 and economy.call("GetBalance") == 100, "la partida nueva empieza con exactamente 100 $")

	greenhouse = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(greenhouse)
	current_scene = greenhouse
	await create_timer(0.3).timeout

	var quickbar = get_first_node_in_group("quickbar")
	var fleet = get_first_node_in_group("collector_fleet")
	var tomato = greenhouse.get_node("GrowingStation0")
	inventory.call("AddItem", "tomato_seeds", 1)
	quickbar.call("Assign", 0, "tomato_seeds")
	quickbar.call("Select", 0)
	tomato.call("Interact")
	tomato.get("_plant").get("Data").set("Water", 33.0)

	inventory.call("AddItem", "lettuce_harvested", 3000)
	economy.call("SellLettuce", 3000)
	greenhouse.get_node("Chest").call("Store", "lettuce_harvested", 7)
	check(greenhouse.get_node("ExpansionManager").call("PurchaseFromBase", 0), "ampliación comprada")
	await create_timer(0.2).timeout
	for node in get_nodes_in_group("chest"):
		if node != greenhouse.get_node("Chest"):
			node.call("Store", "lettuce_harvested", 3)
	fleet.call("Purchase")
	fleet.call("PurchaseAdministrator")
	await create_timer(0.2).timeout
	var collector = get_first_node_in_group("collector")
	collector.call("TogglePause")
	var admin = get_first_node_in_group("office_administrator")
	admin.call("SetMultiplier", 2)
	get_first_node_in_group("central_warehouse").call("Deposit", "lettuce_harvested", 4)
	var agro = get_first_node_in_group("robot")
	agro.call("LoadProgram", agro.get("DefaultProgram"))
	agro.call("Run")
	inventory.call("AddItem", "lettuce_seeds", 2)
	var balance_saved = economy.call("GetBalance")
	var seeds_saved = inventory.call("GetQuantity", "lettuce_seeds")

	check(save.call("WriteSlot", greenhouse, PATH_A), "la partida se guarda en user://saves")
	check(FileAccess.file_exists(PATH_A), "el archivo de partida existe")

	inventory.call("AddItem", "lettuce_seeds", 40)
	economy.call("TrySpend", "Prueba", "cambio posterior", 50)
	get_first_node_in_group("central_warehouse").call("Deposit", "lettuce_harvested", 99)
	collector.call("TogglePause")

	simulator.call("ClearPlants")
	greenhouse.queue_free()
	await create_timer(0.3).timeout
	economy.call("ResetToStart")
	inventory.call("ResetToDefaults")

	greenhouse = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(greenhouse)
	current_scene = greenhouse
	await create_timer(0.3).timeout
	check(save.call("ReadSlotInto", greenhouse, PATH_A), "la partida se carga en un mundo nuevo")
	await create_timer(0.3).timeout

	check(economy.call("GetBalance") == balance_saved, "el dinero se restaura exactamente")
	check(inventory.call("GetQuantity", "lettuce_seeds") == seeds_saved, "el inventario y las semillas se restauran sin duplicar")
	check(greenhouse.get_node("Chest").get("Total") == 7, "el baúl de la base recupera su contenido")
	check(get_nodes_in_group("chest").size() == 2, "el módulo ampliado se reconstruye con su baúl (2 baúles)")
	check(get_first_node_in_group("central_warehouse").get("Total") == 4, "el almacén central recupera sus productos sin duplicar")
	var tomato_data = greenhouse.get_node("GrowingStation0").get("_plant").get("Data")
	check(tomato_data != null and tomato_data.get("CropType") == "tomato" and is_equal_approx(tomato_data.get("Water"), 33.0), "la planta de tomate recupera su tipo y agua")
	check(get_nodes_in_group("robot").size() == 2 and get_nodes_in_group("collector").size() == 1 and get_nodes_in_group("office_administrator").size() == 1, "robots sin duplicar: 2 agrícolas, 1 recolector y 1 administrativo")
	var restored_collector = get_first_node_in_group("collector")
	check(restored_collector.get("IsPaused") == true, "el recolector sigue pausado tras cargar")
	check(get_first_node_in_group("office_administrator").get("Multiplier") == 2, "la configuración administrativa se restaura")
	check(get_first_node_in_group("robot").get("IsRunning") == true, "el programa agrícola vuelve a ejecutarse")

	var dir = DirAccess.open("user://saves")
	dir.copy(PATH_A, PATH_B)
	var broken = FileAccess.open(PATH_B, FileAccess.WRITE)
	broken.store_string("{ esto no es json")
	broken.close()
	check(save.call("ReadSlotInto", greenhouse, PATH_B) == false, "un archivo dañado no se carga y no altera la partida")
	check(get_nodes_in_group("robot").size() == 2, "tras un archivo dañado el mundo sigue intacto")

	dir.remove(PATH_A.get_file())
	dir.remove(PATH_B.get_file())
	print("Save test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
