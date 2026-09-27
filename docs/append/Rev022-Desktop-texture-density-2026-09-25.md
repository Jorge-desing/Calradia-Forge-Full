# Rev022 — Corrección de nitidez y composición en Desktop WPF

**Fecha:** 25 de septiembre de 2026  
**Versión:** Calradia Forge 22.0.0, sin cambios  
**Alcance:** recursos WPF locales, composición de cabecera y validación del render. No se modifican el módulo del juego, el atlas Gauntlet ni los ZIP.

## Problema observado y corrección del ornamento

La ilustración botánica de las tarjetas de herramienta se mostraba en un espacio de 270×90 DIP, pero el recurso empaquetado tenía solo 270×90 píxeles. Con una escala de pantalla del 200 %, WPF debía ampliarlo a 540×180 píxeles físicos. Además, el generador cortaba la fuente de 2172×724 píxeles a una franja de 2172×220 y la comprimía a proporción 3:1, descartando gran parte de las ramas laterales y deformando el motivo.

El recurso optimizado se regenera ahora desde la fuente completa de proporción 3:1 a 540×180 píxeles. El tamaño lógico en XAML permanece en 270×90 DIP, por lo que el adorno conserva la columna derecha reservada en la revisión anterior y dispone de resolución nativa hasta el 200 %. La imagen sigue a baja opacidad, sin foco ni captación de eventos, y no cubre título, estado, campos ni evidencia. Las fuentes de autoría no se alteraron.

## Muestreo y jerarquía visual

La tarjeta botánica, la franja de la barra superior, el tablero cartográfico y la banda ilustrada del rail solicitan muestreo de mapas de bits de alta calidad en WPF. El subtítulo localizado de la cabecera pasa a varias líneas y conserva su texto completo en una ayuda emergente; se elimina el corte sistemático con puntos suspensivos visible en las capturas anteriores. La comprobación de layout exige que el subtítulo permanezca dentro del grupo de marca en los idiomas y escalas cubiertos.

## Recursos empaquetados y reproducción

El generador `tools/prepare_desktop_textures.py` comprueba que un redimensionado conserve la proporción de la fuente tras cualquier recorte. En modo `--check`, coteja el contenido de cada derivado y el conjunto exacto de PNG empaquetados, de modo que variantes obsoletas no entren en el ensamblado por la inclusión con comodín del proyecto WPF.

Se retiraron solo del directorio `Optimized` tres derivados sin referencias en el XAML de ejecución: `tool-card-top-corners-v1.png`, `desktop-titlebar-heraldic-frame-v1.png` y `desktop-rail-etched-field-v1.png`. Permanecen sus imágenes maestras para una eventual revisión de diseño. El paquete optimizado suma 7.202.682 bytes comprimidos y 15.334.592 bytes RGBA decodificados, por debajo de los límites existentes de 7.950.120 y 15.810.047 bytes. Frente a la cifra documentada en Rev034, libera 75.910 bytes comprimidos y 444.400 bytes decodificados, aun con la tarjeta al doble de resolución por eje.

## Validación y límites de la evidencia

- `tools/Prepare-CalradiaForge-Desktop-Textures.bat --check` pasó con inventario y bytes deterministas; informó el SHA-256 de cada derivado. El nuevo ornamento 540×180 produjo SHA-256 `9152DD5182AE4E46FFE4D7086179463E304B3B2F428C5B25593797714CC8E99A`.
- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` compiló con cero advertencias y cero errores. Pasaron 51 pruebas Desktop y 273 casos WPF con 150 pases de layout. Se conservaron las comprobaciones de rutas, 13 idiomas, tres temas y escalas del 100 %, 125 %, 150 % y 200 %.
- Se revisaron las capturas generadas por el harness a escala normal y al 200 %, incluidas Mesa de guerra y Alto contraste. La última ejecución del harness duró 4.886 ms. Es un tiempo del harness, no una medición de latencia de interacción en la aplicación abierta.
- La inspección de una ventana WPF en vivo sigue pendiente. Las pruebas de píxeles y composición del harness no prueban la visualización en todos los monitores físicos ni una importación de recursos en Bannerlord.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Mantiene los comandos, las rutas, MVVM, las API públicas y la versión del producto.
