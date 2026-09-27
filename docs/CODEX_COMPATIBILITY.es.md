# Compatibilidad con Codex y Arquitectura Multi-Agente

Este documento describe la arquitectura, la integración de herramientas y las directrices operativas para OpenAI Codex CLI y los sistemas multi-agente en Calradia Forge.

---

## 1. Visión General y Alineación del Ecosistema

Calradia Forge está diseñado para ser totalmente operable por diversos agentes autónomos de IA y asistentes de pair programming. Para lograr paridad entre Google Antigravity / Gemini y OpenAI Codex, el repositorio proporciona superficies de configuración y adaptadores en tiempo de ejecución.

```text
┌──────────────────────────────────────────────────────────────┐
│                    Developer / Agent CLI                     │
├──────────────────────────────┬───────────────────────────────┤
│    Google Antigravity        │       OpenAI Codex CLI        │
│   (GEMINI.md, .agents/)      │  (AGENTS.md, CODEX.md, .codex)│
└──────────────┬───────────────┴───────────────┬───────────────┘
               │                               │
               ▼                               ▼
┌──────────────────────────────────────────────────────────────┐
│                  Espacio de Trabajo Calradia Forge           │
├──────────────────────────────────────────────────────────────┤
│  • Reglas Invariantes de Anti-Shadowing y Sin Estado         │
│  • Solución Multi-Target (net472 / net8.0-windows)           │
│  • Verificación Automatizada y Suite de Pruebas de 5 Fases   │
│  • CodexCaptureCompat (Capa Computer Use para Windows 10)    │
│  • ForgeEncyclopediaExtender (API de Codex en el Juego)      │
└──────────────────────────────────────────────────────────────┘
```

---

## 2. Superficies de Configuración

### 2.1 Manifiestos de Instrucciones e Interoperabilidad de Reglas (`AGENTS.md`, `CODEX.md`, `.codex/config.json`)
- **`AGENTS.md`**: Especificación universal multi-agente que define capas arquitectónicas, restricciones de anti-shadowing, invariantes de ausencia de estado, manual de comandos, taxonomía de skills y la taxonomía gateway de 43 reglas.
- **`CODEX.md`**: Punto de entrada directo para sesiones de OpenAI Codex CLI con comandos de referencia rápida, enrutamiento de skills e índice de reglas modulares.
- **`.codex/config.json`**: Metadatos del proyecto legibles por máquina (versión 25.2.0), comandos de ejecución de pruebas, mapeo de target frameworks y `"rulesPath": ".agents/rules"`.
- **Protocolo de Descubrimiento de Reglas Modulares**: Mientras que Google Antigravity descubre `.agents/rules/*.md` automáticamente, Codex CLI utiliza la taxonomía compartida en `AGENTS.md` (Sección 7) y `CODEX.md` (Sección 5) para aplicar de forma idéntica los invariantes de arquitectura, gameplay, UI, documentación y empaquetado.

### 2.2 Reglas Invariantes Aplicadas
- **Restricción de Anti-Shadowing**: Nunca crear carpetas, espacios de nombres o clases llamadas `Campaign` o `Localization` en los ensamblados del juego. Las colisiones con `TaleWorlds.CampaignSystem.Campaign` rompen `Campaign.Current`.
- **CampaignBehaviors sin Estado**: Cero herencia de `SaveableTypeDefiner` y cero persistencia en `SyncData(IDataStore)` dentro de comportamientos en `src/CalradiaForge.Mod`.
- **Contratos Fuente Estáticos de Escritorio**: Pruebas automáticas de reflexión verifican cadenas inmutables en `src/CalradiaForge.Desktop`.

---

## 3. Compatibilidad con Codex Computer Use en Windows 10 (`CodexCaptureCompat`)

### 3.1 Contexto y Necesidad Técnica
OpenAI Operator y Codex Computer Use (`codex-computer-use.exe`) utilizan Windows Graphics Capture (WGC). En Windows 10:
1. La interfaz `IGraphicsCaptureSession3` (específicamente `IsBorderRequired`) no está disponible de forma nativa.
2. La conversión síncrona de imágenes dentro de las retrollamadas de WGC puede inducir bloqueos mutuos (deadlocks).

### 3.2 Arquitectura de la Solución
El espacio de trabajo incorpora `CodexCaptureCompat`:
- **DLL Proxy (`version.dll`)**: Intercepta `RoGetActivationFactory` para `Direct3D11CaptureFramePool`.
- **Compatibilidad de Borde**: Devuelve una interfaz compatible cuando se consulta `IGraphicsCaptureSession3`, evitando llamadas a APIs del sistema inexistentes.
- **Despacho de Retrollamadas en MTA**: Delega retrollamadas `FrameArrived` a subprocesos de trabajo MTA del threadpool de Windows, eliminando deadlocks de ingesta de fotogramas.

### 3.3 Gestión mediante `tools/Manage-CodexCaptureCompat.ps1`

```powershell
# Comprobar binarios dist y proceso helper en ejecución
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Status

# Ejecutar suite de pruebas de instalación
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test

# Previsualizar instalación en el helper objetivo
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action WhatIf -HelperPath C:\Path\To\codex-computer-use.exe
```

---

## 4. Extensor de Codex / Enciclopedia en el Juego

Para mecánicas de lore, tropas y asentamientos en el juego:
- **Clase**: `CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender`
- **Características**:
  - `RegisterEntry(EncyclopediaEntry)`: Añade definiciones de lore o entidades personalizadas al códex.
  - `Search(query, category)`: Búsqueda rápida por subcadena en nombres, etiquetas y descripciones.
  - `AddBookmark(id)` / `IsBookmarked(id)` / `GetBookmarks()`: Sistema de marcadores rápidos para el jugador.
  - `RegisterFilter(category, tag, predicate)` / `Filter(category, tag)`: Categorización dinámica y filtrado por facetas.
- **Seguridad en Hilos**: Totalmente sincronizado mediante bloqueos de monitor internos; seguro para consultas concurrentes entre el hilo del juego y subprocesos de trabajo.

---

## 5. Comandos de Verificación

```powershell
# 1. Aceptación de Ausencia de Estado
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1

# 2. Suite Completa de Pruebas
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"

# 3. Verificación de Codex Capture Compat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Manage-CodexCaptureCompat.ps1 -Action Test
```
