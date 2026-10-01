# Herramientas Python

Calradia Forge usa Python para validar el repositorio, preparar recursos, ejecutar utilidades de documentación y, de forma opcional, agentes Antigravity. Estos paquetes son herramientas de desarrollo; no son dependencias del módulo de Bannerlord, el SDK, Core ni la aplicación WPF, y tampoco forman parte de los paquetes del mod.

## Entorno admitido

Usa Python 3.12 en el entorno local `.venv` de la raíz del repositorio. Git ignora ese entorno virtual, que mantiene las herramientas del proyecto separadas del Python del sistema. Instálalo o actualízalo explícitamente desde la raíz:

```bat
tools\Setup-CalradiaForge-Python.bat
```

El launcher de preparación crea o reutiliza `.venv` e instala el perfil de desarrollo. No modifica la instalación global de Python. Los launchers de pruebas y validación requieren que la preparación ya se haya completado; no instalan dependencias automáticamente. Si falta Python 3.12 o algún paquete requerido, ejecuta la preparación y luego vuelve a ejecutar el launcher `.bat` solicitado.

## Perfiles de dependencias

| Manifiesto | Contenido y propósito |
| --- | --- |
| [`requirements-tools.txt`](../requirements-tools.txt) | Bibliotecas fijadas que usan las herramientas del repositorio, incluidas PyYAML para validar metadatos de skills, Pillow para procesar imágenes y `lxml`/`python-docx` para los flujos documentales. |
| [`requirements-dev.txt`](../requirements-dev.txt) | Perfil de desarrollo que incluye las herramientas y Ruff fijado para análisis estático. |
| [`agents/requirements.txt`](../agents/requirements.txt) | Dependencias opcionales y separadas para el runtime de agentes Google Antigravity. |

La preparación normal instala `requirements-dev.txt`. Para añadir el runtime opcional de agentes al mismo entorno aislado, usa:

```bat
tools\Setup-CalradiaForge-Python.bat --agents
```

La opción `--agents` es explícita; la preparación normal no instala el runtime Antigravity. Mantén sus paquetes en `agents/requirements.txt` en vez de incorporarlos al perfil general de herramientas.

## Validación de metadatos de skills

Pasa el directorio de la skill al launcher del repositorio para validar el frontmatter de `SKILL.md` con el validador de Codex Skill Creator y el entorno del proyecto:

```bat
tools\Validate-CalradiaForge-Skills.bat .agents\skills\calradia-forge-desktop
```

El launcher busca `quick_validate.py` en la instalación activa de Codex Skill Creator. El perfil de herramientas del proyecto proporciona PyYAML, así que el validador puede analizar el frontmatter YAML sin instalar paquetes globalmente. Si no está disponible el propio script validador, instala o restaura la skill Skill Creator en Codex y vuelve a ejecutar el launcher.

## Ruff

Ruff es una dependencia de desarrollo. Ejecuta su comprobación de solo lectura así:

```bat
.venv\Scripts\ruff.exe check
```

La configuración del repositorio limita la comprobación inicial a los puntos de entrada y pruebas Python que se mantienen. Usa solo `check`: no ejecutes autofix, `format` ni comandos de validación que reescriban archivos.

## Pruebas y comprobaciones

Ejecuta las pruebas Python y los validadores del proyecto mediante sus launchers `.bat`. Así se usa la cadena de herramientas configurada y las opciones del repositorio. Completa primero la preparación; los launchers deben fallar con una indicación clara si falta el entorno, en lugar de recurrir silenciosamente a un Python global o antiguo. Los ejecutores de pruebas no preparan ni actualizan el entorno automáticamente.

CI selecciona Python 3.12 e instala el perfil de herramientas antes de ejecutar comprobaciones Python. El trabajo de pruebas de agentes prepara explícitamente el perfil opcional y ejecuta `tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause`; las comprobaciones locales normales no requieren Antigravity. Ninguno de los perfiles se copia a los ZIP de distribución ni se carga durante la ejecución del mod.
