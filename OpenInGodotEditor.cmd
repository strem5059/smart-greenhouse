@echo off
cd /d "%~dp0"
if not defined GODOT_EXE set "GODOT_EXE=%USERPROFILE%\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
"%GODOT_EXE%" --path . --editor
