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

### Día 3 — Economía, oficina y panel administrativo
- `EconomyManager` (autoload): saldo, venta de lechugas, compra de semillas y fertilizante, historial. Valores provisionales pendientes de aprobación: saldo inicial 100, lechuga 12, semilla 4, fertilizante 6 (`[Export]`). Saldo solo en sesión (sin guardado).
- `ManagementPanel`: estados Closed/Opening/Open/Closing, tween de 0.6 s; pestañas Resumen, Tienda, Almacén (incl. venta), Finanzas, Gestión y Expansión (bloqueada hasta Fase 4). Compras y ventas piden confirmación.
- `ComputerInteraction` (escritorio de la oficina): `[E] Abrir administración`; enciende la pantalla y abre el panel. ESC o el botón cierran el panel.
- `PlayerController.SetInputLocked`: bloquea movimiento, cámara e interacción mientras el panel está abierto.
- Oficina `scenes/office/office.tscn` fuera del invernadero, al este, escalada ×1.5 (4.5 × 12 × 4.5 m) para igualar el largo del invernadero; puerta a z=-2.0 (global z=-3.0) para evitar la columna central. Hueco de 1.5 m en el cristal este (`GlassWallRightN/S`) alineado con la puerta. El suelo de la oficina queda a 1.5 cm del del invernadero.
- Pruebas headless: `test_economy.gd` passed; `test_scene.tscn` y `test_agriculture.gd` passed.
- Pendiente de validación manual: visibilidad de la oficina, mensaje sobre la computadora, animación de apertura/cierre, paneles y bloqueo del jugador.

### Estado
Día 2 terminado. Día 3 implementado; pendiente de validación manual en el editor.

### Siguiente paso (Día 3)
Economía y panel: saldo, compra/venta, almacén e interfaz administrativa.

---

## Día 3 — Cierre: expansión modular y correcciones (estado actualizado)

### Decisiones autorizadas por el supervisor
- Expansión mediante gestor modular que reutiliza los nodos arquitectónicos del invernadero (`scripts/expansion/ExpansionManager.cs`).
- Cada módulo es una réplica completa: 24 `GrowingStation` nuevas, coste **250 $** (`EconomyManager.ExpansionPrice`) y valores económicos existentes sin cambios.
- Agua gratuita e ilimitada; se eliminó la comprobación de un artículo `water` inexistente.

### Cambios
- `GrowingStation`: el riego ya no consulta inventario; la cosecha entrega **1 semilla** (antes 2).
- `EconomyManager`: `ExpansionPrice` centralizado y `TrySpend()` para cobros únicos registrados en el historial como `Expansión`.
- `ExpansionManager` (nuevo, nodo hijo de `greenhouse.tscn`):
  - Duplica suelo, columnas, vigas, cristales, mesas, canales y tuberías del módulo base (20 × 12 m).
  - Crea 24 estaciones nuevas (`GrowingStation24…47` para el primer módulo) con las mismas exportaciones y colisión.
  - Cuatro direcciones: Izquierda, Derecha, Frente (−Z), Atrás (+Z).
  - Elimina el cristal de la cara compartida para conectar ambos módulos; el módulo nuevo no duplica su cara compartida.
  - Colisión con la oficina y con módulos existentes mediante rectángulos de obstáculos y rejilla de celdas; si el destino está ocupado o bloqueado, no se compra.
  - Genera el módulo antes de cobrar; si el cobro falla, libera el módulo. No hay cobro duplicado.
- `ManagementPanel`: pestaña **Expansión** real con saldo, coste, faltante de fondos, estado de cada dirección y confirmación con saldo restante y capacidad adicional. Sustituye el botón deshabilitado.
- Lanzadores `.cmd`: rutas relativas al proyecto (`%~dp0`) y ejecutable de Godot mediante la variable `GODOT_EXE`, con valor por defecto en `%USERPROFILE%\Downloads\…`.
- `tests/test_loader.gd`: corregida la ruta de la cámara de tercera persona.
- `tests/test_expansion.gd` (nuevo).

### Pruebas ejecutadas (headless, Godot 4.7.2 Mono)
| Prueba | Resultado |
|---|---|
| `dotnet build SmartGreenhouse.csproj` | 0 errores, 0 advertencias |
| `test_expansion.gd` | **passed**, exit 0 |
| `test_economy.gd` | **passed** |
| `test_agriculture.gd` | **passed** |
| `test_loader.gd` | Todas las comprobaciones OK; el proceso terminó con código 1 por avisos de fugas de recursos al cerrar (ver pendientes) |

### No verificado todavía
- Visualización 3D real de los módulos, alineación de juntas y tamaño percibido. Solo se verificó por estructura y nodos generados.
- Recorrido manual con jugador: caminar a la oficina, abrir panel con E, animaciones, bloqueo de controles y compra desde la interfaz.
- Mensajes `[E]` sobre las estaciones nuevas.
- Iluminación interior de los módulos nuevos: no tienen luces propias; dependen de la luz direccional.
- Guardado permanente: las ampliaciones existen solo durante la sesión.

### Pendientes conocidos
- `ExpansionManager.cs` no tiene `.uid` todavía; Godot lo generará al abrir el proyecto en el editor.
- `greenhouse.tscn` referencia el script por ruta (sin UID) para evitar generar UIDs manualmente.
- `test_loader.gd` devuelve código 1 al cerrar por fugas de recursos del motor; no se comprobó si ocurre también antes de estos cambios.
- `test_scene.tscn` sigue siendo un smoke test de dos segundos.
- Día 4 (robots), Día 5 (programación visual) y Día 6 (automatización) no iniciados.

### Estado del Día 3
**Parcialmente terminado: pendiente de validación visual y manual.** Toda la lógica de economía y expansión está implementada y comprobada en pruebas headless; la aceptación final requiere revisar el flujo en el editor.

---

## Precierre Día 3 — Subfase 3.1: HUD y experiencia visual

### Componentes del HUD (`scripts/interfaces/GameHud.cs`)
- Creado por `PlayerController._Ready` y sustituye las etiquetas anteriores (`SetupHUD`, `UpdateInventoryDisplay`).
- **Saldo:** sincronizado con `EconomyManager.BalanceChanged`; animación de color y escala al cambiar. No tiene variable propia.
- **Recursos:** semillas, fertilizante y lechugas leídos de `InventoryManager` en cada frame.
- **Puntero:** `Crosshair` reutilizado, ahora con contorno oscuro para contrastar con fondos claros.
- **Interacción:** tarjeta inferior con tecla y acción derivadas de `IInteractable.GetInteractionPrompt()`. Las acciones que empiezan por "Sin" se muestran atenuadas. Aparece con animación de fundido.
- **Información agrícola:** tarjeta derecha visible solo al observar una estación ocupada. Muestra nombre, etapa, progreso, agua, nutrientes y salud desde `CropData` (`GrowingStation.Crop`). No recalcula crecimiento.
- **Notificaciones:** pila superior con hasta 4 avisos, fundido de entrada y salida, codificación por color (éxito, advertencia, información). Se ocultan mientras el panel administrativo está abierto.
- **Barra rápida:** tres ranuras vacías (`QuickSlot1..3`) preparadas en la parte inferior. Sin funcionalidad.
- **Controles:** indicación compacta de `V` y `Esc` arriba a la derecha.

### Canal de notificaciones (`scripts/interfaces/Notifications.cs`)
- Evento estático `Notifications.Published` y método `Publish(texto, tipo)`.
- Cada operación publica en un único punto:
  - `GrowingStation`: `Semillas insuficientes`, `Fertilizante insuficiente`, `Lechuga plantada`, `+1 Lechuga` y `+1 Semilla` (cosecha).
  - `EconomyManager`: `Compra realizada`, `Venta realizada`, `Fondos insuficientes`, `Sin lechugas para vender`.
  - `ExpansionManager`: `Expansión adquirida`.
- El panel administrativo no publica notificaciones; mantiene su mensaje de pie.

### Cambios en sistemas existentes
- `PlayerController`: reescrito para delegar en `GameHud`. Movimiento, cámaras, raycast e input no cambian. `SetInputLocked` oculta el HUD y `SetMouseCaptured` controla el apuntador.
- `EconomyManager`: nuevo evento `BalanceChanged` (todas las modificaciones de saldo pasan por `SetBalance`).
- `GrowingStation`: propiedad `Crop` y publicación de notificaciones. El riego sigue sin inventario.
- `Crosshair`: contorno oscuro y dibujo en dos capas.

### Archivos modificados o creados
- Nuevos: `scripts/interfaces/GameHud.cs`, `scripts/interfaces/Notifications.cs`.
- Modificados: `scripts/player/PlayerController.cs`, `scripts/player/Crosshair.cs`, `scripts/economy/EconomyManager.cs`, `scripts/agriculture/GrowingStation.cs`, `scripts/expansion/ExpansionManager.cs`.

### Pruebas (headless, Godot 4.7.2 Mono)
| Prueba | Resultado |
|---|---|
| `dotnet build SmartGreenhouse.csproj` | 0 errores, 0 advertencias |
| `test_expansion.gd` | passed, exit 0 |
| `test_economy.gd` | passed, exit 0 |
| `test_agriculture.gd` | passed, exit 0 |
| `tests/test_scene.tscn` (carga real de la escena con HUD) | passed, exit 0; sin excepciones de C# ni errores de script |

### No verificado visualmente
- Disposición real en pantalla, legibilidad de textos y contraste del apuntador.
- Animaciones de saldo, fundidos de notificaciones y del aviso de interacción.
- Comportamiento en distintas resoluciones y relación del HUD con el panel administrativo.
- Visibilidad de la tarjeta de cultivo al mirar cada estación.
- Interacción de la barra rápida: no implementada, solo preparada.

### Pendientes
- Verificar en el editor todos los puntos de la sección anterior.
- Las notificaciones de compra realizadas dentro del panel no se muestran, porque el HUD está oculto mientras el panel está abierto. Se ve el mensaje del pie del panel.
- Los avisos de cultivo se muestran solo para la estación observada.

### Preparado para la subfase 3.2
- `GameHud.Quickbar` expone el contenedor inferior con las ranuras `QuickSlot1..3`.
- `Notifications` es el canal único para avisos.
- La tarjeta de cultivo y la barra de recursos se pueden ampliar sin tocar la lógica de interacción.

### Estado
**Subfase 3.1 implementada y comprobada en pruebas headless; pendiente de revisión visual del supervisor.** No se ha iniciado la subfase 3.2.

---

## Subfase 3.2 — Bloque A: corrección del hueco de expansión

- Causa: los módulos de Frente y Atrás copiaban `GlassWallRightN/S` del invernadero, cuyo hueco es el de la puerta de la oficina. Daba al vacío exterior y permitía salir del mapa.
- Solución (opción A): las plantillas omiten `GlassWallRightN/S` y cada módulo recibe una pared este completa `GlassWallRightFull` (0.1 × 5 × 12, en x = +9.9). Se omite si esa cara es la compartida con el módulo de origen.
- La base y la oficina no cambian; el hueco original se conserva.
- Archivos: `scripts/expansion/ExpansionManager.cs`, `tests/test_expansion.gd`.
- Prueba headless: `test_expansion.gd` passed, código 0. Comprueba el hueco de la base, la ausencia de hueco y la pared completa en un módulo frontal, y que la cara frontal de la base se retira al conectar.
- Pendiente: verificación visual en el editor (junta entre módulos y que el jugador no puede salir por la pared este).
- Límite de beta autorizado: **4 módulos en total** (base + 3 ampliaciones), equivalente a 72 estaciones adicionales. Constante `ExpansionManager.MaxModules`; nuevo estado `LimitReached` que muestra "Límite de beta alcanzado" en el panel. Probado en `test_expansion.gd` (passed, código 0).
- Para subir el límite en fases posteriores basta con cambiar `MaxModules`; no se han implementado nuevos tipos de módulo (opción B descartada por ahora).

---

## Subfase 3.2 — Bloque B: inventario visual

- **Catálogo:** `scripts/inventory/ItemCatalog.cs` define id, nombre, descripción, categoría y color de icono de los tres objetos existentes (semillas de lechuga, lechuga, fertilizante). `EconomyManager.ItemName` lo usa; los precios no cambian.
- **Inventario:** `InventoryManager` añade el evento `Changed`. No cambia su almacenamiento.
- **Interfaz:** `scripts/interfaces/InventoryScreen.cs` (capa 15, por debajo del panel administrativo en capa 20). Incluye categorías, celdas con icono dibujado y cantidad, panel de detalle y apertura/cierre animados. `InventorySlot` es la celda; dibuja la representación del objeto arrastrado con `SetDragPreview`.
- **Controles:** TAB abre y cierra el inventario. Mientras está abierto, se bloquean movimiento, cámara e interacción (`ControlsLocked`), el HUD se oculta y el cursor queda visible. ESC lo cierra y la pulsación se consume. No se abre con el panel administrativo abierto.
- **Arrastre:** las celdas inician el arrastre con el botón izquierdo. No hay destinos de soltar: eso corresponde al Bloque C. Arrastrar no modifica cantidades.
- **Archivos:** nuevos `scripts/inventory/ItemCatalog.cs`, `scripts/interfaces/InventoryScreen.cs`, `tests/test_inventory.gd`. Modificados `scripts/inventory/InventoryManager.cs`, `scripts/economy/EconomyManager.cs`, `scripts/player/PlayerController.cs`.
- **Pruebas headless:** `test_inventory.gd` passed, código 0; `test_scene.tscn` passed, código 0, carga la escena con inventario y HUD sin excepciones; `test_economy.gd` passed, código 0.
- **Pendiente de verificación visual en el editor:** apertura y cierre con TAB y ESC, cursor, bloqueo del jugador, disposición de la cuadrícula, legibilidad de iconos y que el arrastre muestra la vista previa.
- **Preparado para el Bloque C:** `InventorySlot` expone `Item` y `Count`; el arrastre ya envía `item_id` en el diccionario de datos, que el destino de la barra rápida puede leer en `_CanDropData`/`_DropData`; el panel de detalle puede mostrar la asignación más adelante.

---

## Subfase 3.2 — Bloque C: barra rápida y selección de semillas

- **Estado:** `scripts/inventory/Quickbar.cs` (nodo hijo del jugador, grupo `quickbar`). Guarda solo identificadores de semillas en tres ranuras y la ranura seleccionada. Las cantidades siguen en `InventoryManager`. Solo acepta objetos de la categoría Semillas, y no acepta tipos sin existencias.
- **Vista:** `scripts/interfaces/QuickbarBar.cs` (`QuickbarBar` y `QuickbarSlot`). Muestra número, icono, cantidad, resalte de la ranura seleccionada y el nombre y cantidad de la semilla equipada. Acepta soltar objetos del inventario (sustituye) y arrastres entre ranuras (intercambia).
- **Teclas:** 1, 2 y 3 seleccionan las ranuras mediante `quickbar_1..3`. Están bloqueadas mientras los controles están bloqueados (panel administrativo o inventario).
- **HUD:** con el inventario abierto, la barra permanece visible y el resto del HUD se oculta (`GameHud.SetInventoryMode`). El panel administrativo sigue ocultando todo el HUD.
- **Iconos:** `scripts/interfaces/ItemIconDrawer.cs` centraliza el dibujo de iconos, usado por inventario y barra.
- **Agricultura:** plantar sigue descontando una semilla mediante el sistema existente. Que la estación plante la semilla equipada corresponde al Bloque D y no se ha modificado.
- **Archivos:** nuevos `scripts/inventory/Quickbar.cs`, `scripts/interfaces/QuickbarBar.cs`, `scripts/interfaces/ItemIconDrawer.cs`, `tests/test_quickbar.gd`. Modificados `scripts/interfaces/GameHud.cs`, `scripts/interfaces/InventoryScreen.cs` (usa `ItemIconDrawer`), `scripts/player/PlayerController.cs`.
- **Pruebas headless:**
  - `test_quickbar.gd`: passed, código 0. Comprueba asignación válida e inválida, que asignar no consume, equipar ranura vacía y con lechuga, que plantar descuenta exactamente una semilla, que la selección se conserva, que una ranura conserva el tipo con cantidad cero y no permite reasignarlo sin existencias, y el intercambio entre ranuras.
  - `test_inventory.gd`: passed, código 0.
  - `test_scene.tscn`: passed, código 0, sin excepciones.
  - `test_agriculture.gd`: passed, código 0.
- **Pendiente de verificación visual en el editor:** arrastre desde el inventario a la barra, intercambio entre ranuras, resalte, teclas 1-3, que el HUD se oculta correctamente al abrir el inventario y el panel administrativo, y que TAB, ESC, E y las cámaras no entran en conflicto.
- **Pendiente para el Bloque D:** la estación debe plantar la semilla equipada en lugar de solo lechugas, y mostrar mensajes `[E] Plantar <cultivo>`.

---

## Subfase 3.2 — Bloque D: integración agrícola

- `GrowingStation` consulta la semilla equipada en la barra rápida (`SeedBar`, búsqueda perezosa en el grupo `quickbar`, porque la estación puede inicializarse antes que el jugador).
- Sin semilla equipada: no planta y muestra "Selecciona una semilla en la barra rápida". Con una semilla que no tiene cultivo: "Cultivo no disponible". Sin existencias: "Semillas insuficientes".
- Mensajes: "[E] Sin semilla equipada", "[E] Plantar lechuga" o "[E] Sin semillas", según la selección y las existencias.
- Plantar descuenta exactamente una semilla de la ranura equipada, mediante `InventoryManager`.
- Solo existe el cultivo de lechuga; no se han añadido cultivos.
- Archivos: `scripts/agriculture/GrowingStation.cs`, `tests/test_agriculture.gd`, `tests/test_expansion.gd`, `tests/test_quickbar.gd`.
- Pruebas headless:
  - `test_quickbar.gd`: passed, código 0. Incluye sin semilla equipada (no consume, no planta, mensaje) y con lechuga equipada (mensaje).
  - `test_agriculture.gd`: passed. Planta con la semilla equipada, cosecha y devuelve una semilla.
  - `test_expansion.gd`: passed. Las estaciones de los módulos nuevos plantan con la semilla equipada.
  - `test_scene.tscn`: passed, sin excepciones.
- Nota: en la última ejecución no quedó capturado el código de salida de `test_agriculture`, `test_expansion` y `test_scene`. Los tres mostraron su mensaje de éxito y no hubo errores de script.
- Pendiente de verificación visual en el editor: que el mensaje contextual cambie según la ranura equipada y que plantar desde el HUD funcione con E.

---

## Corrección de arrastre inventario → barra rápida (Bloque C)

- **Causa:** la raíz del inventario (capa 15) era un `Control` con `MouseFilter = Stop` a pantalla completa, así que capturaba los eventos del ratón sobre la barra rápida (capa 10). Además, el panel centrado solapaba la zona de las ranuras.
- **Solución:** en `InventoryScreen`, raíz y fondo con `MouseFilter = Ignore`; el panel ocupa el espacio por encima de la barra mediante anclajes (margen superior 56 px, inferior 170 px).
- **Archivos:** `scripts/interfaces/InventoryScreen.cs`, `tests/test_inventory.gd`.
- **Pruebas headless:** `test_inventory.gd` passed (comprueba el filtro de la raíz y que el panel no tapa la barra en 1280×720). `test_quickbar.gd` y `test_scene.tscn` passed.
- **Limitación:** con alturas inferiores a unos 650 px, el panel (altura mínima ≈436 px) podría solaparse con la barra. Pendiente de decisión si se quiere soportar esas resoluciones.
- **Pendiente de verificación visual:** arrastre real a cada una de las tres ranuras, sustitución, soltar fuera y control del cursor con el inventario abierto.
- **Punto de control Git:** rama `checkpoint/precierre-3-2` creada en el commit `616f746`. No contiene los cambios sin confirmar, que siguen en el árbol de trabajo. No se ha hecho ningún commit.

---

## Cierre de la subfase 3.2 — COMPLETADA

**Validación visual del supervisor (confirmada):**
- Las paredes de los módulos expandidos están completas y no presentan agujeros accidentales.
- El límite de cuatro módulos funciona y la interfaz impide adquirir ampliaciones adicionales.
- Las pruebas visuales de 3.1 y 3.2 se han realizado; el supervisor ha confirmado el funcionamiento del HUD, el inventario, la barra rápida y el arrastre tras la corrección.

**Prueba automatizada añadida:**
- `tests/test_expansion_chain.gd`: expansión frontal desde la base, expansión derecha encadenada desde el módulo frontal y expansión izquierda desde la base. Comprueba cobro único, límite de cuatro módulos, caras compartidas retiradas, paredes completas donde corresponde, hueco de la oficina conservado y una estación de un módulo encadenado que planta y consume una semilla. Resultado: passed.
- `tests/test_expansion.gd` sigue passed tras añadir `ExpansionManager.PurchaseFrom`.

**Cobertura de direcciones:** con el límite de 4 módulos no es posible encadenar las cuatro direcciones en una sola configuración, porque serían necesarias cuatro compras además del base. La cobertura queda así:
- Izquierda: desde la base y en la prueba encadenada.
- Frente: desde la base; también en la prueba encadenada.
- Derecha: encadenada desde un módulo frontal (la base tiene la derecha bloqueada por la oficina).
- Atrás: desde la base, en `test_expansion.gd`.

Validar las cuatro direcciones encadenadas exigiría un límite mayor. Queda como decisión para más adelante.

**Pendientes no bloqueantes, trasladados al cierre del Día 3:**
- Aviso de "Semillas insuficientes" probado en una estación real.
- Prioridad de una estación ocupada frente a una ranura equipada.
- Altura mínima del panel de inventario en resoluciones inferiores a unos 650 px.
- `test_loader.gd` devuelve código 1 al cerrar por fugas del motor (problema heredado).

**Estado de la subfase 3.2: COMPLETADA.**

---

## Subfase 3.3 — Bloque 1: rediseño del panel administrativo (COMPLETADO, validado visualmente por el supervisor)

- **Validación visual del supervisor:** confirmada para el Bloque 1.
- **Errores del editor observados durante la validación:** avisos repetidos de `scene/gui/text_edit.cpp` (gutter). Son errores internos del editor de Godot 4.7, ya documentados como heredados. No provienen del panel.

- **Estructura:** marco al 88 % × 86 %, encabezado con identidad y saldo, navegación lateral de 190 px, área central dinámica y pie con el último mensaje.
- **Secciones:** Resumen, Tienda, Almacén, Finanzas, Gestión y Expansión. Se conservan las operaciones de compra, venta y expansión, las confirmaciones, la animación de apertura y cierre, y el bloqueo del jugador.
- **Componentes nuevos** (`scripts/management/`): `PanelTheme` (colores y estilos), `PanelWidgets` (`GlyphControl`, `NavButton`, `ItemIcon`, `StatCard`, `ProductCard`, `OccupancyBar`, `TableTile`) y `ExpansionDiagram`.
- **Cambio en expansión:** `ExpansionManager.Modules`, de solo lectura, para que el diagrama lea los módulos. No se ha modificado la lógica de expansión.
- **Archivos:** `scripts/management/ManagementPanel.cs` (reescrito como controlador), nuevos `PanelTheme.cs`, `PanelWidgets.cs`, `ExpansionDiagram.cs`, `tests/test_panel.gd`, y `scripts/expansion/ExpansionManager.cs`.
- **Pruebas headless:** `test_panel.gd` passed (abre y cierra en 1152×648, 1280×720 y 1600×900; el marco queda dentro de la ventana; el saldo no cambia). `test_expansion.gd` y `test_scene.tscn` passed.
- **Pendiente de validación visual:** aspecto de cada sección, legibilidad, scroll de Finanzas y Tienda, diagrama de expansión y funcionamiento real de compras y ventas desde el panel.
- **Limitación:** las pruebas headless no comprueban solapamientos ni contenido cortado dentro del panel, solo que el marco queda dentro de la ventana.

---

## Subfase 3.3 — Bloque 2: diversidad agrícola (implementado, pendiente de validación visual)

- **Cultivos:** lechuga, zanahoria, tomate, pepino y espinaca.
- **Arquitectura:** `CropCatalog` con `CropDefinition` (estática) y `CropData` (estado de cada planta). Detalles en `TECHNICAL_DECISIONS.md`.
- **Integración:** tienda con cuatro semillas nuevas (precio provisional 4 $), inventario, Quickbar, plantación con consumo de una semilla, cosecha con producto y semilla devuelta, venta genérica por producto (precio provisional 12 $).
- **Visuales:** representación procedural por cultivo en `CropVisualBuilder`, reutilizando los grupos de etapas de `lettuce.tscn`.
- **Escenas nuevas:** `scenes/crops/carrot.tscn`, `tomato.tscn`, `cucumber.tscn`, `spinach.tscn`, generadas desde la escena de la lechuga.
- **Pruebas headless:**
  - `test_crops.gd`: passed. Cubre los cinco cultivos: compra, inventario, Quickbar, plantación, consumo, tipo de cultivo, visuales, cosecha, semilla devuelta, venta y rechazo sin existencias.
  - `test_inventory.gd`: passed. Corregido el solapamiento que aparecía al añadir cultivos (cuadrícula de 5 columnas).
  - `test_agriculture.gd`, `test_quickbar.gd`, `test_economy.gd`, `test_scene.tscn`: passed.
- **Limitaciones:**
  - La madurez de los cultivos nuevos se fuerza en la prueba; no se verificó el crecimiento completo de cada uno.
  - Todos los cultivos usan la velocidad de crecimiento de referencia; sus parámetros propios llegan con el balance del Bloque 3.
  - Plantar cultivos nuevos en estaciones expandidas no está probado de forma explícita.
  - Los precios de venta y de semilla de los cultivos nuevos son provisionales y no forman parte del balance aprobado.
- **Pendiente de validación visual del supervisor:** que cada cultivo se distinga en la estación, que las etapas se vean correctas y que la tienda, el inventario y el HUD muestren los cultivos nuevos.

### Bloque 2 — actualización tras el Bloque 3

- Crecimiento completo de cada cultivo comprobado en tiempo real (`test_growth.gd`).
- Plantación y cosecha de un cultivo nuevo en estación expandida comprobadas (`test_growth.gd`).
- Inventario de cinco columnas: comprobado numéricamente que no solapa la barra a 1280×720 (`test_inventory.gd`); validación visual pendiente.
- Pendientes visuales: cultivos, etapas, tienda, inventario y HUD.

---

## Subfase 3.3 — Bloque 3: balance económico (implementado, pruebas headless pasan)

- **Valores aprobados** según la tabla de `TECHNICAL_DECISIONS.md`.
- **Fuente única:** `CropDefinition` define semilla, venta, velocidad, rendimiento, semillas devueltas y consumo de nutrientes. `EconomyManager` consulta `CropCatalog`; ya no contiene precios de cultivos.
- **Consumo de nutrientes por cultivo:** `GrowthSimulator` usa la definición del cultivo.
- **Pruebas headless:** `test_crops.gd`, `test_growth.gd`, `test_economy.gd`, `test_agriculture.gd`, `test_quickbar.gd`, `test_inventory.gd`, `test_expansion.gd` y `test_scene.tscn` pasan.
- **Pendiente:** validación visual del balance y de los cultivos en el juego.

## Estado de la subfase 3.3

- Bloque 1: completado y validado visualmente.
- Bloque 2: implementado; pruebas automatizadas pasan; pendiente validación visual.
- Bloque 3: implementado; pruebas automatizadas pasan; pendiente validación visual.
- **Día 3: no cerrado.** Faltan las validaciones visuales del Bloque 2 y del Bloque 3 para cumplir los criterios de cierre.
- **Fase 4 (Robots): no iniciada.**

---

## Fase de entrega — Robot agrícola programable (demostración, pendiente de validación visual)

- **Robot:** `AgroRobot` (`scripts/robots/RobotController.cs`), junto a la estación `GrowingStation0`. Se abre con E.
- **Sensor:** `leerHumedad()` devuelve el agua real del cultivo de la estación (0-100).
- **Actuador:** `activarRiego()` / `desactivarRiego()` aplican 8 unidades de agua por segundo al cultivo real (`CropData`).
- **Intérprete** (`scripts/robots/RobotInterpreter.cs`): subconjunto de C/C++ tipo Arduino. Soporta `void setup()`, `void loop()`, variables `float/int/bool`, `if`/`else`, comparaciones, `&&`, `||`, aritmética básica, `delay(ms)`, `Serial.begin/print/println`, `leerHumedad`, `activarRiego`, `desactivarRiego`. No ejecuta código arbitrario; los errores indican línea.
- **Panel** (`scripts/robots/RobotPanel.cs`): editor, Validar, Ejecutar, Detener, consola y estado. ESC cierra. Bloquea al jugador mientras está abierto.
- **Limitaciones:** no hay `leerTemperatura()` (el juego no tiene sensor de temperatura); el bucle `loop()` pausa 50 ms por iteración; sin programación por bloques; sin comunicación con hardware real.
- **Pruebas headless:** `test_robot.gd` passed (error de sintaxis con línea, función no admitida, riego real, umbral respetado, detención). `test_scene.tscn` y `test_quickbar.gd` passed.
- **Pendiente de validación visual:** apariencia del robot, apertura del panel, edición, consola y controles.

---

## MVP de entrega — robots agrícolas autónomos (headless validado)

- **Ciclo:** el robot de cada módulo busca una estación madura de su propio módulo, se desplaza a ella, cosecha con la lógica existente (`GrowingStation.Interact`), replanta con la semilla devuelta, transporta hasta el baúl del módulo y deposita. Capacidad de carga: 6 unidades.
- **Baúl:** `scripts/robots/Chest.cs`. E abre el panel; "Recoger 1" y "Recoger todo" transfieren al inventario del jugador, que sigue siendo la única fuente de cantidades. La venta usa `EconomyManager`/`ManagementPanel` existentes.
- **Módulos:** cada módulo (base y expansiones) tiene exactamente un robot y un baúl; se crean dentro de `ExpansionManager.BuildModuleRoot`, por lo que una compra fallida no deja huérfanos. Las plantillas excluyen robots y baúles.
- **Panel administrativo:** la pestaña Gestión muestra por módulo el robot, su tarea, su carga y el contenido del baúl.
- **Cambio en `GrowingStation`:** `PlantCrop` separa la plantación de la semilla del inventario; el jugador y el robot la usan igual.
- **Pruebas headless:** `test_robot_agent.gd` passed (cosecha de la madura, no de la inmadura, replantación con la semilla devuelta, depósito sin duplicar, transferencia, venta, un robot y un baúl por módulo). `test_robot.gd`, `test_expansion.gd`, `test_crops.gd`, `test_panel.gd`, `test_scene.tscn` passed.
- **Limitaciones:**
  - El desplazamiento es en línea recta, sin navegación ni evitación de obstáculos: el robot puede atravesar mesas visualmente.
  - El inventario del jugador no tiene capacidad máxima (no existe en el proyecto); la transferencia desde el baúl no la limita.
  - No hay animaciones ni estados visuales adicionales.
- **Pendiente de validación visual:** posición del robot y del baúl en cada módulo, recorrido, depósito y transferencia desde el panel del baúl.

---

## Corrección de navegación del robot (headless validado)

- **Solución:** `scripts/robots/AisleNavigator.cs`. Cuadrícula de 0,25 m sobre el suelo del módulo; obstáculos de mesas, canales, estaciones, baúles y columnas expandidos por el radio del robot (0,4 m); ruta A* por celdas libres. El robot sigue la ruta punto a punto (`FollowPath`) en coordenadas locales de su módulo.
- **Acceso a las estaciones:** se elige el primer punto libre a 0,95 m de la estación; en la práctica, el pasillo entre filas de mesas.
- **Sin ruta:** el robot no se mueve, deja un aviso en su consola y reintenta a los 5 s.
- **Sin cambios:** cosecha, replantación, carga, baúles, expansiones y panel administrativo.
- **Archivos:** `AisleNavigator.cs` (nuevo), `RobotController.cs`, `tests/test_navigation.gd` (nuevo), `tests/test_robot_agent.gd` (la comprobación de replantación pasa a exigir cultivo nuevo sin madurar, porque el recorrido ahora dura más).
- **Pruebas headless:** `test_navigation.gd` passed (0 muestras con colisión en el invernadero base ni en un módulo expandido; cosecha y depósito dentro de cada módulo). `test_robot_agent.gd` passed.
- **Limitaciones:** la ruta no evita a otros robots ni personas; no hay animación de giro; no se ha validado visualmente el recorrido.

---

## Programación funcional del robot (headless validado)

- **Obediencia:** el robot solo actúa mediante su programa. Sin programa en ejecución no cosecha, no replanta ni transporta. Detener el programa cancela la acción en curso.
- **Funciones del programa:**
  - `configurarVelocidad(v)`: velocidad real, limitada a 0,5–5,0 m/s.
  - `hayPlantasEnModulo()`: 1 si alguna estación del módulo tiene planta.
  - `activarCosecha()`: habilita la cosecha. Sin ella, `recolectar()` produce error.
  - `recolectar()`: va a cada planta madura de su módulo y la cosecha con la lógica existente. No toca plantas inmaduras.
  - `replantar()`: planta cultivos en estaciones libres con las semillas que el robot tiene.
  - `moverABaul()`: transporta la carga al baúl de su módulo y la deposita.
  - `delay(ms)`, `Serial.print/println`, `setup()`, `loop()`, variables, `if/else` y expresiones, como antes.
- **Eliminado:** `leerHumedad`, `activarRiego` y `desactivarRiego`, junto con el riego automático y el comportamiento autónomo anterior.
- **Acciones:** son bloqueantes dentro del programa (el intérprete espera a que el robot termine la acción, sin bloquear el hilo principal). Un fallo de la acción detiene el programa e indica la línea.
- **Editor:** el programa de ejemplo usa `configurarVelocidad`, `activarCosecha`, `recolectar`, `replantar` y `moverABaul`. Muestra módulo, velocidad, carga, acción actual, línea en ejecución y consola.
- **Archivos:** `RobotInterpreter.cs`, `RobotController.cs`, `RobotPanel.cs`, `tests/test_robot.gd`, `tests/test_robot_agent.gd`, `tests/test_navigation.gd`.
- **Pruebas headless:** `test_robot.gd` passed (sintaxis, función desconocida, velocidad y límites, recolectar sin activarCosecha, detención), `test_robot_agent.gd` passed (ciclo completo con el programa, sin duplicación), `test_navigation.gd` passed (rutas sin colisiones con el programa), `test_panel.gd` y `test_scene.tscn` passed.
- **Pendiente de validación visual:** edición y ejecución del programa, velocidad visible y comportamiento del robot en el juego.

---

## Menú principal y persistencia local (implementado, pendiente de validación visual)

- **Menú principal** (scenes/menu/main_menu.tscn, escena principal): fondo con el invernadero real (sin jugador ni HUD activos), Nueva partida, Continuar (deshabilitado sin partidas), Cargar partida, Opciones y Salir.
- **Partidas:** lista con nombre, fecha y dinero. Eliminar pide confirmación. Partidas dañadas o de versión incompatible se marcan y no se cargan.
- **Nueva partida:**  MXN reales (EconomyManager.StartingBalance = 100), inventario inicial, sin robots comprados.
- **Guardado:** user://saves/<id>.json, escritura atómica (temporal y renombrado). Manual desde el menú de pausa, automático cada 5 minutos (desactivable en Opciones, guardado en user://settings.json) y al cerrar la ventana.
- **Menú de pausa:** Esc abre Continuar, Guardar, Cargar, Volver al menú principal y Salir del juego. Cargar, volver al menú y salir piden confirmación.
- **Contenido guardado:** saldo e historial, inventario, estaciones con cultivo, etapa, progreso, agua, nutrientes y salud, módulos expandidos, baúles, almacén central, robots agrícolas (carga, semillas, fertilizante, programa, pausa), recolectores (posición, velocidad, capacidad, carga, programa, pausa) y administrativo (configuración, totales, programa, pausa).
- **Restauración:** reconstruye el mundo sin cobrar expansiones ni robots y sin duplicar nodos. Las tareas en curso no se reanudan (las cosechas, compras y transferencias empiezan de nuevo desde el estado guardado); el programa vuelve a empezar desde su primera instrucción. Las solicitudes entre robots pendientes tampoco se guardan.
- **Pruebas:** 	est_save.gd cubre guardado, cambios posteriores, restauración en un mundo nuevo, robots sin duplicar, pausa conservada y archivo dañado. Pasan también las pruebas de economía, robots, interacción, abastecimiento, panel y escena.
- **Pendiente:** validación visual del menú y del menú de pausa; verificación del autoguardado a los 5 minutos y del guardado al cerrar en partida real; flujo completo Nueva partida → jugar → Salir → Continuar.

---

## Release Beta — estado (parcial)

- **Menú principal:** corregido. El fondo ya no contiene jugador ni HUD, el cursor queda visible en el menú y los controles del jugador no están activos hasta iniciar una partida. Prueba headless `test_menu.gd` aprobada: menú sin HUD ni jugador; Nueva partida abre el invernadero con $100 y jugador activo. El bloqueo real del cursor en partida no se puede comprobar en headless: pendiente de validación visual.
- **Exportación Windows x64:** completada con `export_presets.cfg` (`Windows Desktop`). Runtime .NET 8 autocontenido: no requiere .NET instalado. Ejecutable de prueba en `Escritorio\SMART_GREENHOUSE_BETA\SMART_GREENHOUSE.exe` y ZIP `Escritorio\SMART_GREENHOUSE_BETA.zip`; ambos arrancan headless sin errores de script (menú y escena de invernadero cargados).
- **Limitación:** el `.pck` incluye los archivos fuente `.cs`, porque el cargador de escenas de Godot Mono los necesita; excluirlos rompe la carga de escenas.
- **README de distribución:** `distribution/README.md` (copiado como `LEEME.txt`).
- **Pendiente:** validación visual del ejecutable exportado y de la entrega en GitHub.
