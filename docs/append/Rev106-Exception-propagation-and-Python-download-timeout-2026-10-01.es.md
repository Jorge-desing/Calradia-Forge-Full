# Rev106 — Propagación de excepciones y timeout de descarga de Python

**Fecha:** 1 de octubre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Regresiones de la revisión final y resiliencia del bootstrap de Python.

## Problema observado

La revisión final encontró que la regresión de reemplazo atómico comprobaba la identidad de la excepción solo dentro de los manejadores catch. Un helper que absorbiera incorrectamente un fallo podía satisfacer las demás aserciones. Por separado, la ejecución de documentación de GitHub 36832115292 falló antes de su auditoría al descargar el wheel de Windows de lxml 6.1.3: pip informó un timeout de lectura desde files.pythonhosted.org después de 3.1 de 4.0 MB. Es un fallo de descarga, no un resultado de validación del registro.

## Corrección

Los casos de error persistente, error no reintentable y origen ausente ahora exigen después de cada llamada que la instancia capturada sea la excepción inyectada. El comportamiento de producción del reemplazo no cambia. Ambos perfiles de preparación de Python establecen el timeout de socket de pip en 120 segundos y su límite de reintentos de conexión en cinco. Esto no garantiza recuperar un cuerpo de respuesta interrumpido ni un servicio de paquetes inaccesible; los fallos de instalación siguen propagándose.

## Validación y límites

El BAT de Core pasó 402/402, ForgeWeave 73/73 y el fixture aislado y serial de detours/hooks tras reforzar las aserciones. La preparación de Python y el BAT del registro se comprueban por separado antes del push. Los checks remotos deben terminar para el SHA exacto de la corrección antes de entregarla. No cambian API, dependencias, versión, recursos del juego ni ZIPs; los registros anteriores permanecen inmutables.
