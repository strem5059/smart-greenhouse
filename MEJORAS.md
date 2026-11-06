# MEJORAS.md — Hoja de ruta de SMART GREENHOUSE

> Documento de diseño. Describe mejoras **propuestas** para futuras versiones.
> Nada de lo descrito en las secciones "Propuesta" existe todavía en el código.

---

## 1. Visión general

**Objetivo.** Convertir SMART GREENHOUSE en un invernadero automatizado en el que el jugador pueda delegar, de forma progresiva, la producción, la logística, la administración y la economía.

**Principios de diseño.**

1. **Transparencia.** Todo robot informa de lo que hace, por qué lo hace y qué resultado obtiene.
2. **Control del jugador.** La autonomía es opcional, configurable y reversible en cualquier momento.
3. **Tiempo real del juego.** La automatización no genera recursos ni acelera el crecimiento de los cultivos.
4. **Una única fuente de verdad.** Cada inventario y cada operación económica tiene un único responsable.
5. **Reutilización.** Las nuevas capacidades se apoyan en los sistemas existentes antes de crear otros nuevos.

**Resultado esperado.** El jugador puede dejar el invernadero funcionando durante largos periodos (modo AFK) con intervención mínima, manteniendo reservas, presupuestos y alertas que le permitan intervenir cuando sea necesario.

---

## 2. Estado actual (implementado)

Esta sección describe **solo** lo que existe en el código a fecha de este documento.

| Área | Sistema | Estado | Notas |
|---|---|---|---|
| Invernadero | Escena principal, 24 estaciones base, cristales, mesas, canales y tuberías | Implementado | `scenes/world/greenhouse.tscn` |
| Expansión | `ExpansionManager`: módulos Izquierda, Derecha, Frente y Atrás | Implementado | Límite de beta de 4 módulos. Cada módulo añade 24 estaciones |
| Agricultura | `GrowingStation`, `PlantController`, `CropData`, `GrowthSimulator` | Implementado | Etapas, agua, nutrientes, salud, cosecha |
| Cultivos | `CropCatalog`: lechuga, zanahoria, tomate, pepino, espinaca | Implementado | Semilla, producto, velocidad, rendimiento y consumo por cultivo |
| Inventario | `InventoryManager` (fuente única de cantidades), `ItemCatalog` | Implementado | Sin límite de capacidad |
| Interfaz | `GameHud`, `InventoryScreen`, `QuickbarBar`, `Quickbar` | Implementado | TAB, barra rápida con teclas 1-3, arrastrar y soltar |
| Economía | `EconomyManager`: compras, ventas, historial, saldo | Implementado | Precios de cultivos leídos de `CropCatalog` |
| Administración | `ManagementPanel`: Resumen, Tienda, Almacén, Finanzas, Gestión, Expansión | Implementado | Lista de robots por módulo en Gestión |
| Robot agrícola | `RobotController` (una unidad por módulo) | Implementado | Busca cultivos maduros, cosecha, replanta, transporta |
| Navegación | `AisleNavigator`: cuadrícula de 0,25 m y A* por pasillos | Implementado | No atraviesa bases ni paredes |
| Almacenamiento | `Chest`: un baúl por módulo | Implementado | E abre; recoger una unidad o todo |
| Programación | `RobotInterpreter`: subconjunto de C/C++ tipo Arduino | Implementado | `setup`, `loop`, `if`, `delay`, `Serial`, `leerHumedad`, `activarRiego`, `desactivarRiego` |

**Limitaciones conocidas actuales:**
- El robot agrícola no tiene capacidad de inventario ni prioridades configurables.
- No existe persistencia de partida.
- El riego del robot programable y el riego gratuito de la estación son independientes de la autonomía agrícola.
- No hay robots de oficina ni recolector central.

---

## 3. Ecosistema de robots

El ecosistema se compone de cuatro categorías. Solo la primera existe hoy, en versión básica.

### 3.1. Robots agrícolas (existente, a ampliar)

**Responsabilidades.**
- Recorrer su módulo por los pasillos.
- Identificar cultivos maduros de su propio módulo.
- Cosechar con la lógica existente de la estación.
- Replantar con semillas que ya posee.
- Transportar sus productos a su baúl.
- Informar de su estado y productividad.

**Reglas.**
- Un robot agrícola por módulo, asignado de forma exclusiva.
- Nunca cosecha cultivos inmaduros.
- No crea recursos: solo mueve los que devuelve cada cosecha.
- Si no tiene semilla para replantar, deja la estación libre y continúa.

**Ampliación propuesta.** Exponer en la interfaz tarea, carga, productividad acumulada y motivo de bloqueo.

### 3.2. Robot recolector central (propuesta)

**Compra.** Desde `ManagementPanel`, sección **Comprar robots**. Precio configurable.

**Funciones.**
- Recorrer los baúles de todos los módulos.
- Recoger los productos depositados por los robots agrícolas.
- Transportarlos al **almacén central** de la oficina.
- Registrar cantidades por módulo y por cultivo.
- Avisar cuando un baúl está vacío o cuando hay productos pendientes.
- No vender por su cuenta: solo entrega al sistema administrativo.

**Capacidad.** Inventario de carga con capacidad configurable (valor inicial propuesto: 12 unidades). Cada viaje se registra como tarea.

**Dependencias.** `Chest` (lectura y retirada), `CentralWarehouse` (depósito), `LogisticsManager` (tareas).

### 3.3. Robot administrativo de oficina (propuesta)

**Compra.** Desde `ManagementPanel`, sección **Comprar robots**. Se ubica en la oficina.

**Funciones.**
- Consultar existencias de insumos, cosechas y dinero.
- Detectar necesidades de compra según la regla de reposición (sección 4).
- Solicitar al recolector los productos necesarios.
- Vender cosechas cuando falte dinero y esté autorizado (sección 4.2).
- Comprar fertilizantes, semillas y otros insumos.
- Recomendar cultivos según rentabilidad y disponibilidad (sección 5).
- Informar decisiones y resultados (sección 8).

**Interfaz.** Pestaña propia dentro de `ManagementPanel` con su estado, su historial y sus parámetros.

**Límite.** No inventa dinero, productos ni recursos. Todas sus operaciones pasan por `EconomyManager` y `InventoryManager`.

### 3.4. Robots de apoyo futuros (solo documentación)

| Robot | Función prevista | Prioridad |
|---|---|---|
| Mantenimiento | Revisar robots y estaciones, reparar averías | Baja |
| Inspección | Medir condiciones del cultivo y reportar anomalías | Baja |
| Transporte de insumos | Llevar semillas y fertilizante desde el almacén a las estaciones | Media |
| Reparación | Reactivar robots detenidos | Baja |
| Supervisión | Registrar condiciones del invernadero y generar estadísticas | Baja |

No se implementarán hasta completar las etapas 1 a 6 (sección 12).

---

## 4. Sistema de compras automáticas

### 4.1. Inventario mínimo y reposición

Cada insumo tiene tres parámetros configurables:

| Parámetro | Descripción | Ejemplo |
|---|---|---|
| `stock_minimo` | Umbral por debajo del cual se solicita reposición | 10 |
| `multiplicador_reposicion` | Factor sobre el mínimo para calcular la compra | 3 |
| `prioridad` | Orden de atención cuando hay varias necesidades | Alta |

**Regla de reposición.**

```
si stock_actual < stock_minimo:
    cantidad_compra = stock_minimo * multiplicador_reposicion
```

Ejemplo: `stock_minimo = 10`, `stock_actual = 9`, `multiplicador = 3` → se compran `30` unidades.

**Comprobaciones antes de comprar.**
1. El artículo está realmente por debajo del mínimo en el momento de la compra.
2. Existe dinero suficiente después de respetar la reserva (sección 4.2).
3. La tienda permite la cantidad solicitada.
4. Existe espacio de almacenamiento para la cantidad.
5. La misma necesidad no se ha convertido ya en una compra pendiente (evitar duplicados).

### 4.2. Falta de dinero

Flujo cuando el robot administrativo no puede comprar:

1. Informa del insumo que falta y de la cantidad necesaria.
2. Consulta el saldo disponible.
3. Solicita al recolector los productos de los baúles.
4. Espera la entrega en el almacén central.
5. Identifica cosechas disponibles para vender.
6. Calcula la cantidad vendible **sin tocar las reservas** (sección 9).
7. Vende mediante `EconomyManager`.
8. Comprueba de nuevo el saldo.
9. Compra los insumos pendientes si ahora es posible.
10. Si el saldo sigue siendo insuficiente, detiene la compra y comunica el motivo.

**Restricciones.**
- Nunca vende todas las existencias: existe una reserva configurable por producto.
- Nunca vende semillas necesarias para mantener la producción.
- No puede generar dinero ni productos.

---

## 5. Recomendación inteligente de cultivos

### 5.1. Criterios de análisis

El robot administrativo calcula, para cada cultivo del catálogo:

| Criterio | Fuente de datos |
|---|---|
| Precio de venta | `CropDefinition.SalePrice` |
| Rendimiento por cosecha | `CropDefinition.Yield` |
| Tiempo de crecimiento | `CropDefinition.GrowthRate` |
| Coste de semillas | `CropDefinition.SeedPrice` |
| Coste de fertilizante | Consumo de nutrientes del cultivo |
| Existencias de semillas | `InventoryManager` |
| Demanda de insumos | Historial de compras y reposiciones |

**Métrica principal.** Beneficio neto por minuto de crecimiento, teniendo en cuenta semillas devueltas y coste de nutrientes.

### 5.2. Comportamiento esperado

Ejemplo de recomendación:

> "El tomate presenta actualmente el mayor beneficio estimado por minuto. Recomiendo aumentar su producción."

Si se agotan sus semillas:

> "No hay semillas de tomate disponibles. Recomiendo continuar temporalmente con lechuga."

**Reglas.**
- Comprueba que existan semillas antes de recomendar un cambio.
- No cambia de cultivo por diferencias pequeñas (umbral de histéresis configurable, por ejemplo 10 %).
- Mantiene reservas de un cultivo alternativo.
- La recomendación **no modifica** la producción automáticamente: solo la aplica si el jugador ha autorizado la prioridad correspondiente.

### 5.3. Autonomía total (futura)

Modalidad en la que el jugador define reglas de prioridad (por ejemplo, "priorizar beneficio por minuto", "mantener al menos 5 tomates", "no superar 40 % de la producción en un solo cultivo"). El robot aplica las reglas sin consultar, y registra cada decisión.

---

## 6. Configuración individual de robots

### 6.1. Parámetros comunes

| Parámetro | Descripción | Aplica a |
|---|---|---|
| Velocidad de movimiento | Metros por segundo | Todos |
| Capacidad de carga | Unidades transportables | Agrícola, recolector |
| Frecuencia de revisión | Intervalo entre comprobaciones | Administrativo, recolector |
| Prioridad de tareas | Orden de atención | Todos |
| Tiempo de espera | Pausa antes de reintentar | Todos |
| Radio de detección | Distancia de búsqueda de tareas | Agrícola |
| Módulo asignado | Zona de trabajo | Agrícola, recolector (lista) |
| Estado de actividad | Activo, pausado, detenido | Todos |

### 6.2. Control por el jugador

- **Detener** y **reanudar** cada robot desde su ficha.
- **Modificar** los parámetros configurables dentro de los límites definidos.
- **Consultar** tarea, estado, carga y última acción.

### 6.3. Programación del robot (idea futura)

Conservar el intérprete actual como base y ampliarlo con:
- Parámetros configurables.
- Secuencias de tareas predefinidas.
- Opcionalmente, bloques visuales.

**Restricciones de seguridad (obligatorias).**
- No se permite ejecución arbitraria de código.
- No se permite acceso al sistema operativo.
- Solo se invocan operaciones seguras del juego.
- Todo programa tiene límite de pasos por ciclo.

No se desarrollará en esta etapa.

---

## 7. Modo AFK y autonomía

### 7.1. Comportamiento

Con el modo activado:
- Los robots agrícolas continúan cosechando y replantando.
- El recolector vacía los baúles.
- El administrador revisa inventarios.
- Se reponen insumos si hay dinero suficiente.
- Se venden cosechas cuando es necesario y está autorizado.
- Se priorizan cultivos rentables según las reglas configuradas.
- Se reportan errores y bloqueos.
- Se evitan tareas duplicadas.
- Se mantienen las reservas de seguridad.

### 7.2. Controles del jugador

| Control | Función |
|---|---|
| Activar / desactivar autonomía | Interruptor general |
| Pausar todos los robots | Parada coordinada, reanudable |
| Detener un robot | Parada individual |
| Presupuesto máximo de compras | Límite por periodo de juego |
| Reserva mínima de dinero | No se gasta por debajo de ese saldo |
| Stock mínimo por insumo | Parámetro de la sección 4 |
| Reservas por producto | Cantidad que nunca se vende |
| Prioridades de producción | Orden de cultivos recomendados |

### 7.3. Restricciones

- El modo AFK **no genera recursos** ni acelera el crecimiento.
- Respeta los tiempos reales de cada cultivo.
- Cualquier decisión queda registrada en el historial (sección 8).

---

## 8. Alertas e informes

### 8.1. Eventos a comunicar

| Evento | Prioridad | Origen |
|---|---|---|
| Insumo por debajo del mínimo | Alta | Administrador |
| Compra completada | Media | Administrador |
| Compra fallida | Alta | Administrador |
| Dinero insuficiente | Alta | Administrador |
| Productos pendientes de recoger | Media | Recolector |
| Baúl lleno | Alta | Recolector |
| Robot detenido | Alta | Cualquiera |
| Cultivo sin semillas para replantar | Media | Agrícola |
| Producción detenida | Alta | Agrícola |
| Cultivo más rentable | Baja | Administrador |
| Cambio de prioridad | Baja | Administrador |
| Falta de espacio de almacenamiento | Alta | Recolector |

### 8.2. Historial de operaciones

Cada entrada registra:
- Tiempo de juego.
- Robot responsable.
- Acción.
- Producto.
- Cantidad.
- Resultado (éxito, fallo, motivo).

El historial es consultable desde `ManagementPanel`.

### 8.3. Evitar repetición

Un evento no se vuelve a notificar mientras su condición siga activa. Solo se notifica de nuevo si la condición desaparece y reaparece.

---

## 9. Gestión económica y prevención de fallos

### 9.1. Reglas

| Riesgo | Regla |
|---|---|
| Gastar dinero reservado | La reserva mínima nunca se usa para compras automáticas |
| Comprar por encima de la capacidad | Antes de comprar se comprueba el espacio libre |
| Vender productos reservados | La venta respeta la reserva por producto |
| Vender semillas necesarias | Las semillas de cultivos activos no se venden |
| Compras duplicadas | Cada necesidad tiene un identificador de pedido |
| Ciclos de compra y venta sin beneficio | Se rechaza vender algo que se compró en el mismo ciclo a menor precio |
| Espera indefinida | Cada tarea tiene un tiempo máximo; al agotarse pasa a "fallida" |
| Errores de transporte | Una carga que no llega se devuelve a su origen |
| Inconsistencias | Baúles, cargas, almacén e inventario se conciliarán en cada ciclo |

### 9.2. Protección ante quiebra

Si el saldo cae por debajo de un mínimo configurable y no hay productos vendibles, el sistema suspende las compras no esenciales y avisa al jugador.

---

## 10. Interfaz de administración

Ampliación propuesta de `ManagementPanel`, manteniendo el diseño actual:

| Sección | Contenido |
|---|---|
| Robots | Lista de todos los robots: módulo, tarea, estado, carga |
| Comprar robots | Oferta de recolector y administrador, con precio |
| Almacén central | Existencias consolidadas |
| Operaciones | Compras, ventas e historial |
| Recomendaciones | Ranking de cultivos y motivo |
| Autonomía | Modo AFK y controles de la sección 7 |
| Configuración | Parámetros de robots y reglas de reposición |

**Principios.** Reutilizar los componentes de `PanelTheme`. Evitar pantallas nuevas si una pestaña basta. Mantener las operaciones actuales intactas.

---

## 11. Arquitectura técnica propuesta

### 11.1. Componentes

| Componente | Responsabilidad | Relación con lo existente |
|---|---|---|
| `AgroRobot` / `RobotController` | Comportamiento agrícola | Existente. Se amplía con parámetros |
| `ExpansionManager` | Asignación de módulos | Existente |
| `Chest` | Almacén por módulo | Existente |
| `InventoryManager` | Inventario del jugador | Existente. Fuente única de cantidades |
| `EconomyManager` | Compras, ventas, saldo | Existente |
| `ManagementPanel` | Administración | Existente. Se amplía |
| `RobotManager` | Registro y coordinación de robots | Nuevo |
| `CentralWarehouse` | Almacén de la oficina | Nuevo |
| `LogisticsManager` | Tareas de recolección y transporte | Nuevo |
| `AdministrationRobot` | Compras, ventas y planificación | Nuevo |
| `ResourcePlanner` | Mínimos, reservas y reposición | Nuevo |
| `CropAdvisor` | Análisis de rentabilidad | Nuevo |
| `AutomationManager` | Coordinación del modo AFK | Nuevo |

Los nombres son propuestas. Si un componente existente cubre la función, se reutiliza.

### 11.2. Fuentes de verdad

| Dato | Fuente única |
|---|---|
| Cantidad de cada artículo del jugador | `InventoryManager` |
| Cantidad en baúles | `Chest` |
| Cantidad en almacén central | `CentralWarehouse` |
| Cantidad transportada | Carga del robot que la lleva |
| Saldo y transacciones | `EconomyManager` |
| Estado de tareas | `LogisticsManager` |

Una cantidad solo se mueve entre fuentes mediante una transferencia que descuenta en origen y añade en destino en la misma operación.

### 11.3. Tareas

Estados: **Pendiente**, **Asignada**, **En ejecución**, **Completada**, **Fallida**.

Reglas:
- Un recurso reservado por una tarea no puede ser asignado a otra.
- Dos robots no recogen los mismos productos.
- Una tarea fallida libera sus reservas.

### 11.4. Dependencias

```
AutomationManager
 ├── RobotManager ── robots agrícolas, recolector, administrador
 ├── LogisticsManager ── Chest, CentralWarehouse
 ├── AdministrationRobot ── ResourcePlanner, CropAdvisor, EconomyManager
 └── ManagementPanel (lectura y órdenes)
```

No deben existir dependencias circulares. La interfaz no contiene lógica de automatización.

---

## 12. Plan de implementación por etapas

| Etapa | Objetivo | Dependencias | Criterios de aceptación |
|---|---|---|---|
| **1. Configuración y control** | Detener y reanudar robots; parámetros básicos; visualización de tareas | `RobotController` | Un robot detenido no se mueve ni cosecha; su estado se muestra en el panel |
| **2. Recolector** | Compra desde el panel; visita a baúles; transporte al almacén central | Etapa 1, `Chest`, `CentralWarehouse` | Ningún producto se duplica ni se pierde en 50 transferencias de prueba |
| **3. Administrador** | Compra desde el panel; revisión de existencias; compras automáticas; control de presupuesto | Etapa 2, `ResourcePlanner` | Una compra se ejecuta una sola vez por necesidad; no se supera el presupuesto |
| **4. Economía autónoma** | Venta autorizada; reservas; reposición; historial | Etapa 3, `EconomyManager` | Las reservas nunca se venden; el historial registra cada operación |
| **5. Planificación agrícola** | Análisis de rentabilidad; recomendaciones; gestión de semillas; prioridades | Etapa 4, `CropAdvisor` | La recomendación cambia solo si supera el umbral de histéresis |
| **6. Modo AFK** | Coordinación de todos los robots; ciclos autónomos; recuperación de errores; alertas | Etapas 1 a 5 | Durante 30 minutos de juego sin intervención no hay tareas duplicadas ni robots bloqueados sin aviso |
| **7. Ampliaciones** | Programas editables; robots especializados; mejoras de navegación | Etapa 6 | Cada ampliación tiene su propio criterio de aceptación antes de empezar |

**Regla general.** Cada etapa termina con pruebas automatizadas de sus reglas y validación visual del jugador. No se inicia una etapa sin cerrar la anterior.

---

## 13. Ideas adicionales priorizadas

| Prioridad | Idea | Utilidad |
|---|---|---|
| Alta | Recuperación automática de robots atascados | Evita bloqueos silenciosos en AFK |
| Alta | Reservas de semillas por cultivo | Garantiza continuidad de producción |
| Alta | Detección de estaciones sin producir | Señala desperdicio de capacidad |
| Media | Capacidad y saturación de almacenes | Hace significativa la logística |
| Media | Priorización de cosechas | Permite decidir qué se recoge antes |
| Media | Estadísticas de productividad | Sustenta las recomendaciones |
| Media | Coste operativo de robots | Equilibra la inversión en automatización |
| Media | Alertas de falta de recursos | Complementa la sección 8 |
| Baja | Mantenimiento preventivo de robots | Añade gestión sin bloquear el juego |
| Baja | Programación horaria | Útil cuando exista AFK |
| Baja | Objetivos económicos automáticos | Motivación a largo plazo |
| Baja | Control de autonomía por módulo | Flexibilidad para el jugador |
| Baja | Gestión de energía o batería | Mecánica opcional; no es prioritaria |
| Baja | Protección ante quiebra | Ya cubierta parcialmente en la sección 9 |

---

## 14. Riesgos principales

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Duplicación de productos o semillas | Alto | Fuente única y transferencias atómicas (sección 11.2) |
| Robots bloqueados sin aviso | Alto | Tiempo máximo por tarea y alertas (sección 8) |
| Compras sin control de presupuesto | Alto | Reserva mínima y presupuesto máximo (sección 7.2) |
| Ciclos económicos de pérdida | Medio | Regla de no vender lo recién comprado a menor precio (sección 9) |
| Complejidad de la interfaz | Medio | Reutilizar `ManagementPanel` y `PanelTheme` |
| Rendimiento con muchos robots | Medio | Revisiones espaciadas y tareas asíncronas |
| Desincronización entre paneles | Medio | Lectura desde las fuentes únicas; sin copias |

---

## 15. Decisiones pendientes del jugador

Estas decisiones requieren aprobación antes de implementar la etapa correspondiente:

1. **Precio de los robots** de recolector y administrador.
2. **Capacidad inicial** del recolector y del almacén central.
3. **Valores por defecto** de stock mínimo y multiplicador de reposición.
4. **Reserva mínima de dinero** por defecto.
5. **Umbral de histéresis** para cambiar de cultivo.
6. **Alcance del modo AFK**: qué robots y qué compras puede decidir sin consultar.
7. **Modalidad de programación**: solo parámetros, secuencias o bloques visuales.
8. **Persistencia** de partidas, necesaria para AFK prolongado.

---

## 16. Criterios generales de calidad

- Cada regla económica tiene una prueba automatizada.
- Cada transferencia de productos conserva la suma total de unidades.
- Los nombres de identificadores coinciden con `CropCatalog` e `ItemCatalog`.
- Ninguna mecánica nueva altera los tiempos reales de crecimiento.
- Las decisiones de los robots son explicables mediante el historial.

---

## 17. Estado de implementación

| Etapa | Estado | Detalle |
|---|---|---|
| **1. Configuración y control** | Implementada (pendiente de validación visual) | Pausar y reanudar robot; detener programa; velocidad y capacidad configurables desde el panel del robot; estado de actividad (Preparado, Ejecutando, Ejecutando acción, Pausado, Error, Inactivo); tarea y línea en curso |
| **2. Recolector y almacén central** | Implementada (pendiente de validación visual) | Compra desde Gestión por $12,000; capacidad 30; velocidad 3 m/s; almacén central en la oficina; todos los módulos |
| **3. Robot administrativo** | Implementada (pendiente de validación visual) | Presente en la oficina desde el inicio; revisa cada 3 s; compra insumos, vende excedentes del almacén central y respeta una reserva de $5 000 |
| 4. Economía autónoma | Pendiente | — |
| 5. Planificación agrícola | Pendiente | — |
| 6. Modo AFK | Pendiente | — |
| 7. Ampliaciones | Pendiente | — |

**Etapa 1: detalle de lo implementado**
- `RobotController.TogglePause()`: pausa y reanuda sin perder la acción en curso. Un robot pausado no avanza, no cosecha, no replanta y no transporta.
- `RobotController.StopProgram()`: detiene el programa, cancela la acción y quita la pausa.
- `RobotController.SetSpeed()`: velocidad limitada a 0,5–5,0 m/s. Puede cambiarse desde el panel o desde `configurarVelocidad()`; prevalece el último cambio.
- `RobotController.SetCapacity()`: capacidad de carga limitada a 1–12 unidades (valor inicial 6). Afecta a la cosecha desde el siguiente ciclo.
- `ManagementPanel`: la fila de cada robot muestra la carga según la capacidad configurada.

**Pendiente de Etapa 1**
- Validación visual del jugador.
- Detener y reanudar robots desde `ManagementPanel`. Hoy se controla desde el panel del propio robot.
- Persistencia de parámetros entre sesiones (no existe persistencia de partida).

**Etapa 2: detalle de lo implementado**
- `CollectorFleet`: gestiona la compra (`RobotPrice` = 12 000 $, descontados con `EconomyManager.TrySpend`) y crea el almacén central. Se añade al invernadero al cargar `ExpansionManager`.
- `CollectorRobot`: ciclo de recogida. Visita los baúles con existencias de todos los módulos (el más cercano primero), carga hasta 30 unidades, regresa al almacén al alcanzar la capacidad o cuando no quedan existencias, deposita y repite mientras esté activo.
- Transferencias: `Chest.Withdraw` retira solo lo que existe, sin tocar el inventario del jugador. `CentralWarehouse.Deposit` recibe la carga. Las unidades siempre suman baúles + carga + almacén; la prueba lo comprueba en cada muestra.
- `GreenhouseRouter`: navegación global con A* sobre el área de todos los módulos. Entra en la oficina por la puerta (`DoorPoint`) y deposita en `WarehouseSpot`.
- Control desde Gestión: cada recolector muestra estado, ubicación, carga y capacidad, con botones **Pausar/Reanudar** (conserva carga y tarea) y **Detener/Iniciar** (cancela el viaje y conserva la carga). La sección muestra también el contenido del almacén central.
- Preparación para el robot administrativo: `CentralWarehouse.Contents` y `Deposit` son la única fuente de existencias centrales. La venta y la compra no se han implementado.

**Etapa 2: pendiente de validación y limitaciones conocidas**
- Validación visual del jugador: que la puerta de la oficina coincida con una abertura de la pared este del invernadero. El recorrido usa el corredor de la puerta, pero el cristal no es un obstáculo para la ruta.
- El recolector no esquiva a otros robots (igual que el robot agrícola).
- El almacén central no tiene capacidad máxima.
- La velocidad (3 m/s) no se ajusta desde la interfaz todavía; `CollectorRobot.Speed` está preparado.
- No hay persistencia.

**Etapa 3: detalle de lo implementado**
- `AdministrationRobot` (oficina, creado por `CollectorFleet`): cada 3 s revisa los insumos (fertilizante y semillas de los cinco cultivos).
- Regla de reposición: si `existencias < mínimo`, compra `objetivo − existencias`, con objetivo = mínimo × multiplicador. Ejemplo: 9 fertilizantes → objetivo 30 → compra 21.
- Valores por defecto: mínimo 10 por insumo, multiplicador 3, reserva $5 000. Se pueden cambiar desde la página **Administración** del panel (mínimos por insumo, multiplicador y reserva, dentro de límites).
- Dinero: el robot nunca gasta por debajo de la reserva. Si falta dinero, vende de su almacén central la cantidad mínima de cosechas necesaria (`CentralWarehouse.Take` → `EconomyManager.SellFromWarehouse`). Si ni siquiera con todas las cosechas vendibles alcanza, no vende nada, no compra y lo notifica una sola vez.
- Las compras usan `EconomyManager.BuyItem` y entran en el inventario del jugador, que es donde la producción consume los insumos. Las semillas nunca llegan al almacén central, así que no se venden.
- Anti-duplicación: cada revisión compra y actualiza las existencias de forma síncrona. Una segunda revisión sin cambios no compra nada.
- Control: Pausar/Reanudar y Detener/Iniciar desde el panel. Historial de las últimas 8 operaciones y totales de compras y ventas del robot.

**Etapa 3: decisiones de interpretación y pendientes**
- El robot está disponible desde el inicio. Su precio de compra desde "Comprar robots" queda pendiente de decisión.
- "Reserva de productos" no existe todavía. Como las semillas no llegan al almacén y las cosechas solo se venden cuando faltan fondos, no hay reserva de producción.
- Compra todo-o-nada por revisión: no compra parcialmente.
- Pendiente de validación visual: la página Administración y el comportamiento del robot en partida.

**Etapa 4: robots configurables y automatización económica (implementada, pendiente de validación visual)**
- Compra: el recolector cuesta $12 000 y el administrativo $20 000, ambos desde Gestión. Ninguno existe al iniciar. Una compra fallida no crea instancias ni cobra. Hay una sola instancia administrativa; los recolectores pueden ser varios.
- Programas: ambos robots usan `RobotInterpreter` con su propio `IRobotHardware`. Las funciones se despachan con `TryCall`, así que cada robot solo admite las suyas.
  - Recolector: `configurarVelocidad`, `configurarCapacidad`, `buscarBaules`, `cargaActual`, y las acciones `recogerCultivos`, `regresarAlmacen` y `depositar`. `depositar` falla fuera de la oficina.
  - Administrativo: `configurarFrecuencia`, `comprobarExistencias`, `comprarInsumos`, `venderCosechas`, `reservarDinero`, `reservarCosecha`, `activarVentas`, y la acción `esperarRevision`.
  - Robot agrícola: `configurarVelocidad`, `hayPlantasEnModulo`, `activarCosecha` y sus acciones, sin cambios funcionales.
- Panel de programa: Validar, Ejecutar, Pausar/Reanudar y Detener. Los ajustes numéricos se muestran junto al editor. El código y la última configuración se guardan en la instancia durante la partida.
- Ventas automáticas: `venderCosechas` vende solo productos del almacén central, por encima de la reserva de cada cultivo, en orden de prioridad, y únicamente cuando el total vendible alcanza la cantidad mínima acumulada (5 por defecto). Las semillas nunca llegan al almacén.
- Compras: `comprarInsumos` compra la diferencia hasta el objetivo, respeta la reserva monetaria y, si falta dinero, vende excedentes solo si las ventas automáticas están activadas. Si no alcanza, no compra ni vende, y lo notifica una vez.
- Panel Administración: estado, dinero, reserva, almacén, compras pendientes, ventas, ingresos, historial, configuración de mínimos, multiplicador, reserva monetaria, ventas automáticas, reserva y prioridad por cultivo, cantidad mínima para vender, y el botón del programa.
- Scroll: Almacén, Gestión y Administración se envuelven en un `ScrollContainer`. Se quitaron los scrolls internos que anidaban (Gestión, Administración), para que la página sea la única que se desplaza. Sin validación visual todavía.

**Etapa 4: decisiones y pendientes**
- La configuración del panel es el valor inicial. Mientras el programa se ejecuta, sus instrucciones (por ejemplo `configurarVelocidad`) prevalecen.
- Los programas y la configuración se guardan solo en la instancia de la sesión. No hay persistencia en disco, que el proyecto aún no tiene.
- El robot agrícola base sigue existiendo al iniciar: pertenece a la granja original y no se incluyó en la lista de robots de pago.
- Pendiente de validación visual: el scroll de las tres páginas, la compra de cada robot, la edición y ejecución de programas, y las ventas automáticas en partida.
- Pendiente: decidir si la reserva de productos debe ser también configurable por el jugador fuera del panel, y si los programas deben poder guardarse en disco.

**Corrección de interacción (implementada, pendiente de validación visual)**
- Los editores de programa se abren solo con `E` junto a cada robot (agrícola, recolector y administrativo), usando el mismo sistema de interacción. Gestión ya no abre editores.
- Al abrir un editor se bloquea el control del jugador (`SetInputLocked`), se libera el cursor y se da foco al código. Cerrar con el botón o `Esc` devuelve el control y captura de nuevo el ratón.
- Causa corregida: los editores abiertos desde Gestión no recibían al jugador, así que el ratón quedaba capturado y el jugador seguía moviéndose.
- Cada robot conserva su programa en la instancia durante la sesión.

**Correcciones de agricultura, comunicación y editor (implementadas, pendiente de validación visual)**
- Necesidades por cultivo en `CropDefinition` (`NeedsWater`, `NeedsFertilizer`): lechuga ninguna; zanahoria y espinaca solo agua; tomate y pepino agua y fertilizante. Es una asunción a confirmar; no cambia precios ni crecimiento.
- Funciones por planta del robot agrícola: `numeroPlantas`, `necesitaRiego(i)`, `regar(i)`, `necesitaFertilizante(i)`, `fertilizar(i)`, `hayFertilizante`. El agua es gratuita. Fertilizar consume 1 unidad local y no actúa sin existencias.
- Intérprete: sentencia `while (cond) { ... }` (con pausa entre iteraciones para no bloquear el hilo), constante `Fertilizante`, y `Peticion(Fertilizante)` / `esperarRecurso(Fertilizante)`.
- Solicitudes: `CollectorFleet` guarda una solicitud por robot y recurso, con cantidad = plantas que lo necesitan − existencias locales. Se fusionan y no se duplican. `atenderPeticiones()` del administrativo compra solo lo que falta respetando la reserva, descuenta del inventario y entrega al robot solicitante. Sin dinero, la solicitud queda pendiente. Detener el robot la cancela.
- Editor: `E` se marca como gestionada al abrir y el foco del editor se da de forma diferida, así que la tecla no se escribe.

**Pendientes:** validación visual del editor con `E` y del recorrido de las plantas; verificación en módulos expandidos (no probada aún); confirmar las necesidades por cultivo.

**Menú principal y persistencia local (implementados, pendiente de validación visual)**
- Menú principal con Nueva partida, Continuar, Cargar partida, Opciones y Salir, sobre el invernadero real como fondo.
- Partidas independientes en `user://saves/`, con nombre, fecha y dinero. Guardado manual (menú de pausa), automático cada 5 minutos y al cerrar. Confirmación al cargar, eliminar, volver al menú o salir.
- Saldo inicial de $100 MXN (`EconomyManager.StartingBalance = 100`).
- Restauración completa de dinero, historial, inventario, cultivos, expansiones, baúles, almacén, robots, programas, configuraciones y estado de pausa, sin cobrar ni duplicar.
- Pendiente: validación visual del menú y del menú de pausa, autoguardado real a los 5 minutos y el flujo completo de Nueva partida → jugar → salir → continuar.
- No se guardan tareas en curso ni solicitudes entre robots: se rehacen desde el estado guardado.
- Release Beta: menú corregido (sin HUD ni jugador, cursor visible). Exportación Windows x64 bloqueada por la falta de las plantillas de exportación de Godot Mono 4.7.2; preset y README preparados.

**Siguiente etapa:** Etapa 5, planificación agrícola (análisis de rentabilidad y recomendaciones de cultivo). Depende de `CropCatalog`, `EconomyManager` y del historial de ventas.

*Documento vivo. Se actualizará al cerrar cada etapa.*
