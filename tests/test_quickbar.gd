extends SceneTree

var inventory
var greenhouse
var quickbar
var failures := 0


func _init():
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	var simulator = load("res://scripts/agriculture/GrowthSimulator.cs").new()
	simulator.name = "GrowthSimulator"
	get_root().add_child(simulator)

	greenhouse = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(greenhouse)

	call_deferred("_run")


func _run():
	quickbar = get_first_node_in_group("quickbar")
	check(quickbar != null, "barra rápida presente en el jugador")
	if quickbar == null:
		quit(1)
		return

	check(quickbar.call("Assign", 0, "lettuce_seeds") == true, "asignar semillas a la ranura 1")
	check(quickbar.call("Assign", 1, "fertilizer") == false, "no asignar fertilizante")
	check(not quickbar.call("GetSlot", 1), "ranura 2 sigue vacía")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 5, "asignar no consume semillas")

	quickbar.call("Select", 1)
	check(not quickbar.get("EquippedItemId"), "ranura vacía no equipa nada")
	quickbar.call("Select", 0)
	check(quickbar.get("EquippedItemId") == "lettuce_seeds", "ranura 1 equipa lechuga")

	greenhouse.get_node("GrowingStation0").call("Interact")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 4, "plantar descuenta exactamente una semilla")
	check(quickbar.get("EquippedItemId") == "lettuce_seeds", "la selección se conserva tras plantar")

	inventory.call("RemoveItem", "lettuce_seeds", 4)
	check(quickbar.call("GetSlot", 0) == "lettuce_seeds", "la ranura conserva el tipo con cantidad cero")
	check(quickbar.call("Assign", 2, "lettuce_seeds") == false, "sin existencias no se puede asignar")

	inventory.call("AddItem", "lettuce_seeds", 4)
	quickbar.call("Swap", 0, 2)
	check(quickbar.call("GetSlot", 2) == "lettuce_seeds" and not quickbar.call("GetSlot", 0), "intercambio entre ranuras")
	quickbar.call("Select", 2)
	check(quickbar.get("SelectedIndex") == 2, "selección de la ranura 3")

	var empty_station = greenhouse.get_node("GrowingStation1")
	quickbar.call("Select", 1)
	empty_station.call("Interact")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 4, "sin semilla equipada no se consume nada")
	check(empty_station.get("_plant") == null, "sin semilla equipada no se planta")
	check(empty_station.call("GetInteractionPrompt") == "[E] Sin semilla equipada", "mensaje sin semilla equipada")
	quickbar.call("Select", 2)
	check(empty_station.call("GetInteractionPrompt") == "[E] Plantar lechuga", "mensaje con lechuga equipada")

	print("Quickbar test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
