# Lanzamiento de Calradia Forge 22.0.0

## Alcance

22.0.0 es una versión preliminar para desarrolladores, primero en inglés, para Bannerlord Native 1.4.8 y .NET Framework 4.7.2. Refuerza el analizador de recursos de Gauntlet existente con validación estructural de la canalización de sprites de Game-icons. El SDK de extensiones 21.0.0, el banco de trabajo estático de ensamblados, la ayuda sin conexión, la aplicación Desktop y el comportamiento de ForgeWeave siguen formando parte de la superficie de lanzamiento compatible.

## Validación de sprites

Para un módulo seleccionado con metadatos de sprites generados, el analizador verifica los ID y las dimensiones de categorías y hojas, las referencias a hojas duplicadas o ausentes, las rutas seguras de las partes de sprites, la existencia de las partes de origen, las dimensiones y coordenadas positivas, y que los rectángulos de las partes queden dentro de la hoja declarada. Lee la firma PNG de 24 bytes y la cabecera IHDR para comparar las dimensiones de las partes de origen y del atlas. No decodifica píxeles, no inspecciona los datos de color o alfa de la imagen, no valida los CRC de PNG ni certifica la calidad visual. La ruta TPAC solo comprueba la presencia del archivo, que su longitud no sea cero y el marcador `TPAC` de cuatro bytes; no analiza el contenido del paquete.

`tools/validate_game_icon_assets.py` aplica las comprobaciones correspondientes de referencias y dimensiones a los recursos Game-icons distribuidos. `tools/Inspect-CalradiaForge-Tpac.bat` sigue siendo una comprobación previa de solo lectura del tamaño, la cabecera y SHA-256. TpacTool se proporciona localmente y no se redistribuye; su compatibilidad con el objetivo 1.4.8 no está verificada.

## Empaquetado

El lanzamiento contiene exactamente `CalradiaForge-Modules-22.0.0.zip`, `CalradiaForge-Source-SDK-22.0.0.zip` y `CalradiaForge-Desktop-22.0.0.zip`. El empaquetador omite las copias de seguridad de Resource Browser con marca de tiempo y la auditoría del archivo las rechaza. Modules incluye recursos de ejecución, atribución de iconos, avisos y las dependencias requeridas del módulo del juego. Source-SDK incluye el código fuente, el SDK, ejemplos, la configuración y el sitio de DocFX, la documentación y las herramientas de desarrollo. Desktop se mantiene separado y no incluye un `.exe` de app-host.

## Validación

Ejecuta la compilación y las pruebas breves por lotes, la auditoría Python de recursos de iconos, la auditoría de localización nativa, la comprobación previa de TPAC, DocFX y las auditorías de archivos ZIP y de empaquetado. Informa por separado las comprobaciones del código fuente y una sesión nativa de Bannerlord. No cargues una campaña o batalla, no ejecutes pruebas de resistencia ni captures muestras de rendimiento prolongadas.
