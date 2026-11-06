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

	print("Robot test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
