extends SceneTree

# Crecimiento real con los tiempos aprobados: lechuga 21 s, espinaca 12 s,
# zanahoria 30 s, pepino 48 s y tomate 60 s. Duración total de la prueba: unos 65 s.

const PLAN = [
	{"seed": "lettuce_seeds", "product": "lettuce_harvested", "yield": 1, "station": "GrowingStation0"},
	{"seed": "carrot_seeds", "product": "carrot_harvested", "yield": 1, "station": "GrowingStation1"},
	{"seed": "cucumber_seeds", "product": "cucumber_harvested", "yield": 1, "station": "GrowingStation2"},
	{"seed": "spinach_seeds", "product": "spinach_harvested", "yield": 1, "station": "GrowingStation3"},
	{"seed": "tomato_seeds", "product": "tomato_harvested", "yield": 2, "station": "GrowingStation4"},
]

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


func plant(seed: String, station_path: String) -> Node:
	economy.call("BuyItem", seed, 1)
	quickbar.call("Assign", 0, seed)
	quickbar.call("Select", 0)
	var station = greenhouse.get_node(station_path)
	station.call("Interact")
	return station.get("_plant")


func _run():
	quickbar = get_first_node_in_group("quickbar")
	inventory.call("AddItem", "lettuce_harvested", 25)
	economy.call("SellLettuce", 25)

	# Expansión frontal para comprobar cultivos en una estación expandida.
	var expansion = greenhouse.get_node("ExpansionManager")
	check(expansion.call("PurchaseFromBase", 2) == true, "expansión disponible para la prueba")
	inventory.call("AddItem", "carrot_seeds", 1)

	var plants = []
	for entry in PLAN:
		plants.append(plant(entry["seed"], entry["station"]))
	var expanded = plant("carrot_seeds", "Module1/GrowingStation24")
	check(expanded != null, "zanahoria plantada en estación expandida")
	check(inventory.call("GetQuantity", "fertilizer") == 5, "fertilizante inicial sin consumo")

	await create_timer(20.0).timeout
	var lettuce = plants[0].get("Data")
	var tomato = plants[4].get("Data")
	check(abs(lettuce.get("Nutrients") - 40.0) < 3.0, "lechuga consume 0,5 nutrientes por segundo")
	check(abs(tomato.get("Nutrients") - 30.0) < 3.0, "tomate consume 1,0 nutriente por segundo")

	await create_timer(15.0).timeout
	greenhouse.get_node("GrowingStation4").call("Interact")
	check(tomato.get("Water") > 40.0, "tomate regado a mitad de ciclo")

	await create_timer(10.0).timeout
	greenhouse.get_node("GrowingStation4").call("Interact")
	check(inventory.call("GetQuantity", "fertilizer") == 4, "tomate consume un fertilizante")

	await create_timer(22.0).timeout
	var all_plants = plants.duplicate()
	all_plants.append(expanded)
	for i in range(all_plants.size()):
		var plant_node = all_plants[i]
		var name = PLAN[i]["station"] if i < PLAN.size() else "Module1/GrowingStation24"
		check(plant_node.get("Data").get("IsMature") == true, name + ": cultivo maduro en su tiempo")

	for i in range(PLAN.size()):
		var entry = PLAN[i]
		var before = inventory.call("GetQuantity", entry["product"])
		greenhouse.get_node(entry["station"]).call("Interact")
		check(inventory.call("GetQuantity", entry["product"]) == before + entry["yield"], entry["station"] + ": rendimiento cosechado")
		check(greenhouse.get_node(entry["station"]).get("_plant") == null, entry["station"] + ": estación libre tras cosechar")

	var expanded_before = inventory.call("GetQuantity", "carrot_harvested")
	greenhouse.get_node("Module1/GrowingStation24").call("Interact")
	check(inventory.call("GetQuantity", "carrot_harvested") == expanded_before + 1, "zanahoria de estación expandida cosechada")

	print("Growth test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
