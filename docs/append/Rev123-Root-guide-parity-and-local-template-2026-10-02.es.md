# Rev123 — Seguridad del builder SDK, paridad de guías y validación local de la plantilla

**Fecha:** 02-10-2026  
**Versión del producto:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Documentación, reglas e inicio con el SDK. Guías raíz de agentes y evidencia de evolución del SDK.

## Problema observado y justificación técnica

Una revisión de solo lectura encontró que `AGENTS.md` y `CODEX.md` prohibían los nombres `Campaign` y `Localization` dentro del ámbito del módulo, mientras que `GEMINI.md` solo prohibía `Campaign`. La guía de evolución del SDK también citaba únicamente las entradas iniciales Rev115–Rev116, aunque el seguimiento se había anexado hasta Rev122.

El reporte previo de onboarding verificó la generación portable de la plantilla y la restauración de paquetes, pero no compiló el módulo consumidor generado contra un `GameBin` local de Bannerlord con licencia. Por ello, las referencias locales requerían una comprobación separada y explícita. Una revisión focalizada del builder también encontró que un índice disperso muy grande creaba todas las variaciones intermedias y que un objetivo de mejora repetido podía rechazarse después de alcanzar el límite de dos ramas.

## Solución técnica y decisiones arquitectónicas

Las tres guías raíz ahora comparten los nombres anti-sombreado, su ámbito, justificación y alternativas aprobadas. Las guías de evolución del SDK en inglés y español enlazan la evidencia inicial y las revisiones posteriores Rev119–Rev123. `ForgeTroopBuilder` ahora almacena únicamente las claves de variación no negativas declaradas en un diccionario ordenado, conservando la salida ascendente sin asignar huecos dispersos. Rechaza los índices negativos explícitamente. También devuelve correctamente cuando el objetivo de mejora ya existe, antes de aplicar el límite de dos objetivos distintos; el tercer objetivo diferente sigue rechazado. Las firmas públicas, los TFM, la versión del producto y el orden XML de los índices normales no cambian.

Se ejecutó el BAT de onboarding con la raíz local de Bannerlord. Generó y restauró la plantilla consumidora y compiló su módulo `net472` usando las referencias del GameBin local. La compilación terminó con cero advertencias y cero errores. Esto solo comprueba la compilación local; no demuestra la integración visual de Visual Studio/Rider ni la carga o el render dentro del juego.

## Cambios en activos, código y dependencias

Se actualizaron `GEMINI.md`, `CODEX.md`, `docs/SDK_EVOLUTION.md`, `docs/SDK_EVOLUTION.es.md`, los changelogs bilingües y `ForgeTroopBuilder`, con regresiones específicas. No se modificó ninguna dependencia, ensamblado del juego, firma pública de API ni manifiesto de paquete.

## Validación y límites de la evidencia

- `tools/Test-CalradiaForge-Developer-Onboarding.bat --game-path "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord" --no-pause` aprobó; la restauración del módulo generado y su compilación contra GameBin local terminaron con cero advertencias/errores.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` aprobó tras la corrección: Core 411/411 y ForgeWeave 73/73, con compilaciones net472/net8 sin advertencias ni errores. Las regresiones cubren índice disperso `int.MaxValue`, orden ascendente, rechazo de negativos, duplicados idempotentes al límite y rechazo de una tercera rama.
- Antes de la corrección del builder, la suite BAT completa aprobó en este objetivo: Core 411/411, ForgeWeave 73/73, Desktop 65/65 y render WPF 295/295, con compilación sin advertencias ni errores. El pipeline final de paquetes se vuelve a ejecutar tras este anexo.
- Las comprobaciones Python CI, skills, conocimiento de onboarding, auditoría de paquete fuente y empaquetado/hash canónico aprobaron durante este objetivo; las comprobaciones actuales de integridad/paridad se repiten tras este anexo.
- No se inició Bannerlord ni Modding Kit para la compilación local. No se afirma render en vivo ni funcionamiento visual de los asistentes IDE.

Este anexo amplía el registro protegido sin reescribir revisiones anteriores. Conserva la versión 25.2.0 y todos los contratos públicos del SDK.
