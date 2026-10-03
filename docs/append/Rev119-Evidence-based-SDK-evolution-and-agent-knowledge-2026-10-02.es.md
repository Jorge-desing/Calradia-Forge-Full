# Rev119 — Evolución del SDK y consolidación del conocimiento de agentes basada en evidencia

**Fecha:** 2 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Incorporación local de desarrolladores, ejemplo de contenido generado, diagnósticos de patches propios de Forge, guía de time-slicing, documentación bilingüe y reglas/skills de agentes del proyecto.

## Problema observado y justificación técnica

La evolución propuesta reunía capacidades actuales con ideas que todavía requieren evidencia de ejecución o del ecosistema. Algunas descripciones de rendimiento podían sugerir una distribución uniforme por grupos o menos trabajo aunque `ForgeTimeSlicer` sigue recorriendo la colección fuente. La compatibilidad con Harmony debía seguir siendo opcional, sin restaurar una dependencia ni presentar un destino compartido como prueba de conflicto.

## Solución técnica y decisiones arquitectónicas

- Se añadió un paquete local compilable de `CalradiaForge.Sdk` y una plantilla de módulo `.NET new` para `net472`, con referencias del juego resueltas localmente y sin distribuir ensamblados TaleWorlds.
- Se añadió generación determinista de tropas/ítems y una muestra instalable de contenido, además de un ejemplo de página Gauntlet estática a nivel de código fuente. Las facciones y las afirmaciones sobre UI en el motor quedan aplazadas hasta contar con un esquema o caso de ejecución verificado.
- Se sustituyó el informe Harmony retirado por diagnósticos de hooks/patches propios de Forge. La compatibilidad opcional inspecciona por reflexión una identidad `0Harmony` exacta y compatible que ya esté cargada; solo se ejecuta cuando se solicita y no carga, aplica, retira ni reordena Harmony.
- Se corrigió la recomendación de planificación por ID estable en código, pruebas, mapas, skills y guías de agentes. Los grupos estables pueden variar de tamaño; filtrar todavía recorre toda la colección de entrada en O(N), por lo que no se afirma una mejora general de rendimiento.
- Se sincronizaron las guías técnicas bilingües y las instrucciones comunes de agentes; los procedimientos especializados permanecen en sus skills. El registro protegido se amplía sin reescribir entradas anteriores.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: compilaciones limpias (0 advertencias/errores), Core 410/410, ForgeWeave 73/73, Desktop 65/65, render WPF con 295 casos y 308 pases de layout/render. Los 24.476 ms de WPF son tiempo del arnés, no latencia de la app en vivo.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat --portable`: pasaron los cuatro criterios de aceptación.
- `tools/Run-CalradiaForge-Python-Checks.bat --no-pause`: pasaron Ruff y las comprobaciones mantenidas de Python, imágenes, assets y estructura.
- `tools/Test-CalradiaForge-Developer-Onboarding.bat --no-pause`: pasaron el empaquetado local de SDK/plantilla, instalación aislada de plantilla y restauración/compilación del módulo `net472` generado, con cero advertencias/errores.
- `tools/Test-CalradiaForge-ContentShowcase.bat --no-pause`: pasaron generación determinista, validación con esquema local, compilación del módulo, benchmark sintético y comprobaciones Core/ForgeWeave. Las cifras del benchmark describen solo su arnés sintético.
- `tools/Validate-CalradiaForge-Skills.bat` validó 26 directorios de skills modificadas. La paridad documental era 42/42 antes de este apéndice; la cadena protegida se verifica después de anexar Rev119.
- No se probó una sesión real de Bannerlord, campaña, batalla, instalación/coexistencia real con Harmony, publicación pública en NuGet ni integración con mercados de IDE. La instalación opcional de dependencias Antigravity sigue sin verificarse porque anteriormente su espejo de paquetes devolvió `InvalidChunkLength`.

La versión del producto permanece en 25.2.0; `ForgeApi.Version` sigue en 13, el sobre de solicitud permanece en v1 y el protocolo de diagnósticos de patches en v2. Estos resultados son evidencia de fuente, compilación y pruebas, no prueba de renderizado, rendimiento dentro del juego o coexistencia con terceros.
