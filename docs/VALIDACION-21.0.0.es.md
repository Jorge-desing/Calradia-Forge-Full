# Validación de Calradia Forge 21.0.0

Validación de mantenimiento: 22-09-2026. La versión sigue en 21.0.0. No se usaron campañas, batallas, pruebas de resistencia ni capturas prolongadas de rendimiento.

## Correcto en el espacio de trabajo actual

- `tools/Run-CalradiaForge-Tests.bat --no-pause` compiló la solución con 0 advertencias y 0 errores y ejecutó las suites mediante sus BAT: Core 231/231, ForgeWeave 31/31, Desktop 39/39 y renderizado/recursos WPF 263 casos en 14,024 segundos.
- Los fixtures de Resource Browser pasaron las comprobaciones de simulación, hashes de copias, respaldos, ID del módulo, paquetes ausentes/vacíos/incorrectos y preflight de solo lectura. Rechazan marcadores TPAC inválidos o truncados; también prueban categorías SpriteData ausentes, metadatos malformados y diferencias en el número de atlas.
- `tools/Inspect-CalradiaForge-Tpac.bat --validate-only --no-pause` revisó el paquete fuente: 538 bytes, marcador `TPAC` y SHA-256 `7DAC28A71B23C00D28F59F5E110B62F17692F2D82910F05A185DC713F5A579E8`. Lee el marcador de cuatro bytes y calcula el hash; no analiza ni certifica todo el contenido.
- `python tools/validate_game_icon_assets.py --require-tpac` validó nueve iconos atribuidos, metadatos del atlas y el paquete de ejecución presente. El ZIP Modules existente contiene el mismo TPAC de 538 bytes, con el mismo marcador y hash.
- `python tools/audit_localization.py` validó los 13 catálogos nativos y de Desktop: 195 claves nativas por idioma y 193 claves de Desktop por idioma.
- `python tools/audit_package.py --version 21.0.0` verificó los tres ZIP existentes, manifiestos, XML, integridad, archivos requeridos y exclusiones. La evidencia está en `artifacts/package-audit-2100.json`; los hashes están en `artifacts/package-sha256-2100.txt`.

## Límites y estado de distribución

- En esta ronda no se inició Bannerlord ni TpacTool. El changelog 21.0.0 registra una comprobación breve anterior en el menú del Modding Kit; no se repitió ahora. No se abrió campaña ni batalla.
- El TpacTool local es 0.4.0. Upstream indica que esa versión apunta a Bannerlord 1.8.0 beta; su compatibilidad con el objetivo 1.4.8 no está verificada. Es un visor/exportador opcional, no importa ni empaqueta TPAC.
- Los tres ZIP 21.0.0 son los artefactos de la versión ya generada. Pasaron la auditoría, pero no incluyen las nuevas modificaciones aún sin publicar del analizador y del script de inspección. Regenera los ZIP con la próxima versión antes de distribuir estos cambios.
