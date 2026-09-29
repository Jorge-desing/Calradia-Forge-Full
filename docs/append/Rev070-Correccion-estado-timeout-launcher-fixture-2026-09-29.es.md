# Rev070 — Corrección de estado del timeout del launcher del fixture

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Lanzamiento del fixture x64 desechable y evidencia reciente de pruebas del motor de parches.

## Corrección de Rev069

Rev069 indicó que el BAT del fixture limitaba la espera del proceso hijo. Se probó por BAT un wrapper PowerShell inline de timeout, pero no devolvió el control de forma fiable mientras el hijo nativo seguía bloqueado. Se retiró el wrapper y se restauró el lanzamiento directo del proceso hijo. Ya no se afirma una garantía de timeout.

## Evidencia actual

- La salida más reciente del BAT Core administrado pasó 363/363 regresiones; el fixture aislado x64 `net472` compiló con cero advertencias y errores.
- El fixture nativo quedó bloqueado después de iniciarse y se detuvo al interrumpir únicamente esa ejecución desechable por BAT. Por ello, no se aprueba ni se informa como exitosa la ejecución global más reciente del BAT Core ni el smoke nativo.
- El éxito serial anterior del fixture sigue siendo evidencia del árbol anterior registrado en Rev088. No demuestra el comportamiento del árbol más reciente.
- Las regresiones administradas del preflight de límite de página siguen verificando el rechazo sin lectura ni escritura y la limpieza del registro con el adaptador simulado. No se inició Bannerlord ni el Modding Kit.

Esta corrección append-only reemplaza la afirmación de timeout de Rev069 sin modificar ese documento histórico. La versión del producto permanece en 25.2.0, `ForgeApi.Version` en 11 y los ZIP de distribución no cambian. El detour experimental sigue sin garantizar seguridad si otro hilo ejecuta un destino durante la escritura de código.
