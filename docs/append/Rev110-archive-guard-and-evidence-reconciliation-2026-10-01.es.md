# Rev110 — Protección del archivo de distribución y conciliación de evidencia

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Staging/auditoría de distribución, registros de evidencia del workbench de hooks, pruebas y documentación.

## Motivo de esta revisión

La cadena de integridad Rev109 y sus documentos append-only eran válidos, pero una auditoría independiente detectó que el ZIP Source-SDK anterior podía incluir scripts de desarrollo y un árbol `node_modules` instalado. El JSON retenido de render Rev109 también difería 3 ms del resumen de consola, y los logs archivados de la suite y la utilidad eran anteriores a las últimas pruebas agregadas. El registro histórico Rev109 permanece intacto; esta revisión conserva evidencia nueva y corrige la brecha de distribución.

## Cambios y validación

- `tools/package.ps1` ahora elimina los `.bat` y `.ps1` de staging de módulos y código fuente, y poda `node_modules` en cualquier profundidad. El único script permitido es el launcher exacto `Desktop/Run-CalradiaForge-Desktop.bat`.
- `tools/audit_package.py` rechaza cualquier otra entrada `.bat` o `.ps1` y toda ruta que contenga un segmento `node_modules`. `tests/CalradiaForge.AssetPipeline.Tests.py` agrega regresiones para rechazar scripts, permitir únicamente la excepción Desktop, filtrar dependencias anidadas y rechazar payloads ZIP.
- `cmd.exe /c "tests\\CalradiaForge.AssetPipeline.Tests.bat --no-pause <nul"` terminó con código 0 y 20/20 pruebas.
- `cmd.exe /c "tools\\Test-CalradiaForge-HookUtility.bat"` terminó con código 0. La salida conservada `artifacts/hook-utility-rev110.txt` registra 29 casos de argumentos y 22 casos de transporte. La fixture aislada simula el rechazo de tokens; no verifica vencimiento, sesión, pantalla ni estado del menú del Runtime.
- `cmd.exe /c "tools\\Run-CalradiaForge-Tests.bat --skip-build --no-pause --render-output artifacts\\hook-rev110-render.json <nul"` terminó con código 0: Core 404/404, ForgeWeave 73/73, Desktop 65/65 y 294 casos de render WPF. El resumen de consola indicó 13.183 ms y el JSON estructurado registró 13.187 ms. Ambos informan 182 pasadas de render/layout y 5.827,1 ms en llamadas de layout. Se conservan como lecturas separadas, sin elegir una en silencio. El tiempo del arnés no equivale a latencia interactiva de Desktop.

## Límites

Pasaron el filtro de staging y las pruebas sintéticas de auditoría; todavía debe ejecutarse la compuerta canónica de los tres ZIP contra esta revisión antes de aceptar los archivos de distribución. Este anexo no afirma hashes ni resultado de auditoría de los ZIP. La mutación de hooks en Bannerlord sigue sin verificarse; no se emitió Apply/Verify/Revert ni se abrió campaña o batalla. Los fixtures seriales no demuestran seguridad si otro hilo ejecuta el destino durante la instalación o retirada del detour. La versión del producto permanece en 25.2.0 y `ForgeApi.Version` en 13.

Esta revisión agrega evidencia sin modificar Rev001–Rev109.
