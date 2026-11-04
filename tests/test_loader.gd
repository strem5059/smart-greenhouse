extends SceneTree

func _init():
    print("Loading greenhouse scene...")
    var greenhouse = load("res://scenes/world/greenhouse.tscn")
    if greenhouse == null:
        printerr("Failed to load greenhouse scene")
        quit(1)
        return

    var instance = greenhouse.instantiate()
    if instance == null:
        printerr("Failed to instantiate greenhouse")
        quit(1)
        return

    print("Greenhouse instantiated: ", instance.name)

    var player = instance.get_node_or_null("Player")
    if player == null:
        printerr("Player not found in greenhouse")
        quit(1)
        return

    print("Player found: ", player.name)

    var fp_cam = player.get_node_or_null("Head/FirstPersonCamera")
    var tp_cam = player.get_node_or_null("ThirdPersonPivot/ThirdPersonCamera")
    if fp_cam == null or tp_cam == null:
        printerr("Cameras missing")
        quit(1)
        return

    print("First-person camera: ", fp_cam.name)
    print("Third-person camera: ", tp_cam.name)
    print("All checks passed.")
    quit(0)
