# Rev129 — Validación final de la evolución del SDK y límites de evidencia

**Fecha:** 02-10-2026  
**Versión:** Calradia Forge 25.2.0, sin cambios; `ForgeApi.Version` 13  
**Alcance:** SDK, Core, recursos estáticos Desktop/Gauntlet, herramientas de desarrollo, ejemplos, documentación y conocimiento de agentes del proyecto.

## Problema observado y justificación técnica

Fue necesario contrastar la ruta de evolución con el código implementado, separándola de las propuestas. El inicio local de desarrollo, la generación de contenido nativo, los diagnósticos de parches y los helpers de simulación tienen niveles de madurez y límites de evidencia distintos. También había que asegurar que los recursos generados conservaran el texto localizado de origen y que la documentación y las instrucciones para agentes afirmaran solo lo respaldado por el código y las comprobaciones repetibles.

## Solución técnica y decisiones arquitectónicas

El flujo local de onboarding compila un paquete versionado del SDK y una plantilla de módulo `.NET new`, los instala en una colmena aislada, genera un consumidor, lo restaura y lo compila contra referencias licenciadas de GameBin local. El showcase de contenido generado continúa siendo estático y determinista; no afirma creación de entidades en runtime ni render Gauntlet.

`ForgePatchDiagnostics` informa evidencia acotada de hooks/reemplazos propiedad de Forge y puede consultar de forma opcional una superficie pública compatible en un ensamblado exacto `0Harmony` ya cargado. Forge no referencia, carga, distribuye, aplica, retira ni reordena Harmony. La consulta externa se ejecuta sincrónicamente dentro del proceso: los límites de salida no ponen en sandbox ni limitan internamente el CPU, las asignaciones o los efectos de Harmony; los destinos compartidos son señales para revisión, no veredictos de conflicto. Las pruebas usan fixtures y no demuestran coexistencia con mods reales de terceros.

La guía de simulación ahora describe el comportamiento de `ForgeTimeSlicer`: los IDs estables eligen buckets deterministas, pero la colección de entrada se sigue recorriendo. No se afirma una mejora de rendimiento sin medir el callback completo. La guía de recursos Gauntlet y cuatro rótulos de ayuda por categoría usan fuentes localizadas generadas para los 13 idiomas existentes; la versión y los contratos públicos no cambian.

## Cambios en activos, código y dependencias

El objetivo añade empaquetado local del SDK/plantilla y su prueba aislada, un showcase determinista de tropas/ítems y su verificador, el adaptador/pruebas de diagnóstico acotado de parches, soporte del entorno Python de herramientas, ayuda localizada por categoría y actualizaciones basadas en evidencia para reglas, skills especialistas y las guías raíz `AGENTS.md`, `CODEX.md` y `GEMINI.md`. No añade una dependencia obligatoria de Harmony ni distribuye ensamblados propietarios de TaleWorlds. El producto permanece en 25.2.0 y `ForgeApi.Version` en 13.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: compilaciones limpias `net472`, `net8.0` y Desktop; cero advertencias/errores; Core 416/416, ForgeWeave 73/73, Desktop 65/65 y 295 casos WPF con 320 pases de layout/render. El render tomó 19.288 ms en el arnés solamente.
- `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`: 30/30 aprobadas.
- `tools\Test-CalradiaForge-Developer-Onboarding.bat`: pasaron instalación aislada de paquetes SDK/plantilla, generación del consumidor, restauración y compilación `net472` sin advertencias contra referencias licenciadas locales de GameBin. Pasaron `tools\Test-CalradiaForge-ContentShowcase.bat` y `tools\Verify-CalradiaForge-StatelessBehavior.bat`.
- Pasó `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause`. Se intentó aparte el setup opcional `--agents`, pero pip falló con `InvalidChunkLength`; el perfil opcional que requiere esas dependencias queda sin verificar. `--ledger` verificó los 42/42 pares de documentos técnicos mantenidos en inglés y español; este apéndice bilingüe se registra por separado en el historial protegido de mejoras.
- Las 29 skills modificadas aprobaron `tools\Validate-CalradiaForge-Skills.bat`; también pasó `tools\Validate-CalradiaForge-Onboarding-Knowledge.bat`. Los recursos runtime generados coincidieron con las 52 traducciones nuevas (13 idiomas × 4 rótulos).
- El analizador TPAC profundo se omitió porque faltaban `TpacTool.Lib.dll` o su fixture Native local. No se inició Bannerlord ni Modding Kit. No se afirma carga/render en motor, coexistencia real con Harmony, publicación pública de NuGet ni integración con mercados de Visual Studio/Rider.

Este apéndice añade evidencia sin reemplazar revisiones anteriores. Conserva comandos, rutas, contratos y versión; la medición del arnés de render no es latencia de la aplicación.
