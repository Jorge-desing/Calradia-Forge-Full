# Arquitectura de Agentes de IA Autónomos en Calradia Forge

Este documento define la arquitectura, la jerarquía de subagentes, las herramientas del repositorio y los canales de ejecución para los agentes de IA autónomos en Calradia Forge, impulsados por el **SDK de Google Antigravity** (`google.antigravity`).

---

## 1. Visión General y Misión

Calradia Forge implementa una arquitectura multi-agente especializada diseñada para mantener, auditar, desarrollar y verificar el repositorio de forma autónoma a través de todos sus marcos de destino:
- **`src/CalradiaForge.Mod`** (módulo de Bannerlord en el juego, `net472`).
- **`src/CalradiaForge.Desktop`** (banco de trabajo WPF independiente, `net8.0-windows`).
- **`src/CalradiaForge.Core` / `Sdk`** (sistemas centrales compartidos, `net472;net8.0`).

El sistema se integra directamente con las 45 habilidades de dominio en `.agents/skills/`, aplica estrictamente las reglas invariantes del repositorio (Reglas A, B, C y D), y admite tanto la ejecución en la nube mediante Gemini Developer API / Vertex AI como la simulación determinista fuera de línea para integración continua (CI).

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

---

## 2. Jerarquía Multi-Agente y Roles

El sistema se organiza en 6 agentes especializados:

| Nombre del Agente | Dominio Primario | Invariantes Aplicadas | Herramientas Vinculadas |
| :--- | :--- | :--- | :--- |
| **`ForgeMasterAgent`** | Orquestador Raíz | Descomposición de tareas, compuertas de seguridad, síntesis | Todas las herramientas y delegación (`START_SUBAGENT`) |
| **`ForgeArchitectAgent`** | C# y Motor de Bannerlord | Regla A Anti-Shadowing (`GEMINI.md`), límites TFM, decoradores GameModel | `run_dotnet_build`, `inspect_csharp_source` |
| **`StatelessBehaviorAuditor`** | Persistencia y Seguridad | Regla B Comportamientos sin estado, cero `SaveableTypeDefiner`, `SyncData` limpio | `verify_stateless_behavior`, `inspect_csharp_source` |
| **`DesktopWpfSpecialist`** | Workbench WPF e Interfaz | Regla C Contratos estáticos, reciclado de contenedores, bordes Aliased DirectX, UIA | `audit_desktop_contracts`, `run_ui_automation_smoke`, `run_solution_tests` |
| **`DocLedgerAgent`** | Docs e Integridad de Release | Regla D Seguridad, paridad bilingüe, cadena criptográfica SHA-256 del ledger | `audit_documentation_parity`, `audit_ledger_integrity`, `run_package_workflow` |
| **`BugHunterAgent`** | Code Smells y Concurrencia | Interpolación FormattableString, bloqueos CAS, guardas SyncRoot, despacho a hilo principal | `audit_code_smells`, `audit_concurrency_hazards`, `audit_section_playbooks` |

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
- **Rol:** Auditor de persistencia y simulación de campañas.
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
  - Comprobaciones externas de accesibilidad mediante Windows UI Automation con `tools/Test-CalradiaForge-Desktop-Uia.ps1`.

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
  - Audita `ConcurrentDictionary` y bloqueos CAS en `ForgeData`, `SyncRoot` en `ForgeAgentMemory` y `SemaphoreSlim` en `PipeClient`.
  - Hace cumplir el enrutamiento al hilo principal de TaleWorlds mediante `GameThreadActionDispatch`.
  - Audita los 8 playbooks de Gauntlet, 8 árboles de remedio en Desktop y enlaces de prefabs.

---

## 3. Integración con el SDK de Google Antigravity

La suite de agentes autónomos está construida directamente sobre `google.antigravity`:

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

### 3.2 Auto-Descubrimiento de Habilidades del Repositorio
El agente importa automáticamente las 45 habilidades especializadas ubicadas en `.agents/skills/` mediante `skills_paths=[".agents/skills"]`, permitiendo a los agentes consultar patrones de `bannerlord-dotnet-artisan`, `calradia-forge-modding`, `agent-memory-systems` y `calradia-forge-desktop`.

---

## 4. Herramientas de Dominio del Repositorio

Los agentes disponen de 12 herramientas en Python que interactúan con los scripts del repositorio, compiladores MSBuild y verificadores de arquitectura:

| Función de Herramienta | Descripción | Invariante / Regla Comprobada |
| :--- | :--- | :--- |
| `run_dotnet_build` | Compila `CalradiaForge.sln` o proyectos con `dotnet build` | Compilación C#, 0 advertencias, 0 errores |
| `verify_stateless_behavior` | Ejecuta `tools/verify_stateless_behavior.ps1` | Regla B: Cero SaveableTypeDefiner, SyncData sin estado |
| `run_solution_tests` | Ejecuta `tools/Run-CalradiaForge-Tests.bat` | Pruebas Core, ForgeWeave, Desktop MVVM y Render |
| `run_ui_automation_smoke` | Ejecuta `tools/Test-CalradiaForge-Desktop-Uia.ps1` | 29 comprobaciones de accesibilidad en ventana WPF |
| `audit_documentation_parity` | Verifica correspondencia inglés/español en `docs/` | Paridad conceptual en pares `.md` y `.es.md` |
| `audit_ledger_integrity` | Valida la cadena SHA-256 en `.integrity.jsonl` | Integridad criptográfica inmutable del ledger |
| `inspect_csharp_source` | Escaneo estático AST en `src/` | Regla A: Anti-shadowing; Regla B: Statelessness |
| `audit_desktop_contracts` | Inspecciona `src/CalradiaForge.Desktop` | Regla C: 6 tokens contractuales estáticos |
| `audit_section_playbooks` | Audita playbooks, árboles de remedio y prefabs | Macros Gauntlet/Desktop y ForgePlaybookPanel |
| `audit_code_smells` | Detecta formato de cadenas, LINQ en tick, etc. | SyncData limpio y cero LINQ en hot path |
| `audit_concurrency_hazards` | Audita concurrencia, CAS y SemaphoreSlim | Almacenes thread-safe y despacho a hilo principal |
| `run_package_workflow` | Ejecuta `tools/package.ps1` | Regla D: Archivos limpios de distribución en `artifacts/` |

---

## 5. Ejecutor Unificado CLI (`tools/run_forge_agents.py`)

Un script CLI unificado facilita la ejecución para desarrolladores, pipelines de CI/CD y flujos de trabajo autónomos:

### Invocaciones Básicas
```bash
# Benchmark exhaustivo de compactación de tokens en las 12 herramientas
py -3.12 tools/run_forge_agents.py compact

# Auditoría arquitectónica y de seguridad completa
py -3.12 tools/run_forge_agents.py audit

# Auditoría de code smells, antipatrones y riesgos de concurrencia
py -3.12 tools/run_forge_agents.py bughunt

# Auditoría de section playbooks, árboles de remedio y macros
py -3.12 tools/run_forge_agents.py playbooks

# Auditoría de paridad documental bilingüe e integridad SHA-256 del ledger
py -3.12 tools/run_forge_agents.py docs

# Compilar solución e inspeccionar reglas en código fuente C#
py -3.12 tools/run_forge_agents.py architect

# Ejecutar suite de pruebas y comprobación UI Automation en Windows
py -3.12 tools/run_forge_agents.py verify

# Ejecutar una tarea autónoma arbitraria
py -3.12 tools/run_forge_agents.py run "Auditar persistencia sin estado y verificar paridad documental"
```

### Opciones de Línea de Comandos
- `--offline`: Fuerza el modo de simulación determinista fuera de línea (no requiere clave de API).
- `--verbose` / `-v`: Habilita la salida detallada y la transmisión de pensamientos en tiempo real.
- `--model <nombre>`: Modifica el identificador del modelo (por defecto: `gemini-3.8-flash`).
- `--compaction-preset [ultra|balanced|deep]`: Configura el umbral de tokens de la ventana de contexto (`ultra`: 8k, `balanced`: 16k, `deep`: 32k, por defecto: `deep`).
- `--raw-tools`: Omite la destilación y envía registros sin procesar directamente al contexto del LLM.
- `--live`: (Usado con `compact`) Ejecuta todas las herramientas en vivo, incluyendo compilador, pruebas y empaquetado.

---

## 6. Modo de Simulación Determinista Fuera de Línea

Para garantizar pruebas reproducibles y validación en CI/CD sin requerir credenciales de API en la nube:
- Cuando `GEMINI_API_KEY` no está configurada o se pasa `--offline`, `ForgeAgentOrchestrator` se ejecuta automáticamente en **Modo de Simulación Determinista Fuera de Línea**.
- El orquestador analiza la intención de la tarea, delega el trabajo a los subagentes registrados, ejecuta las herramientas de dominio requeridas, valida los invariantes y produce un informe de ejecución estructurado y completo.
- Cuando `GEMINI_API_KEY` está presente, el orquestador se conecta a la API de Gemini usando `google.antigravity.Agent` para razonamiento autónomo y delegación multi-agente en vivo.

---

## 7. Verificación y Pruebas Automatizadas

El sistema de agentes autónomos se valida mediante pruebas unitarias en `tests/test_forge_agents.py`:
```bash
py -3.12 -m unittest tests/test_forge_agents.py
```
Cobertura de la suite de pruebas:
- Fábrica de configuración, ajustes de compactación y creación de instancias de Google Antigravity SDK.
- Validación de esquema de subagentes, instrucciones de sistema y vinculación de herramientas.
- Ejecución de herramientas de dominio y destilación semántica de salidas.
- Estimación de tokens de `ForgeTokenCompactor`, retención sin pérdidas de errores de compilador y registro de artefactos forenses.
- Canal de orquestación multi-agente y simulación fuera de línea.
- Procesamiento y despacho de argumentos de CLI.

---

## 8. Motor de Compactación de Tokens y Optimización de Contexto (`ForgeTokenCompactor`)

Para evitar el desbordamiento o la saturación de la ventana de contexto durante flujos de trabajo multi-agente complejos, Calradia Forge incorpora un motor de compactación de tokens de alta eficiencia y sin pérdida semántica implementado en `agents/compactor.py`:

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

### 8.1 Preservación de la Inteligencia de los Agentes (Telemetría de Errores sin Pérdidas)
El compactador de tokens está regido por una garantía estricta de cero pérdida de información diagnóstica:
1. **Errores de Compilador Sin Pérdidas**: Si `dotnet build` falla, todos los códigos `error CSxxxx`, `error MSBxxxx`, rutas de archivo, coordenadas de línea/columna y descripciones de error se preservan al 100%. Solo se eliminan los mensajes irrelevantes de restauración de paquetes.
2. **Fallos de Pruebas Sin Pérdidas**: Si fallan pruebas unitarias o de renderizado, se extraen y preservan textualmente los nombres de pruebas fallidas, tipos de excepción, diferencias de aserción (`Assert.AreEqual`) y trazas de pila (`stack traces`).
3. **Diagnósticos de Invariantes Sin Pérdidas**: Las violaciones en escaneos AST de C# (Regla A), comprobaciones de persistencia (Regla B) o contratos de desktop (Regla C) conservan todas las coordenadas de archivo y línea.
4. **Trazabilidad Forense Completa**: Cada invocación de herramienta guarda su salida completa sin comprimir en `artifacts/agent-runs/<timestamp>_<tool>.log`. La respuesta destilada incrusta la ruta del archivo, permitiendo que cualquier agente o desarrollador inspeccione el registro íntegro si requiere mayor detalle.

### 8.2 Perfiles Adaptativos de Compactación (`CompactionConfig`)
La compactación de la ventana de contexto se controla dinámicamente mediante perfiles:
- **`ultra` (8.000 tokens)**: Diseñado para entornos con restricciones de cuota, modelos de ventana pequeña o comprobaciones deterministas rápidas.
- **`balanced` (16.000 tokens)**: Equilibrio operativo recomendado entre historial conversacional y consumo de contexto eficiente.
- **`deep` (32.000 tokens, por defecto)**: Ventana de contexto expandida para razonamiento profundo, refactorizaciones multi-agente complejas y revisiones de múltiples archivos.

### 8.3 Métricas Empíricas de Reducción de Tokens

| Operación de Herramienta | Caracteres de Salida Cruda | Tokens Crudos | Tokens Compactados | Ahorro Neto | Ratio de Compresión |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`run_dotnet_build`** (Release Limpio) | 2.366 car. | ~622 tokens | ~56 tokens | 566 tokens | **87,5% - 91,0%** |
| **`run_solution_tests`** (726 Pruebas) | 53.222 car. | ~14.005 tokens | ~134 tokens | 13.871 tokens | **99,0%** |
| **`verify_stateless_behavior`** | 3.353 car. | ~882 tokens | ~146 tokens | 702 tokens | **82,8%** |
| **`run_ui_automation_smoke`** (29 Nodos) | 6.840 car. | ~1.800 tokens | ~78 tokens | 1.722 tokens | **95,7%** |
| **`run_package_workflow`** | 3.120 car. | ~183 tokens | ~83 tokens | 100 tokens | **54,6%** |
| **`audit_code_smells`** | 380 car. | ~94 tokens | ~40 tokens | 54 tokens | **57,4%** |
| **`audit_concurrency_hazards`** | 412 car. | ~103 tokens | ~48 tokens | 55 tokens | **53,4%** |
| **`audit_section_playbooks`** | 365 car. | ~91 tokens | ~42 tokens | 49 tokens | **53,8%** |
| **Suite Multi-Agente 12 Herramientas (`compact`)** | ~8.000 car. | 2.005 tokens | 880 tokens | 1.152 tokens | **57,5% de reducción** |

---

## 9. Arquitectura de Memoria Cognitiva CoALA (`agents/memory.py`)

Calradia Forge adopta el estándar de **Arquitecturas Cognitivas para Agentes de Lenguaje (CoALA)** para estructurar el contexto del agente a lo largo de tres niveles cognitivos:

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
- **Naturaleza:** Conocimiento inmutable y sin decaimiento de las reglas del repositorio inyectado en todo contexto de agente.
- **Contenidos:**
  - **Regla A (Anti-Shadowing)**: Cero carpetas, espacios de nombres o clases llamadas `Campaign` o `Localization`.
  - **Regla B (Statelessness)**: Cero `SaveableTypeDefiner` y `SyncData` limpio en `src/CalradiaForge.Mod`.
  - **Regla C (Contratos Estáticos de Desktop)**: Tokens estáticos obligatorios preservados en `src/CalradiaForge.Desktop`.
  - **Regla D (Seguridad de Distribución)**: Exclusión de binarios del motor, partidas y scripts en paquetes de release.
  - **Afinidad de Hilo del Motor**: Las llamadas a entidades de TaleWorlds deben ejecutarse en el hilo principal del juego.
  - **Anti-Lag Time-Slicing**: Distribución de héroes mediante módulo 24 y cero consultas LINQ en ticks de simulación.

### 9.2 Memoria de Trabajo (`WorkingAgentMemory`)
- **Naturaleza:** Marco de ejecución a corto plazo que refleja el estado de la tarea inmediata.
- **Estado:** Registra el objetivo principal activo, el rol del agente en turno (`ForgeArchitectAgent`, `BugHunterAgent`, etc.), el índice del paso actual, la herramienta en ejecución y el último resultado.

### 9.3 Memoria Episódica (`EpisodicTrace`)
- **Naturaleza:** Historial cronológico acotado de acciones ejecutadas y observaciones empíricas.
- **Registro de Traza:** Identificador de paso, nombre del agente, acción, resumen denso, tokens crudos, tokens compactados, tokens netos ahorrados, ratio de compresión, presencia de errores y enlace al registro forense.
- **Poda FIFO con Preservación de Errores:** Cuando el número de trazas supera la capacidad de la cola (`max_episodic_traces = 32`), las trazas ordinarias se descartan por orden FIFO. Sin embargo, cualquier traza con `has_errors == True` queda **estrictamente protegida frente a la poda**, garantizando que los fallos previos y diagnósticos críticos nunca sean olvidados durante el razonamiento en múltiples pasos.

