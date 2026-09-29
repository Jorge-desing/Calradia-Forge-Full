# Rev066 — Ciclo de vida explícito y experimental del motor de parcheo

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, Core, Mod y documentación. Activación explícita de parches, preflight de solo lectura y propiedad reversible de detours.

## Problema observado y justificación técnica

La aplicación de parches debe ser una decisión explícita del operador o del llamador. Un escaneo durante el inicio del módulo oculta cuándo se modifica el código y puede aplicar declaraciones antes de revisar sus destinos y callbacks. El preflight debe indicar si las declaraciones se resuelven sin cargar ensamblados arbitrarios, invocar callbacks ni modificar memoria ejecutable. La reversión debe preservar los cambios hechos por otro componente en vez de restaurar a ciegas un prólogo almacenado.

La implementación sigue siendo una función experimental de desarrollo. `VirtualProtect` y `FlushInstructionCache` no impiden que otro hilo ejecute un método mientras se modifican sus bytes, y el backend actual no decodifica ni reubica instrucciones de máquina sobrescritas.

## Solución técnica y decisiones arquitectónicas

- `SubModule.OnSubModuleLoad()` ya no inicia un escaneo global de parches. `ForgeBootstrapper.InitializeGlobalPatches()` se conserva únicamente como no-op obsoleto de compatibilidad. `ForgePatcher.ApplyAll(assembly)` es explícito y valida el lote declarado completo antes de intentar escrituras.
- Patch Preflight es de solo lectura. Resuelve firmas exactas del destino y callback, comprueba la aridad genérica, IDs duplicados, colisiones de destino, referencias de orden y ciclos usando los ensamblados ya proporcionados a la operación. No carga ensamblados, no invoca callbacks ni aplica hooks; las declaraciones de tipos de hook que el backend no admite permanecen como declaraciones solamente.
- El SDK agrega la capacidad opcional `IForgePatchService` mediante `ForgeApi.Patches`, sin ampliar `IForgeRegistry`. `ForgeApi.Version` avanza de 10 a 11. El servicio expone reemplazos explícitos con propietario descriptivo, snapshots inmutables, verificación y handles idempotentes para revertir por ID, propietario o todos los parches propios. El texto del propietario sirve para atribución; no es un límite de seguridad.
- `ForgeDetour`, `MethodSwapper` y los registros de parches comparten una ruta de detour y escritura. El backend comprueba arquitectura x64 y firmas de método admitidas, cambia la protección de la página ejecutable para escribir, vacía la caché de instrucciones y verifica los bytes registrados. La reversión restaura los bytes originales solo si los actuales coinciden exactamente con los instalados por Forge; los bytes ajenos se registran como conflicto y se dejan intactos. Si falla la restauración, se intenta recuperar el estado y se registra fallo o incertidumbre cuando no se puede verificar.
- La desconexión rechaza nuevas aplicaciones y revierte los parches de este servicio en orden inverso. Los handles siguen disponibles para consultar su estado y limpiar recursos. Los eventos de solicitud prefix/postfix son notificaciones, no un backend de parcheo implícito. Los comandos de consola `cf.patch_status [owner]` y `cf.patch_revert <id|owner|all>` informan o revierten explícitamente registros; Patch Preflight por IPC sigue siendo de solo lectura.

## Cambios en activos, código y dependencias

Los cambios fuente abarcan los contratos y adaptadores de detour del SDK, el preflight y el servicio de Core, el inicio del módulo y los comandos de consola, además de las guías bilingües de planes de parcheo y referencia del SDK. No se añade Harmony, MonoMod, Iced ni otra dependencia de parcheo. ForgeWeave y la versión del producto no cambian.

## Validación y límites de la evidencia

- No se ejecutaron pruebas automatizadas ni compilaciones durante la preparación de esta entrada documental. La validación requerida por `.bat` y la cobertura del fixture desechable x64 siguen pendientes; no se afirma ningún conteo de pruebas aprobadas.
- No se ejecutó Bannerlord ni Modding Kit. El cambio de código fuente y las comprobaciones de memoria simulada, si se ejecutan después, no demuestran seguridad frente a una ejecución concurrente con la escritura de memoria.
- El backend no suspende ni coordina los hilos que ejecutan el destino y no decodifica ni reubica límites de instrucciones. Debe seguir siendo experimental y no describirse como seguro para métodos activos que podrían tener llamadores concurrentes.

Este anexo amplía la revisión previa del registro sin reemplazar sus párrafos. La versión del producto permanece en 25.2.0; la capacidad opcional del SDK se identifica con `ForgeApi.Version` 11, mientras no cambian los implementadores de `IForgeRegistry`, ForgeWeave ni los ZIP de distribución.
