extends SceneTree

const SEEDS = ["lettuce_seeds", "carrot_seeds", "tomato_seeds", "cucumber_seeds", "spinach_seeds"]

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
	economy.set("StartingBalance", 10000)
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
	check(get_nodes_in_group("office_administrator").size() == 0, "el robot administrativo no existe al iniciar")

	var before = economy.call("GetBalance")
	check(fleet.call("PurchaseAdministrator") == false, "sin dinero suficiente no se compra el administrativo")
	check(get_nodes_in_group("office_administrator").size() == 0 and economy.call("GetBalance") == before, "una compra fallida no crea instancia ni cobra")

	inventory.call("AddItem", "lettuce_harvested", 1000)
	economy.call("SellLettuce", 1000)
	before = economy.call("GetBalance")
	check(fleet.call("PurchaseAdministrator") == true, "con dinero suficiente se compra el administrativo")
	check(economy.call("GetBalance") == before - 20000, "la compra cuesta 20000 $")
	check(fleet.call("PurchaseAdministrator") == false, "solo puede existir una instancia administrativa")
	await create_timer(0.2).timeout
	var admin = get_first_node_in_group("office_administrator")
	var warehouse = get_first_node_in_group("central_warehouse")
	check(admin != null and admin.get("IsActive") == true, "el administrativo comprado arranca su programa")
	check(admin.call("MinimumFor", "fertilizer") == 10 and admin.call("TargetFor", "fertilizer") == 30, "mínimo 10 y objetivo 30 (× multiplicador 3)")

	for seed in SEEDS:
		set_stock(seed, 10)
	var fert_price = economy.call("GetPrice", "fertilizer")
	var lettuce_price = economy.call("GetSalePrice", "lettuce_harvested")
	set_stock("fertilizer", 9)
	await create_timer(3.5).timeout
	check(inventory.call("GetQuantity", "fertilizer") == 9 and "insuficiente" in str(admin.get("LastAction")), "sin dinero ni excedentes informa y no compra")

	warehouse.call("Deposit", "lettuce_harvested", 300)
	before = economy.call("GetBalance")
	await create_timer(3.5).timeout
	var sold = 300 - 5
	check(warehouse.call("Count", "lettuce_harvested") == 5, "la venta automática deja la reserva de 5 unidades")
	check(inventory.call("GetQuantity", "fertilizer") == 30, "con los ingresos repone el fertilizante hasta 30")
	check(economy.call("GetBalance") == before + sold * lettuce_price - 21 * fert_price, "ingresos y compra cuadran exactamente")
	check(economy.call("GetBalance") >= 5000, "la reserva monetaria de 5000 se respeta")

	before = economy.call("GetBalance")
	await create_timer(3.5).timeout
	check(economy.call("GetBalance") == before and inventory.call("GetQuantity", "fertilizer") == 30, "sin nuevas necesidades no hay compras ni ventas duplicadas")

	inventory.call("AddItem", "lettuce_harvested", 200)
	economy.call("SellLettuce", 200)
	set_stock("fertilizer", 0)
	admin.call("TogglePause")
	await create_timer(3.5).timeout
	check(admin.get("StateName") == "Pausado" and inventory.call("GetQuantity", "fertilizer") == 0, "pausado: no repone insumos")
	admin.call("TogglePause")
	await create_timer(3.5).timeout
	check(inventory.call("GetQuantity", "fertilizer") == 30, "reanudado: repone automáticamente")

	set_stock("fertilizer", 0)
	admin.call("Stop")
	await create_timer(3.5).timeout
	check(admin.get("StateName") == "Detenido" and inventory.call("GetQuantity", "fertilizer") == 0, "detenido: no repone insumos")
	admin.call("Start")
	await create_timer(3.5).timeout
	check(inventory.call("GetQuantity", "fertilizer") == 30, "iniciado de nuevo: repone automáticamente")

	admin.call("SetAutoSell", false)
	warehouse.call("Deposit", "lettuce_harvested", 50)
	var stock_before = warehouse.call("Count", "lettuce_harvested")
	await create_timer(3.5).timeout
	check(warehouse.call("Count", "lettuce_harvested") == stock_before, "con ventas automáticas desactivadas no vende")
	admin.call("SetAutoSell", true)
	await create_timer(3.5).timeout
	check(warehouse.call("Count", "lettuce_harvested") == 5, "al activar las ventas vende el excedente sobre la reserva")

	admin.call("SetSaleReserve", "lettuce_harvested", 20)
	warehouse.call("Deposit", "lettuce_harvested", 100)
	await create_timer(3.5).timeout
	check(warehouse.call("Count", "lettuce_harvested") == 20, "la reserva por cultivo (20) se respeta")

	admin.call("SetMultiplier", 0)
	check(admin.get("Multiplier") == 1, "el multiplicador se limita a un mínimo de 1")
	admin.call("SetMultiplier", 3)
	admin.call("SetReserve", 7000)
	check(admin.get("MoneyReserve") == 7000, "la reserva monetaria es configurable")
	admin.call("SetReserve", 5000)

	admin.call("StopProgram")
	print("Administrator test ", "passed" if failures == 0 else "FAILED")
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
