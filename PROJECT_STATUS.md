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

### Correcciones realizadas en Fase 2
- Cámara primera persona rebajada a altura de ojos (`Head` en y=1.45).
- Cámara tercera persona colocada detrás del personaje (`ThirdPersonPivot` en z=-4) sin rotación invertida.
- Crosshair central (`+`) visible en primera y tercera persona.
- Raycast de interacción desde el centro de la cámara activa.
- Indicador contextual `[E] Plantar lechuga` / `[E] Regar` / `[E] Fertilizar` / `[E] Estado` / `[E] Cosechar` según el estado de la estación.
- Estaciones con maceta y tierra café visible (`pot_with_soil.tscn`), separadas de la vegetación.
- Vegetación instanciada únicamente al plantar (`lettuce.tscn`).
- 4 etapas visuales diferenciadas: semilla, plántula, crecimiento y madura.

### Funcionalidades terminadas (Día 2 + correcciones visuales)
- Tuberías reorganizadas: líneas principales en el techo, bajantes y ramales sobre cada mesa.
- Interacción con `E`: detección por raycast desde el centro de la cámara activa e indicador contextual en pantalla.
- HUD minimalista: crosshair central, indicador contextual, controles (`V`, `Esc`) e inventario resumido actualizado.
- Plantar lechuga en estaciones de cultivo (consume 1 semilla).
- 4 etapas de crecimiento: Semilla, Plántula, Crecimiento, Madura.
- Maceta con tierra visible: las estaciones comienzan vacías y muestran vegetación progresivamente.
- Representaciones visuales diferenciadas para cada etapa (brotes, hojas, lechuga completa).
- Crecimiento dependiente del tiempo simulado (1 tick/s).
- Agua y nutrientes afectan el crecimiento; déficit reduce salud.
- Regar y fertilizar la planta.
- Consultar estado de la planta.
- Cosechar planta madura (añade 1 lechuga al inventario).
- Inventario inicial con semillas, fertilizante y lechugas cosechadas.

### Errores conocidos / pendientes
- Warning `invalid UID` al cargar `greenhouse.tscn` por el UID autogenerado de `player.tscn`. No impide ejecutar el juego; se corregirá guardando las escenas desde Godot Editor.
- Algunos errores internos de `text_edit.cpp` del editor de Godot 4.7 (no afectan ejecución).
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

### Corrección de cámaras, cursor e interacción (Fase 2)
- Causas: la cámara de tercera persona estaba de espaldas al personaje (`z=-4` sin giro) y no tenía órbita; ESC solo ocultaba el cursor; el rayo no excluía al jugador; el apuntador se dibujaba con un `Label`.
- Tercera persona: `ThirdPersonPivot` (top level, sigue al jugador) > `ThirdPersonPitch` > `SpringArm3D` > `ThirdPersonCamera`. El ratón orbita en yaw y pitch; el personaje gira hacia la dirección de movimiento.
- Primera persona: sin cambios de comportamiento.
- Cursor: ESC alterna entre `Captured` y `Visible` desde un único método (`SetMouseCaptured`). La rotación solo se aplica con el cursor capturado.
- Apuntador: `scripts/player/Crosshair.cs` (`Control` con `_Draw`), visible solo con el cursor capturado.
- Interacción: rayo desde la cámara activa, excluye al jugador; en tercera persona el alcance se suma a la distancia cámara–jugador. Textos según especificación.
- Pendiente de verificación manual: giro del SpringArm (se asume +Z), visibilidad del apuntador y mensajes en el editor.

### Interacción y recompensa de semillas
- Causa del mensaje ausente: las bajantes y ramales de tubería (`PipeDrop_*`, `PipeFeeder_*`) tenían colisión sobre el centro de cada estación y el rayo las golpeaba primero. Se eliminaron los 48 nodos de tubería de mesa (y sus recursos); se conservan las tuberías del techo.
- Cosecha: además de la lechuga, entrega 1 semilla (`lettuce_seeds`). Verificado en prueba headless (4 → 5).
- Pendiente de validación manual: mensajes `[E]` en el editor.
- Pendiente de decisión: texto de cosecha (`[E] Cosechar lechuga` o `[E] Cosechar`).

### Macetas y recompensa
- Cada mesa tiene 3 macetas independientes (24 `GrowingStation` en total, separadas 0.8 m sobre el canal). Cada una tiene su propia planta, agua, fertilizante, crecimiento y cosecha.
- Cosecha: 1 lechuga y 2 semillas (`lettuce_seeds`).
- Pendiente de validación manual: posición de las macetas sobre el canal y mensaje `[E]` por maceta.

### Estado
Día 2 terminado (correcciones de Fase 2 pendientes de validación manual).

### Siguiente paso (Día 3)
Economía y panel: saldo, compra/venta, almacén e interfaz administrativa.
