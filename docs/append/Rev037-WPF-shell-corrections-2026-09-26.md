# Rev037 — Correcciones visuales y de disposición de la shell WPF

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Desktop. Composición de ventana, rail operativo, Split Deck, localización de textos de shell y decoración cartográfica.

## Problema observado y justificación técnica

La ventana mínima dejaba poco espacio para títulos del rail y para el panel Split Deck activo. El panel fijado también podía mostrar títulos derivados obsoletos después de limpiar la evidencia y volvía a valores de texto no localizados cuando estaba vacío. La ilustración cartográfica se mostraba en un marco con proporción distinta a su fuente, y los adornos tenían cachés rasterizadas a una escala fija. Varias etiquetas y ayudas visibles de la shell estaban codificadas en XAML en vez de usar los recursos de idioma.

## Solución técnica y decisiones arquitectónicas

- El rail usa un mínimo de 220 DIP, con máximo de 276 DIP; el área principal conserva 400 DIP mínimos. El panel lateral Split Deck se redujo a 280 DIP y el harness ahora comprueba esa composición con la ventana mínima.
- El marco cartográfico usa 160×90 DIP para la fuente de 448×252 píxeles y la imagen mantiene `Stretch="Uniform"`, sin deformación ni recorte. Se quitaron los `BitmapCache` de escala fija del adorno cartográfico y del sello para evitar una textura rasterizada a resolución fija.
- Las etiquetas de shell, ayudas y fallbacks del Split Deck se resuelven mediante recursos localizados. Los 13 diccionarios mantienen las claves correspondientes. La limpieza del contenido fijado notifica título y categoría derivados después de vaciar la colección de evidencia.

## Cambios en activos, código y dependencias

Se actualizaron `MainWindow.xaml`, `DesktopShellViewModel`, sus regresiones de Desktop/render, y los 13 diccionarios `Strings.*.xaml`. No se añadieron dependencias, rutas de herramientas, API pública, IPC ni preferencias. Los comandos y permisos existentes se conservan. La versión del producto sigue en 25.2.0 y no se regeneraron ni modificaron ZIPs.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause <nul"`: compilación sin advertencias ni errores y pruebas Desktop MVVM 55/55.
- Cinco ejecuciones seriales de `tools\Run-CalradiaForge-Desktop-Render-Tests.bat --no-pause --output artifacts\desktop-render-wpf-final-20260926-01.json <nul`, con archivos de salida consecutivos `-02` a `-05`: 275 casos aprobados y 152 pases de layout por corrida.
- La mediana de tiempo total del harness fue 11.554 ms frente a 13.652 ms de baseline (-15,4 %). La mediana de llamadas de layout fue 2.310 ms frente a 2.397 ms (-3,7 %). Las medianas de navegación/filtro, matriz de localización y matriz de tema fueron 1.959 ms, 2.064 ms y 3.341 ms. El inicio midió 3.181 ms frente a 3.133 ms (+1,5 %). Estas son mediciones del harness, no latencia de interacción en la aplicación abierta.
- La matriz del harness aumenta la resolución de rasterización de las capturas; no simula el layout WPF con DPI del sistema operativo. La inspección en vivo/UI Automation queda pendiente: el runner disponible altera el estado de pestañas y Split Deck, por lo que no se usó como comprobación de solo lectura. No se afirma validación visual en una ventana WPF real.
- Bannerlord no se inició; no se cargó campaña ni batalla. Los cambios no incluyen activos del juego ni pruebas dentro del motor.

Este anexo agrega evidencia a la revisión anterior y mantiene su contenido intacto. Conserva la versión del producto, los comandos y permisos existentes, así como la separación de Desktop frente al módulo del juego.
