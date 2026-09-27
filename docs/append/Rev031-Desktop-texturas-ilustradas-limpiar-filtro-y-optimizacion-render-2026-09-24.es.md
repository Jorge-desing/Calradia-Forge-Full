# Rev031 — texturas ilustradas, limpieza del filtro y optimización del render de Desktop

**Fecha:** 24-09-2026  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** recursos decorativos WPF, limpieza localizada de búsqueda y optimización medida del harness de render.  
**Distribución:** seguimiento solo del código fuente; no se regeneraron ZIP ni paquetes del producto.

## Recursos ilustrados locales

El pipeline local determinista existente procesa tres maestros originales de ImageGen y empaqueta solo sus derivados optimizados:

- `desktop-titlebar-heraldic-frame-v1.png` — derivado RGBA de 512 × 32 para el marco pasivo del borde superior.
- `desktop-rail-etched-field-v1.png` — derivado RGBA de 96 × 288 para el rail de navegación operativo.
- `desktop-card-corners-botanical-v1.png` — derivado RGBA de 270 × 90 para la tarjeta de resumen de herramienta.

Las variantes nuevas empaquetadas suman 92.537 bytes comprimidos y 273.328 bytes RGBA decodificados, dentro del megabyte adicional acordado para ambos presupuestos. Permanecen como recursos locales del ensamblado. Las capas decorativas no reciben hit testing; el ledger de evidencia, el área de comandos, los campos editables y los resultados quedan despejados. El sello existente no cambió. Mesa de guerra y Pergamino claro admiten los adornos; Alto contraste sigue sólido.

## Limpieza localizada de búsqueda

La búsqueda operativa ahora tiene un botón compacto y localizado para borrar el texto. Un trigger XAML lo muestra solo cuando el filtro existente contiene texto. Al activarlo, limpia ese filtro e incrementa la solicitud de foco existente para devolverlo al campo de búsqueda. El control vive en la ventana y el modelo de presentación actuales; no se agregaron propiedades públicas al ViewModel, rutas, identificadores de comandos, contratos IPC ni APIs del producto.

Los diccionarios generados de interfaz incluyen la etiqueta de limpieza y restauran las claves localizadas que ya consumía el XAML visible, con paridad en los 13 idiomas. Esto conserva las etiquetas actuales del rail, los selectores nativos de archivos y carpetas, el estado vacío de evidencia, el sello superior, el control de adornos y los botones de ventana.

## Cambio medido del harness

La línea base previa, medida cinco veces en el mismo equipo mediante el BAT de render, fue de 6,641, 6,730, 6,069, 6,337 y 6,309 segundos; la mediana fue 6,337 segundos. Las cinco ejecuciones finales midieron 6,529, 5,727, 5,481, 5,846 y 6,191 segundos; la mediana fue 5,846 segundos, un 7,7% menor que esa línea base recién medida. Cada corrida pasó 272 casos WPF y registró 148 pases de render/layout. La mediana final del tiempo interno del harness fue 5,192 segundos. La fase de localización y escalas observada antes duró 2.163,6 ms; la mediana final de cinco corridas fue 1.058,8 ms. Esta comparación de fase es diagnóstica porque el valor previo es una sola muestra, no una mediana de cinco fases.

La mejora se limita al harness: su matriz de presentación ya no quita y vuelve a aplicar el mismo diccionario de tema por cada idioma cuando el tema no cambia. Sigue aplicando la localización y renderiza todos los idiomas, temas y escalas compatibles. El tiempo del harness no equivale a la latencia de extremo a extremo del producto en ejecución.

## Validación y límites

- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause`: compilación con cero advertencias/errores, Desktop 51/51 y 272 casos de pruebas WPF de render y recursos.
- Cinco ejecuciones de `tools/Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause`: todas pasaron con 148 pases de layout por corrida; mediana final de 5,846 segundos.
- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check`: pasaron la generación determinista de derivados y el presupuesto de recursos empaquetados.
- La matriz de render conservó las 194 rutas, 13 idiomas, tres temas y escalas de 100%, 125%, 150% y 200%. También ejercitó la visibilidad localizada del botón, la limpieza del filtro y el retorno del foco de teclado.
- La compilación WPF actual se abrió mediante un BAT temporal que apunta al resultado fuente. UI Automation de solo lectura identificó `Calradia Forge 22.0.0`, el campo de búsqueda, la paleta de comandos y los controles del borde superior; no se activaron controles.

El arte nuevo no se ha validado dentro de Bannerlord. No se inició el juego, campaña ni batalla y no se generó ningún ZIP. La versión del producto, API pública, IPC, rutas, comandos, preferencias y recursos del juego permanecen intactos.

Esta entrada es append-only y no revisa Rev030 ni evidencias anteriores.
