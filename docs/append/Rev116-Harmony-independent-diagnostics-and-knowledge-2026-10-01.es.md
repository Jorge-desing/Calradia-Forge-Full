# Rev116 — Diagnósticos independientes de Harmony y consolidación del conocimiento — 1 de octubre de 2026

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK/Core, protocolo Desktop, diagnósticos de parches y guías de agentes. No se modifica el contrato `ForgeApi`.

## Problema observado y justificación técnica

El diagnóstico Harmony había crecido hasta ser una superficie pública de Core, un miembro de informe y una acción de protocolo/UI, lo que confundía compatibilidad opcional con dependencia y podía sugerir garantías de coexistencia que las observaciones no demuestran. La consolidación de onboarding también requería distinguir hechos probados de propuestas como integración visual con IDE, hot reload y comportamiento dentro del juego.

## Solución técnica y decisiones arquitectónicas

- Se retiraron los DTO y la API pública `HarmonyDiagnostics`, `SessionReport.Harmony` y la acción `harmony`; los consumidores deben migrar a `ForgePatchDiagnostics`, `SessionReport.PatchDiagnostics` y `patch-diagnostics`.
- El reemplazo conserva diagnósticos propios de Forge para hooks y reemplazos de método. La observación opcional externa usa reflexión sobre un ensamblado exacto `0Harmony` suministrado por el llamador y ya cargado, y solo sobre los miembros públicos de consulta verificados. No añade referencias, paquetes ni binarios Harmony; tampoco carga el ensamblado ni modifica el estado de parches de terceros.
- `ForgeProtocol.Version` avanza a 2 por la retirada de la acción y sus modelos; `EnvelopeVersion` permanece en 1. `ForgeApi.Version` permanece en 13 y el producto en 25.2.0.
- Los límites acotan la enumeración y la salida de Forge. Los resultados parciales se marcan como incompletos. No se presenta esta reflexión como sandbox: el coste y efectos dentro de getters de terceros síncronos no quedan limitados. Compartir un destino parcheado constituye una señal de revisión, no una detección concluyente de conflicto o de coexistencia.
- Las instrucciones y skills consolidadas separan capacidades implementadas, evidencia local y propuestas aún no verificadas. La plantilla CLI y la muestra estática no demuestran integración de interfaz con Visual Studio/Rider, publicación pública de NuGet, hot reload ni carga/render en Bannerlord.

## Cambios en activos, código y dependencias

Se actualizó la superficie de diagnóstico y migración del protocolo junto con la guía bilingüe, los changelogs y la orientación de agentes. No se agregaron dependencias a Harmony ni cambios a `ForgeApi`; se conservan los TFM del proyecto y la versión del producto. El informe interactivo existente no forma parte de esta revisión y permanece intacto.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause`: compilaciones `net472` y `net8.0` con 0 advertencias y 0 errores; Core 403/403 y ForgeWeave 73/73.
- `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`: 27/27 pruebas enfocadas de diagnóstico. Son fixtures; no ejecutan ni certifican coexistencia con Harmony en Bannerlord.
- Las validaciones BAT de onboarding local SDK/plantilla y de muestra estática se ejecutaron antes de los últimos cambios exclusivos de diagnóstico. El smoke local no equivale a integración de UI con un IDE ni a publicación de un feed público.
- No se abrió Bannerlord, campaña o batalla; tampoco se verificó hot reload o render de la página Gauntlet en el motor. Esta revisión no generó ZIP de distribución.

Este anexo se agrega tras Rev115 sin sustituir ni reescribir párrafos anteriores del registro protegido.
