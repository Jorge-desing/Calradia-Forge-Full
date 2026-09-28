# Rev082 — Correcciones de disposición Gauntlet y revisión de fuente F10

**Fecha:** 28 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Panel Gauntlet, fuentes de texturas generadas, auditoría de disposición y diagnósticos existentes del ciclo de vida F10.

## Necesidad observada y justificación técnica

La auditoría del viewport 1280×720 mostró que el ledger normal de evidencia solo conservaba 102 DIP, comprimiendo innecesariamente la salida técnica. El explorador de resultados de pruebas también permitía que los valores de estado y duración se tocaran o solaparan cuando el estado era largo. Ocho fuentes decorativas de cabecera de ruta medían 128×64, pero se mostraban a 24×12, donde se perdía la mayor parte de su detalle. La aserción F10 histórica ya había recibido correcciones previas en la fuente; esta revisión volvió a comprobar la detección de flanco y la telemetría de capa existentes, sin tratarlas como prueba de que la aserción estuviera resuelta.

## Solución técnica y decisiones arquitectónicas

Se elevó el origen del ledger normal para reservar al menos 160 DIP para su área de contenido en el perfil de auditoría 1280×720. El explorador de resultados mantiene estado y duración en celdas independientes con recorte y al menos 6 DIP de separación, incluida la medición del ancho de encabezados localizados. Los ocho adornos de cabecera de baja resolución se retiraron de la generación activa de sprites, pero sus archivos master, preparados y de SpriteParts se archivaron con un manifiesto SHA-256. Los iconos semánticos existentes y los indicadores de foco nativos de Gauntlet siguen comunicando el estado de navegación.

Se seleccionaron tres masters de ImageGen con la dirección artística del proyecto `high-quality-image-generation`: tela cartográfica oscura, un textil heráldico vertical para el rail y un tratamiento heráldico transparente de cabecera. Los originales se conservan en `assets/gauntlet-imagegen/` y el flujo local determinista deriva sprites Gauntlet de tamaño fijo y alfa acotado. No se reutiliza arte WPF. Se revisó `SubModule.cs` sin modificarlo; el flanco ascendente exclusivo de F10 y la telemetría del ciclo de vida de `GauntletLayer` siguen siendo la ruta diagnóstica existente.

## Cambios en activos, código y dependencias

| Master de ImageGen | Dimensiones y modo | SHA-256 |
| --- | --- | --- |
| `forge_war_table_cloth_v3_master.png` | 2172×724 RGB | `32206FCD23A74379412DAC373CB26984553A6404CB9A732495D10700C2ADF552` |
| `forge_heraldic_rail_v4_master.png` | 887×1774 RGB | `9EA66B7F9F91828539D637EC2EE939E61BD7A8B66238CF7426C8420EE7F80FCF` |
| `forge_heraldic_header_v3_master.png` | 2048×768 RGBA, alfa 0–254 | `FA854C634D051EBD9D98909DD9F332963FD09FA78A628B69FFE6DFF78CAC0083` |

El producto permanece en 25.2.0. Los ocho adornos heredados se conservan en `assets/gauntlet-imagegen/archive/2026-09-28/` con `SHA256SUMS.txt`; retirarlos de la generación activa no elimina el arte histórico. Este trabajo no cambia API pública, rutas, comandos, permisos, dependencias ni ZIPs.

## Validación y límites de la evidencia

- Pasó `tools/Prepare-CalradiaForge-ImageGenTextures.bat --check --no-pause` para comprobar el determinismo de preparación.
- La ejecución reportada de `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` pasó Core 343/343 y ForgeWeave 73/73 sin advertencias de compilación. La regresión existente del flanco F10 y el ciclo de vida de la capa es solo evidencia de fuente.
- La auditoría visual Gauntlet y la regeneración del atlas fuente seguían pendientes al redactar este anexo. La importación por Resource Browser y el render Gauntlet en vivo siguen pendientes; las comprobaciones de fuente no demuestran comportamiento en ejecución.
- No se realizó observación F10 en vivo ni se inició campaña o batalla para esta revisión.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos.
