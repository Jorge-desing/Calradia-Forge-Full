# Rev024 — Sustitución por texturas materiales ImageGen en Gauntlet

**Fecha:** 2026-09-24  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** Texturas decorativas de la interfaz Gauntlet y su procesamiento reproducible como sprites.  
**Distribución:** Actualización posterior del código fuente; no se modificaron ZIP ni TPAC.

## Maestros materiales creados con ImageGen

Esta ronda sustituye el conjunto de adornos geométricos por cuatro maestros materiales creados con ImageGen: madera de pino, aguada de tinta, latón envejecido y fieltro de pino. Un procesamiento determinista local convierte los maestros aprobados en sprites listos para el atlas, con dimensiones, transparencia y tratamiento de color fijos. Se retiran del diseño visual los contornos cartográficos, la roseta, las costuras y los demás motivos lineales geométricos; la interfaz pasa a utilizar texturas de materiales en vez de marcas geométricas repetidas. ImageGen se usa únicamente durante la autoría. El juego no requiere generación de imágenes ni conexión de red en tiempo de ejecución.

Las texturas se reservan para el marco visual de la interfaz: cabecera, rail de navegación, marco exterior y separadores de secciones reales. La evidencia y su salida técnica, los campos editables y los comandos conservan superficies limpias y de alto contraste. Los widgets decorativos siguen siendo pasivos, respetan las zonas de interacción y no ocultan el foco de teclado. Las ocho rutas, sus comandos, etiquetas, permisos, API pública y comportamiento permanecen sin cambios.

## Validación y estado de entrega

La generación y las comprobaciones de referencias de sprites, la regeneración del atlas, la importación mediante Resource Browser y el renderizado dentro del juego quedan pendientes de confirmación. Esta entrada no afirma que se haya aprobado ninguna prueba, atlas o renderizado en tiempo de ejecución. El producto permanece en 22.0.0; no se reemplazó ningún TPAC ni se generaron ZIPs.

Este registro es append-only y no modifica Rev021–Rev023 ni evidencias anteriores.
