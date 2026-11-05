extends SceneTree

var inventory
var economy
var greenhouse
var expansion
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


func _run():
	expansion = greenhouse.get_node("ExpansionManager")
	var quickbar = get_first_node_in_group("quickbar")
	quickbar.call("Assign", 0, "lettuce_seeds")
	quickbar.call("Select", 0)

	inventory.call("AddItem", "lettuce_harvested", 75)
	economy.call("SellLettuce", 75)
	check(economy.call("GetBalance") == 1000, "saldo 1000 para tres ampliaciones")

	check(expansion.call("PurchaseFromBase", 2) == true, "ampliación frontal desde la base (Module1)")
	check(expansion.call("PurchaseFrom", 1, 1) == true, "ampliación derecha encadenada desde Module1 (Module2)")
	check(expansion.call("PurchaseFrom", 0, 0) == true, "ampliación izquierda desde la base (Module3)")
	check(economy.call("GetBalance") == 250, "cada ampliación se cobra una sola vez")
	check(expansion.get("ModuleCount") == 4, "cuatro módulos en total")
	check(expansion.get("CanExpand") == false, "límite de beta alcanzado")
	check(expansion.call("PurchaseFrom", 3, 3) == false, "no se compra fuera del límite")
	check(economy.call("GetBalance") == 250, "sin cobro fuera del límite")

	await process_frame
	await process_frame

	check(absent("Module1/GlassWallRightFull"), "Module1 pierde la cara compartida con Module2")
	check(present("Module2/GlassWallRightFull"), "Module2 conserva pared este completa")
	check(absent("Module2/GlassWallLeft"), "Module2 no duplica la cara compartida con Module1")
	check(absent("Module3/GlassWallRightFull"), "Module3 no duplica la cara compartida con la base")
	check(present("Module3/GlassWallLeft"), "Module3 conserva su pared izquierda completa")
	check(absent("GlassWallFront"), "la base retira su cara frontal al conectar Module1")
	check(present("GlassWallRightN") and present("GlassWallRightS"), "la base conserva el hueco de la oficina")

	var chained_station = greenhouse.get_node("Module2/GrowingStation48")
	chained_station.call("Interact")
	check(chained_station.get("_plant") != null, "estación de un módulo encadenado planta")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 4, "la siembra encadenada consume una semilla")

	print("Expansion chain test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func present(path: String) -> bool:
	var node = greenhouse.get_node_or_null(path)
	return node != null and not node.is_queued_for_deletion()


func absent(path: String) -> bool:
	return not present(path)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
