# Decisiones técnicas — SMART GREENHOUSE

## Diversidad agrícola: definiciones estáticas separadas del estado de cada planta

**Problema.** La lechuga estaba escrita a mano en varios sistemas (plantación, cosecha, mensajes, venta y HUD). Añadir cultivos habría duplicado esa lógica.

**Decisión.**
- `CropDefinition` (`scripts/agriculture/CropCatalog.cs`): datos estáticos de cada cultivo (identificador, nombre, semilla, producto, velocidad de crecimiento, rendimiento y semillas devueltas).
- `CropCatalog`: registro único. Valida que los identificadores sean únicos y no vacíos y ofrece consultas por cultivo, semilla o producto.
- `CropData`: estado de una planta concreta (etapa, progreso, agua, nutrientes, salud y `CropType`). No copia las definiciones.
- `ItemCatalog` sigue siendo la fuente de nombres e iconos de los artículos. `EconomyManager` conserva los precios.
- Las escenas visuales por cultivo se cargan desde `scenes/crops/<id>.tscn`.

**Alternativas consideradas.**
- Recursos `.tres` de Godot: editables desde el editor, pero añaden archivos y pasos manuales.
- Mantener las cadenas fijas: rechazado por duplicar lógica.

**Consecuencias.**
- Añadir un cultivo requiere una entrada en `CropCatalog`, en `ItemCatalog` y una escena o variante visual.
- Los parámetros de balance (precios, tiempos, consumo) se ajustarán en el Bloque 3 sin tocar la lógica.

**Sistemas afectados.** `GrowingStation`, `GrowthSimulator`, `PlantController`, `CropData`, `EconomyManager`, `ItemCatalog`, `GameHud`, `ManagementPanel` y las escenas de cultivo.

## Robot agrícola programable: intérprete restringido, no compilador

**Problema.** La entrega necesita demostrar un programa tipo Arduino que controle el riego, sin el coste ni el riesgo de ejecutar código del usuario.

**Decisión.** `RobotInterpreter` analiza y ejecuta un subconjunto fijo del lenguaje. Las funciones disponibles se conectan a `IRobotHardware`, implementado por el robot en el juego. La ejecución es cooperativa: `delay()` cede el control al bucle de Godot mediante un iterador, sin bloquear el hilo principal. Hay límite de pasos por cuadro y se detiene al fallar.

**Alternativas descartadas.** Compilar C++ real o ejecutar código dinámico (riesgo y complejidad); programación por bloques (se deja como alternativa futura sobre el mismo intérprete).

**Consecuencias.** Añadir una función es añadir un caso en `Invoke` y su operación en `IRobotHardware`. El intérprete no conoce Godot, así que se puede probar y reutilizar.

## Balance económico de los cinco cultivos (Bloque 3, aprobado)

**Decisión.** Los precios de semilla y venta, la velocidad de crecimiento, el rendimiento y el consumo de nutrientes viven en `CropDefinition`. `EconomyManager` consulta el catálogo y no conserva precios propios de cultivos; sus únicos precios fijos son el fertilizante y la expansión.

**Valores aprobados:**

| Cultivo | Semilla | Venta/u | Rendimiento | Crecimiento | Nutrientes/s |
|---|---:|---:|---:|---:|---:|
| Lechuga | 4 | 12 | 1 | 21 s | 0,5 |
| Espinaca | 3 | 6 | 1 | 12 s | 0,5 |
| Zanahoria | 5 | 15 | 1 | 30 s | 0,5 |
| Pepino | 8 | 24 | 1 | 48 s | 0,5 |
| Tomate | 10 | 16 | 2 | 60 s | 1,0 |

**Reglas.** Todos los cultivos devuelven una semilla por cosecha. El riego es gratuito e ilimitado. El fertilizante cuesta 6 y aporta 30 nutrientes. La velocidad se expresa en puntos por segundo; cada etapa necesita 100 puntos.

**Razonamiento.** Con la semilla devuelta, el coste de semilla solo pesa en el primer ciclo; el beneficio por ciclo es igual al ingreso por venta. Así, todos los cultivos rinden alrededor de 30 por minuto (lechuga 34), y la variedad viene del tiempo, el rendimiento y la necesidad de recursos. El tomate necesita fertilizante por su consumo de 1,0/s, lo que da al fertilizante una función concreta.

**Alternativas descartadas.** Devolver semilla solo en la lechuga (dejaba a la lechuga claramente dominante) y no devolver semilla en ningún cultivo (cambiaba una regla validada).

**Verificación.** `test_growth.gd` comprueba el crecimiento real en su tiempo, el consumo de nutrientes, el riego y la fertilización del tomate, la cosecha con el rendimiento correcto y un cultivo en estación expandida.
