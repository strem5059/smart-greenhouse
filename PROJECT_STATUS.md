# SMART GREENHOUSE — Estado del proyecto

## Día 2 — Agricultura, interacción y corrección del entorno

### Entorno
- Godot: 4.7.2.stable.mono.official (usar siempre la versión **mono**)
- .NET SDK: 8.0.427
- Git: 2.56.0.windows.4
- Plataforma: Windows PC

> Abrir el proyecto con Godot `stable` (no mono) produce `No loader found for resource: res://scripts/player/PlayerController.cs`.

### Estructura actual
```
scenes/
  world/
  player/
  crops/
  interactables/
  interfaces/
scripts/
  core/
  player/
  agriculture/
  inventory/
  interfaces/
assets/
  models/
  materials/
  textures/
  audio/
resources/
tests/
```

### Escenas y scripts implementados
- `scenes/world/greenhouse.tscn` — invernadero con tuberías en techo, mesas, canales y estaciones de cultivo.
- `scenes/player/player.tscn` — jugador con cámaras primera/tercera persona e interacción.
- `scenes/crops/lettuce.tscn` — representación visual de la lechuga en 4 etapas.
- `scripts/player/PlayerController.cs` — movimiento, salto, cámaras e interacción con `E`.
- `scripts/agriculture/CropData.cs` — datos de la planta.
- `scripts/agriculture/GrowthSimulator.cs` — simulación de crecimiento por tiempo.
- `scripts/agriculture/PlantController.cs` — control visual de la planta.
- `scripts/agriculture/GrowingStation.cs` — estación de cultivo interactiva.
- `scripts/inventory/InventoryManager.cs` — inventario básico.
- `scripts/interfaces/IInteractable.cs` — interfaz de interacción.

### Funcionalidades terminadas (Día 2)
- Tuberías reorganizadas: líneas principales en el techo, bajantes y ramales sobre cada mesa.
- Interacción con `E`: detección por raycast e indicación en pantalla.
- Plantar lechuga en estaciones de cultivo (consume 1 semilla).
- 4 etapas de crecimiento: Semilla, Plántula, Crecimiento, Madura.
- Crecimiento dependiente del tiempo simulado (1 tick/s).
- Agua y nutrientes afectan el crecimiento; déficit reduce salud.
- Regar y fertilizar la planta.
- Consultar estado de la planta.
- Cosechar planta madura (añade 1 lechuga al inventario).
- Inventario inicial con semillas, fertilizante y lechugas cosechadas.

### Errores conocidos / pendientes
- Warning `invalid UID` al cargar `greenhouse.tscn` por el UID autogenerado de `player.tscn`. No impide ejecutar el juego; se corregirá guardando las escenas desde Godot Editor.
- Leaks de recursos al cerrar tests headless (no críticos).

### Comandos de compilación y prueba
```powershell
cd "C:\Users\jafet\OneDrive\Desktop\Juego zacek\smart-greenhouse"
dotnet build SmartGreenhouse.sln
& 'C:\Users\jafet\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe' --path . --headless tests/test_scene.tscn
& 'C:\Users\jafet\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe' --path . --headless --script tests/test_agriculture.gd
```

### Resultado de compilación
```
Compilación correcta.
0 Advertencia(s)
0 Errores
```

### Pruebas
- `test_scene.tscn` — carga de escena y ejecución: **passed**.
- `test_agriculture.gd` — plantar, crecer hasta madurez y cosechar: **passed**.

### Estado
Día 2 terminado.

### Siguiente paso (Día 3)
Economía y panel: saldo, compra/venta, almacén e interfaz administrativa.
