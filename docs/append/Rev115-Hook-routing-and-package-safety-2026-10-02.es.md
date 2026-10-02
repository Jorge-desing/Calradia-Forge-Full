# Enrutamiento verificado de hooks y seguridad de paquetes — 2026-10-02

**Versión:** Calradia Forge 25.2.0, sin cambios; API SDK 13
**Alcance:** Registro de pruebas Core, bindings Gauntlet de Hook Workbench, seguridad de salida de paquetes, guía bilingüe de parches y validación de distribución.

## Problema observado y justificación técnica

La revisión final detectó una colisión de IDs entre registros en el host de pruebas: `TestEngine.RegisterTranspiler` enviaba el registro directamente al servicio de hooks y omitía el conjunto de IDs sin distinción de mayúsculas usado por los comandos. Una regresión independiente de Gauntlet mostró que la visibilidad derivada de la ruta podía quedar desactualizada al navegar entre secciones o cambiar el foco de evidencia. El empaquetado también necesitaba un límite explícito contra sobrescrituras, porque repetirlo en el mismo directorio artifacts podía reemplazar archivos previos.

## Solución técnica y decisiones arquitectónicas

`RegisterTranspiler` ahora valida y reserva IDs bajo `registryGate`, comparte el conjunto global de IDs del host y revierte la reserva si falla el registro del servicio. Gauntlet notifica los cambios de `IsPatchPreflightActive` e `IsHookWorkbenchVisible` durante los cambios de layout/ruta y al cambiar el foco de evidencia. Hook Workbench sigue siendo una superficie condicional de acciones dentro de Patch Preflight; no reemplaza el rail de navegación ni el ledger de evidencias. Los paquetes solo pueden escribirse en subdirectorios ignorados de `artifacts/`, se rechazan destinos existentes antes de compilar y la limpieza solo afecta al staging creado por la ejecución actual.

## Cambios en código, activos y dependencias

El trabajo modifica `TestEngine`, su regresión de IDs globales, `PanelViewModel`, las pruebas de navegación Gauntlet, los scripts de salida/auditoría de paquetes, la configuración de launchers y los 13 catálogos de idioma regenerados por la compilación canónica. No cambian API, dependencias, versión del producto, contrato de hooks en ejecución, comandos ni superficie IPC. Se mantienen producto 25.2.0, API SDK 13 y MonoMod 25.3.6.

## Validación y límites de la evidencia

- `tools/package.ps1 -PackageOutputDirectory artifacts/deliverables-2026-10-02-r2` terminó mediante el pipeline canónico. Sus launchers de pruebas usaron archivos `.bat`; no se ejecutó directamente ningún binario de pruebas.
- La compilación Release y DocFX terminaron con cero advertencias y cero errores. Pasaron las suites BAT: Core 414/414, ForgeWeave 73/73, Desktop 65/65 y WPF con 295 casos de render / 308 pases de layout. El harness WPF tardó 14.552 ms; no es la latencia interactiva de la aplicación.
- La aceptación stateless pasó 4/4 mediante `tools/Verify-CalradiaForge-StatelessBehavior.bat`.
- Los tres archivos 25.2.0 pasaron la auditoría y las comprobaciones SHA-256 independientes coincidieron con el manifiesto. Windows denegó la eliminación de tres directorios de staging propios; el script lo notificó y los conservó bajo `artifacts/` ignorado, sin barrer otros directorios de staging.
- Bannerlord no se abrió. El render Gauntlet en vivo, la aplicación real de hooks desde el menú principal y el comportamiento de Resource Browser siguen sin verificarse; no se cargó campaña ni batalla.

Este anexo añade evidencia al registro protegido sin reescribir revisiones anteriores.
