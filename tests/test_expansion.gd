extends SceneTree

var inventory
var economy
var simulator
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

	simulator = load("res://scripts/agriculture/GrowthSimulator.cs").new()
	simulator.name = "GrowthSimulator"
	get_root().add_child(simulator)

	greenhouse = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(greenhouse)

	call_deferred("_run")


func _run():
	expansion = greenhouse.get_node_or_null("ExpansionManager")
	check(expansion != null, "ExpansionManager presente en la escena")
	if expansion == null:
		quit(1)
		return

	get_first_node_in_group("quickbar").call("Assign", 0, "lettuce_seeds")
	get_first_node_in_group("quickbar").call("Select", 0)
	check(expansion.call("GetBaseDirectionStatus", 1) == 2, "Derecha bloqueada por la oficina")
	check(expansion.call("GetBaseDirectionStatus", 0) == 0, "Izquierda disponible")
	check(economy.call("GetBalance") == 100, "saldo inicial 100")
	check(expansion.call("PurchaseFromBase", 0) == false, "no compra sin fondos")
	check(economy.call("GetBalance") == 100, "sin cobro al fallar")
	check(expansion.get("ModuleCount") == 1, "sin módulos tras el fallo")

	inventory.call("AddItem", "lettuce_harvested", 25)
	check(economy.call("SellLettuce", 25) == true, "venta de 25 lechugas")
	check(economy.call("GetBalance") == 400, "saldo 400 tras la venta")

	check(expansion.call("PurchaseFromBase", 0) == true, "compra de expansión a la izquierda")
	check(economy.call("GetBalance") == 150, "cobro único de 250")
	check(expansion.get("ModuleCount") == 2, "módulo añadido")
	check(greenhouse.get_node_or_null("Module1") != null, "nodo Module1 generado")
	check(greenhouse.get_node_or_null("Module1/GrowingStation24") != null, "estación nueva generada")
	check(expansion.call("PurchaseFromBase", 0) == false, "no duplica la misma dirección")
	check(economy.call("GetBalance") == 150, "sin doble cobro")
	check(expansion.call("GetBaseDirectionStatus", 0) == 1, "izquierda marcada como ocupada")

	var new_station = greenhouse.get_node("Module1/GrowingStation24")
	new_station.call("Interact")
	check(new_station.get("_plant") != null, "estación de expansión planta lechuga")

	var original = greenhouse.get_node("GrowingStation0")
	original.call("Interact")
	check(original.get("_plant") != null, "estación original planta lechuga")
	check(inventory.call("GetQuantity", "lettuce_seeds") == 3, "dos siembras consumen dos semillas")

	check(expansion.call("PurchaseFromBase", 1) == false, "no compra dirección bloqueada por la oficina")
	check(economy.call("GetBalance") == 150, "sin cobro en dirección bloqueada")

	check(greenhouse.get_node_or_null("GlassWallRightN") != null and greenhouse.get_node_or_null("GlassWallRightS") != null, "base conserva el hueco de la oficina")
	check(greenhouse.get_node_or_null("Module1/GlassWallRightN") == null and greenhouse.get_node_or_null("Module1/GlassWallRightS") == null, "módulo izquierdo sin piezas de hueco")

	inventory.call("AddItem", "lettuce_harvested", 25)
	check(economy.call("SellLettuce", 25) == true, "segunda venta de 25 lechugas")
	check(expansion.call("PurchaseFromBase", 2) == true, "compra de expansión al frente")
	check(greenhouse.get_node_or_null("Module2") != null, "módulo frontal generado")
	check(greenhouse.get_node_or_null("Module2/GlassWallRightN") == null and greenhouse.get_node_or_null("Module2/GlassWallRightS") == null, "módulo frontal sin hueco en la pared este")
	check(greenhouse.get_node_or_null("Module2/GlassWallRightFull") != null, "módulo frontal con pared este completa")
	var front_wall = greenhouse.get_node_or_null("GlassWallFront")
	check(front_wall == null or front_wall.is_queued_for_deletion(), "cara frontal de la base retirada al conectar")
	check(greenhouse.get_node_or_null("GlassWallRightN") != null and greenhouse.get_node_or_null("GlassWallRightS") != null, "base sigue con el hueco de la oficina")
	check(expansion.get("CanExpand") == true, "con 3 módulos aún se puede expandir")

	inventory.call("AddItem", "lettuce_harvested", 25)
	check(economy.call("SellLettuce", 25) == true, "tercera venta de 25 lechugas")
	check(expansion.call("PurchaseFromBase", 3) == true, "tercera ampliación (atrás)")
	check(expansion.get("ModuleCount") == 4, "límite de beta: 4 módulos en total")
	check(expansion.get("CanExpand") == false, "sin más expansiones al alcanzar el límite")
	check(expansion.call("GetBaseDirectionStatus", 0) == 1, "las direcciones ocupadas siguen marcadas como ocupadas")

	inventory.call("AddItem", "lettuce_harvested", 25)
	check(economy.call("SellLettuce", 25) == true, "venta extra para comprobar el límite")
	check(economy.call("GetBalance") == 550, "saldo antes de intentar un quinto módulo")
	check(expansion.call("PurchaseFromBase", 2) == false, "no compra fuera del límite de beta")
	check(economy.call("GetBalance") == 550, "sin cobro al alcanzar el límite")

	print("Expansion test ", "passed" if failures == 0 else "FAILED")
	quit(0 if failures == 0 else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	if not condition:
		failures += 1
	return condition
