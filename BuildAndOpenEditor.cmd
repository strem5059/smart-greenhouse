@echo off
cd /d "%~dp0"
if not defined GODOT_EXE set "GODOT_EXE=%USERPROFILE%\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
dotnet build SmartGreenhouse.sln
if %errorlevel% neq 0 (
    echo Error al compilar. Presiona una tecla para cerrar.
    pause > nul
    exit /b %errorlevel%
)
start "" "%GODOT_EXE%" --path . --editor
