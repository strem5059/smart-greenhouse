extends SceneTree


var inventory
var economy

func _init():
	inventory = load("res://scripts/inventory/InventoryManager.cs").new()
	inventory.name = "InventoryManager"
	get_root().add_child(inventory)

	economy = load("res://scripts/economy/EconomyManager.cs").new()
	economy.name = "EconomyManager"
	get_root().add_child(economy)

	call_deferred("_run")


func _run():
	var ok = true
	ok = check(economy.call("GetBalance") == 100, "saldo inicial 100") and ok
	ok = check(economy.call("BuyItem", "lettuce_seeds", 2) == true, "compra de 2 semillas") and ok
	ok = check(economy.call("GetBalance") == 92, "saldo tras compra 92") and ok
	ok = check(inventory.call("GetQuantity", "lettuce_seeds") == 7, "semillas 5 + 2 = 7") and ok
	ok = check(economy.call("SellLettuce", 1) == false, "no vende sin existencias") and ok
	inventory.call("AddItem", "lettuce_harvested", 3)
	ok = check(economy.call("SellLettuce", 2) == true, "venta de 2 lechugas") and ok
	ok = check(economy.call("GetBalance") == 116, "saldo tras venta 116") and ok
	ok = check(inventory.call("GetQuantity", "lettuce_harvested") == 1, "existencias 3 - 2 = 1") and ok
	ok = check(economy.call("BuyItem", "fertilizer", 100) == false, "no compra sin saldo") and ok
	ok = check(economy.call("GetBalance") == 116, "saldo no negativo") and ok

	print("Economy test ", "passed" if ok else "FAILED")
	quit(0 if ok else 1)


func check(condition, label):
	print(("OK   " if condition else "FAIL ") + label)
	return condition
