# Rev108 — Finalizer, Transpiler y utilidades protegidas de hooks

**Fecha:** 1 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios; API SDK de compilación 13  
**Alcance:** SDK, backend Core net472, consola/BAT, WPF, Gauntlet y guías bilingües.

## Problema observado y justificación técnica

Prefix/Postfix no ofrecían manejo explícito de excepciones ni autoría IL local. La gestión necesitaba selección consistente de IDs registrados, diagnóstico y confirmación. Un Undo IL fallido de MonoMod puede limpiar IsApplied antes de reconstruir correctamente; ese indicador solo no certifica restauración.

## Solución técnica y decisiones arquitectónicas

Finalizer recibe la excepción pendiente después de las fases anteriores, la conserva mediante ExceptionDispatchInfo por defecto y permite sustitución o supresión explícita con validación del retorno. Un Finalizer que lanza agrega el fallo existente. Los hooks sin Finalizer conservan la política anterior de fallos de callbacks.

Transpiler usa MonoMod.RuntimeDetour 25.3.6 fijado e ILHook mediante un adaptador ILContext exclusivo de Core net472. Registro/preflight permanecen inertes; SDK/Desktop no referencian MonoMod. El IL instalado permanece activo fuera del menú hasta Undo. La gestión sigue explícita y limitada al menú principal aprobado y al hilo del juego.

Un Undo IL fallido conserva conflicto, handle y reserva y requiere reiniciar el host. La descarga bloquea nuevas aplicaciones y callbacks; solo Undo propio síncrono bajo el bloqueo de gestión, alcance del hilo y contexto aprobado puede reconstruir manipuladores restantes. Runtime.Dispose siempre cierra el pipe en finally; la recuperación a nivel de API no demuestra supervivencia del pipe tras descargar el módulo.

## Cambios en código, interfaces y dependencias

Los snapshots inmutables exponen metadatos opcionales Finalizer/Transpiler sin retirar constructores. La consola ofrece inventario por propietario/destino/tipo, verificación y exportación JSON. Las solicitudes BAT limitan IDs, tamaños y tiempos y no reintentan confirmaciones. WPF incorpora filtros, verificación y exportación; Gauntlet usa el coordinador local de planes, sin IPC. Ambos mantienen coincidencia exacta de metadatos/sesión, caducidad, uso único y confirmación explícita. Las etiquetas mantienen paridad en trece idiomas; WPF distingue inventario vacío de filtro sin coincidencias.

## Validación y límites de la evidencia

- tools/Run-CalradiaForge-Tests.bat --no-pause pasó: Core 403, ForgeWeave 73, Desktop 65 y WPF 294; compilaciones sin advertencias ni errores. Render final: 12.508 ms y 182 pases de layout; no es latencia observada del juego.
- tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause pasó controles estructurales/decorativos y validación anidada Core/fixture serial.
- tools/Test-CalradiaForge-HookUtility.bat pasó 29 casos de argumentos y catorce de transporte aislado, incluyendo versiones, IDs, UTF-8, límites y timeout inválidos.
- Los fixtures x64 seriales mediante BAT cubren errores de Finalizer, supresión válida/inválida, sustitución y agregación; orden IL, reconstrucción/convivencia, Apply fallido, reservas tras Undo fallido, descarga limpia, desconexión y colisiones raw. No se inició directamente ningún EXE de pruebas ni se llamó al destino concurrentemente con la escritura.
- Pasaron cinco ejecuciones BAT del benchmark. Mediana de medianas calientes por corrida: Prefix 242,1 ns/168,43 bytes; Postfix 189,8/136,31; ambos 256,8/168,43; Finalizer 156,0/136,31; solo IL 17,9/0,00. Las asignaciones corresponden al harness AppDomain serial. Los valores históricos de una corrida no son una referencia comparable; no se afirma aceleración ni ausencia general de asignaciones.
- Se compilaron y desplegaron los perfiles Client y Modding Kit con respaldos de archivos instalados. El TPAC importado existente se respaldó verificando SHA-256 y se conservó sin cambios. Steam llegó al menú principal; Computer Use observó F10 abriendo Forge 25.2.0 y navegación a Framework a 1920x1080. No se abrió campaña ni batalla. La consola nativa no se abrió con las combinaciones probadas; siguen pendientes aplicación/reversión del objetivo propio y controles de hooks Gauntlet en vivo. Después se cerró Bannerlord y se reinició Computer Use.
- La revisión documenta integración y dependencias de Forge; no es una auditoría independiente ni prueba de seguridad al modificar destinos ejecutados concurrentemente. El backend permanece experimental.

Este anexo agrega evidencia sin modificar revisiones protegidas anteriores. La distribución se registra por separado tras la auditoría canónica de tres ZIP y SHA-256; no se autoriza publicación remota.
