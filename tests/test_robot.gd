extends SceneTree

var inventory
var economy
var greenhouse
var robot
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


func _run():
	robot = get_first_node_in_group("robot")
	quickbar = get_first_node_in_group("quickbar")
	check(robot != null, "robot agrícola presente en el invernadero")
	if robot == null:
		quit(1)
		return

	check(robot.call("LoadProgram", "void loop() { x = 1 }") == false, "programa con error de sintaxis rechazado")
	check(robot.get("ErrorLine") == 1, "el error indica la línea")
	check(robot.call("LoadProgram", "void loop() { delay(100); }") == true, "programa válido aceptado")

	check(robot.call("LoadProgram", "void loop() { leerTemperatura(); }") == true, "función desconocida se detecta al ejecutar")
	robot.call("Run")
	await create_timer(0.3).timeout
	check(robot.get("StateName") == "Error", "función no admitida produce estado de error")
	check(robot.get("IsRunning") == false, "el programa se detiene al fallar")

	# Cultivo de lechuga bajo el robot: sensor y actuador reales.
	quickbar.call("Assign", 0, "lettuce_seeds")
	quickbar.call("Select", 0)
	var station = greenhouse.get_node("GrowingStation0")
	station.call("Interact")
	var crop = station.get("_plant").get("Data")
	check(crop != null, "cultivo disponible para el robot")

	robot.call("LoadProgram", "void loop() {\n float h = leerHumedad();\n if (h < 30) { activarRiego(); } else { desactivarRiego(); }\n delay(100);\n}")
	crop.set("Water", 10.0)
	robot.call("Run")
	await create_timer(1.0).timeout
	var water_irrigated = crop.get("Water")
	check(water_irrigated > 10.0, "riego activado: el cultivo gana agua")
	check(robot.get("StateName") == "Ejecutando" or robot.get("StateName") == "Esperando", "estado de ejecución visible")

	robot.call("StopProgram")
	check(robot.get("IsRunning") == false, "el programa se detiene")
	var water_at_stop = crop.get("Water")
	await create_timer(1.2).timeout
	check(crop.get("Water") < water_at_stop, "tras detener, el riego queda desactivado")

	crop.set("Water", 80.0)
	robot.call("Run")
	await create_timer(0.5).timeout
	check(crop.get("Water") < 80.0, "humedad por encima del umbral: no se riega")
	robot.call("StopProgram")

	print("Robot test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
