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

*Documento vivo. Se actualizará al cerrar cada etapa.*
