# Arquitectura de Agentes de IA Autónomos en Calradia Forge

Este documento define la arquitectura, la jerarquía de subagentes, las herramientas del repositorio y los canales de ejecución de la integración opcional con Google Antigravity en Calradia Forge. El SDK es una herramienta de desarrollo opcional, no una dependencia de ejecución de Forge o Bannerlord.

---

## 1. Visión General y Misión

Calradia Forge implementa una arquitectura multi-agente especializada diseñada para mantener, auditar, desarrollar y verificar el repositorio de forma autónoma a través de todos sus marcos de destino:
- **`src/CalradiaForge.Mod`** (módulo de Bannerlord en el juego, `net472`).
- **`src/CalradiaForge.Desktop`** (banco de trabajo WPF independiente, `net8.0-windows`).
- **`src/CalradiaForge.Core` / `Sdk`** (sistemas centrales compartidos, `net472;net8.0`).

El sistema descubre las habilidades del proyecto disponibles en `.agents/skills/`, aplica las reglas invariantes del repositorio (Reglas A, B, C y D), y admite la ejecución online mediante el SDK opcional de Antigravity o la ejecución local offline de herramientas seleccionadas del repositorio. El conjunto de habilidades cambia con el repositorio; no se presupone una cantidad fija.

```
                      ┌────────────────────────────┐
                      │      ForgeMasterAgent      │
                      │ (Orquestador Raíz / AGY)   │
                      └──────────────┬─────────────┘
                                     │
         ┌──────────────────┬────────┴─────────┬──────────────────┐
         ▼                  ▼                  ▼                  ▼
┌──────────────────┐ ┌──────────────┐ ┌──────────────────┐ ┌──────────────┐
│  ForgeArchitect  │ │  Stateless   │ │    DesktopWpf    │ │  DocLedger   │
│      Agent       │ │   Auditor    │ │    Specialist    │ │    Agent     │
│ (C# / Motor)     │ │(Seguridad SB)│ │ (WPF / Render)   │ │ (Docs/Ledger)│
└──────────────────┘ └──────────────┘ └──────────────────┘ └──────────────┘
```

La tabla siguiente es el roster vigente: un coordinador y cinco roles especialistas configurados, incluido `BugHunterAgent`. El diagrama es ilustrativo y no enumera a todos los especialistas.

---

## 2. Jerarquía Multi-Agente y Roles

El roster configurado consta de un coordinador y cinco roles especialistas:

| Nombre del Agente | Dominio Primario | Invariantes Aplicadas | Herramientas Vinculadas |
| :--- | :--- | :--- | :--- |
| **`ForgeMasterAgent`** | Orquestador Raíz | Descomposición de tareas, compuertas de seguridad, síntesis | Online: herramientas del repositorio configuradas y especialistas configurados mediante el SDK de Antigravity; offline: herramientas locales seleccionadas (no hay herramienta local `START_SUBAGENT`) |
| **`ForgeArchitectAgent`** | C# y Motor de Bannerlord | Regla A Anti-Shadowing (`GEMINI.md`), límites TFM, decoradores GameModel | `run_dotnet_build`, `inspect_csharp_source` |
| **`StatelessBehaviorAuditor`** | Persistencia y Seguridad | Regla B Comportamientos sin estado, cero `SaveableTypeDefiner`, `SyncData` limpio | `verify_stateless_behavior`, `inspect_csharp_source` |
| **`DesktopWpfSpecialist`** | Workbench WPF e Interfaz | Regla C Contratos estáticos, reciclado de contenedores, bordes Aliased DirectX, UIA | `audit_desktop_contracts`, `run_ui_automation_smoke`, `run_solution_tests` |
| **`DocLedgerAgent`** | Docs e Integridad de Release | Regla D Seguridad, paridad bilingüe, cadena criptográfica SHA-256 del ledger | `audit_documentation_parity`, `audit_ledger_integrity`, `run_package_workflow` |
| **`BugHunterAgent`** | Code Smells y Concurrencia | Interpolación FormattableString, bloqueos CAS, guardas SyncRoot, despacho a hilo principal | `audit_code_smells`, `audit_concurrency_hazards`, `inspect_csharp_source` |

### 2.1 ForgeMasterAgent (Orquestador Raíz)
- **Rol:** Planificador estratégico de alto nivel y coordinador.
- **Instrucciones del Sistema:** Analiza instrucciones en lenguaje natural, descompone solicitudes de funciones complejas o auditorías de errores en subtareas especializadas, delega en agentes de dominio y verifica que se cumplan todas las invariantes antes de concluir.

### 2.2 ForgeArchitectAgent (C# / Motor de Bannerlord)
- **Rol:** Arquitecto de sistemas para ensamblados de juego e interacciones con el motor TaleWorlds.
- **Invariantes:**
  - Preserva estrictamente el aislamiento de marcos de destino (`net472` para el mod de juego, `net8.0-windows` para la aplicación de escritorio).
  - Aplica la **Regla A (Anti-Shadowing de GEMINI.md)**: Nunca permite carpetas, espacios de nombres o clases llamadas `Campaign` o `Localization` en `src/CalradiaForge.Mod`.
  - Hace cumplir el patrón Decorador en GameModels envolviendo `_previousModel` con bonificaciones mediante `ExplainedNumber`.

### 2.3 StatelessBehaviorAuditor (Seguridad de Guardado y CampaignBehavior)
- **Rol:** Auditor de persistencia y comportamiento de campaña.
- **Invariantes:**
  - Aplica la **Regla B (Comportamiento de Campaña sin Estado)**: Los comportamientos del mod deben permanecer completamente sin estado respecto a los archivos de guardado.
  - Cero herencia de `SaveableTypeDefiner` en `src/CalradiaForge.Mod`.
  - Cero llamadas de serialización mutable `dataStore.SyncData(...)` dentro de `SyncData(IDataStore dataStore)`.
  - Prohíbe la serialización directa de entidades dinámicas del motor (`Hero`, `Settlement`, `MobileParty`).

### 2.4 DesktopWpfSpecialist (Workbench WPF y Optimización Gráfica)
- **Rol:** Ingeniero de interfaz del banco de trabajo y auditor de rendimiento de layout.
- **Invariantes:**
  - Virtualización gráfica pura: `VirtualizingPanel.ScrollUnit="Pixel"`, `VirtualizationMode="Recycling"`, `CacheLength="1,1"`.
  - Bordes rectos Aliased DirectX: `RenderOptions.EdgeMode="Aliased"` y `SnapsToDevicePixels="True"` en separadores de 1px.
  - Aplica la **Regla C (Contratos Estáticos de Desktop)**: Valida los tokens estáticos exigidos por `tests/CalradiaForge.Desktop.Tests/Program.cs`.
  - Comprobaciones externas de accesibilidad mediante Windows UI Automation con `tools/Test-CalradiaForge-Desktop-Uia.bat`.

### 2.5 DocLedgerAgent (Documentación Bilingüe e Integridad de Ledger)
- **Rol:** Redactor técnico, auditor de ledger y guardián de entregas.
- **Invariantes:**
  - Paridad estricta inglés/español para la documentación técnica en `docs/`.
  - Verifica la cadena de hashes criptográficos SHA-256 en `docs/CalradiaForge-Registro-Mejoras.integrity.jsonl`.
  - Aplica la **Regla D (Seguridad de Distribución)**: Excluye DLLs propietarias de TaleWorlds, partidas guardadas y scripts en los paquetes de release.

### 2.6 BugHunterAgent (Code Smells y Concurrencia)
- **Rol:** Inspector estático de antipatrones, auditor de seguridad de hilos y validador de playbooks procedimentales.
- **Invariantes:**
  - Valida invariantes de sustitución de cadenas `FormattableString` en Gauntlet y Desktop.
  - Audita `ConcurrentDictionary` y bloqueos CAS en `ForgeData`, `SyncRoot` en `ForgeAgentMemory` del SDK C# y `SemaphoreSlim` en `PipeClient`. Esta memoria del juego es distinta de `CoALAAgentMemory`, usada por el orquestador Python en `agents/memory.py`.
  - Hace cumplir el enrutamiento al hilo principal de TaleWorlds mediante `GameThreadActionDispatch`.
  - Audita los 8 playbooks de Gauntlet, 8 árboles de remedio en Desktop y enlaces de prefabs.

---

## 3. Integración con el SDK de Google Antigravity

Cuando está instalado el perfil opcional `google-antigravity`, la integración de agentes usa `google.antigravity`:

### 3.1 Configuración con LocalAgentConfig
```python
from google.antigravity import LocalAgentConfig, types
from agents.config import ForgeAgentConfig
from agents.subagents import get_subagent_configs
from agents.tools import ALL_REPO_TOOLS

capabilities = types.CapabilitiesConfig(
    enable_subagents=True,
    max_subagent_depth=2,
    allowed_subagents=[sa.name for sa in get_subagent_configs()],
    agent_behavior=types.AgentBehavior.AUTONOMOUS,
    run_command_config=types.RunCommandConfig(enable_sandbox=False),
)

budget_config = types.BudgetConfig(
    max_model_calls=30,
    max_tool_calls=60,
    max_total_tokens=300_000,
)

compaction_config = types.CompactionConfig(
    token_threshold=32_000,  # Perfil deep (32k tokens por defecto)
)

config = LocalAgentConfig(
    model="gemini-3.8-flash",
    capabilities=capabilities,
    budget_config=budget_config,
    compaction_config=compaction_config,
    skills_paths=[".agents/skills"],
    tools=ALL_REPO_TOOLS,
    subagents=get_subagent_configs(),
)
```

Esta configuración establece `enable_sandbox=False`; no habilita el sandbox de comandos de Antigravity ni demuestra aislamiento de procesos o herramientas. No se debe afirmar que hay aislamiento salvo que se haya verificado un límite de autoridad separado a nivel del sistema operativo.

### 3.2 Auto-Descubrimiento de Habilidades del Repositorio
Cuando existe el directorio `.agents/skills/`, el agente lo expone mediante `skills_paths=[".agents/skills"]`. Las habilidades disponibles pueden cambiar independientemente del paquete de agentes; algunos ejemplos son `bannerlord-dotnet-artisan`, `calradia-forge-modding`, `agent-memory-systems` y `calradia-forge-desktop`.

---

## 4. Herramientas de Dominio del Repositorio

El repositorio expone herramientas Python que interactúan con sus scripts, compilaciones MSBuild y verificadores de arquitectura; las funciones actuales se enumeran a continuación:

| Función de Herramienta | Descripción | Invariante / Regla Comprobada |
| :--- | :--- | :--- |
| `run_dotnet_build` | Compila `CalradiaForge.sln` o proyectos con `dotnet build` | Compilación C#, 0 advertencias, 0 errores |
| `verify_stateless_behavior` | Ejecuta `tools/Verify-CalradiaForge-StatelessBehavior.bat` | Regla B: Cero SaveableTypeDefiner, SyncData sin estado |
| `run_solution_tests` | Ejecuta `tools/Run-CalradiaForge-Tests.bat` | Pruebas Core, ForgeWeave, Desktop MVVM y Render |
| `run_ui_automation_smoke` | Ejecuta `tools/Test-CalradiaForge-Desktop-Uia.bat` | Comprobaciones de UI Automation en la ventana WPF |
| `audit_documentation_parity` | Verifica correspondencia inglés/español en `docs/` | Paridad conceptual en pares `.md` y `.es.md` |
| `audit_ledger_integrity` | Valida la cadena SHA-256 en `.integrity.jsonl` | Integridad criptográfica inmutable del ledger |
| `inspect_csharp_source` | Escaneo estático AST en `src/` | Regla A: Anti-shadowing; Regla B: Statelessness |
| `audit_desktop_contracts` | Inspecciona `src/CalradiaForge.Desktop` | Regla C: 6 tokens contractuales estáticos |
| `audit_section_playbooks` | Audita playbooks, árboles de remedio y prefabs | Macros Gauntlet/Desktop y ForgePlaybookPanel |
| `audit_code_smells` | Detecta formato de cadenas, LINQ en tick, etc. | SyncData limpio y cero LINQ en hot path |
| `audit_concurrency_hazards` | Audita concurrencia, CAS y SemaphoreSlim | Almacenes thread-safe y despacho a hilo principal |
| `run_package_workflow` | Ejecuta `tools/package.ps1` | Regla D: Archivos limpios de distribución en `artifacts/` |

---

## 5. Ejecutor Unificado CLI (`tools/Run-CalradiaForge-Agents.bat`)

Un launcher mantenido ejecuta `tools/run_forge_agents.py` con el intérprete del `.venv` del repositorio. Si falta el entorno, muestra instrucciones para prepararlo y no recurre a un Python global. El CLI facilita la ejecución para desarrolladores, pipelines de CI/CD y flujos de trabajo autónomos:

### Invocaciones Básicas
```bat
REM Benchmark de compactación de tokens en las herramientas registradas actualmente
tools\Run-CalradiaForge-Agents.bat compact

REM Auditoría arquitectónica y de seguridad completa
tools\Run-CalradiaForge-Agents.bat audit

REM Auditoría de code smells, antipatrones y riesgos de concurrencia
tools\Run-CalradiaForge-Agents.bat bughunt

REM Auditoría de section playbooks, árboles de remedio y macros
tools\Run-CalradiaForge-Agents.bat playbooks

REM Auditoría de paridad documental bilingüe e integridad SHA-256 del ledger
tools\Run-CalradiaForge-Agents.bat docs

REM Compilar solución e inspeccionar reglas en código fuente C#
tools\Run-CalradiaForge-Agents.bat architect

REM Ejecutar suite de pruebas y comprobación UI Automation en Windows
tools\Run-CalradiaForge-Agents.bat verify

REM Ejecutar una tarea autónoma arbitraria
tools\Run-CalradiaForge-Agents.bat run "Auditar persistencia sin estado y verificar paridad documental"
```

### Opciones de Línea de Comandos
- `--offline`: Fuerza la ejecución local offline de herramientas (no requiere clave de API).
- `--verbose` / `-v`: Habilita la transmisión detallada de la salida del asistente.
- `--model <nombre>`: Modifica el identificador del modelo (por defecto: `gemini-3.8-flash`).
- `--compaction-preset [ultra|balanced|deep]`: Configura el umbral de tokens (`ultra`: 8k, `balanced`: 16k, `deep`: 32k; por defecto: `deep`).
- `--raw-tools`: Omite la destilación y envía registros sin procesar directamente al contexto del LLM.
- `--live`: (Usado con `compact`) Ejecuta todas las herramientas en vivo, incluyendo compilador, pruebas y empaquetado.

El paquete SDK opcional no se necesita para importar el CLI ni para la ejecución local offline de herramientas. La ruta online de Antigravity requiere que el SDK se pueda importar, que haya credenciales y que `offline_mode=False`; el perfil opcional `--agents` sí necesita el paquete porque prueba la configuración del SDK. Instálalo en el entorno aislado con `tools\Setup-CalradiaForge-Python.bat --agents --no-pause` y ejecuta `tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause`. El modo offline evita la ejecución en la nube; no la valida.

---

## 6. Ejecución Local Offline de Herramientas

La ejecución local offline de herramientas llama funciones seleccionadas del repositorio sin credenciales de API en la nube. `ForgeAgentOrchestrator` solo elige la ruta online cuando el SDK opcional se puede importar, hay credenciales y el modo offline está desactivado; en caso contrario, informa que ejecutó herramientas locales del repositorio en modo offline e indica el motivo de esa ruta.
- La ejecución local offline de herramientas llama a las funciones seleccionadas del repositorio; no inicia workers de subagentes del SDK. El informe clasifica cada resultado como `PASS`, `FAIL` o `INDETERMINATE` según marcadores explícitos. Solo informa `COMPLETE` cuando todas las llamadas seleccionadas devolvieron un informe y todos los veredictos son positivos explícitos; no implica que se ejecutaran comprobaciones no seleccionadas.
- Con SDK importable, credenciales presentes y modo offline desactivado, el orquestador usa `google.antigravity.Agent` para la ejecución online.

---

## 7. Verificación y Pruebas Automatizadas

El sistema de agentes autónomos se valida mediante pruebas unitarias en `tests/test_forge_agents.py`:
```bat
tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause
```
Cobertura de la suite de pruebas:
- Fábrica de configuración, ajustes de compactación y creación de instancias de Google Antigravity SDK.
- Validación de esquema de subagentes, instrucciones de sistema y vinculación de herramientas.
- Ejecución de herramientas de dominio y destilación semántica de salidas.
- Estimaciones heurísticas de tokens de `ForgeTokenCompactor`, retención de diagnósticos según patrones con cobertura de fixtures representativos y registro opcional de artefactos.
- Canal de orquestación multi-agente y ejecución local offline de herramientas.
- Procesamiento y despacho de argumentos de CLI.

---

## 8. Motor de Compactación de Tokens y Optimización de Contexto (`ForgeTokenCompactor`)

Para reducir el volumen de contexto durante flujos de trabajo multi-agente complejos, Calradia Forge utiliza un motor heurístico de destilación semántica implementado en `agents/compactor.py`. La destilación es deliberadamente resumida y con pérdida; el texto compacto no sustituye la salida original.

```
               Salida Cruda de Herramientas (Consola, MSBuild, Pruebas, UIA)
                                      │
                      ┌───────────────┴───────────────┐
                      ▼                               ▼
            Persistencia Forense              Destilador Semántico
        (artifacts/agent-runs/*.log)     (Retención de Errores y Métricas)
                      │                               │
                      └───────────────┬───────────────┘
                                      ▼
                        Informe Compacto de Alta Densidad
                     + Enlace de Referencia Forense para el LLM
```

### 8.1 Retención de diagnósticos y límites de la evidencia
Los destiladores intentan conservar líneas diagnósticas reconocidas en los resúmenes, pero no garantizan cero pérdidas ni cobertura completa. Los formatos que no coincidan con sus patrones, las listas de fallos acotadas y las salidas inesperadas pueden omitirse del texto compacto.
1. **Diagnósticos de compilación**: El destilador de MSBuild reconoce patrones compatibles de errores de compilación y construcción e incluye las líneas coincidentes. Hay regresiones para casos representativos; eso no demuestra que se capture cada diagnóstico de todos los compiladores, versiones de MSBuild, idiomas o herramientas personalizadas.
2. **Fallos de pruebas**: El destilador extrae algunos marcadores de fallo y líneas de aserción o error, con una salida acotada. No reproduce de forma fiable todos los nombres de pruebas, diferencias de aserción ni trazas completas.
3. **Diagnósticos de invariantes**: Los resúmenes de auditoría dependen del formato y de los patrones que reconoce cada destilador. Consulta la salida original para verificar todas las rutas y coordenadas de archivo y línea.
4. **Registro forense opcional**: Si el guardado de la salida cruda está habilitado, la salida no está vacía y la escritura concluye correctamente, el compactador conserva el texto sin comprimir en `artifacts/agent-runs/<timestamp>_<tool>.log` e incorpora su ruta al resumen. El registro es la evidencia más completa; no se garantiza si se deshabilita el guardado o falla la escritura.

### 8.2 Perfiles Adaptativos de Compactación (`CompactionConfig`)
La compactación de la ventana de contexto se controla dinámicamente mediante perfiles:
- **`ultra` (8.000 tokens)**: Diseñado para entornos con restricciones de cuota, modelos de ventana pequeña o comprobaciones deterministas rápidas.
- **`balanced` (16.000 tokens)**: Equilibrio operativo recomendado entre historial conversacional y consumo de contexto eficiente.
- **`deep` (32.000 tokens, por defecto)**: Ventana de contexto expandida para razonamiento profundo, refactorizaciones multi-agente complejas y revisiones de múltiples archivos.

### 8.3 Comparaciones reproducibles de estimaciones de tokens

Esta guía no publica líneas base fijas de ahorro de tokens ni recuentos de pruebas. Las estimaciones de `ForgeTokenCompactor` son heurísticas y dependen de la entrada exacta y de la versión del destilador. Para comparar, registra la revisión del repositorio, las versiones de Python y paquetes, el nombre de la herramienta, el fixture o hash exacto de la salida, los textos crudo y compactado y ambas estimaciones de cada ejecución. Trata esos valores como evidencia de esa entrada, no como umbrales universales ni garantías de rendimiento.

---

## 9. Arquitectura de Memoria Cognitiva CoALA (`agents/memory.py`)

El ejecutor de agentes del repositorio usa un modelo de memoria propio y acotado, inspirado en conceptos de **Arquitecturas Cognitivas para Agentes de Lenguaje (CoALA)**. Esto no afirma conformidad con un estándar formal CoALA ni garantiza que cada invocación reciba el mismo contexto semántico:

```
               ┌────────────────────────────────────────────────┐
               │              CoALAAgentMemory                  │
               └───────────────────────┬────────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
┌──────────────────┐         ┌──────────────────┐         ┌──────────────────┐
│ Memoria Semántica│         │ Memoria de Trabajo│        │ Memoria Episódica│
│(Cero Decaimiento/│         │ (Objetivo Activo/ │        │ (FIFO Acotada /  │
│   Invariantes)   │         │    Paso Actual)   │        │Guarda de Errores)│
└──────────────────┘         └──────────────────┘         └──────────────────┘
```

### 9.1 Memoria Semántica (`SemanticRepositoryMemory`)
- **Naturaleza:** Reglas inmutables del repositorio incluidas al generar el contexto semántico; no garantiza que todas las ejecuciones de agentes reciban exactamente el mismo contexto.
- **Contenidos:**
  - **Regla A (Anti-Shadowing)**: Cero carpetas, espacios de nombres o clases llamadas `Campaign` o `Localization`.
  - **Regla B (Statelessness)**: Cero `SaveableTypeDefiner` y `SyncData` limpio en `src/CalradiaForge.Mod`.
  - **Regla C (Contratos Estáticos de Desktop)**: Tokens estáticos obligatorios preservados en `src/CalradiaForge.Desktop`.
  - **Regla D (Seguridad de Distribución)**: Exclusión de binarios del motor, partidas y scripts en paquetes de release.
  - **Afinidad de Hilo del Motor**: Las llamadas a entidades de TaleWorlds deben ejecutarse en el hilo principal del juego.
  - **División temporal opcional**: Usa `ForgeTimeSlicer.ShouldProcess` solo para trabajo cuya semántica permita aplazarlo. La distribución de buckets puede ser desigual, el filtrado sigue recorriendo la colección y las afirmaciones sobre asignaciones o duración requieren medir el callback correspondiente.

### 9.2 Memoria de Trabajo (`WorkingAgentMemory`)
- **Naturaleza:** Marco de ejecución a corto plazo que refleja el estado de la tarea inmediata.
- **Estado:** Registra el objetivo principal activo, el rol del agente en turno (`ForgeArchitectAgent`, `BugHunterAgent`, etc.), el índice del paso actual, la herramienta en ejecución y el último resultado.

### 9.3 Memoria Episódica (`EpisodicTrace`)
- **Naturaleza:** Historial cronológico acotado de acciones ejecutadas y observaciones empíricas.
- **Registro de Traza:** Identificador de paso, nombre del agente, acción, resumen denso, tokens crudos, tokens compactados, tokens netos ahorrados, ratio de compresión, presencia de errores y enlace al registro forense.
- **Poda FIFO con preferencia por errores:** El valor predeterminado de `max_episodic_traces` es 32 y puede configurarse. Al superar el límite, se prefiere eliminar la traza sin errores más antigua entre las entradas antiguas elegibles; si no hay una disponible, se elimina la más antigua. Por tanto, se da preferencia a las trazas de error durante la poda, pero no se garantiza que permanezcan indefinidamente; el resumen también puede omitir detalles que solo estén en un registro forense opcional.

