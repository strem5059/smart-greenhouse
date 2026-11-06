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

	check(robot.call("LoadProgram", "void loop() { leerTemperatura(); }") == true, "función desconocida se detecta al ejecutar")
	robot.call("Run")
	await create_timer(0.3).timeout
	check(robot.get("StateName") == "Error", "función no admitida produce estado de error")

	robot.call("LoadProgram", "void loop() { configurarVelocidad(4.0); delay(100); }")
	robot.call("Run")
	await create_timer(0.3).timeout
	check(is_equal_approx(robot.get("Speed"), 4.0), "configurarVelocidad cambia la velocidad real")

	robot.call("LoadProgram", "void loop() { configurarVelocidad(99); delay(100); }")
	robot.call("Run")
	await create_timer(0.3).timeout
	check(is_equal_approx(robot.get("Speed"), 5.0), "la velocidad se limita a un valor seguro")

	quickbar.call("Assign", 0, "lettuce_seeds")
	quickbar.call("Select", 0)
	var station = greenhouse.get_node("GrowingStation0")
	station.call("Interact")
	station.get("_plant").get("Data").set("Stage", 3)

	robot.call("LoadProgram", "void loop() { recolectar(); delay(500); }")
	robot.call("Run")
	await create_timer(0.5).timeout
	check(robot.get("StateName") == "Error", "recolectar sin activarCosecha produce error")
	check(station.get("_plant") != null, "sin activarCosecha la planta madura no se cosecha")

	robot.call("LoadProgram", "void loop() { delay(10000); }")
	robot.call("Run")
	await create_timer(0.2).timeout
	check(robot.get("IsRunning") == true, "el programa se ejecuta")
	robot.call("StopProgram")
	check(robot.get("IsRunning") == false, "Detener para la ejecución")

	robot.call("LoadProgram", "void loop() { activarCosecha(); recolectar(); delay(10000); }")
	robot.call("Run")
	await create_timer(0.1).timeout
	robot.call("TogglePause")
	check(robot.get("IsPaused") == true and robot.get("StateName") == "Pausado", "pausar cambia el estado a Pausado")
	var paused_at = robot.position
	await create_timer(0.4).timeout
	check(robot.position.distance_to(paused_at) < 0.001, "el robot pausado no se mueve")
	check(station.get("_plant") != null, "el robot pausado no cosecha")
	robot.call("TogglePause")
	check(robot.get("IsPaused") == false and robot.get("StateName") != "Pausado", "reanudar quita la pausa")
	var harvested := false
	for i in range(60):
		await create_timer(0.1).timeout
		if station.get("_plant") == null:
			harvested = true
			break
	check(harvested, "tras reanudar el robot completa la cosecha")
	robot.call("StopProgram")
	check(robot.get("IsPaused") == false and robot.get("IsRunning") == false, "detener quita la pausa y para el programa")

	robot.call("SetCapacity", 50)
	check(robot.get("CapacityLimit") == 12, "la capacidad se limita al máximo de 12")
	robot.call("SetCapacity", 0)
	check(robot.get("CapacityLimit") == 1, "la capacidad se limita al mínimo de 1")
	robot.call("SetCapacity", 8)
	check(robot.get("CapacityLimit") == 8, "la capacidad es configurable")

	print("Robot test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
