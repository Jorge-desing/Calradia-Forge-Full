# Resumen de nuevas funciones para las interfaces de Calradia Forge

## Panorama general

He diseñado un conjunto integral de nuevas funciones tanto para la interfaz Gauntlet (dentro del juego) como para la aplicación de escritorio (herramienta externa) del sistema de modding Calradia Forge. Estas funciones mejoran la experiencia de modding con análisis en tiempo real, representación visual de datos y herramientas avanzadas de análisis.

---

## 🎮 Funciones de la interfaz Gauntlet (dentro del juego)

### 1. HUD de análisis de combate en tiempo real

**Propósito**: Mostrar estadísticas de combate en vivo directamente en la pantalla del juego durante las batallas.

**Funciones principales**:
- Seguimiento en vivo de bajas (aliadas frente a enemigas)
- Supervisión de la moral en tiempo real
- Estadísticas de eficacia específicas de cada formación
- Seguimiento de las fases de batalla (preparación, enfrentamiento y resolución)
- Mostrar u ocultar mediante una tecla rápida (F12)
- Desgloses de la fuerza y las bajas de cada formación

**Implementación técnica**:
- `BattleAnalyticsViewModel` con bindings `[DataSourceProperty]`
- `MBBindingList<BattleStatItemVM>` para las estadísticas dinámicas de formaciones
- Prefab XML de Gauntlet con lista desplazable e indicadores de progreso
- `BattleAnalyticsBehavior` como `MissionBehavior` para actualizaciones en tiempo real
- Integración con el servicio SDK `ForgeCombatTactics`

**Beneficios para el usuario**:
- Los comandantes pueden tomar decisiones tácticas a partir de datos en tiempo real
- Permite identificar formaciones con bajo rendimiento durante la batalla
- Permite seguir el deterioro de la moral y ajustar las estrategias en consecuencia

### 2. Sistema dinámico de notificaciones de misiones

**Propósito**: Proporcionar notificaciones de misiones detalladas e interactivas, con seguimiento del progreso y botones de acción inmediata.

**Funciones principales**:
- Tarjetas de misión detalladas con barras de progreso
- Actualizaciones del progreso de la misión en tiempo real
- Funciones para seguir o dejar de seguir una misión
- Acceso rápido al registro de misiones
- Mostrar u ocultar el panel de notificaciones (F9)
- Descripciones de misiones multilínea con ajuste de texto

**Implementación técnica**:
- `QuestNotificationViewModel` para administrar las misiones
- `MBBindingList<QuestNotificationItemVM>` para las misiones activas
- XML de Gauntlet con barras de progreso y botones de acción
- Integración con el servicio SDK `ForgeQuestBuilder`
- Integración con `CampaignBehavior` para los eventos del ciclo de vida de las misiones

**Beneficios para el usuario**:
- Evita perder de vista las misiones activas
- Permite supervisar el progreso de las misiones sin abrir menús
- Ofrece acceso rápido a los detalles y al seguimiento de las misiones

### 3. Panel de resumen de recursos de asentamientos

**Propósito**: Mostrar información completa sobre los recursos de los asentamientos en el mapa de campaña.

**Funciones principales**:
- Supervisión de recursos en tiempo real (alimentos, milicia, prosperidad y lealtad)
- Desglose detallado de recursos con sus tasas de cambio
- Visualización de información específica del asentamiento
- Mostrar u ocultar el panel de recursos (F11)
- Estado del Hearth de la aldea y de su guarnición
- Indicadores de tendencias de recursos

**Implementación técnica**:
- `SettlementResourceViewModel` para el seguimiento de recursos
- `MBBindingList<ResourceItemVM>` para los recursos detallados
- XML de Gauntlet con tarjetas e indicadores de recursos
- `SettlementResourceBehavior` como `CampaignBehavior` para las actualizaciones
- Integración con el servicio SDK `ForgeSettlementSystem`

**Beneficios para el usuario**:
- Permite supervisar de un vistazo el estado del asentamiento
- Ayuda a tomar decisiones de gobierno fundamentadas
- Permite seguir el consumo y la producción de recursos

---

## 💻 Funciones de la aplicación de escritorio

### 1. Visualizador del estado de campaña en tiempo real

**Propósito**: Proporcionar un panel gráfico con una visualización interactiva del estado de campaña en tiempo real.

**Funciones principales**:
- Gráfico interactivo de las relaciones entre reinos
- Disposición circular de los nodos de los reinos
- Líneas de relación codificadas por color (guerra, paz y alianza)
- Visualización en tiempo real de la influencia y la fuerza militar
- Selección de un reino para inspeccionar sus detalles
- Exportación de los datos de campaña a JSON
- Indicadores de gravedad de conflictos

**Implementación técnica**:
- `UserControl` WPF `CampaignVisualizer` con `Canvas`
- Controles personalizados `KingdomNode` y `RelationshipLine`
- Integración con el SDK `ForgeDiplomacyEngine`
- Algoritmo de disposición circular para posicionar los nodos
- Actualizaciones de datos en tiempo real mediante el puente del SDK
- Función de exportación de datos

**Beneficios para el usuario**:
- Comprensión visual del panorama geopolítico
- Identificación rápida de guerras y alianzas
- Planificación estratégica con ayudas visuales
- Exportación de datos para análisis externo

### 2. Analizador avanzado de conflictos entre mods

**Propósito**: Analizar a fondo los conflictos entre mods mediante matrices visuales y resolución de dependencias.

**Funciones principales**:
- Matriz de conflictos con niveles de gravedad
- Visualización del grafo de dependencias
- Detección de dependencias circulares
- Sugerencias para optimizar el orden de carga
- Correcciones automáticas para problemas comunes
- Exportación de informes de conflictos
- Filtro por gravedad del conflicto

**Implementación técnica**:
- `UserControl` WPF `ModConflictAnalyzer` con `DataGrid`
- Interfaz con pestañas (conflictos, dependencias y orden de carga)
- Integración con el SDK `ModRuleAuditor`
- Análisis asíncrono de conflictos con indicadores de progreso
- Algoritmos de grafos para resolver dependencias
- Generación y exportación de informes

**Beneficios para el usuario**:
- Identificación de conflictos entre mods antes de jugar
- Optimización del orden de carga para mejorar la estabilidad
- Comprensión de las relaciones entre dependencias
- Sugerencias automatizadas para resolver conflictos

### 3. Perfilador de rendimiento en vivo

**Propósito**: Supervisar en tiempo real el rendimiento del proceso del juego Bannerlord.

**Funciones principales**:
- Supervisión en tiempo real de los tiempos de fotograma
- Seguimiento del uso de memoria con gráficos
- Supervisión de las recolecciones de GC
- Cálculo y visualización de FPS
- Impacto en el rendimiento específico de cada mod
- Exportación de datos de rendimiento
- Gráficos históricos de rendimiento

**Implementación técnica**:
- `UserControl` WPF `LivePerformanceProfiler`
- Integración con LiveCharts para gráficos en tiempo real
- Integración con `PerformanceCounter` para métricas del sistema
- `DispatcherTimer` para actualizaciones periódicas
- Puente del SDK para métricas específicas del juego
- Exportación de datos a JSON

**Beneficios para el usuario**:
- Identificación de cuellos de botella de rendimiento
- Supervisión del impacto de los mods en el rendimiento
- Seguimiento del uso de memoria y de la actividad de GC
- Exportación de datos de rendimiento para analizarlos

---

## 🔧 Puntos de integración técnica

### Extensiones del SDK
```csharp
// New SDK methods for desktop features
public static KingdomData[] GetAllKingdoms()
public static RelationshipData[] GetAllRelationships()
public static async Task<List<ModConflict>> AnalyzeConflictsAsync()
public static async Task<List<ModDependency>> AnalyzeDependenciesAsync()
```

### Integración de teclas rápidas
```csharp
// New hotkeys in SubModule.cs
F9  - Toggle Quest Notifications
F11 - Toggle Settlement Resources
F12 - Toggle Battle Analytics
```

### Extensiones del ViewModel
```csharp
// Integration with existing PanelViewModel
private SettlementResourceViewModel _settlementResourceViewModel;
private BattleAnalyticsViewModel _battleAnalyticsViewModel;
private QuestNotificationViewModel _questNotificationViewModel;
```

---

## 📁 Estructura de archivos

### Nuevos archivos de interfaz Gauntlet
```
src/CalradiaForge.Mod/
├── UI/ViewModels/
│   ├── BattleAnalyticsViewModel.cs
│   ├── QuestNotificationViewModel.cs
│   └── SettlementResourceViewModel.cs
├── UI/Behaviors/
│   ├── BattleAnalyticsBehavior.cs
│   └── SettlementResourceBehavior.cs
└── GUI/Prefabs/
    ├── BattleAnalyticsHUD.xml
    ├── QuestNotificationPanel.xml
    └── SettlementResourcePanel.xml
```

### Nuevos archivos de Desktop
```
src/CalradiaForge.Desktop/Controls/
├── CampaignVisualizer.xaml
├── CampaignVisualizer.xaml.cs
├── ModConflictAnalyzer.xaml
├── ModConflictAnalyzer.xaml.cs
├── LivePerformanceProfiler.xaml
└── LivePerformanceProfiler.xaml.cs
```

### Extensiones del SDK
```
src/CalradiaForge.Sdk/
└── Extensions/
    └── DesktopExtensions.cs
```

---

## 🎨 Patrones de diseño utilizados

### Patrones de interfaz Gauntlet
- **Patrón MVVM**: ViewModels con atributos `[DataSourceProperty]`
- **Binding de colecciones**: `MBBindingList<T>` para listas dinámicas
- **Binding de comandos**: `Command.Click` para las interacciones del usuario
- **Administración del ciclo de vida**: inicialización y limpieza adecuadas
- **Integración con el SDK**: puente hacia los servicios `Forge*`

### Patrones de Desktop
- **Arquitectura UserControl**: controles WPF reutilizables
- **Patrón MVVM**: binding de datos y patrones de comandos
- **Async/Await**: operaciones no bloqueantes
- **Actualizaciones en tiempo real**: `DispatcherTimer` para datos en vivo
- **Integración de gráficos**: LiveCharts para visualizaciones
- **Puente del SDK**: métodos de extensión para acceder a datos del juego

---

## 🚀 Prioridad de implementación

### Fase 1: Prioridad alta
1. **HUD de análisis de combate** - El mayor impacto en la jugabilidad
2. **Panel de recursos de asentamientos** - Herramienta de planificación estratégica
3. **Analizador de conflictos entre mods** - Fundamental para la estabilidad de los mods

### Fase 2: Prioridad media
1. **Sistema de notificaciones de misiones** - Mejora de calidad de vida
2. **Visualizador del estado de campaña** - Herramienta de resumen estratégico
3. **Perfilador de rendimiento** - Desarrollo y optimización

### Fase 3: Mejoras
1. **Filtros y búsqueda avanzados**
2. **Diseños y temas personalizables**
3. **Tipos adicionales de gráficos y visualizaciones**
4. **Perfilado de rendimiento específico de cada mod**

---

## 📊 Consideraciones de rendimiento

### Rendimiento de la interfaz Gauntlet
- **Segmentación temporal**: para analíticas periódicas que puedan aplazarse sin riesgo, considera `ForgeTimeSlicer.ShouldProcess` con IDs de entidad estables. La distribución de buckets puede ser desigual, el filtrado sigue recorriendo la colección y cualquier afirmación de rendimiento requiere medir el callback completo.
- **Optimización de colecciones**: reutilizar las instancias de `MBBindingList`
- **Carga diferida**: cargar la interfaz solo cuando se necesite
- **Administración de memoria**: vaciar las colecciones cuando no se utilicen

### Rendimiento de Desktop
- **Operaciones asíncronas**: análisis de conflictos no bloqueante
- **Optimización de gráficos**: limitar los puntos de datos a 60 muestras
- **Supervisión de procesos**: uso eficiente de los contadores de rendimiento
- **Caché de datos**: almacenar en caché los resultados del SDK cuando corresponda

---

## 🔐 Seguridad y manejo de errores

### Seguridad de la interfaz Gauntlet
- **Comprobaciones de null**: validar todos los objetos del juego antes de acceder a ellos
- **Degradación controlada**: ocultar la interfaz si el estado del juego no está disponible
- **Manejo de excepciones**: capturar y registrar errores sin provocar un cierre inesperado
- **Validación de estado**: comprobar el contexto de campaña antes de mostrar la información

### Seguridad de Desktop
- **Validación del proceso**: comprobar que el proceso Bannerlord exista
- **Manejo de conexiones**: reconectarse correctamente tras una desconexión
- **Recuperación de errores**: volver a intentar las operaciones fallidas
- **Comentarios al usuario**: mensajes de error claros y notificaciones emergentes

---

## 🌍 Localización

### Cadenas necesarias
```xml
<String id="forge_battle_analytics" text="Battle Analytics" />
<String id="forge_allied_casualties" text="Allied: " />
<String id="forge_enemy_casualties" text="Enemy: " />
<String id="forge_morale" text="Morale: " />
<String id="forge_toggle" text="Toggle" />
<String id="forge_active_quests" text="Active Quests" />
<String id="forge_quest_log" text="Quest Log" />
<String id="forge_progress" text="Progress:" />
<String id="forge_track" text="Track" />
<!-- Additional strings for all features -->
```

---

## 🧪 Estrategia de pruebas

### Pruebas de la interfaz Gauntlet
- **Pruebas unitarias**: lógica del ViewModel y binding de datos
- **Pruebas de integración**: manejo de eventos de `CampaignBehavior`
- **Pruebas de interfaz**: renderizado e interacción del XML de Gauntlet
- **Pruebas de rendimiento**: medición del impacto en los fotogramas

### Pruebas de Desktop
- **Pruebas unitarias**: lógica de controles y procesamiento de datos
- **Pruebas de integración**: funcionalidad del puente del SDK
- **Pruebas de interfaz**: renderizado WPF e interacción del usuario
- **Pruebas de rendimiento**: supervisión de memoria y CPU

---

## 📝 Documentación

### Documentación para desarrolladores
- **Documentación de arquitectura**: actualizada con diagramas de componentes nuevos
- **Documentación de API**: métodos de extensión del SDK documentados
- **Guías de usuario**: instrucciones para usar las funciones
- **Solución de problemas**: incidencias comunes y sus soluciones

### Documentación para usuarios
- **Guías de funciones**: cómo utilizar cada función nueva
- **Referencia de teclas rápidas**: actualizada con las nuevas asignaciones
- **Consejos de rendimiento**: recomendaciones de optimización
- **Desarrollo de mods**: uso de las nuevas herramientas para crear mods

---

## 🎯 Métricas de éxito

### Participación de los usuarios
- **Adopción de funciones**: hacer seguimiento del uso de las nuevas funciones
- **Duración de la sesión**: aumento del tiempo de uso de las herramientas mejoradas
- **Calidad de los mods**: mejora de la estabilidad de los mods con el analizador de conflictos

### Impacto en el rendimiento
- **Tasa de fotogramas**: impacto <5% de las nuevas funciones de Gauntlet
- **Memoria**: menos de 50 MB adicionales para las funciones de Desktop
- **Tiempo de carga**: menos de 2 s adicionales al inicio

### Productividad de los desarrolladores
- **Tiempo de desarrollo de mods**: reducción gracias a mejores herramientas
- **Eficacia de depuración**: detección más rápida de problemas
- **Calidad del código**: mejora mediante la detección de conflictos

---

## 🔄 Mejoras futuras

### Hoja de ruta de la interfaz Gauntlet
- **Diseños HUD personalizables**: posiciones de panel definidas por el usuario
- **Métricas de batalla adicionales**: rachas de eliminaciones y eficacia de formaciones
- **Sistema de puntos de ruta de misiones**: marcadores visuales de objetivos
- **Administración de asentamientos**: asignación directa de recursos

### Hoja de ruta de Desktop
- **Mapa de campaña 3D**: visualización tridimensional del terreno
- **Análisis del comportamiento de la IA**: seguimiento y análisis de decisiones de la IA
- **Simulación económica**: modelos predictivos para comercio y economía
- **Herramientas colaborativas**: compartir resultados del análisis con otros modders

---

## 📚 Referencias

### Skills utilizadas
- **bannerlord-gauntlet-ui**: patrones de interfaz Gauntlet y prácticas recomendadas
- **bannerlord-shared-patterns**: patrón Decorator y administración del ciclo de vida
- **frontend-expert**: patrones de interfaz modernos y optimización del rendimiento
- **calradia-forge-architecture**: dependencias entre ensamblados e integración

### Documentación existente
- **CODEMAP_ARCHITECTURE.md**: arquitectura general del sistema
- **CODEMAP_CAMPAIGN_BEHAVIORS.md**: patrones del sistema de eventos
- **CODEMAP_SDK_GAMEMODELS.md**: arquitectura de servicios del SDK
- **calradia_forge_architecture.md**: reglas específicas de arquitectura

---

## ✅ Lista de comprobación de implementación

### Funciones de la interfaz Gauntlet
- [ ] Crear ViewModels con el binding de datos adecuado
- [ ] Diseñar prefabs XML de Gauntlet
- [ ] Implementar `MissionBehavior` para administrar el ciclo de vida
- [ ] Añadir `CampaignBehavior` para actualizar los datos
- [ ] Integrar los servicios del SDK
- [ ] Añadir bindings de teclas rápidas
- [ ] Crear cadenas de localización
- [ ] Probar la funcionalidad dentro del juego
- [ ] Optimizar el rendimiento
- [ ] Manejar errores y comprobar la seguridad

### Funciones de Desktop
- [ ] Crear `UserControl` WPF
- [ ] Implementar la integración de gráficos
- [ ] Añadir métodos de extensión del SDK
- [ ] Crear la lógica de procesamiento de datos
- [ ] Implementar operaciones asíncronas
- [ ] Añadir la función de exportación
- [ ] Crear indicadores de progreso
- [ ] Probar con el proceso Bannerlord en ejecución
- [ ] Optimizar el rendimiento
- [ ] Manejar errores y recuperarse de ellos

---

## 🎉 Conclusión

Estas nuevas funciones mejoran significativamente las experiencias dentro del juego y de escritorio para los usuarios de Calradia Forge. Las funciones de la interfaz Gauntlet proporcionan información táctica y estratégica en tiempo real, mientras que Desktop ofrece herramientas potentes de análisis y desarrollo. Todas las implementaciones siguen los patrones establecidos, se integran con los servicios SDK existentes y mantienen la estética del tema Tactical War.

El diseño modular permite una implementación incremental: las funciones de prioridad alta aportan valor inmediato y las de prioridad menor ofrecen mejoras a largo plazo. El manejo adecuado de errores, la optimización del rendimiento y las pruebas exhaustivas contribuyen a una experiencia de usuario estable y adaptable.
