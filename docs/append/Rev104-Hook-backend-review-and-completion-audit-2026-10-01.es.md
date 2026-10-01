# Rev104 — Revisión del backend de hooks y auditoría de finalización

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Revisión de SDK, Core, Mod, Desktop, dependencias de ejecución y evidencia de pruebas. Sin nuevos tipos ejecutables de hook ni paquetes de distribución.

## Problema observado y justificación técnica

La biblioteca de detours auditada solicitada necesita un límite de evidencia definido. El usuario aceptó una revisión documentada del código y las dependencias, en lugar de exigir una auditoría de seguridad independiente publicada. Esta revisión abarca la integración de Forge; no es una auditoría exhaustiva de MonoMod ni una certificación de ejecución nativa concurrente.

El BAT maestro completo pasó, pero una ejecución posterior del BAT Core devolvió 400/401: la prueba de guardado concurrente de ajustes con alias de distinta capitalización informó un guardado fallido. La aserción anterior descartaba el resultado tipado y la excepción interna. La causa sigue sin confirmarse; las ejecuciones posteriores aprobadas no resuelven esa observación. Ocho repeticiones adicionales de Core pasaron; después, otra ejecución falló en la prueba distinta Atomic settings replacement con una IOException de File.Replace: Windows no pudo retirar el archivo que iba a reemplazarse. Esta evidencia amplía la investigación al límite de almacenamiento; no identifica un proceso externo responsable.

## Solución técnica y decisiones arquitectónicas

- `ForgeBootstrapper.InitializeGlobalPatches` sigue siendo un no-op obsoleto. `SubModule.OnSubModuleLoad` no escanea ni aplica parches. `ForgePatcher.ApplyAll` resuelve el ensamblado indicado antes de confirmar el lote; los adaptadores antiguos usan reservas compartidas de destinos y recibos que distinguen generaciones.
- `ForgeDetour` comprueba arquitectura, firmas, límites de página, bytes instalados, cambios de protección y vaciado de caché de instrucciones. La reversión rechaza bytes ajenos. Los fallos Win32 simulados y los fixtures nativos desechables seriales cubren recuperación sin ejecutar un destino simultáneamente con escrituras de código.
- `ForgeHookService.Register` es inerte. Prefix/Postfix usan MonoMod.RuntimeDetour 25.3.6 fijado únicamente bajo `NETFRAMEWORK`; Core net8.0 y Desktop no instalan ese backend. Finalizer y Transpiler siguen sin ser tipos ejecutables admitidos.
- La integración conserva el invocador original exacto durante el despacho, difiere la retirada propia hasta terminar esa invocación serial, conserva handles inciertos y reservas de destino, y desactiva los callbacks retenidos durante la descarga. Las reservas compartidas impiden mezclar detours directos y hooks administrados sobre un destino de Forge.
- Las capacidades opcionales del SDK siguen separadas de los implementadores del registro. `ForgeApi.Version` es 12 después de la extensión de hooks; la disponibilidad se determina mediante `ForgeApi.Patches` y `ForgeApi.Hooks`, no solo por la constante de compilación. Los snapshots copian su estado, los propietarios son etiquetas y los handles conservan verificación y recuperación explícitas.
- La mutación exige el hilo del juego, el tipo CLR exacto del menú principal oficial y ausencia de campaña, misión o multijugador. WPF selecciona únicamente IDs registrados. Los planes IPC admiten hasta 32 IDs, caducan a los 60 segundos, quedan ligados a sesión/servicio/pantalla/época de contexto y consumen el token coincidente antes de comprobar la mutación. Los resultados desconocidos requieren reconciliación; la aplicación por lotes es secuencial y puede ser parcial.
- El workbench usa una casilla y acción de confirmación explícitas; cancelar requiere sesión y token coincidentes. Los comandos de consola de estado/aplicación/reversión y los BAT de fixture/benchmark cubren las superficies solicitadas. La ACL del pipe para el mismo usuario no demuestra identidad del proceso WPF.

## Revisión de dependencias

`THIRD_PARTY_NOTICES.md` registra versiones fijadas, commits fuente, avisos MIT y el aviso de Iced incluido. El proyecto Core condiciona RuntimeDetour a net472; el empaquetado usa una lista explícita de dependencias de ejecución. No se cambió ninguna versión de dependencia durante esta revisión.

La guía upstream de RuntimeDetour describe orden de cadenas, delegados originales y liberación. Sus garantías de sincronización se limitan a esa cadena y no establecen seguridad para cualquier ciclo de vida del host. En esta fecha, las páginas upstream de GitHub mostraban ausencia de política SECURITY.md y de avisos publicados. Ninguna observación demuestra ausencia de vulnerabilidades ni una auditoría independiente. Fuentes: https://monomod.dev/docs/RuntimeDetour/Usage.html ; https://github.com/MonoMod/MonoMod/security/policy ; https://github.com/MonoMod/MonoMod/security/advisories .

## Cambios en código y registros

La aserción de guardado concurrente ahora incluye el índice fallido, estado tipado, razón acotada y excepción interna como causa, incluido HRESULT. La aserción del reemplazo JSON atómico también registra HRESULT y el idioma conservado cuando ocurre una IOException. Conserva la exigencia de que todos los guardados programados de alias tengan éxito y que la caché final coincida con el archivo confirmado. No agrega reintentos, supresión de excepciones, cambios de almacenamiento de producción ni reducción de cobertura.

Este anexo y su equivalente inglés documentan la revisión y la observación pendiente. Se conservan las revisiones anteriores del registro. No se inició Bannerlord, Modding Kit, campaña ni batalla; no se regeneraron ZIPs.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: compilación limpia sin advertencias ni errores; pasaron las suites seleccionadas, incluidas Desktop 65/65 y 293 casos WPF con 182 pases de layout. El harness informó 14,643 ms, que no equivalen a latencia de la aplicación.
- `tools/Verify-CalradiaForge-StatelessBehavior.bat`: pasaron 4/4 controles arquitectónicos.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat --no-pause`: pasaron Prefix/Postfix, orden, retirada propia serial, cancelación void, invocador tipado, cambios de contexto, recuperación de descarga/desconexión, estado incierto, exclusión de backends y restauración exacta directa. Una biblioteca fue alojada mediante BAT; no se inició DetourFixture.exe.
- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` después del diagnóstico: Core 401/401 y ForgeWeave 73/73 aprobados. Las repeticiones adicionales de Core son evidencia diagnóstica, no una solución del fallo intermitente anterior.
- `tools/Benchmark-CalradiaForge-Hooks.bat`: cinco muestras por escenario. Medianas calientes de hooks sin trabajo: 230.3 ns/llamada (Prefix), 192.3 ns/llamada (Postfix) y 252.6 ns/llamada (ambos); los contadores aproximados AppDomain informaron 160.56, 128.45 y 160.56 bytes/llamada. Son microbenchmarks seriales, no latencia de frames ni garantías de cero asignaciones.
- El fallo de guardado sigue pendiente hasta capturar una causa reproducible mediante el diagnóstico. Este anexo no declara completado el objetivo íntegro. Tras subir el cambio, se deben revisar los checks de CI para el SHA exacto.

Esta revisión añade evidencia sin reemplazar párrafos anteriores ni afirmar comprobación del juego en vivo.
