extends SceneTree

const CROPS = ["lettuce", "carrot", "tomato", "cucumber", "spinach"]


func _init():
	for id in CROPS:
		var path = "res://scenes/crops/%s.tscn" % id
		if FileAccess.file_exists(path):
			print("Ya existe: ", path)
			continue
		var scene = load("res://scenes/crops/lettuce.tscn").duplicate()
		var error = ResourceSaver.save(scene, path)
		print("Escena creada: ", path, " resultado: ", error)
	quit(0)
