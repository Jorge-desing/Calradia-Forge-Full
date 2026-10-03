# Rev125 — Corrección de evidencia diagnóstica y del informe de fallos

**Fecha:** 02-10-2026  
**Versión del producto:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Validez del informe JSON de fallos, límites de evidencia, documentación de agentes y regresiones.

## Hallazgos y cambios

La revisión encontró que `SubModule.OnUnhandledException` escapaba manualmente solo comillas y saltos de línea, por lo que una ruta Windows u otro carácter de control JSON podía invalidar el informe `.cfcrash`. El manejador ahora serializa los mismos campos `Timestamp`, `IsTerminating` y `Exception` con el ensamblado Newtonsoft.Json que el módulo ya referencia desde el juego. Una regresión invoca el formateador de producción y parsea un informe con ruta Windows, comillas, CR/LF, tabulación y un carácter de control. El BAT Core también invoca ahora la suite existente del gate F10, por lo que esa prueba ya no queda inactiva.

La skill de depuración y el texto Desktop para análisis de fallos describen ahora `.cfcrash` como texto de excepción; donde corresponde, `.dmp` y `.sav` se limitan a metadatos del archivo. Las guías de agentes coinciden con la lista configurada de herramientas de `BugHunterAgent`; el comando Core de CODEX usa el flag no interactivo que admite el launcher. Los informes históricos 25.0.0–25.2.0 permanecen intactos. La guía actual de evolución del SDK aclara que las frases previas de anti-lag y cero asignaciones no representan una medición del callback de campaña completo; el arnés sintético `ProcessBatch` no demuestra rendimiento dentro del juego.

## Validación y límites

- `tools\Run-CalradiaForge-Tests.bat --core-only --no-pause` aprobó con compilaciones limpias `net472` y `net8.0` (0 advertencias, 0 errores), Core 414/414 y ForgeWeave 73/73. Incluye las regresiones del flanco F10 y la serialización del informe.
- `tools\Run-CalradiaForge-Desktop-Tests.bat --no-pause` aprobó 65/65.
- `tools\Validate-CalradiaForge-Skills.bat .agents\skills\debugging-master` indicó que la skill es válida.
- No se inició Bannerlord ni Modding Kit. Estas comprobaciones no verifican la entrega real de F10 ni el render dentro del juego.
- La auditoría completa de paquetes/hashes y la integridad del registro protegido se verifican después de este apéndice.

Esta entrada es append-only; la versión 25.2.0 y los contratos públicos del SDK no cambian.
