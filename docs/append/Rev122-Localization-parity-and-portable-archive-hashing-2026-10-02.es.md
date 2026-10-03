# Rev122 — Paridad de localización y cálculo portable de hashes de archivo

**Fecha:** 2 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** catálogo fuente español del SDK y generación de hashes del empaquetado.

## Problema observado y justificación técnica

El pipeline canónico de empaquetado se detuvo al generar la UI nativa porque dos ayudas traducidas al español usaban texto español como clave del catálogo, en vez de las claves fuente inglesas correspondientes. Tras corregirlo, el pipeline llegó a auditar los archivos, pero el runspace actual de Windows PowerShell no pudo resolver `Get-FileHash` y terminó antes de escribir el manifiesto SHA-256.

## Solución técnica y decisiones arquitectónicas

- Se actualizaron únicamente las dos claves del catálogo español para que coincidan con sus textos fuente ingleses, preservando las traducciones existentes.
- Se sustituyó la búsqueda del comando por el hash streaming de `System.Security.Cryptography.SHA256` para cada archivo ya creado, y la disposición determinista del algoritmo y el stream.
- Los ZIP, el JSON de auditoría y el manifiesto permanecen en `artifacts/`, ignorado por Git; no se incrementó la versión, publicó el producto ni prepararon los archivos generados para Git.

## Cambios en activos, código y dependencias

Se modificaron `localization/es.xml`, `tools/package.ps1` y el changelog/anexo bilingüe. El flujo de empaquetado continúa detrás de `tools/Package-CalradiaForge.bat`; no cambian las dependencias de ejecución.

## Validación y límites de la evidencia

- El pipeline final `tools\Package-CalradiaForge.bat` pasó con compilación limpia, Core 411/411, ForgeWeave 73/73, Desktop 65/65, WPF con 295 casos/308 pases de layout-render, generación determinista y XSD del showcase, DocFX y auditoría de archivos.
- Los tres archivos de versión 25.2.0 existen en `artifacts/`; los SHA-256 calculados de forma independiente coincidieron con el manifiesto generado.
- Los datos WPF pertenecen solo al arnés. No se inició Bannerlord, campaña, batalla ni publicación externa.

El producto permanece en 25.2.0; `ForgeApi.Version` permanece en 13.
