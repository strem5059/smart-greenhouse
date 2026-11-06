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
	economy.set("StartingBalance", 20000)
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
	check(fleet != null, "la flota de recolectores existe en el invernadero")
	check(get_nodes_in_group("collector").size() == 0, "no hay recolectores gratuitos al iniciar")
	check(get_nodes_in_group("office_administrator").size() == 0, "no hay robot administrativo gratuito al iniciar")

	var before = economy.call("GetBalance")
	check(fleet.call("Purchase") == true, "con dinero suficiente se compra el recolector")
	check(economy.call("GetBalance") == before - 12000, "la compra cuesta 12000 $")
	await create_timer(0.2).timeout
	var collector = get_first_node_in_group("collector")
	var warehouse = get_first_node_in_group("central_warehouse")
	check(collector != null and collector.get("IsActive") == true, "el recolector comprado arranca su programa")
	check(collector.get("Capacity") == 30, "capacidad inicial 30")
	check(is_equal_approx(collector.get("Speed"), 3.0), "velocidad inicial 3 m/s")

	inventory.call("AddItem", "lettuce_harvested", 200)
	economy.call("SellLettuce", 200)
	check(greenhouse.get_node("ExpansionManager").call("PurchaseFromBase", 0), "ampliación con baúl propio")
	await create_timer(0.2).timeout

	var base_chest = greenhouse.get_node("Chest")
	var module_chest = null
	for node in get_nodes_in_group("chest"):
		if node != base_chest:
			module_chest = node
	check(module_chest != null, "el módulo ampliado tiene baúl")

	base_chest.call("Store", "lettuce_harvested", 40)
	module_chest.call("Store", "lettuce_harvested", 10)
	var total := 50
	var max_cargo := 0
	var conserved := true
	var elapsed := 0.0
	while warehouse.call("Count", "lettuce_harvested") < total and elapsed < 150.0:
		await create_timer(0.1).timeout
		elapsed += 0.1
		max_cargo = max(max_cargo, collector.get("CargoTotal"))
		if units(base_chest, module_chest, collector, warehouse) != total:
			conserved = false
	check(conserved, "las unidades se conservan en todo momento (baúles + carga + almacén)")
	check(warehouse.get("Total") == total, "todas las unidades llegan al almacén central (" + str(warehouse.get("Total")) + "/50)")
	check(max_cargo == 30, "la carga llega a la capacidad de 30 y no la supera (máximo " + str(max_cargo) + ")")
	check(base_chest.get("Total") == 0 and module_chest.get("Total") == 0, "se vacían los baúles de la base y del módulo")
	check(collector.get("CargoTotal") == 0, "la carga se descarga al final")

	collector.call("LoadProgram", "void loop() { configurarVelocidad(4.0); configurarCapacidad(10); delay(100000); }")
	collector.call("Run")
	await create_timer(0.3).timeout
	check(is_equal_approx(collector.get("Speed"), 4.0) and collector.get("Capacity") == 10, "configurarVelocidad y configurarCapacidad actúan en el robot")
	collector.call("SetCapacity", 30)
	collector.call("StopProgram")

	collector.position = Vector3(0, 0, 0)
	collector.call("LoadProgram", "void loop() { depositar(); }")
	collector.call("Run")
	await create_timer(0.3).timeout
	check(collector.get("StateName") == "Error", "depositar() fuera de la oficina produce error y detiene el programa")
	collector.call("LoadProgram", collector.get("DefaultProgram"))
	collector.call("Run")

	base_chest.call("Store", "lettuce_harvested", 40)
	await create_timer(2.5).timeout
	var moving_from = collector.position
	await create_timer(0.5).timeout
	check(collector.position.distance_to(moving_from) > 0.1, "con nuevas existencias el programa vuelve a mover el robot")

	collector.call("TogglePause")
	var paused_at = collector.position
	var paused_cargo = collector.get("CargoTotal")
	await create_timer(0.5).timeout
	check(collector.get("StateName") == "Pausado" and collector.position.distance_to(paused_at) < 0.001, "pausado: el robot no se mueve")
	check(collector.get("CargoTotal") == paused_cargo, "pausado: conserva la carga")
	collector.call("TogglePause")
	await create_timer(0.3).timeout

	collector.call("StopProgram")
	var stopped_at = collector.position
	await create_timer(0.5).timeout
	check(collector.get("StateName") == "Detenido" and collector.position.distance_to(stopped_at) < 0.001, "detenido: el robot no se mueve")
	check(units(base_chest, module_chest, collector, warehouse) == 90, "detenido: las unidades siguen contabilizadas")

	collector.call("Run")
	elapsed = 0.0
	conserved = true
	while warehouse.get("Total") < 90 and elapsed < 150.0:
		await create_timer(0.1).timeout
		elapsed += 0.1
		if units(base_chest, module_chest, collector, warehouse) != 90:
			conserved = false
	check(conserved, "tras reanudar, las unidades siguen conservadas")
	check(warehouse.get("Total") == 90, "tras reanudar, el almacén recibe las 40 unidades restantes (" + str(warehouse.get("Total")) + "/90)")

	print("Collector test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func units(base_chest, module_chest, collector, warehouse) -> int:
	return base_chest.get("Total") + module_chest.get("Total") + collector.get("CargoTotal") + warehouse.get("Total")


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
