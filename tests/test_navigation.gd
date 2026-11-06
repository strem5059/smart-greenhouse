extends SceneTree

var inventory
var economy
var greenhouse
var quickbar
var failures := 0
const EXAMPLE = "void loop() {\n configurarVelocidad(3.0);\n if (hayPlantasEnModulo()) {\n activarCosecha();\n recolectar();\n replantar();\n moverABaul();\n }\n delay(1000);\n}\n"


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


func plant_mature(station_path: String) -> void:
	economy.call("BuyItem", "lettuce_seeds", 1)
	quickbar.call("Assign", 0, "lettuce_seeds")
	quickbar.call("Select", 0)
	var station = greenhouse.get_node(station_path)
	station.call("Interact")
	station.get("_plant").get("Data").set("Stage", 3)


# Cuenta muestras de la posición del robot que invaden obstáculos o salen de su módulo.
func violations(robot: Node3D, parent: Node) -> int:
	var p = robot.position
	var bad := 0
	if abs(p.x) > 9.85 or abs(p.z) > 5.85:
		bad += 1
	for child in parent.get_children():
		if child == robot or not (child is Node3D):
			continue
		var n: String = child.name
		var hx := 0.0
		var hz := 0.0
		if n.begins_with("Table"):
			hx = 1.4
			hz = 0.5
		elif n.begins_with("Channel"):
			hx = 1.3
			hz = 0.35
		elif n.begins_with("GrowingStation"):
			hx = 0.25
			hz = 0.25
		elif n == "Chest":
			hx = 0.5
			hz = 0.35
		else:
			continue
		if abs(p.x - child.position.x) < hx + 0.4 and abs(p.z - child.position.z) < hz + 0.4:
			bad += 1
	return bad


func _run():
	quickbar = get_first_node_in_group("quickbar")
	var chest = greenhouse.get_node("Chest")
	var robot = greenhouse.get_node("AgroRobot")

	# Cultivos maduros en extremos opuestos del invernadero: la ruta debe cruzar los pasillos.
	plant_mature("GrowingStation0")
	plant_mature("GrowingStation12")
	robot.call("LoadProgram", EXAMPLE)
	robot.call("Run")

	var bad := 0
	for i in range(900):
		await create_timer(0.1).timeout
		bad += violations(robot, greenhouse)
		if chest.call("Count", "lettuce_harvested") >= 2:
			break
	check(chest.call("Count", "lettuce_harvested") == 2, "el robot llega a ambas estaciones y deposita la cosecha")
	check(bad == 0, "el robot no atraviesa bases, plantas, baúles ni paredes (" + str(bad) + " muestras con colisión)")

	inventory.call("AddItem", "lettuce_harvested", 25)
	economy.call("SellLettuce", 25)
	check(greenhouse.get_node("ExpansionManager").call("PurchaseFromBase", 0) == true, "expansión para la prueba del módulo")
	await create_timer(0.2).timeout

	plant_mature("Module1/GrowingStation24")
	var module = greenhouse.get_node("Module1")
	var module_robot = null
	for node in get_nodes_in_group("robot"):
		if node.get_parent() == module:
			module_robot = node
	check(module_robot != null, "el módulo expandido tiene su robot")
	module_robot.call("LoadProgram", EXAMPLE)
	module_robot.call("Run")
	var module_chest = module.get_node("Chest")
	var module_bad := 0
	for i in range(600):
		await create_timer(0.1).timeout
		module_bad += violations(module_robot, module)
		if module_chest.call("Count", "lettuce_harvested") >= 1:
			break
	check(module_chest.call("Count", "lettuce_harvested") == 1, "el robot del módulo expandido cosecha y deposita dentro de su módulo")
	check(module_bad == 0, "el robot del módulo expandido no atraviesa obstáculos ni sale del módulo (" + str(module_bad) + " muestras)")

	print("Navigation test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
