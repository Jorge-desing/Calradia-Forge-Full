# Rev042 — Aislamiento del render WPF respecto al foco del usuario

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Infraestructura de pruebas Desktop WPF. Escritorio de render aislado y política de foco para UI Automation de solo lectura.

## Problema observado y justificación técnica

Se informó repetidamente que las ejecuciones de render WPF movían el foco fuera de la ventana Desktop del usuario. El arnés creaba ventanas WPF nativas y su guard de primer plano también conservaba una ruta de restauración de último recurso mediante `SetForegroundWindow`. El launcher opcional de UI Automation tenía una rama explícita de restauración similar. Incluso una restauración condicionada modifica activamente el primer plano y hace menos confiable el comportamiento de las pruebas.

## Solución técnica y decisiones arquitectónicas

- El arnés crea un hilo STA de trabajo dedicado, lo conecta a un escritorio privado de Windows con nombre único mediante `CreateDesktopW` y `SetThreadDesktop`, y solo después inicializa WPF, ventanas o hooks.
- `ForegroundWindowGuard` ahora es pasivo y está limitado al proceso. Registra cuándo los HWND de prueba pasan a primer plano dentro del escritorio privado, pero no contiene llamadas para modificar ni restaurar el primer plano.
- La prueba del estado de la barra nativa ahora comprueba bindings de visibilidad, estilos y presencia de manejadores sin maximizar, minimizar ni restaurar una ventana.
- El ejecutor UIA opcional conserva el observador de primer plano previo al inicio y la comprobación de `WS_EX_NOACTIVATE`, pero ya no llama a `SetForegroundWindow`; una transición observada a primer plano se informa como fallo y no se intenta restaurar.

## Cambios en activos, código y dependencias

Se actualizaron el guard de primer plano y el ejecutor de pruebas de render, además del ejecutor smoke UIA. No cambiaron el comportamiento de producción, la API pública, las dependencias, la versión del producto ni los ZIP. Windows libera el handle del escritorio aislado cuando termina el proceso de pruebas.

## Validación y límites de la evidencia

- Se ejecutó dos veces, oculto, el launcher `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output <ruta-json-única>`. Ambas compilaciones terminaron sin advertencias ni errores; Desktop pasó 57/57 y el render WPF 278/278.
- En las dos ejecuciones el HWND interactivo de primer plano fue `0x171168` antes y después. El informe final registra 158 pases de layout, 2.043,3 ms en llamadas de layout y 9.492 ms totales del arnés.
- El observador de render vio que el proceso de pruebas pasó a primer plano únicamente en su escritorio privado con nombre único. Es un evento interno esperado, aislado del escritorio interactivo del usuario.
- El ejecutor UIA pasó la comprobación de sintaxis PowerShell y la comprobación estática de que se quitó la restauración del foco. No se repitió su smoke UIA, por lo que no se afirma que haya pasado después del cambio.
- No se afirma inspección visual en vivo, inicio del juego ni medición de latencia de interacción de la aplicación.

Este anexo añade evidencia a la revisión protegida anterior sin sustituir párrafos ni registros previos.
