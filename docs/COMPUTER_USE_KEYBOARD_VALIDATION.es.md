# Reparación del teclado de Computer Use — 2026-10-01

El runtime actual por usuario es `1b30f7d4d73226ca`, con `@oai/sky` 0.7.5. Se reaplicó la corrección anterior del alias Ctrl mediante el perfil reconocido explícitamente y un respaldo original verificado. Por separado, el helper nativo construía las entradas INPUT de pulsación y liberación con `wScan = 0`; ese defecto persistía tras corregir el alias.

El proxy local intercepta SendInput solo cuando coinciden el perfil PE x64 cargado, la importación y los bytes exactos del constructor auditado. Completa los códigos ausentes con la distribución del hilo de la ventana activa. Conserva las entradas originales, Unicode/ratón, códigos existentes, liberaciones, conteo devuelto y errores Win32. Los perfiles desconocidos no activan esta corrección. No cambiaron APIs públicas, IPC, MSIX ni WindowsApps.

El registro instalado conservaba una ruta de staging después de promover el runtime. Una migración acotada y recuperable acepta únicamente el runtime, propietario, hashes de helper/DLL y rutas relativas observados, sin puntos de reanálisis. Respalda el registro y la DLL antes de corregir la ruta. Las demás instalaciones movidas o desconocidas se conservan; Status sigue siendo de solo lectura.

## Validación

- `CodexCaptureCompat/capture-compat/build.bat`: compilación y pruebas nativas de parser, adaptador de teclado, errores, ciclo de captura y callbacks aprobadas.
- `CodexCaptureCompat/capture-compat/tests/Test-CodexCaptureCompatPersistence.bat`: aprobadas propiedad, preservación de estados ajenos/corruptos, Status sin escrituras, recuperación, respaldos, reparse y guardas de promoción de staging.
- `Manage-CodexCaptureCompatKeyAliases.bat Status`: Applied, con respaldo original verificado.
- BAT de instalación gestionada: reemplazo verificado y watcher propio saludable. El registro ya apunta al runtime definitivo y los hashes de la DLL fuente e instalada coinciden.
- Fixture iniciado únicamente mediante `tests/Start-CodexCaptureCompatFixture.bat`: observados F10 `0x44/0x44`, Alt+OEM3 `0x27/0x27`, Alt+OEM5 `0x29/0x29`, Ctrl+K y clic inocuo. Ctrl, Alt y Shift quedaron liberados tras cada combinación. La captura funcionó y el proceso cargó tanto el proxy local como version.dll del sistema.

Después se cerró el fixture y se reinició la sesión de Computer Use. No se cerraron Codex Desktop, Explorer ni otras aplicaciones. Esta evidencia verifica la entrada aislada, no el sondeo de Bannerlord ni la ejecución de hooks. Sigue pendiente una nueva comprobación en el juego; los nombres OEM dependen de la distribución y no deben remapearse globalmente.

## Hashes exactos

| Elemento | SHA-256 |
| --- | --- |
| Helper actual | `ABDD75DF576B3CBCC7ED170DE1B4F27A65C81E25768A9B0B46D682FB586FB483` |
| Respaldo del adaptador original | `617D8E6E18FDDE25F06D4CBA2C84C994E076E05F30A8C55D09E918401C48B171` |
| Adaptador corregido | `F40CEF88BF48349EA769FE6A10D290D2B25F1623D504C37EE11BA30A10CD4517` |
| Respaldo del proxy anterior | `52CA7BFD990F99344F233E136D6F9B03BB793D906B8493E2A4BD368843DFBE84` |
| Proxy instalado | `965164B41DC83144C159CADE60ACB560BA9D3D89327AB389EED778741F2BE3D4` |

Los respaldos permanecen en `%LOCALAPPDATA%\OpenAI\Codex\CodexCaptureCompat`; los binarios generados y registros de pruebas no son fuentes del repositorio. Los runtimes futuros desconocidos necesitan otro perfil verificado, sin forzar estas anclas.
