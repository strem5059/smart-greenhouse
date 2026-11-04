# SMART GREENHOUSE — Estado del proyecto

## Día 1 — Base del proyecto y mundo explorable

### Entorno
- Godot: 4.7.2.stable.mono.official
- .NET SDK: 8.0.427
- Git: 2.56.0.windows.4
- Plataforma: Windows PC

### Estructura creada
```
scenes/
  world/
  player/
  interfaces/
scripts/
  core/
  player/
  interfaces/
assets/
  models/
  materials/
  textures/
  audio/
resources/
tests/
```

### Escenas implementadas
- `scenes/world/greenhouse.tscn` — invernadero blockout con suelo, estructura metálica, cristales, mesas hidropónicas, canales, tuberías azules y vegetación provisional.
- `scenes/player/player.tscn` — CharacterBody3D con cámaras de primera y tercera persona.
- `tests/test_scene.tscn` — escena de prueba headless con auto-quit.

### Scripts principales
- `scripts/player/PlayerController.cs` — movimiento WASD, salto, gravedad, colisiones, control de cámara con ratón y cambio de cámara con `V`.

### Funcionalidades terminadas
- Movimiento WASD con aceleración/desaceleración.
- Salto y gravedad.
- Colisiones (CharacterBody3D + MoveAndSlide).
- Cámara primera persona con ratón.
- Cámara tercera persona (detrás del personaje).
- Cambio entre cámaras con tecla `V` sin reiniciar posición.
- Escenario explorable con geometría provisional y materiales coherentes.

### Errores conocidos
- Ninguno crítico. Al ejecutar tests headless aparecen leaks de recursos por cierre forzado, pero no afectan la ejecución normal.

### Comandos de compilación y prueba
```powershell
cd "C:\Users\jafet\OneDrive\Desktop\Juego zacek\smart-greenhouse"
dotnet build SmartGreenhouse.sln
& 'C:\Users\jafet\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe' --path . --headless --verbose tests/test_scene.tscn
```

### Estado
Terminado.

### Siguiente paso (Día 2)
Agricultura e interacción: planta base, sembrar, crecimiento por etapas, cosecha e inventario mínimo.
