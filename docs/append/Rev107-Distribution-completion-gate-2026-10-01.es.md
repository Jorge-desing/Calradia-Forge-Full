# Rev107 — Gate de finalización de distribución

**Fecha:** 1 de octubre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Instrucciones de agentes, skill de desarrollo y reglas de distribución.

## Problema observado

La entrega anterior del motor de hooks terminó la verificación de código y CI sin generar ZIPs actuales. Las reglas modulares exigían empaquetar los hitos, mientras que la descripción y el diagrama de la skill de desarrollo indicaban incorrectamente que solo se empaquetaba bajo petición. Las instrucciones raíz no tenían un gate explícito de finalización de distribución.

## Corrección

AGENTS.md y CODEX.md ahora exigen directamente los tres ZIPs de la versión actual al terminar versiones, hitos o cambios significativos de código/funciones y bajo petición explícita, incluso sin cambiar la versión. La skill y las reglas usan el mismo disparador. Prevalecen las instrucciones explícitas de no generar ZIPs para la petición actual; el mantenimiento exclusivamente documental no dispara por sí solo el empaquetado.

La entrega exige empaquetado canónico exitoso, auditoría y hashes SHA-256 calculados independientemente que coincidan, con enlaces absolutos a salidas y evidencia. Prerrequisitos o archivos ausentes dejan la entrega incompleta. Los archivos generados quedan en artifacts ignorado y no se incorporan al commit. Empaquetar no autoriza cambiar versión, publicar, hacer push, instalar ni abrir el juego. Se aclara la exclusión de scripts para conservar únicamente la excepción del launcher Desktop existente sin app-host, no scripts de desarrollo/pruebas. Los nombres de evidencia usan el sufijo real de versión sin puntos.

## Validación y límites

La skill de desarrollo pasó su validador estructural BAT. Para esta petición se invoca el empaquetado canónico con -Quick, reutilizando atlas y documentación existentes sin regenerar recursos, manteniendo los gates de compilación, pruebas BAT y auditoría. La entrega final informa su resultado y hashes reales. Los registros protegidos anteriores no cambian; empaquetar no demuestra importación ni render en vivo del juego.
