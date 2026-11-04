extends SceneTree

const PLAYER_SCENE_PATH := "res://scenes/player/player.tscn"
const PLAYER_SCRIPT_PATH := "res://scripts/player/PlayerController.cs"
const GREENHOUSE_SCENE_PATH := "res://scenes/world/greenhouse.tscn"

func _init():
    print("Generating valid UIDs for scenes...")

    # 1. Ensure the C# script has a .uid file and read it.
    var script_uid_text := _ensure_script_uid(PLAYER_SCRIPT_PATH)
    if script_uid_text.is_empty():
        printerr("Could not ensure script UID")
        quit(1)
        return
    print("PlayerController.cs UID: ", script_uid_text)

    # 2. Generate or read UID for player.tscn.
    var player_uid_text := _ensure_scene_uid(PLAYER_SCENE_PATH)
    if player_uid_text.is_empty():
        printerr("Could not ensure player scene UID")
        quit(1)
        return
    print("player.tscn UID: ", player_uid_text)

    # 3. Patch player.tscn header and script ext_resource.
    var player_file := FileAccess.open(PLAYER_SCENE_PATH, FileAccess.READ)
    var player_text := player_file.get_as_text()
    player_file.close()

    var header_old := player_text.split("\n")[0]
    if header_old.begins_with("[gd_scene "):
        var header_new := "[gd_scene load_steps=6 format=3 uid=\"" + player_uid_text + "\"]"
        player_text = header_new + player_text.substr(header_old.length())

    player_text = player_text.replace(
        '[ext_resource type="Script" path="res://scripts/player/PlayerController.cs"',
        '[ext_resource type="Script" uid="' + script_uid_text + '" path="res://scripts/player/PlayerController.cs"'
    )

    var player_write := FileAccess.open(PLAYER_SCENE_PATH, FileAccess.WRITE)
    player_write.store_string(player_text)
    player_write.close()
    print("Patched ", PLAYER_SCENE_PATH)

    # 4. Patch greenhouse.tscn to reference player.tscn by UID.
    var gh_file := FileAccess.open(GREENHOUSE_SCENE_PATH, FileAccess.READ)
    var gh_text := gh_file.get_as_text()
    gh_file.close()

    # Replace any existing player.tscn ext_resource line with the new UID.
    var re := RegEx.new()
    re.compile('\\[ext_resource type="PackedScene"( uid="uid://[^"]+")? path="res://scenes/player/player.tscn" id="([^"]+)"\\]')
    gh_text = re.sub(gh_text, '[ext_resource type="PackedScene" uid="' + player_uid_text + '" path="res://scenes/player/player.tscn" id="$2"]')

    var gh_write := FileAccess.open(GREENHOUSE_SCENE_PATH, FileAccess.WRITE)
    gh_write.store_string(gh_text)
    gh_write.close()
    print("Patched ", GREENHOUSE_SCENE_PATH)

    print("Done.")
    quit(0)

func _ensure_script_uid(path: String) -> String:
    var uid_path := path + ".uid"
    if FileAccess.file_exists(uid_path):
        var f := FileAccess.open(uid_path, FileAccess.READ)
        var text := f.get_as_text().strip_edges()
        f.close()
        return text

    var id := ResourceUID.create_id()
    var text := ResourceUID.id_to_text(id)
    var f := FileAccess.open(uid_path, FileAccess.WRITE)
    f.store_string(text)
    f.close()
    ResourceUID.add_id(id, path)
    return text

func _ensure_scene_uid(path: String) -> String:
    # Always generate a fresh UID from Godot and replace any existing one.
    var id := ResourceUID.create_id()
    var uid_text := ResourceUID.id_to_text(id)
    ResourceUID.add_id(id, path)
    return uid_text
