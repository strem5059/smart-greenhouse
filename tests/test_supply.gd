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
	economy.set("StartingBalance", 30000)
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
	var quickbar = get_first_node_in_group("quickbar")
	var agro = get_first_node_in_group("robot")
	check(fleet.call("PurchaseAdministrator") == true, "se compra el robot administrativo")
	await create_timer(0.2).timeout
	var admin = get_first_node_in_group("office_administrator")

	inventory.call("AddItem", "tomato_seeds", 1)
	inventory.call("AddItem", "lettuce_seeds", 1)
	var tomato = greenhouse.get_node("GrowingStation0")
	var lettuce = greenhouse.get_node("GrowingStation1")
	quickbar.call("Assign", 0, "tomato_seeds")
	quickbar.call("Select", 0)
	tomato.call("Interact")
	quickbar.call("Assign", 0, "lettuce_seeds")
	quickbar.call("Select", 0)
	lettuce.call("Interact")
	var tomato_data = tomato.get("_plant").get("Data")
	var lettuce_data = lettuce.get("_plant").get("Data")
	check(tomato_data != null and lettuce_data != null, "se plantan un tomate y una lechuga en el mismo módulo")

	tomato_data.set("Water", 0.0)
	tomato_data.set("Nutrients", 0.0)
	lettuce_data.set("Water", 0.0)
	lettuce_data.set("Nutrients", 0.0)
	set_stock("fertilizer", 0)

	agro.call("LoadProgram", agro.get("DefaultProgram"))
	agro.call("Run")
	var elapsed := 0.0
	while tomato_data.get("Nutrients") < 20.0 and elapsed < 40.0:
		await create_timer(0.2).timeout
		elapsed += 0.2
	check(tomato_data.get("Nutrients") >= 20.0, "el tomate que lo necesita recibe fertilizante")
	check(tomato_data.get("Water") >= 20.0, "el tomate que lo necesita recibe agua")
	check(lettuce_data.get("Water") == 0.0 and lettuce_data.get("Nutrients") == 0.0, "la lechuga, que no necesita recursos, no recibe nada")
	check(agro.get("FertilizerStock") == 0, "el fertilizante recibido se aplica por completo a la planta")
	check(fleet.get("RequestCount") == 0, "la solicitud se completa y se cierra")
	check(agro.get("IsRunning") == true, "el programa continúa tras recibir el fertilizante")

	tomato_data.set("Nutrients", 0.0)
	set_stock("fertilizer", 0)
	admin.call("TogglePause")
	agro.call("Run")
	await create_timer(1.0).timeout
	check(fleet.get("RequestCount") == 1, "sin administrador activo la solicitud queda pendiente")
	agro.call("StopProgram")
	check(fleet.get("RequestCount") == 0, "detener el robot cancela su solicitud sin duplicarla")
	admin.call("TogglePause")

	print("Supply test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func set_stock(item_id: String, amount: int) -> void:
	var current = inventory.call("GetQuantity", item_id)
	if current > amount:
		inventory.call("RemoveItem", item_id, current - amount)
	elif current < amount:
		inventory.call("AddItem", item_id, amount - current)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
