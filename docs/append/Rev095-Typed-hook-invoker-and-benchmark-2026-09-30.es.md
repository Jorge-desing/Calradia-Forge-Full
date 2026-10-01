# Rev095 — Invocador tipado de hooks y benchmark de distribución

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Distribución Prefix/Postfix de Core, fixture aislada de detours, herramientas de benchmark y documentación.

## Problema observado y justificación técnica

La ruta normal de distribución invocaba `Delegate.DynamicInvoke` para cada callback. Esa invocación basada en reflexión añadía costo y asignaciones por llamada a Prefix/Postfix en el hilo que invoca el destino. El cambio se limita a esa ruta medida; no afirma mejorar el tiempo de cuadro de Bannerlord ni la integración completa con el juego.

## Solución técnica y decisiones arquitectónicas

Durante el registro, Core ahora almacena los tipos de parámetros del delegado y compila un invocador tipado del delegado original mediante `DynamicMethod`. El invocador generado realiza las conversiones/desempaquetado de argumentos y el empaquetado del retorno necesarios sin usar `DynamicInvoke` en la ruta normal. La distribución reutiliza el arreglo de argumentos del callback y solo clona los argumentos originales cuando hay un Prefix que los necesita. El fallback de invocación dinámica permanece únicamente para el caso inesperado de que falte una entrada de distribución. El invocador compilado se libera al retirar la entrada.

El ciclo de vida de hooks y el contrato de Apply/Revert explícito no cambian. Prefix y Postfix siguen ejecutándose sincrónicamente en el hilo que llama al destino. El backend continúa siendo experimental: estos fixtures seriales no suspenden otros hilos ni demuestran que el destino esté inactivo durante la modificación del código.

## Cambios en activos, código y dependencias

- Se añadió un invocador tipado a `src/CalradiaForge.Core/ForgeHookService.cs`, que almacena metadatos al registrar el hook y evita `Delegate.DynamicInvoke` en la distribución normal.
- Se amplió el fixture aislado `net472` para comprobar orden de callbacks, IDs duplicados, destinos de instancia, cambios del valor de retorno, callbacks `void` y propagación de excepciones.
- Se añadieron `tools/Benchmark-CalradiaForge-Hooks.bat` y el modo optativo `--benchmark` del fixture. El benchmark informa llamadas directas y con hook, duración, contadores de asignación del AppDomain, colecciones Gen0, duración de Apply/Revert y costo de la primera llamada.
- Se actualizó en ambos idiomas la guía Patch Blueprint y se registraron los avisos de terceros de las dependencias de hooks fijadas.
- La versión del producto permanece en 25.2.0 y `ForgeApi.Version` en 12. No se modificaron API pública, IPC ni rutas del juego, y no se regeneraron ZIP.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: Core 394/394 y ForgeWeave 73/73 pasaron; las compilaciones informaron cero advertencias y cero errores.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: Desktop 65/65 pasó; la compilación informó cero advertencias.
- `tests/CalradiaForge.DetourFixture/Run-DetourFixture.bat`: el fixture desechable `net472` x64 compiló con cero advertencias y errores y pasó las comprobaciones del invocador tipado, orden de callbacks, IDs duplicados, restauración y excepciones. No se inició directamente el `.exe` del fixture.
- `tools/Benchmark-CalradiaForge-Hooks.bat`: una corrida BAT previa al cambio registró cinco muestras internas; su p50 fue 773,1 ns / 401,08 B por llamada para Prefix, 656,0 ns / 401,33 B para Postfix y 657,9 ns / 401,08 B para ambos. Cinco corridas BAT completas posteriores produjeron medianas p50 por corrida de 218,5 ns / 160,5 B, 159,5 ns / 128,45 B y 218,6 ns / 160,56 B, respectivamente: reducciones aproximadas de 71,7%, 75,7% y 66,8% en el tiempo p50. Se compara una corrida base con cinco muestras internas frente a la mediana de cinco corridas BAT posteriores; no es un estudio equivalente de cinco corridas antes y cinco después. Estos tiempos y contadores de asignación miden el fixture/harness aislado `net472`, no Bannerlord, su tiempo de cuadro ni la latencia de una aplicación en vivo.
- No se midieron hooks en una sesión en vivo de Bannerlord o Modding Kit. El fixture serial no demuestra que los hilos estén detenidos ni la seguridad mientras otro hilo ejecuta el destino; el backend sigue siendo experimental.
- El anexo y el changelog fuente se registran como Rev095, pero el DOCX protegido y la cadena de integridad siguen en Rev076. La reconciliación está pendiente: faltan los anexos fuente Rev086–Rev092. Se omitió deliberadamente ejecutar el generador DOCX y el registrador de integridad; por tanto, esta entrada aún no forma parte del registro protegido.

Este anexo documenta la optimización medida de la distribución de hooks y conserva el historial anterior. El registro protegido queda pendiente de reconciliar las revisiones faltantes.
