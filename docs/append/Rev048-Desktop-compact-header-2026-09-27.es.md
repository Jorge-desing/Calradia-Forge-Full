# Rev048 — Cabecera Desktop compacta y resumen de evidencia

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Cabecera WPF de Desktop independiente, botón del dossier contextual, tarjeta de resumen de evidencia y aserciones de render fuera de pantalla.

## Problema observado y justificación técnica

En la ventana mínima de 980×680 DIP, las etiquetas anchas y las acciones secundarias de la cabecera comprimían la disposición en una sola fila. La etiqueta del dossier y el texto de exportación de evidencia también ocupaban el ancho que necesitaban los ajustes y el resumen de evidencia retenida.

## Solución técnica y decisiones

La etiqueta visible de adornos ahora usa la clave localizada compacta `Ui.DecorativeAccentsShort`, pero conserva el nombre accesible completo y el tooltip. El botón del dossier muestra un glifo de libro local en vez de una etiqueta visible, y conserva el binding, el comportamiento de alternancia, el ID de automatización, el nombre accesible y el tooltip existentes. La exportación de evidencia muestra un icono local pasivo en un botón compacto de 36×30 DIP y conserva su comando y contrato de accesibilidad. El ancho recuperado mantiene legible el resumen de evidencia en la tarjeta compacta de estado.

El arnés de render comprueba la posición de la cabecera en tamaño mínimo, las dimensiones del resumen/exportación de evidencia y la accesibilidad del dossier y del interruptor decorativo. Los 13 catálogos de idioma contienen la etiqueta decorativa corta. No cambiaron rutas, comandos, API pública, contrato IPC, versión del producto ni ZIPs.

## Cambios en activos, código y dependencias

- Se actualizaron la presentación WPF existente de cabecera/estado y las aserciones de render; se agregó la etiqueta corta a los 13 diccionarios localizados.
- No se añadieron imágenes, dependencias de ejecución ni capacidades nuevas de la aplicación.
- El código fuente del producto sigue en 25.2.0; no se regeneró ni modificó ningún ZIP.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-rev068-final.json"` pasó con cero advertencias/errores de compilación, Desktop 59/59 y render WPF 285/285 con 158 pases de layout.
- El artefacto final de una ejecución registró 13.135 ms totales y 3.072,1 ms en llamadas de layout. La línea base de una ejecución en la misma sesión registró 9.120 ms y 2.033,3 ms. Son tiempos del arnés, no latencia de interacción de la aplicación; la corrida final fue más lenta y no se afirma una mejora de rendimiento.
- El arnés de render usa un host WPF fuera de pantalla. El comportamiento de la ventana en vivo, el layout con DPI real de Windows y la entrada del puntero del sistema operativo siguen sin verificarse. No se inició Bannerlord, ninguna campaña ni batalla.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Rev068 del changelog fuente y Rev048 del Registro protegido son contadores independientes.
