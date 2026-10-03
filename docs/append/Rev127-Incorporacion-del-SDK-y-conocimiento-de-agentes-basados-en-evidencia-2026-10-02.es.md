# Rev127 — Incorporación del SDK y consolidación del conocimiento de agentes basados en evidencia

**Fecha:** 02-10-2026  
**Versión del producto:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Incorporación local al SDK/plantilla, showcase determinista de contenido nativo, diagnósticos acotados de parches, ejemplo Gauntlet estático, guía de simulación medida y consolidación del conocimiento del proyecto.

## Objetivo observado y decisiones de diseño

El proyecto necesitaba una ruta reproducible para crear el primer mod y guías respaldadas por fuentes que no exageraran capacidades de runtime experimentales. La matriz de capacidades distingue lo implementado, experimental y propuesto. Las superficies de producto basadas en Harmony se sustituyeron por diagnósticos acotados de Forge; la observación opcional de runtime es solo por reflexión, no crea dependencia de Harmony ni demuestra compatibilidad entre mods reales. El ejemplo de contenido usa esquemas Native verificados; las facciones se posponen hasta verificar un esquema y caso.

## Artefactos entregados

- Se añadió un paquete NuGet local y versionado `CalradiaForge.Sdk` y una plantilla de módulo `.NET new` con dependencia explícita de Forge y referencias Bannerlord resueltas localmente. No se distribuyen ensamblados propietarios TaleWorlds.
- Se añadieron generadores deterministas de tropas/ítems y un showcase estático instalable. Los flujos BAT mantenidos comprueban prefab, ViewModel, localización, manifiesto, referencias nativas y salida del paquete.
- Se añadieron evidencia diagnóstica acotada y de solo lectura para parches y una página Gauntlet estática. Los destinos de parches compartidos son únicamente señales de revisión. Forge atribuye fallos solo en límites de callbacks que controla; no reordena ni desactiva parches de terceros.
- Se consolidaron lecciones verificadas de .NET, Bannerlord, SDK, pruebas, documentación y agentes en especialistas del proyecto y se sincronizaron las guías raíz. La documentación de time-slicing indica que filtrar aún recorre la fuente y no afirma mejoras de rendimiento sin respaldo.

## Validación

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: compilaciones limpias `net472`, `net8.0` y Desktop, cero advertencias/errores; Core 415/415; Patch Diagnostics 30/30; ForgeWeave 73/73; Desktop 65/65; 295 casos WPF y 320 pases de layout/render. Los 20.903 ms son tiempo del arnés, no latencia de la aplicación.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat`: 4/4 comprobaciones aprobadas.
- `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause`: Ruff 0.16.9, 25 pruebas de assets/archivos, cinco de imágenes, 18 casos de auditoría de agentes, 15 del orquestador offline, tres del launcher CLI y comprobaciones Gauntlet/sprites aprobadas. La ejecución `--ledger` verificó 42/42 pares EN/ES.
- Los 29 directorios de skills modificados aprobaron `quick_validate.py`; el BAT de conocimiento de onboarding aprobó la paridad/enlaces de guías raíz y nueve validadores de skills.
- La prueba aislada de onboarding `20261003T012628Z-f3bebd08` empaquetó e instaló localmente el SDK y la plantilla 25.2.0, generó y restauró un módulo consumidor y lo compiló como `net472` contra referencias licenciadas del GameBin local sin advertencias/errores. El informe registra el contrato SDK estable 13 y `workingTreeApiChangesIncluded: false`. SHA-256 de artefactos locales: SDK `2f4aa6863129bd8ea67ae6e7b8b71de643975d962c0e007e02f38ece5f98718c`; plantilla `f9b3a74577e9ae5b9caa7134b37dab8214111396a876fad8e57fd499e2115a64`.
- El BAT del showcase aprobó generación determinista, esquemas Native Items/NPCCharacters instalados, comprobaciones de manifiesto/referencias y compilación limpia `net472` del módulo generado.

## Límites de evidencia

No se inició Bannerlord ni Modding Kit para esta validación. La carga/render del juego, la coexistencia real con Harmony, la publicación pública en NuGet y la integración con mercados de Visual Studio/Rider siguen sin verificar. Las comprobaciones estáticas de Gauntlet no son evidencia de render en el juego. No se afirma una mejora de rendimiento en runtime. Las referencias HTML de análisis se preservaron tal como fueron proporcionadas y no se reescribieron como autoridad técnica.
