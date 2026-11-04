extends SceneTree

var scene

func _init():
	scene = load("res://scenes/world/greenhouse.tscn").instantiate()
	get_root().add_child(scene)
	create_timer(2.0).timeout.connect(_on_timeout)

func _on_timeout():
	var player = scene.get_node_or_null("Player")
	if player == null:
		printerr("Player missing during run test")
		quit(1)
		return
	print("Run test passed: scene ran for 2 seconds with player active.")
	quit(0)
