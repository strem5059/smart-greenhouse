extends SceneTree

# Identificadores esperados de cada cultivo: semilla, producto y nombre.
const CROPS = [
	{"id": "lettuce", "seed": "lettuce_seeds", "product": "lettuce_harvested", "name": "lechuga", "yield": 1},
	{"id": "carrot", "seed": "carrot_seeds", "product": "carrot_harvested", "name": "zanahoria", "yield": 1},
	{"id": "tomato", "seed": "tomato_seeds", "product": "tomato_harvested", "name": "tomate", "yield": 2},
	{"id": "cucumber", "seed": "cucumber_seeds", "product": "cucumber_harvested", "name": "pepino", "yield": 1},
	{"id": "spinach", "seed": "spinach_seeds", "product": "spinach_harvested", "name": "espinaca", "yield": 1},
]
const MATURE_STAGE = 3

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


func _run():
	quickbar = get_first_node_in_group("quickbar")
	check(quickbar != null, "barra rápida disponible")

	for i in range(CROPS.size()):
		var crop = CROPS[i]
		var seed = crop["seed"]
		var product = crop["product"]
		var station = greenhouse.get_node("GrowingStation%d" % i)

		check(economy.call("GetPrice", seed) > 0, crop["name"] + ": semilla a la venta")
		var balance_before = economy.call("GetBalance")
		var seeds_before = inventory.call("GetQuantity", seed)
		check(economy.call("BuyItem", seed, 1) == true, crop["name"] + ": compra de una semilla")
		check(inventory.call("GetQuantity", seed) == seeds_before + 1, crop["name"] + ": semilla en inventario")
		check(economy.call("GetBalance") == balance_before - economy.call("GetPrice", seed), crop["name"] + ": descuento correcto")

		check(quickbar.call("Assign", 0, seed) == true, crop["name"] + ": asignación a Quickbar")
		quickbar.call("Select", 0)
		check(quickbar.get("EquippedItemId") == seed, crop["name"] + ": semilla equipada")
		check(station.call("GetInteractionPrompt") == "[E] Plantar " + crop["name"], crop["name"] + ": mensaje de plantación")

		var seeds_before_plant = inventory.call("GetQuantity", seed)
		station.call("Interact")
		var plant = station.get("_plant")
		check(plant != null, crop["name"] + ": planta creada")
		check(inventory.call("GetQuantity", seed) == seeds_before_plant - 1, crop["name"] + ": consume exactamente una semilla")
		if plant != null:
			check(plant.get("Data").get("CropType") == crop["id"], crop["name"] + ": tipo de cultivo correcto")
			check(plant.get_node("SeedVisual").visible == true, crop["name"] + ": etapa inicial visible")
			check(plant.get_node("MatureVisual").get_child_count() > 0, crop["name"] + ": visual de madurez con geometría")

		# Madurez forzada: prueba la cosecha sin esperar el crecimiento completo.
		if plant != null:
			plant.get("Data").set("Stage", MATURE_STAGE)
		var product_before = inventory.call("GetQuantity", product)
		var seeds_before_harvest = inventory.call("GetQuantity", seed)
		station.call("Interact")
		check(station.get("_plant") == null, crop["name"] + ": la estación queda libre tras cosechar")
		check(inventory.call("GetQuantity", product) == product_before + crop["yield"], crop["name"] + ": rendimiento correcto")
		check(inventory.call("GetQuantity", seed) == seeds_before_harvest + 1, crop["name"] + ": una semilla devuelta")

		var sale_price = economy.call("GetSalePrice", product)
		var balance_before_sale = economy.call("GetBalance")
		var units = inventory.call("GetQuantity", product)
		check(economy.call("SellHarvest", product, units) == true, crop["name"] + ": venta del producto")
		check(economy.call("GetBalance") == balance_before_sale + sale_price * units, crop["name"] + ": saldo actualizado tras la venta")

		var sale_invalid = economy.call("SellHarvest", product, 1)
		check(sale_invalid == false, crop["name"] + ": no se vende sin existencias")

	print("Crops test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
