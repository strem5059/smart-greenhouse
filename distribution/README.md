# Smart Greenhouse — Beta

Invernadero agrícola automatizado con robots programables. Juego de simulación en 3D para Windows.

## Requisitos

- Windows 10 u 11, de 64 bits (x64).
- Tarjeta gráfica compatible con Vulkan (Forward+).
- No hace falta instalar .NET: el runtime viene incluido en la carpeta del juego.
- Espacio libre: unos 300 MB.

## Ejecución

1. Descomprime el ZIP en cualquier carpeta.
2. Abre `SmartGreenhouse.exe`.
3. No hace falta instalar Godot ni Visual Studio.

## Menú principal

- **Nueva partida:** empieza con **$100 MXN** y el inventario inicial.
- **Continuar:** carga la partida guardada más reciente.
- **Cargar partida:** lista las partidas guardadas con nombre, fecha y dinero. Eliminar pide confirmación.
- **Opciones:** activa o desactiva el guardado automático.
- **Salir:** cierra el juego.

## Controles

| Acción | Control |
|---|---|
| Moverse | W A S D |
| Mirar | Ratón |
| Saltar | Espacio |
| Cambiar cámara | V |
| Interactuar (estaciones, baúles, robots) | E |
| Inventario | Tab |
| Barra rápida | 1, 2, 3 |
| Menú de pausa | Esc |

Para abrir el editor de programación de un robot, acércate al robot y pulsa **E**. Dentro del editor el jugador no se mueve; para cerrarlo usa el botón Cerrar o Esc.

## Guardado

- **Manual:** Esc → Guardar partida.
- **Automático:** cada 5 minutos, si está activado en Opciones.
- **Al cerrar:** el juego guarda la partida al cerrar la ventana.

Las partidas se guardan en:

`%APPDATA%\Godot\app_userdata\Smart Greenhouse\saves\`

Para hacer copias de seguridad, copia esa carpeta.

## Limitaciones conocidas

- Las tareas en curso no se guardan: al cargar, cosechas, compras y transferencias empiezan de nuevo desde el estado guardado.
- Los programas de los robots vuelven a empezar desde su primera instrucción al cargar.
- Las solicitudes pendientes entre robots no se guardan.
- Los robots no se esquivan entre sí.
- No hay límite de capacidad en el almacén central.
- Una partida guardada con una versión posterior del juego no se puede cargar.

## Si algo falla

- Si el juego no abre, comprueba que no has movido archivos fuera de la carpeta: el `.exe`, el `.pck` y la carpeta `data_SmartGreenhouse_windows_x86_64` deben permanecer juntos.
- Si una partida aparece como "archivo dañado", no se puede cargar; las demás partidas no se ven afectadas.
