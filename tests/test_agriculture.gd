extends SceneTree

var scene
var station
var inventory
var simulator
var plant

func _init():
	# Autoloads are not added automatically when running --script, add them manually.
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	simulator = load("res://scripts/agriculture/GrowthSimulator.cs").new()
	simulator.name = "GrowthSimulator"
	get_root().add_child(simulator)

	scene = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(scene)

	station = scene.get_node_or_null("GrowingStation0")

	if station == null:
		printerr("Missing station for agriculture test")
		quit(1)
		return

	print("Starting agriculture test...")
	print("Station script: ", station.get_script())
	print("Initial lettuce_seeds: ", inventory.call("GetQuantity", "lettuce_seeds"))

	# Wait one frame so _Ready runs on all nodes
	call_deferred("_start")

func _start():
	# Plant
	print("Calling Interact...")
	station.call("Interact")
	print("Interact done")
	plant = station.get("_plant")
	if plant == null:
		printerr("Plant was not created")
		quit(1)
		return
	print("Plant created. Stage: ", plant.get("Data").get("Stage"))

	# Wait for growth ticks
	create_timer(6.0).timeout.connect(_check_growth)

func _check_growth():
	var data = plant.get("Data")
	print("After 6s - Stage: ", data.get("Stage"), " Progress: ", data.get("GrowthProgress"), " Water: ", data.get("Water"), " Nutrients: ", data.get("Nutrients"))

	# Ensure water and nutrients are full
	plant.call("AddWater", 100.0)
	plant.call("AddNutrients", 100.0)

	# Wait for maturity
	create_timer(30.0).timeout.connect(_check_mature)

func _check_mature():
	var data = plant.get("Data")
	print("After 26s - Stage: ", data.get("Stage"), " IsMature: ", data.get("IsMature"))

	if not data.get("IsMature"):
		printerr("Plant did not reach mature stage")
		quit(1)
		return

	# Harvest
	station.call("Interact")
	var harvested = get_root().get_node_or_null("InventoryManager").call("GetQuantity", "lettuce_harvested")
	print("Lettuce harvested: ", harvested)

	if harvested < 1:
		printerr("Harvest did not add lettuce to inventory")
		quit(1)
		return

	print("Agriculture test passed.")
	quit(0)
