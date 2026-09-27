# Rev047 — Refinamiento de la jerarquía WPF de estado y comandos

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Cabecera, tarjetas de estado de orden, recursos de tema y cobertura de render fuera de pantalla de Desktop WPF.

## Problema observado y justificación técnica

La paleta de comandos tenía un peso visual similar al de los controles secundarios, mientras que la tarjeta de herramienta activa era la única tarjeta de estado sin una etiqueta localizada clara. La exportación de evidencia también tenía un área de activación más baja que la acción principal de la cabecera. La opacidad de los iconos de estado era fija y no estaba ajustada a las tres paletas, por lo que Alto contraste no recibía el tratamiento más visible elegido para los detalles decorativos.

## Solución técnica y decisiones

La acción existente de la paleta ahora usa una superficie primaria de latón, un icono local de búsqueda y una altura mínima de activación de 40 DIP, conservando su ID de automatización, nombre accesible localizado, comando y comportamiento de rutas. Las cinco tarjetas de estado tienen glifos semánticos locales uniformes; la tarjeta de herramienta activa tiene una etiqueta y un borde de latón más marcado. El token por tema `Token.StatusIconOpacity` mantiene War Table discreto, eleva ligeramente Parchment Light y hace que los glifos se distingan con claridad en Alto contraste. Los iconos no reciben hit testing ni foco. El botón de exportación de evidencia mide 30 DIP de alto.

El arnés de render ahora comprueba etiquetas localizadas, tamaño/nombre/icono de la acción principal, comportamiento pasivo y opacidad configurada en cada tema. Los 13 catálogos incluyen `Ui.ActiveTool`.

## Cambios en activos, código y dependencias

- Se actualizaron el XAML de cabecera y estado Desktop, los diccionarios de tema, los recursos localizados y las aserciones de render.
- No se agregaron imágenes ni dependencias. Se reutiliza la geometría local existente de Game-icons.
- La versión del producto sigue en 25.2.0. No cambiaron API pública, rutas, comandos, permisos, contrato IPC ni ZIPs.

## Validación y límites de la evidencia

- BAT de línea base: `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev067-baseline.json"` — compilación limpia; 59 pruebas Desktop; 281 casos de render; 158 pases de layout; 10.893 ms totales y 2.564 ms en llamadas de layout.
- BAT final: `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev067-final.json"` — cero advertencias/errores; Desktop 59/59; 285 casos de render; 158 pases de layout; 8.681 ms totales y 2.013,7 ms en llamadas de layout.
- Son mediciones de una ejecución del arnés, no latencia de interacción de la aplicación ni una afirmación de rendimiento. Se revisaron capturas fuera de pantalla de War Table y Alto contraste; no se probó la app WPF en vivo, la entrada del sistema operativo ni el DPI real de Windows.
- No se inició Bannerlord, ninguna campaña ni batalla.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Rev067 del changelog fuente y Rev047 del anexo del Registro protegido son contadores independientes.
