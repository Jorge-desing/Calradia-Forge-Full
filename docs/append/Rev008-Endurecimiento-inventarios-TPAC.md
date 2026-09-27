# Anexo Rev008 — endurecimiento del diff de inventarios TPAC

**Regla de anexado.** Este anexo agrega evidencia técnica al historial. No reemplaza ni corrige el texto de las revisiones Rev001–Rev007.

## Validación estructural y temporal

La lectura de los informes JSON ahora valida campos requeridos mediante diagnósticos explícitos, incluso bajo `Set-StrictMode`: raíz, lector, origen, paquete y activos no pueden omitir propiedades ni proporcionar valores nulos. Las versiones y longitudes sin signo se analizan con cultura invariable; los valores inválidos ya no producen excepciones genéricas de conversión.

El informe debe declarar `tableOfContentsBytes`, mayor que cero y dentro del tamaño de origen después de la cabecera fija TPAC v2 de 36 bytes. El comparador conserva este dato normalizado en cada rama de evidencia. El lector de metadatos sigue siendo la única fuente de inventario; estas validaciones no autentican que un JSON externo haya sido producido por TpacTool ni vuelven a abrir el TPAC.

La fecha `createdUtc` debe coincidir completamente con el formato ISO aceptado, con `Z` o un desplazamiento explícito. Se rechazan saltos de línea finales y `-00:00`, que expresa un desplazamiento local desconocido en lugar de una hora UTC conocida. Los desplazamientos conocidos se normalizan a UTC.

## Pruebas breves y límites

`tests/CalradiaForge.TpacInventoryCompare.Tests.bat` pasó con fixtures para campos ausentes/nulos, números inválidos, GUID duplicados, marcas de captura con LF/CRLF, hora sin zona, desplazamiento `-00:00`, tabla ausente/vacía/fuera de rango, normalización de `+02:00`, exportación, conservación de entradas y protección contra sobrescritura.

El test normal de lectura local continúa como `SKIP` porque Windows bloquea el `TpacTool.Lib.dll` suministrado con `0x80131515`; no se afirma que el lector real haya pasado. El modo estricto frente a un lector/fixture ausente o bloqueado devuelve error no exitoso. Los archivos TPAC, la carpeta original de TpacTool y los paquetes de distribución permanecieron intactos; no se reconstruyeron archivos ZIP ni se ejecutó Bannerlord.
