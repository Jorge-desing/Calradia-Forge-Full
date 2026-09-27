# Validación de Calradia Forge 21.0.1

Validación: 22-09-2026. Fue una ronda breve de mantenimiento y empaquetado. No se inició Bannerlord; tampoco se cargaron campañas o batallas ni se ejecutaron pruebas de resistencia o mediciones prolongadas.

## Correcto

- `tools/Run-CalradiaForge-Tests.bat --no-pause` pasó después del cambio de versión: compilación con 0 advertencias y 0 errores; Core 231/231; ForgeWeave 31/31; Desktop 39/39; renderizado/recursos WPF 263 casos en 14,293 segundos.
- El empaquetador regeneró el sitio DocFX y 24 textos de ayuda contextual para 13 idiomas, regeneró nueve partes de sprite atribuidas y validó el atlas de 2048×256 y el TPAC de ejecución.
- `python tools/audit_localization.py` validó los 13 catálogos nativos con 195 claves por idioma y los catálogos Desktop con 193 claves por idioma.
- `tools/Inspect-CalradiaForge-Tpac.bat --validate-only --no-pause` comprobó el paquete fuente: 538 bytes, marcador `TPAC` y SHA-256 `7DAC28A71B23C00D28F59F5E110B62F17692F2D82910F05A185DC713F5A579E8`. Solo comprueba existencia, tamaño, marcador y hash; no analiza ni certifica el contenido.
- `python tools/audit_package.py --version 21.0.1` pasó para los tres ZIP. La auditoría comprueba contenido e integridad, versiones de módulos y ensamblados, XML, duplicados y rutas inseguras, ausencia de DLL de TaleWorlds y partidas, y ausencia de ejecutables anfitrión `.exe`. El JSON de auditoría y la lista SHA-256 quedan en `artifacts/` del espacio de trabajo.

## Límites

- Esta ronda no repitió una comprobación en vivo de Resource Browser o del menú de Bannerlord. El changelog 21.0.0 registra una comprobación visual breve anterior; no corresponde a esta validación 21.0.1.
- TpacTool local es un visor/exportador opcional proporcionado por el usuario. El preflight no lo inicia ni afirma compatibilidad con Bannerlord 1.4.8.
- No se inició ningún ejecutable de pruebas directamente; los `.bat` del repositorio alojaron las bibliotecas de pruebas.
