extends SceneTree

var inventory
var economy
var greenhouse
var quickbar
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


func plant(station_name: String) -> Node:
	economy.call("BuyItem", "lettuce_seeds", 1)
	quickbar.call("Assign", 0, "lettuce_seeds")
	quickbar.call("Select", 0)
	var station = greenhouse.get_node(station_name)
	station.call("Interact")
	return station


func _run():
	quickbar = get_first_node_in_group("quickbar")
	var chest = greenhouse.get_node("Chest")
	var robot = get_first_node_in_group("robot")
	check(robot != null and chest != null, "robot y baúl del módulo base presentes")

	var mature = plant("GrowingStation0")
	mature.get("_plant").get("Data").set("Stage", 3)
	var young = plant("GrowingStation1")
	check(young.get("_plant") != null, "cultivo inmaduro plantado")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 5, "dos compras y dos siembras del jugador se compensan")

	var delivered = false
	for i in range(80):
		await create_timer(0.5).timeout
		if chest.call("Count", "lettuce_harvested") >= 1:
			delivered = true
			break
	check(delivered, "el robot cosecha, transporta y deposita la lechuga en el baúl")
	check(chest.call("Count", "lettuce_harvested") == 1, "el baúl contiene exactamente una unidad")
	check(inventory.call("GetQuantity", "lettuce_harvested") == 0, "la cosecha no duplica el inventario del jugador")
	check(mature.get("_plant") != null, "el robot replanta la estación cosechada")
	check(mature.get("_plant").get("Data").get("Stage") < 3, "la replantación es un cultivo nuevo sin madurar")
	check(young.get("_plant") != null and young.get("_plant").get("Data").get("IsMature") == false, "el robot no cosecha cultivos inmaduros")
	check(robot.get("CargoTotal") == 0, "la carga se vacía al depositar")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 5, "la semilla del robot no sale del inventario del jugador")

	var balance = economy.call("GetBalance")
	check(chest.call("TakeAll", "lettuce_harvested") == 1, "el jugador recoge la cosecha del baúl")
	check(inventory.call("GetQuantity", "lettuce_harvested") == 1, "el producto llega al inventario")
	check(economy.call("SellHarvest", "lettuce_harvested", 1) == true, "el producto se vende con la economía existente")
	check(economy.call("GetBalance") == balance + 12, "la venta suma el precio del cultivo")

	inventory.call("AddItem", "lettuce_harvested", 25)
	economy.call("SellLettuce", 25)
	check(economy.call("GetBalance") >= 250, "fondos suficientes para ampliar")
	check(expansion_purchase(0), "expansión con robot y baúl propios")
	await create_timer(0.1).timeout
	check(get_nodes_in_group("robot").size() == 2, "cada módulo tiene su robot")
	check(get_nodes_in_group("chest").size() == 2, "cada módulo tiene su baúl")

	print("Robot agent test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func expansion_purchase(direction: int) -> bool:
	return greenhouse.get_node("ExpansionManager").call("PurchaseFromBase", direction)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
