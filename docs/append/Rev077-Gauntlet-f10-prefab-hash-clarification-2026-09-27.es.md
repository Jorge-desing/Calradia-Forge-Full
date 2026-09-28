# Rev077 — Cronología de hashes del prefab F10 y límites de la instrumentación

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Aclaración append-only de la comparación de prefabs de Rev076 y de los límites de la evidencia F10.

## Cronología de hashes

Rev076 registró una comparación previa a la regeneración en la que el prefab fuente e instalado `GUI/Prefabs/CalradiaForge.xml` tenía el SHA-256 `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45`. Después de regenerar el prefab fuente para la renovación visual heráldica, su hash pasó a ser `C7BDE061D57F142797DF7B046BC93437E53049351A9D1C1821F6326CDC3BAC31`; el prefab instalado conserva `5741D7571C254B6487477D601F0CFE016AA254A1DFC6BD6795766274543B2F45` porque los recursos nuevos no se importaron ni desplegaron. La diferencia actual identifica revisiones fuente e instalada que no están sincronizadas; por sí sola no demuestra corrupción del archivo.

Los prefabs fuente e instalado actuales usan la escritura canónica del motor `<ScrollbarWidget>` y no usan `<ScrollBarWidget>`. Esta observación no identifica ni descarta la causa exacta de la aserción histórica de F10 y tampoco demuestra que el fallo esté resuelto.

## Instrumentación del ciclo de vida F10

`SubModule.Open()` ahora registra, en orden, los hitos `Loading panel brush file.`, `Panel brush file loaded.`, `Loading panel movie.` y `Panel movie loaded.`. La regresión estática de `AdvancedToolsTests` comprueba que estos hitos, la conexión de la capa y las operaciones de foco aparezcan en el orden de código esperado. Aunque las cadenas se emiten desde el código de ejecución, esta comprobación estática del orden de fuente no es una captura de telemetría en tiempo de ejecución ni reproduce la aserción histórica.

Aquí no se afirma una nueva ejecución dentro del juego, reproducción del crash F10, importación del TPAC ni inspección de render en vivo. La causa y resolución históricas siguen sin verificarse; la importación en Resource Browser y el render en vivo continúan pendientes. Este anexo aclara la cronología y los límites de la evidencia sin reescribir Rev076 ni Rev056.
