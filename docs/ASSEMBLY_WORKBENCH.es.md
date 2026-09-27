# Banco de ensamblados dentro del juego

El banco está disponible en la sección Extensions de Forge. Enumera hasta 250 archivos `.dll` de primer nivel en carpetas `bin/Win64_Shipping_Client` de módulos instalados y excluye los ensamblados de TaleWorlds. Escribe el número de una fila para seleccionarla; la ruta absoluta queda visible en el campo de argumento.

## Inspeccionar

`Inspect metadata` analiza un PE/.NET administrado con AsmResolver en un hilo de trabajo. Informa identidad, versión, runtime, modo IL-only y firma strong-name, conteos limitados de tipos y métodos, hasta 512 referencias, tamaño y SHA-256. La entrada está limitada a 256 MiB. El archivo se analiza como datos: nunca se carga en el CLR del juego ni se ejecuta. Imágenes nativas, metadatos dañados, formatos no compatibles y entradas que exceden el límite producen un error sin bloquear el hilo de Gauntlet.

## Vista previa y copia de versión

Escribe una versión de cuatro componentes, por ejemplo `1.2.3.0`, en el campo Version separado. Ejecuta primero `Preview version`. La vista previa no crea archivos y muestra entrada, salida, respaldo, versiones actual y solicitada, y hash de entrada. Después usa `Apply copy` solo si la vista previa es correcta.

La aplicación escribe una DLL separada en `%LocalAppData%\CalradiaForge\AssemblyWorkbench`, crea `<salida>.source.bak`, registra los hashes SHA-256 de entrada y salida, escribe mediante un archivo temporal y vuelve a abrir el resultado para validar el PE y la versión. No sobrescribe una ruta de salida o respaldo existente ni modifica la entrada. Rechaza ensamblados firmados y mixtos porque no puede conservar sus firmas ni secciones nativas. La cancelación y los errores de escritura eliminan temporales y salidas parciales.

Es una transformación limitada de los metadatos de versión, no un editor IL arbitrario, una recarga en caliente, una prueba de compatibilidad ni un escáner de seguridad. Conserva el respaldo y prueba cualquier salida lejos de archivos de mods importantes.

AsmResolver 6.0.1 y sus dependencias de runtime para .NET Framework 4.7.2 se incluyen junto al módulo del juego. DocFX solo se necesita al compilar; la acción Help muestra resúmenes offline derivados de la documentación XML del SDK.

## Auditoría Estática de Ensamblados y Guardado en Desktop

En el banco de trabajo de escritorio .NET 8 WPF independiente, AsmResolver también impulsa la auditoría estática offline profunda para binarios compilados `.dll` y `.exe` mediante las rutas `SaveTypeDefinerAuditor` y `CampaignNamespaceGuard`:
- **GEMINI.md Regla A (Anti-Shadowing):** Comprueba que ningún tipo ni espacio de nombres oculte `TaleWorlds.CampaignSystem.Campaign` o `TaleWorlds.Localization`.
- **Regla B (Contrato de Comportamiento Sin Estado):** Garantiza que las clases que heredan de `CampaignBehaviorBase` contengan cero atributos `[SaveableField]` o `[SaveableProperty]`.
- **Seguridad de Base ID en SaveableTypeDefiner:** Desensambla las instrucciones IL del constructor para verificar que los IDs base sean $\ge 2.500.000$, evitando colisiones con tipos nativos de TaleWorlds (0–100.000) u otros mods.
- **Serialización Directa MBGUID de Entidades del Motor:** Detecta la serialización directa peligrosa de entidades del motor (`Hero`, `MobileParty`, `Settlement`, `Clan`, `Kingdom`) y recomienda resolución por StringId.
- **Metadatos de Seguridad para Distribución:** Audita los atributos de metadatos de ensamblado (`Company`, `Product`, `Description`, `Copyright`) para prevenir falsos positivos en motores antivirus.
- **Radar Interactivo de Seguridad CLR (Rev032):** Visualiza tarjetas de metadatos CLR (Tipos Declarados, Tabla de Métodos, Referencias de Ensamblado, Conteo de Resolución StringId), 5 barras de cumplimiento de seguridad y preajustes canónicos de auditoría alternables mediante `PresetActionButton`.

## Expansión de Comandos en Estudios Tácticos, Personalización Híbrida y Jerarquización CoALA (Rev046)

En el banco de trabajo de escritorio .NET 8 WPF, los 8 Tactical Studios integran un catálogo ampliado de comandos curados organizados en 4 familias y funciones de personalización avanzada:
- **Expansión de Comandos por Familias:**
  - *Memoria Cognitiva CoALA:* `cf.agent_memory_stats`, `cf.agent_memory_query`, `cf.agent_memory_salience`, `cf.agent_memory_decay`, `cf.agent_memory_cluster`, `cf.agent_memory_export`.
  - *Simulación Táctica y Equilibrio:* `cf.sim_tactics cavalry/infantry/archery`, `cf.siege_tactics`, `cf.sim_economy workshops`, `cf.sim_settlements all`, `cf.sim_dynasty all`, `cf.sim_crime all`, `cf.sim_trade grain`.
  - *Diagnóstico y Flujo de Trabajo:* `cf.audit`, `cf.model_audit`, `cf.dump_diagnostics`, `cf.audit_save`, `cf.audit_localization`, `cf.harmony_summary`, `cf.patch_preflight`, `cf.gc_profile`.
  - *Andamiaje y Generación:* `cf.novice_scaffold quest/behavior/troop/item/armor/submodule`, `cf.novice_checklist`, `cf.novice_events all`.
- **Personalización Híbrida y Paletas de Estudio:**
  - Anclaje de comandos con glifo interactivo (`★`/`☆`), ejecución rápida de la acción primaria del estudio (`QuickActionCommand` / `QuickActionLabel`) y notas de desarrollo persistentes (`ScratchpadNotes`).
  - Modelo de utilidad cognitiva CoALA con ordenación adaptativa basada en recencia ($R$), frecuencia ($F$) e importancia/anclaje ($I$).

## Playbooks de Ingeniería, Árboles de Solución de Problemas y Macros Procedurales en Estudios Tácticos (Rev047)

En el banco de trabajo de escritorio .NET 8 WPF, los 8 Tactical Studios incorporan guías paso a paso de desarrollo, diagnóstico de fallos y automatización procedural:
- **Playbooks de Ingeniería Paso a Paso:**
  - Los 8 ViewModels de Tactical Studio exponen un `PlaybookTitle` estructurado y 3 `PlaybookSteps` accionables que definen recetas estándar de ingeniería (validación DAG de tropas, calibración de mezcla acústica, ajuste de elasticidad de mercado, poda de memoria CoALA, escaneo de instrucciones IL, resolución de dependencias sin ciclos, estabilización diplomática de casus belli y andamiaje seguro de XML).
- **Árboles de Solución de Problemas y Remedios de Subsistema:**
  - Encabezados de diagnóstico concretos (`TroubleshootingHeader`) y remedios prescriptivos (`TroubleshootingRemedy`) que abordan directamente los modos de fallo en runtime de TaleWorlds (p. ej., caídas por ciclos en árboles DAG, incompatibilidades de categoría de audio, hiperinflación económica, retención descontrolada de memoria y corrupción de guardado).
- **Acciones Macro Procedurales:**
  - Cada estudio integra una acción compuesta especializada (`ProceduralMacroAction`) que ejecuta flujos de diagnóstico de extremo a extremo con un solo clic o atajo.
- **Alternador de Densidad de Vista:**
  - Soporte para alternar densidad compacta vs. detallada (`IsCompactMode`), permitiendo alternar entre paletas operativas mínimas y manuales técnicos exhaustivos.



