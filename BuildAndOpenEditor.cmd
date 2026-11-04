@echo off
cd /d "C:\Users\jafet\OneDrive\Desktop\Juego zacek\smart-greenhouse"
dotnet build SmartGreenhouse.sln
if %errorlevel% neq 0 (
    echo Error al compilar. Presiona una tecla para cerrar.
    pause > nul
    exit /b %errorlevel%
)
start "" "C:\Users\jafet\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path "C:\Users\jafet\OneDrive\Desktop\Juego zacek\smart-greenhouse" --editor
