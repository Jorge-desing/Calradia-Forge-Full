# Rev035 — Resultados explícitos y ciclo de vida fiable

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.0.0; `ForgeApi.Version = 10`  
**Alcance:** SDK y Core compartido; guardado de ajustes, auto-registro, memoria de agentes y notificaciones de disponibilidad.

## Problema observado y justificación técnica

Las operaciones heredadas de guardado y descubrimiento no exponían resultados tipados que permitieran distinguir de forma segura entre persistencia completada, serializador ausente y fallos de serialización/almacenamiento o de registro. Las lecturas de memoria devolvían el valor predeterminado para claves ausentes, vencidas o incompatibles con el tipo solicitado. Además, una transición reentrante de conexión podía dejar callbacks pendientes asociados a una generación del registro ya reemplazada, y una desregistración concurrente podía regresar antes de impedir una entrega que ya había pasado su comprobación de vigencia.

## Solución técnica y decisiones arquitectónicas

`ModSettings.TrySave<T>` devuelve `ModSettingsSaveResult` con los estados `Saved`, `SerializerUnavailable`, `SerializationFailed` y `StorageFailed`. Rechaza ajustes nulos antes de serializar, escribe un archivo temporal en el mismo directorio y mueve o reemplaza el destino antes de actualizar la caché. Un fallo conserva el archivo y la caché previos. Los IDs de módulo comparten identidad sin distinguir mayúsculas/minúsculas en Windows; serializadores y deserializadores se ejecutan fuera del bloqueo de confirmación por módulo y una carga antigua no sobrescribe una operación concurrente o reentrante más reciente. Sin serializador, `Register<T>` conserva el valor predeterminado suministrado, pero no crea un archivo vacío que aparente persistirlo. `Save<T>` conserva su firma y registra el fallo tipado.

`ForgeApi.AutoRegisterWithReport` devuelve conteos y hasta 64 detalles acotados por tipo/contrato, continúa con candidatos independientes y conserva los registros que sí tuvieron éxito. Distingue un host no conectado de una pasada parcial. `AutoRegister` mantiene su firma histórica y registra los errores reportados mediante el logger disponible.

`ForgeMemoryReadResult<T>` permite distinguir `Found`, `Missing`, `Expired` y `TypeMismatch` en Semantic; Procedural informa los estados no relacionados con TTL. Las APIs `Get<T>` existentes conservan su retorno predeterminado al no encontrar un valor compatible. Las entregas administradas de `RegisterWhenAvailable` usan un token por suscripción y comprueban registro y generación antes de llamar fuera del bloqueo global. Las desregistraciones eliminan primero el token y esperan cualquier callback en curso; las instantáneas antiguas y las entregas posteriores al retorno se omiten. El callback puede desregistrarse a sí mismo.

## Cambios en activos, código y dependencias

Se añadieron resultados tipados y APIs aditivas en `src/CalradiaForge.Sdk/ModSettings.cs`, `Contracts.cs` y `ForgeAgentMemory.cs`; se agregaron regresiones de persistencia, auto-registro, estados de memoria y ciclo de vida concurrente en las suites Core y ForgeWeave. Se actualizó el contrato SDK a 10, la versión fuente/manifiestos a 25.0.0 y las guías/codemaps EN/ES. Se mantienen `net472` y `net8.0`; no se añadieron dependencias, referencias TaleWorlds al SDK/Core ni cambios al módulo de juego. Los ZIP existentes no se regeneraron. El changelog de fuentes registra la implementación inicial como Rev053 y el endurecimiento final de nulidad/desregistro como Rev054; la numeración DOCX es una secuencia independiente.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --core-only --no-pause <nul"` terminó correctamente.
- Los builds seleccionados `net472` y `net8.0` terminaron con cero advertencias y cero errores.
- Core: 318/318 pruebas aprobadas. ForgeWeave: 71/71 pruebas aprobadas. El proceso de prueba se inició mediante el launcher `.bat`; no se iniciaron directamente ejecutables de prueba.
- Bannerlord no se inició; no se afirma validación dentro del motor. No se generaron ni modificaron ZIPs.

Este anexo amplía el registro anterior sin reemplazar sus párrafos ni reescribir revisiones previas.
